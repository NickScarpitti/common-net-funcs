#if !NET6_0_OR_GREATER
using System.IO.Compression;

namespace CommonNetFuncs.Compression.Internal;

/// <summary>
/// Minimal RFC 1950 (zlib) stream wrapper used in place of <c>System.IO.Compression.ZLibStream</c> (added in .NET 6) for netstandard2.1 targets.
/// </summary>
/// <remarks>
/// Compression writes a standard 2-byte zlib header followed by raw deflate data and a trailing big-endian Adler-32 checksum of the uncompressed
/// bytes, producing a fully RFC 1950 compliant stream. Decompression only validates/consumes the 2-byte header and then delegates directly to
/// <see cref="DeflateStream"/>; the trailing checksum bytes are left unread in the underlying stream since DeflateStream stops once it has decoded
/// all deflate blocks, and are not needed for successful decompression.
/// </remarks>
internal sealed class ZLibCompatStream : Stream
{
	// Standard zlib header for CM=8 (deflate), CINFO=7 (32K window). FLG's FLEVEL bits are only a hint and don't affect decodability.
	private static readonly byte[] ZLibHeader = [0x78, 0x9C];

	private readonly Stream innerStream;
	private readonly DeflateStream deflateStream;
	private readonly CompressionMode mode;
	private readonly bool leaveOpen;
	private uint adlerA = 1;
	private uint adlerB;
	private bool trailerWritten;

	public ZLibCompatStream(Stream stream, CompressionLevel level, bool leaveOpen)
	{
		mode = CompressionMode.Compress;
		innerStream = stream;
		this.leaveOpen = leaveOpen;
		stream.Write(ZLibHeader, 0, ZLibHeader.Length);
		deflateStream = new DeflateStream(stream, level, leaveOpen: true);
	}

	public ZLibCompatStream(Stream stream, CompressionMode mode, bool leaveOpen)
	{
		this.mode = mode;
		innerStream = stream;
		this.leaveOpen = leaveOpen;

		int b1 = stream.ReadByte();
		int b2 = stream.ReadByte();
		if (b1 < 0 || b2 < 0)
		{
			throw new InvalidDataException("Invalid ZLib stream: missing header.");
		}

		deflateStream = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen);
	}

	public override bool CanRead => mode == CompressionMode.Decompress;

	public override bool CanWrite => mode == CompressionMode.Compress;

	public override bool CanSeek => false;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return deflateStream.Read(buffer, offset, count);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		deflateStream.Write(buffer, offset, count);
		UpdateAdler32(buffer, offset, count);
	}

	public override void Flush()
	{
		deflateStream.Flush();
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	private void UpdateAdler32(byte[] buffer, int offset, int count)
	{
		const uint modAdler = 65521;
		for (int i = offset; i < offset + count; i++)
		{
			adlerA = (adlerA + buffer[i]) % modAdler;
			adlerB = (adlerB + adlerA) % modAdler;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !trailerWritten)
		{
			trailerWritten = true;
			deflateStream.Dispose();

			if (mode == CompressionMode.Compress)
			{
				uint checksum = (adlerB << 16) | adlerA;
				byte[] trailer = [(byte)(checksum >> 24), (byte)(checksum >> 16), (byte)(checksum >> 8), (byte)checksum];
				innerStream.Write(trailer, 0, trailer.Length);
			}

			if (!leaveOpen)
			{
				innerStream.Dispose();
			}
		}

		base.Dispose(disposing);
	}
}
#endif
