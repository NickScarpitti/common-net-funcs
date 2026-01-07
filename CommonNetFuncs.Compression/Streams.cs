using System.Buffers;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace CommonNetFuncs.Compression;

public static class Streams
{
	public enum ECompressionType : short
	{
		Brotli = 1,
		Gzip = 2,
		Deflate = 3,
		ZLib = 4,
		None = 5
	}

	//private const int ChunkThreshold = 16 * 1024 * 1024; // 16 MB
	private const int ChunkSize = 1024 * 1024; // 1 MB
	private const int MaxCompressionRatio = 1000; // Detect compression bombs

	private const string UnreadableUncompressedStreamErr = "Uncompressed stream does not support reading.";

	private static readonly ArrayPool<byte> bufferPool = ArrayPool<byte>.Shared;

	public class CompressionLimitExceededException : Exception
	{
		public CompressionLimitExceededException(string message) : base(message) { }

		public CompressionLimitExceededException(string message, Exception innerException) : base(message, innerException) { }

		public CompressionLimitExceededException() { }
	}

	// Helper method to create compression stream - reduces code duplication
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Stream CreateCompressionStream(Stream stream, ECompressionType compressionType, CompressionLevel level, bool leaveOpen)
	{
		return compressionType switch
		{
			ECompressionType.Brotli => new BrotliStream(stream, level, leaveOpen),
			ECompressionType.Gzip => new GZipStream(stream, level, leaveOpen),
			ECompressionType.Deflate => new DeflateStream(stream, level, leaveOpen),
			ECompressionType.ZLib => new ZLibStream(stream, level, leaveOpen),
			_ => throw new NotImplementedException($"Compression type {compressionType} is not supported.")
		};
	}

	// Helper method to create decompression stream - reduces code duplication
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Stream CreateDecompressionStream(Stream stream, ECompressionType compressionType, bool leaveOpen)
	{
		return compressionType switch
		{
			ECompressionType.Brotli => new BrotliStream(stream, CompressionMode.Decompress, leaveOpen),
			ECompressionType.Gzip => new GZipStream(stream, CompressionMode.Decompress, leaveOpen),
			ECompressionType.Deflate => new DeflateStream(stream, CompressionMode.Decompress, leaveOpen),
			ECompressionType.ZLib => new ZLibStream(stream, CompressionMode.Decompress, leaveOpen),
			_ => throw new NotImplementedException($"Compression type {compressionType} is not supported.")
		};
	}

	/// <summary>
	/// Compress a <see cref="Stream" /> using a supported compression type.
	/// </summary>
	/// <param name="uncompressedStream">Stream to compress.</param>
	/// <param name="compressedStream">Stream to receive compressed form of uncompressedStream.</param>
	/// <param name="compressionType">Type of compression to use on stream (GZip, Brotli, Deflate, or ZLib).</param>
	/// <param name="compressionLevel">Optional: Compression level to use (defaults to Optimal).</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <exception cref="NotSupportedException">Thrown if the compressed stream does not support writing or the uncompressed stream does not support reading.</exception>
	public static async Task CompressStream(this Stream uncompressedStream, Stream compressedStream, ECompressionType compressionType, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
	{
		if (!compressedStream.CanWrite)
		{
			throw new NotSupportedException("Compressed stream does not support writing.");
		}

		if (!uncompressedStream.CanRead)
		{
			throw new NotSupportedException(UnreadableUncompressedStreamErr);
		}

		// Only flush if stream is writable (avoid exception on readonly streams)
		if (uncompressedStream.CanWrite)
		{
			await uncompressedStream.FlushAsync(cancellationToken).ConfigureAwait(false);
		}

		if (uncompressedStream.CanSeek)
		{
			uncompressedStream.Position = 0;
		}

		await using (Stream compressionStream = CreateCompressionStream(compressedStream, compressionType, compressionLevel, true))
		{
			await uncompressedStream.CopyToAsync(compressionStream, cancellationToken).ConfigureAwait(false);
		}

		await compressedStream.FlushAsync(cancellationToken).ConfigureAwait(false);

		if (compressedStream.CanSeek)
		{
			compressedStream.Position = 0;
		}
	}

	/// <summary>
	/// Compress a <see cref="Stream" /> that was compressed using a supported compression type.
	/// </summary>
	/// <param name="uncompressedStream">Stream to compress.</param>
	/// <param name="compressedStream">Stream to receive compressed form of uncompressedStream.</param>
	/// <param name="compressionType">Type of compression to use on stream (GZip, Brotli, Deflate, or ZLib).</param>
	/// <param name="compressionLevel">Optional: Compression level to use (defaults to Optimal).</param>
	/// <exception cref="NotSupportedException">Thrown if the compressed stream does not support writing or the uncompressed stream does not support reading.</exception>
	public static void CompressStreamSynchronous(this Stream uncompressedStream, Stream compressedStream, ECompressionType compressionType, CompressionLevel compressionLevel = CompressionLevel.Optimal)
	{
		if (!compressedStream.CanWrite)
		{
			throw new NotSupportedException("Compressed stream does not support writing.");
		}

		if (!uncompressedStream.CanRead)
		{
			throw new NotSupportedException(UnreadableUncompressedStreamErr);
		}

		// Only flush if stream is writable
		if (uncompressedStream.CanWrite)
		{
			uncompressedStream.Flush();
		}

		if (uncompressedStream.CanSeek)
		{
			uncompressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning
		}

		using (Stream compressionStream = CreateCompressionStream(compressedStream, compressionType, compressionLevel, true))
		{
			uncompressedStream.CopyTo(compressionStream);
		}

		compressedStream.Flush();
		if (compressedStream.CanSeek)
		{
			compressedStream.Position = 0;
		}
	}

	/// <summary>
	/// Decompress a <see cref="Stream" /> that was compressed using a supported compression type.
	/// </summary>
	/// <param name="compressedStream">Stream with compressed data.</param>
	/// <param name="decompressedStream">Stream to receive decompressed form of compressedStream.</param>
	/// <param name="compressionType">Type of compression used in stream (GZip, Brotli, Deflate, or ZLib).</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <exception cref="NotSupportedException">Thrown if the decompressed stream does not support writing or the compressed stream does not support reading.</exception>
	public static async Task DecompressStream(this Stream compressedStream, Stream decompressedStream, ECompressionType compressionType, CancellationToken cancellationToken = default)
	{
		if (!decompressedStream.CanWrite)
		{
			throw new NotSupportedException("Decompressed stream does not support writing.");
		}

		if (!compressedStream.CanRead)
		{
			throw new NotSupportedException(UnreadableUncompressedStreamErr);
		}

		// Only flush if stream is writable
		if (compressedStream.CanWrite)
		{
			await compressedStream.FlushAsync(cancellationToken).ConfigureAwait(false);
		}

		if (compressedStream.CanSeek)
		{
			compressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning
		}

		long maxDecompressedSize = compressedStream.CalculateMaxDecompressedSize();

		await using (Stream decompressionStream = CreateDecompressionStream(compressedStream, compressionType, true))
		{
			await decompressionStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
		}

		await decompressedStream.FlushAsync(cancellationToken).ConfigureAwait(false);

		if (decompressedStream.CanSeek)
		{
			decompressedStream.Position = 0;
		}
	}

	/// <summary>
	/// Decompress a <see cref="Stream" /> that was compressed using a supported compression type.
	/// </summary>
	/// <param name="compressedStream">Stream with compressed data.</param>
	/// <param name="decompressedStream">Stream to receive decompressed form of compressedStream.</param>
	/// <param name="compressionType">Type of compression used in stream (GZip, Brotli, Deflate, or ZLib).</param>
	/// <exception cref="NotSupportedException">Thrown if the decompressed stream does not support writing or the compressed stream does not support reading.</exception>
	public static void DecompressStreamSynchronous(this Stream compressedStream, Stream decompressedStream, ECompressionType compressionType)
	{
		if (!decompressedStream.CanWrite)
		{
			throw new NotSupportedException("Decompressed stream does not support writing.");
		}

		if (!compressedStream.CanRead)
		{
			throw new NotSupportedException(UnreadableUncompressedStreamErr);
		}

		// Only flush if stream is writable
		if (compressedStream.CanWrite)
		{
			compressedStream.Flush();
		}

		if (compressedStream.CanSeek)
		{
			compressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning
		}

		long maxDecompressedSize = compressedStream.CalculateMaxDecompressedSize();

		using (Stream decompressionStream = CreateDecompressionStream(compressedStream, compressionType, true))
		{
			decompressionStream.CopyWithLimit(decompressedStream, maxDecompressedSize);
		}

		decompressedStream.Flush();
		if (decompressedStream.CanSeek)
		{
			decompressedStream.Position = 0;
		}
	}

	/// <summary>
	/// Decompress a <see cref="Stream" /> that was compressed using a supported compression type.
	/// </summary>
	/// <param name="compressedStream">Stream with compressed data</param>
	/// <param name="compressionType">Type of compression used in stream (GZip, Brotli, Deflate, or ZLib).</param>
	/// <param name="leaveOpen">Whether to leave the compressed stream open after decompression.</param>
	/// <returns>A stream containing the decompressed data.</returns>
	/// <exception cref="NotSupportedException">Thrown if stream is not readable.</exception>
	/// <exception cref="NotImplementedException">Thrown if compression type has not been implemented yet.</exception>
	public static Stream Decompress(this Stream compressedStream, ECompressionType compressionType, bool leaveOpen = false)
	{
		if (!compressedStream.CanRead)
		{
			throw new NotSupportedException(UnreadableUncompressedStreamErr);
		}

		if (compressedStream.CanSeek)
		{
			compressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning
		}

		return CreateDecompressionStream(compressedStream, compressionType, leaveOpen);
	}

	/// <summary>
	/// Decompress the data contained within a byte array that was compressed using a supported compression type.
	/// </summary>
	/// <param name="compressedData">The byte array containing the compressed data to decompress.</param>
	/// <param name="compressionType">Type of compression used on the data (GZip, Brotli, Deflate, or ZLib).</param>
	/// <param name="maxCompressionRatio">Optional: If the compressed data exceeds this compression ratio the method will stop execution as a safety mechanism.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Byte array containing the decompressed version of the original byte array.</returns>
	/// <exception cref="CompressionLimitExceededException">Thrown if the decompressed data exceeds the maximum allowed size which defaults to 1000 times the compressed size.</exception>
	public static async Task<byte[]> Decompress(this byte[] compressedData, ECompressionType compressionType, int maxCompressionRatio = MaxCompressionRatio, CancellationToken cancellationToken = default)
	{
		await using MemoryStream compressedStream = new(compressedData);
		// Estimate initial capacity (compressed data typically expands 2-10x)
		int estimatedSize = Math.Min((int)(compressedData.Length * 4L), int.MaxValue / 2);
		await using MemoryStream decompressedStream = new(estimatedSize);

		long maxDecompressedSize = compressedData.LongLength * maxCompressionRatio;

		await using (Stream decompressionStream = CreateDecompressionStream(compressedStream, compressionType, true))
		{
			await decompressionStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
		}

		return decompressedStream.ToArray();
	}

	/// <summary>
	/// Compress a <see cref="Stream" /> using a supported compression type.
	/// </summary>
	/// <param name="uncompressedStream">Stream to compress.</param>
	/// <param name="compressionType">Type of compression to use on stream (GZip, Brotli, Deflate, or ZLib).</param>
	/// <param name="compressionLevel">Optional: Compression level to use (defaults to Optimal).</param>
	/// <param name="leaveOpen">Whether to leave the uncompressed stream open after compression.</param>
	/// <returns>A stream containing the compressed data.</returns>
	/// <exception cref="NotSupportedException">Thrown if stream is not readable.</exception>
	/// <exception cref="NotImplementedException">Thrown if compression type has not been implemented yet.</exception>
	public static Stream Compress(this Stream uncompressedStream, ECompressionType compressionType, CompressionLevel compressionLevel = CompressionLevel.Optimal, bool leaveOpen = false)
	{
		if (!uncompressedStream.CanRead)
		{
			throw new NotSupportedException(UnreadableUncompressedStreamErr);
		}
		if (uncompressedStream.CanSeek)
		{
			uncompressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning
		}
		return CreateCompressionStream(uncompressedStream, compressionType, compressionLevel, leaveOpen);
	}

	/// <summary>
	/// Compress the data contained within a byte array using a supported compression type.
	/// </summary>
	/// <param name="data">The byte array containing the data to compress.</param>
	/// <param name="compressionType">Type of compression to use on stream (GZip, Brotli, Deflate, or ZLib).</param>
	/// <param name="compressionLevel">Optional: Compression level to use (defaults to Optimal).</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Byte array containing the compressed version of the original byte array.</returns>
	public static async Task<byte[]> Compress(this byte[] data, ECompressionType compressionType, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
	{
		// Estimate initial capacity for compressed stream
		// Use better heuristic: smaller for highly compressible data, cap at reasonable size
		int estimatedSize = data.Length switch
		{
			<= 1024 => 512,
			<= 10240 => data.Length / 4,
			_ => Math.Min(data.Length / 3, 512 * 1024) // Cap at 512KB for very large inputs
		};
		await using MemoryStream memoryStream = new(estimatedSize);

		// Write all data at once - compression streams handle internal buffering efficiently
		await using (Stream compressionStream = CreateCompressionStream(memoryStream, compressionType, compressionLevel, true))
		{
			await compressionStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
		}

		return memoryStream.ToArray();
	}


	/// <summary>
	/// Detect the compression type of a <see cref="Stream"/> based on its header without advancing the <see cref="Stream"/> position.
	/// </summary>
	/// <param name="stream">Stream to analyze.</param>
	/// <returns>Detected compression type, or "None" if not recognized.</returns>
	/// <remarks>If the <see cref="Stream"/> is not seekable, there is no way to detect deflate compression.</remarks>
	public static async Task<ECompressionType> DetectCompressionType(this Stream? stream)
	{
		if (stream?.CanRead != true)
		{
			return ECompressionType.None;
		}

		// For seekable streams, use the original approach
		if (stream.CanSeek)
		{
			return await DetectCompressionTypeSeekable(stream).ConfigureAwait(false);
		}

		// For non-seekable streams, we need to wrap it
		return await DetectCompressionTypeNonSeekable(stream).ConfigureAwait(false);
	}

	internal static async Task<ECompressionType> DetectCompressionTypeSeekable(Stream stream)
	{
		long originalPosition = stream.Position;
		byte[]? headerArray = null;

		try
		{
			// Rent small header buffer from pool
			headerArray = ArrayPool<byte>.Shared.Rent(4);
			int bytesRead = await stream.ReadAsync(headerArray.AsMemory(0, 4)).ConfigureAwait(false);

			if (bytesRead < 2)
			{
				return ECompressionType.None;
			}

			ECompressionType result = AnalyzeHeader(headerArray, bytesRead);
			if (result != ECompressionType.None)
			{
				return result;
			}

			// For deflate detection, we need more sophisticated approach
			// Reset position first
			stream.Position = originalPosition;

			// Reuse the header buffer if it's large enough, otherwise rent a larger one
			byte[]? sample = null;
			try
			{
				// Read a larger sample for deflate detection
				sample = ArrayPool<byte>.Shared.Rent(1024);
				int sampleBytesRead = await stream.ReadAsync(sample.AsMemory(0, 1024)).ConfigureAwait(false);

				if (sampleBytesRead > 0 && await IsDeflateCompressed(sample.AsMemory(0, sampleBytesRead)).ConfigureAwait(false))
				{
					return ECompressionType.Deflate;
				}

				return ECompressionType.None;
			}
			finally
			{
				if (sample != null)
				{
					ArrayPool<byte>.Shared.Return(sample);
				}
			}
		}
		finally
		{
			stream.Position = originalPosition;
			if (headerArray != null)
			{
				ArrayPool<byte>.Shared.Return(headerArray);
			}
		}
	}

	internal static async Task<ECompressionType> DetectCompressionTypeNonSeekable(Stream stream)
	{
		// For non-seekable streams, read header directly without BufferedStream overhead
		byte[]? headerArray = null;

		try
		{
			headerArray = ArrayPool<byte>.Shared.Rent(4);
			int bytesRead = await stream.ReadAsync(headerArray.AsMemory(0, 4)).ConfigureAwait(false);

			if (bytesRead < 2)
			{
				return ECompressionType.None;
			}

			return AnalyzeHeader(headerArray, bytesRead);
		}
		finally
		{
			if (headerArray != null)
			{
				ArrayPool<byte>.Shared.Return(headerArray);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ECompressionType AnalyzeHeader(byte[] header, int bytesRead)
	{
		// Check most common formats first for better branch prediction
		// GZIP: 1F 8B
		if (bytesRead >= 2 && header[0] == 0x1F && header[1] == 0x8B)
		{
			return ECompressionType.Gzip;
		}

		// ZLIB: 78 01 / 78 9C / 78 DA (first byte 0x78, second varies)
		// Optimize by checking first byte once
		if (bytesRead >= 2 && header[0] == 0x78)
		{
			byte second = header[1];
			if (second == 0x01 || second == 0x9C || second == 0xDA)
			{
				return ECompressionType.ZLib;
			}
		}

		// Brotli: 0xCE B2 CF 81 (first 4 bytes, but only if at least 4 bytes read)
		if (bytesRead >= 4 && header[0] == 0xCE && header[1] == 0xB2 && header[2] == 0xCF && header[3] == 0x81)
		{
			return ECompressionType.Brotli;
		}

		return ECompressionType.None;
	}

	/// <summary>
	/// Checks a <see cref="byte"/> <see cref="Array"/> to determine if it has been compressed using the deflate compression algorithm.
	/// </summary>
	/// <param name="data">Byte array to check for deflate compression.</param>
	/// <returns><see langword="true"/> if byte array contains data that was compressed using the deflate compression algorithm.</returns>
	public static Task<bool> IsDeflateCompressed(byte[] data)
	{
		return IsDeflateCompressedCore(data.AsMemory());
	}

	/// <summary>
	/// Checks a <see cref="Memory{byte}"/> to determine if it has been compressed using the deflate compression algorithm.
	/// </summary>
	/// <param name="data">Memory to check for deflate compression.</param>
	/// <returns><see langword="true"/> if data was compressed using the deflate compression algorithm.</returns>
	internal static Task<bool> IsDeflateCompressed(Memory<byte> data)
	{
		return IsDeflateCompressedCore(data);
	}

	private static async Task<bool> IsDeflateCompressedCore(Memory<byte> data)
	{
		byte[]? buffer = null;
		try
		{
			// Create MemoryStream and write data directly to avoid ToArray() allocation
			await using MemoryStream memoryStream = new(data.Length);
			await memoryStream.WriteAsync(data).ConfigureAwait(false);
			memoryStream.Position = 0;
			await using DeflateStream deflateStream = new(memoryStream, CompressionMode.Decompress);

			buffer = bufferPool.Rent(1);
			int bytesRead = await deflateStream.ReadAsync(buffer.AsMemory(0, 1)).ConfigureAwait(false);
			return bytesRead > 0;
		}
		catch (Exception)
		{
			return false;
		}
		finally
		{
			if (buffer != null)
			{
				bufferPool.Return(buffer);
			}
		}
	}
	/// <summary>
	/// Asynchronously copies data from the source <see cref="Stream"/> to the destination <see cref="Stream"/>, ensuring that the total number of bytes copied does not exceed the specified limit.
	/// </summary>
	/// <remarks>
	/// This method reads data from the <paramref name="source"/> <see cref="Stream"/> in chunks and writes it to the <paramref name="destination"/> stream.
	/// The operation stops if the total number of bytes copied reaches the specified <paramref name="maxBytes"/> limit.
	/// </remarks>
	/// <param name="source">The source stream to read data from.</param>
	/// <param name="destination">The destination stream to write data to.</param>
	/// <param name="maxBytes">The maximum number of bytes to copy. If the limit is exceeded, an exception is thrown.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>A task that represents the asynchronous copy operation.</returns>
	/// <exception cref="CompressionLimitExceededException">Thrown if the total number of bytes copied exceeds <paramref name="maxBytes"/>.</exception>
	public static async Task CopyWithLimitAsync(this Stream source, Stream destination, long maxBytes, CancellationToken cancellationToken = default)
	{
		byte[] buffer = bufferPool.Rent(ChunkSize);
		long totalBytes = 0;
		int bytesRead;

		try
		{
			while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, ChunkSize), cancellationToken).ConfigureAwait(false)) > 0)
			{
				totalBytes += bytesRead;

				if (totalBytes > maxBytes)
				{
					throw new CompressionLimitExceededException($"Operation would exceed maximum size limit of {maxBytes} bytes");
				}

				await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
			}
		}
		finally
		{
			bufferPool.Return(buffer);
		}
	}

	/// <summary>
	/// Copies data from the source <see cref="Stream"/> to the destination <see cref="Stream"/>, ensuring that the total number of bytes copied does not exceed the specified limit.
	/// </summary>
	/// <remarks>
	/// This method reads data from the <paramref name="source"/> stream in chunks and writes it to the <paramref name="destination"/> <see cref="Stream"/>.
	/// The operation stops if the total number of bytes copied reaches the specified <paramref name="maxBytes"/> limit.
	/// </remarks>
	/// <param name="source">The source stream to read data from.</param>
	/// <param name="destination">The destination stream to write data to.</param>
	/// <param name="maxBytes">The maximum number of bytes to copy. If the limit is exceeded, an exception is thrown.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>A task that represents the asynchronous copy operation.</returns>
	/// <exception cref="CompressionLimitExceededException">Thrown if the total number of bytes copied exceeds <paramref name="maxBytes"/>.</exception>
	public static void CopyWithLimit(this Stream source, Stream destination, long maxBytes, CancellationToken cancellationToken = default)
	{
		long totalBytes = 0;
		int bytesRead;

		byte[] buffer = bufferPool.Rent(ChunkSize);
		try
		{
			while ((bytesRead = source.Read(buffer, 0, ChunkSize)) > 0)
			{
				totalBytes += bytesRead;

				if (totalBytes > maxBytes)
				{
					throw new CompressionLimitExceededException($"Operation would exceed maximum size limit of {maxBytes} bytes");
				}

				cancellationToken.ThrowIfCancellationRequested();

				destination.Write(buffer, 0, bytesRead);
			}
		}
		finally
		{
			bufferPool.Return(buffer);
		}
	}

	/// <summary>
	/// Detect compression type of <see cref="Stream"/> that is non-seekable and bytes are lost after reading (ie. AWS S3 ResponseStream)
	/// </summary>
	/// <param name="stream">Stream to get compression type from.</param>
	/// <returns>A tuple with the compression type and a new stream object that contains the bytes read originally plus the rest of the original stream.</returns>
	public static async Task<(ECompressionType, Stream)> DetectCompressionTypeAndReset(Stream stream)
	{
		const int headerLength = 8; // Enough for all supported compression types

		// Read header bytes (do not advance original stream if seekable)
		if (stream.CanSeek)
		{
			long originalPosition = stream.Position;
			ECompressionType type = await DetectCompressionType(stream).ConfigureAwait(false);
			stream.Position = originalPosition;
			return (type, stream);
		}
		else
		{
			byte[] header = ArrayPool<byte>.Shared.Rent(headerLength);
			try
			{
				int bytesRead = await stream.ReadAsync(header.AsMemory(0, headerLength)).ConfigureAwait(false);
				byte[] headerCopy = new byte[bytesRead];
				header.AsSpan(0, bytesRead).CopyTo(headerCopy);

				MemoryStream headerStream = new(headerCopy, 0, bytesRead, writable: false);
				ECompressionType type = await DetectCompressionType(headerStream).ConfigureAwait(false);
				headerStream.Position = 0;
				ConcatenatedStream combinedStream = new(headerStream, stream);
				return (type, combinedStream);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(header);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long CalculateMaxDecompressedSize(this Stream compressedStream)
	{
		return compressedStream.CanSeek ? compressedStream.Length * MaxCompressionRatio : long.MaxValue;
	}
}

// Helper to concatenate two streams
public class ConcatenatedStream(Stream first, Stream second) : Stream
{
	private readonly Stream first = first;
	private readonly Stream second = second;

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override long Length => throw new NotSupportedException();

	public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

	public override void Flush() { }

	public override int Read(byte[] buffer, int offset, int count)
	{
		int read = first.Read(buffer, offset, count);
		if (read < count)
		{
			read += second.Read(buffer, offset + read, count - read);
		}

		return read;
	}

	public override int Read(Span<byte> buffer)
	{
		int read = first.Read(buffer);
		if (read < buffer.Length)
		{
			Span<byte> remaining = buffer[read..];
			read += second.Read(remaining);
		}

		return read;
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		int read = await first.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
		if (read < buffer.Length)
		{
			Memory<byte> remaining = buffer[read..];
			read += await second.ReadAsync(remaining, cancellationToken).ConfigureAwait(false);
		}
		return read;
	}

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		int read = await first.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
		if (read < count)
		{
			read += await second.ReadAsync(buffer.AsMemory(offset + read, count - read), cancellationToken).ConfigureAwait(false);
		}

		return read;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}
}
