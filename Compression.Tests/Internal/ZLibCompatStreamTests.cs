using System.IO.Compression;

namespace Compression.Tests.Internal;

// ZLibCompatStream only exists in the netstandard2.1 build of CommonNetFuncs.Compression (guarded by #if !NET6_0_OR_GREATER in production);
// on net10.0 the type isn't compiled at all, so these tests only run against the net8.0 leg (which maps to the netstandard2.1 build).
#if !CORE_NATIVE_BUILD
using CommonNetFuncs.Compression.Internal;

public sealed class ZLibCompatStreamTests
{
	[Fact]
	public void CompressAndDecompress_RoundTrips_Data()
	{
		byte[] original = "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog."u8.ToArray();

		using MemoryStream compressedStream = new();
		using (ZLibCompatStream compressStream = new(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
		{
			compressStream.Write(original, 0, original.Length);
		}

		compressedStream.Position = 0;

		using ZLibCompatStream decompressStream = new(compressedStream, CompressionMode.Decompress, leaveOpen: true);
		using MemoryStream resultStream = new();
		decompressStream.CopyTo(resultStream);

		resultStream.ToArray().ShouldBe(original);
	}

	[Fact]
	public void CompressedStream_WritesZLibHeaderBytes()
	{
		using MemoryStream compressedStream = new();
		using (ZLibCompatStream compressStream = new(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
		{
			compressStream.Write([1, 2, 3], 0, 3);
		}

		byte[] result = compressedStream.ToArray();
		result[0].ShouldBe((byte)0x78);
		result[1].ShouldBe((byte)0x9C);
	}

	[Fact]
	public void DecompressConstructor_WithInvalidHeader_ThrowsInvalidDataException()
	{
		using MemoryStream shortStream = new([0x78]); // only one header byte available

		Should.Throw<InvalidDataException>(() => new ZLibCompatStream(shortStream, CompressionMode.Decompress, leaveOpen: true));
	}

	[Fact]
	public void DecompressConstructor_WithEmptyStream_ThrowsInvalidDataException()
	{
		using MemoryStream emptyStream = new();

		Should.Throw<InvalidDataException>(() => new ZLibCompatStream(emptyStream, CompressionMode.Decompress, leaveOpen: true));
	}

	[Fact]
	public void CanRead_TrueOnlyForDecompressMode()
	{
		using MemoryStream stream = new();
		using ZLibCompatStream compressStream = new(stream, CompressionLevel.Optimal, leaveOpen: true);
		compressStream.CanRead.ShouldBeFalse();
		compressStream.CanWrite.ShouldBeTrue();
		compressStream.CanSeek.ShouldBeFalse();
	}

	[Fact]
	public void CanRead_TrueForDecompressStream()
	{
		using MemoryStream compressedStream = new();
		using (ZLibCompatStream compressStream = new(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
		{
			compressStream.Write([1, 2, 3], 0, 3);
		}

		compressedStream.Position = 0;
		using ZLibCompatStream decompressStream = new(compressedStream, CompressionMode.Decompress, leaveOpen: true);

		decompressStream.CanRead.ShouldBeTrue();
		decompressStream.CanWrite.ShouldBeFalse();
	}

	[Fact]
	public void Length_ThrowsNotSupportedException()
	{
		using MemoryStream stream = new();
		using ZLibCompatStream compatStream = new(stream, CompressionLevel.Optimal, leaveOpen: true);

		Should.Throw<NotSupportedException>(() => compatStream.Length);
	}

	[Fact]
	public void Position_Get_ThrowsNotSupportedException()
	{
		using MemoryStream stream = new();
		using ZLibCompatStream compatStream = new(stream, CompressionLevel.Optimal, leaveOpen: true);

		Should.Throw<NotSupportedException>(() => compatStream.Position);
	}

	[Fact]
	public void Position_Set_ThrowsNotSupportedException()
	{
		using MemoryStream stream = new();
		using ZLibCompatStream compatStream = new(stream, CompressionLevel.Optimal, leaveOpen: true);

		Should.Throw<NotSupportedException>(() => compatStream.Position = 0);
	}

	[Fact]
	public void Seek_ThrowsNotSupportedException()
	{
		using MemoryStream stream = new();
		using ZLibCompatStream compatStream = new(stream, CompressionLevel.Optimal, leaveOpen: true);

		Should.Throw<NotSupportedException>(() => compatStream.Seek(0, SeekOrigin.Begin));
	}

	[Fact]
	public void SetLength_ThrowsNotSupportedException()
	{
		using MemoryStream stream = new();
		using ZLibCompatStream compatStream = new(stream, CompressionLevel.Optimal, leaveOpen: true);

		Should.Throw<NotSupportedException>(() => compatStream.SetLength(10));
	}

	[Fact]
	public void Flush_DoesNotThrow()
	{
		using MemoryStream stream = new();
		using ZLibCompatStream compatStream = new(stream, CompressionLevel.Optimal, leaveOpen: true);

		Should.NotThrow(compatStream.Flush);
	}

	[Fact]
	public void Dispose_WithLeaveOpenTrue_DoesNotDisposeInnerStream()
	{
		using MemoryStream stream = new();
		ZLibCompatStream compatStream = new(stream, CompressionLevel.Optimal, leaveOpen: true);
		compatStream.Write([1, 2, 3], 0, 3);

		compatStream.Dispose();

		Should.NotThrow(() => stream.Position = 0);
	}

	[Fact]
	public void Dispose_WithLeaveOpenFalse_DisposesInnerStream()
	{
		MemoryStream stream = new();
		ZLibCompatStream compatStream = new(stream, CompressionLevel.Optimal, leaveOpen: false);
		compatStream.Write([1, 2, 3], 0, 3);

		compatStream.Dispose();

		Should.Throw<ObjectDisposedException>(() => stream.Position = 0);
	}

	[Fact]
	public void Dispose_CalledTwice_DoesNotThrowOrWriteTrailerTwice()
	{
		using MemoryStream stream = new();
		ZLibCompatStream compatStream = new(stream, CompressionLevel.Optimal, leaveOpen: true);
		compatStream.Write([1, 2, 3], 0, 3);

		compatStream.Dispose();
		long lengthAfterFirstDispose = stream.Length;

		Should.NotThrow(compatStream.Dispose);
		stream.Length.ShouldBe(lengthAfterFirstDispose);
	}

	[Fact]
	public void Dispose_OnDecompressStream_DoesNotWriteTrailer()
	{
		using MemoryStream compressedStream = new();
		using (ZLibCompatStream compressStream = new(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
		{
			compressStream.Write([1, 2, 3], 0, 3);
		}

		long compressedLength = compressedStream.Length;
		compressedStream.Position = 0;

		ZLibCompatStream decompressStream = new(compressedStream, CompressionMode.Decompress, leaveOpen: true);
		byte[] buffer = new byte[16];
		_ = decompressStream.Read(buffer, 0, buffer.Length);
		decompressStream.Dispose();

		// Decompression never appends a trailer, so the underlying stream length is unchanged.
		compressedStream.Length.ShouldBe(compressedLength);
	}
}
#endif
