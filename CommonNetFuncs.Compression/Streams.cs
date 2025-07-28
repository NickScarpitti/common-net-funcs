using System.IO.Compression;

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

    private const int ChunkThreshold = 16 * 1024 * 1024; // 16 MB
    private const int ChunkSize = 1024 * 1024; // 1 MB
    private const int MaxCompressionRatio = 1000; // Detect compression bombs

    public class CompressionLimitExceededException : Exception
    {
        public CompressionLimitExceededException(string message) : base(message) { }

        public CompressionLimitExceededException(string message, Exception innerException) : base(message, innerException) { }

        public CompressionLimitExceededException() { }
    }

    /// <summary>
    /// Compress a stream that was compressed using a supported compression type
    /// </summary>
    /// <param name="uncompressedStream">Stream to compress</param>
    /// <param name="compressedStream">Stream to receive compressed form of uncompressedStream</param>
    /// <param name="compressionType">Type of compression to use on stream (GZip, Brotli, Deflate, or ZLib)</param>
    /// <exception cref="NotSupportedException"></exception>
    public static async Task CompressStream(this Stream uncompressedStream, Stream compressedStream, ECompressionType compressionType, CancellationToken cancellationToken = default)
    {
        if (!compressedStream.CanWrite)
        {
            throw new NotSupportedException("Compressed stream does not support writing.");
        }

        if (!uncompressedStream.CanRead)
        {
            throw new NotSupportedException("Uncompressed stream does not support reading.");
        }

        await uncompressedStream.FlushAsync(cancellationToken).ConfigureAwait(false); //Ensure the stream is flushed before compressing

        if (uncompressedStream.CanSeek)
        {
            uncompressedStream.Position = 0;
        }

        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    await uncompressedStream.CopyToAsync(brotliStream, cancellationToken).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    await uncompressedStream.CopyToAsync(gzipStream, cancellationToken).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    await uncompressedStream.CopyToAsync(deflateStream, cancellationToken).ConfigureAwait(false);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    await uncompressedStream.CopyToAsync(zlibStream, cancellationToken).ConfigureAwait(false);
                }
                break;
        }

        await compressedStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        if (compressedStream.CanSeek)
        {
            compressedStream.Position = 0;
        }
    }

    /// <summary>
    /// Compress a stream that was compressed using a supported compression type
    /// </summary>
    /// <param name="uncompressedStream">Stream to compress</param>
    /// <param name="compressedStream">Stream to receive compressed form of uncompressedStream</param>
    /// <param name="compressionType">Type of compression to use on stream (GZip, Brotli, Deflate, or ZLib)</param>
    /// <exception cref="NotSupportedException"></exception>
    public static void CompressStreamSynchronous(this Stream uncompressedStream, Stream compressedStream, ECompressionType compressionType)
    {
        if (!compressedStream.CanWrite)
        {
            throw new NotSupportedException("Compressed stream does not support writing.");
        }

        if (!uncompressedStream.CanRead)
        {
            throw new NotSupportedException("Uncompressed stream does not support reading.");
        }

        uncompressedStream.Flush(); //Ensure the stream is flushed before compressing

        if (uncompressedStream.CanSeek)
        {
            uncompressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning
        }

        switch (compressionType)
        {
            case ECompressionType.Brotli:
                using (BrotliStream brotliStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    uncompressedStream.CopyTo(brotliStream);
                }
                break;
            case ECompressionType.Gzip:
                using (GZipStream gzipStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    uncompressedStream.CopyTo(gzipStream);
                }
                break;
            case ECompressionType.Deflate:
                using (DeflateStream deflateStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    uncompressedStream.CopyTo(deflateStream);
                }
                break;
            case ECompressionType.ZLib:
                using (ZLibStream zlibStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    uncompressedStream.CopyTo(zlibStream);
                }
                break;
        }

        compressedStream.Flush();
        if (compressedStream.CanSeek)
        {
            compressedStream.Position = 0;
        }
    }

    /// <summary>
    /// Decompress a stream that was compressed using a supported compression type
    /// </summary>
    /// <param name="compressedStream">Stream with compressed data</param>
    /// <param name="decompressedStream">Stream to receive decompressed form of compressedStream</param>
    /// <param name="compressionType">Type of compression used in stream (GZip, Brotli, Deflate, or ZLib)</param>
    /// <exception cref="NotSupportedException"></exception>
    public static async Task DecompressStream(this Stream compressedStream, Stream decompressedStream, ECompressionType compressionType, CancellationToken cancellationToken = default)
    {
        if (!decompressedStream.CanWrite)
        {
            throw new NotSupportedException("Decompressed stream does not support writing.");
        }

        if (!compressedStream.CanRead)
        {
            throw new NotSupportedException("Uncompressed stream does not support reading.");
        }

        await compressedStream.FlushAsync(cancellationToken).ConfigureAwait(false); //Ensure the stream is flushed before compressing

        if (compressedStream.CanSeek)
        {
            compressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning
        }

        long maxDecompressedSize = compressedStream.Length * MaxCompressionRatio;
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await brotliStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await gzipStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await deflateStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await zlibStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
                }
                break;
        }

        await decompressedStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        if (decompressedStream.CanSeek)
        {
            decompressedStream.Position = 0;
        }
    }

    /// <summary>
    /// Decompress a stream that was compressed using a supported compression type
    /// </summary>
    /// <param name="compressedStream">Stream with compressed data</param>
    /// <param name="decompressedStream">Stream to receive decompressed form of compressedStream</param>
    /// <param name="compressionType">Type of compression used in stream (GZip, Brotli, Deflate, or ZLib)</param>
    /// <exception cref="NotSupportedException"></exception>
    public static void DecompressStreamSynchronous(this Stream compressedStream, Stream decompressedStream, ECompressionType compressionType)
    {
        if (!decompressedStream.CanWrite)
        {
            throw new NotSupportedException("Decompressed stream does not support writing.");
        }

        if (!compressedStream.CanRead)
        {
            throw new NotSupportedException("Uncompressed stream does not support reading.");
        }

        compressedStream.Flush(); //Ensure the stream is flushed before compressing
        if (compressedStream.CanSeek)
        {
            compressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning
        }

        long maxDecompressedSize = compressedStream.Length * MaxCompressionRatio;
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    brotliStream.CopyWithLimit(decompressedStream, maxDecompressedSize);
                }
                break;
            case ECompressionType.Gzip:
                using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    gzipStream.CopyWithLimit(decompressedStream, maxDecompressedSize);
                }
                break;
            case ECompressionType.Deflate:
                using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    deflateStream.CopyWithLimit(decompressedStream, maxDecompressedSize);
                }
                break;
            case ECompressionType.ZLib:
                using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    zlibStream.CopyWithLimit(decompressedStream, maxDecompressedSize);
                }
                break;
        }
        decompressedStream.Flush();
        if (decompressedStream.CanSeek)
        {
            decompressedStream.Position = 0;
        }
    }

    public static async Task<byte[]> Compress(this byte[] data, ECompressionType compressionType, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (data.Length < ChunkThreshold)
        {
            // Small data: write all at once
            switch (compressionType)
            {
                case ECompressionType.Brotli:
                    await using (BrotliStream brotliStream = new(memoryStream, CompressionLevel.Optimal, true))
                    {
                        await brotliStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case ECompressionType.Gzip:
                    await using (GZipStream gzipStream = new(memoryStream, CompressionLevel.Optimal, true))
                    {
                        await gzipStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case ECompressionType.Deflate:
                    await using (DeflateStream deflateStream = new(memoryStream, CompressionLevel.Optimal, true))
                    {
                        await deflateStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case ECompressionType.ZLib:
                    await using (ZLibStream zlibStream = new(memoryStream, CompressionLevel.Optimal, true))
                    {
                        await zlibStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                    }
                    break;
            }
        }
        else
        {
            // Large data: write in chunks
            switch (compressionType)
            {
                case ECompressionType.Brotli:
                    await using (BrotliStream brotliStream = new(memoryStream, CompressionLevel.Optimal, true))
                    {
                        for (int offset = 0; offset < data.Length; offset += ChunkSize)
                        {
                            int count = Math.Min(ChunkSize, data.Length - offset);
                            await brotliStream.WriteAsync(data.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
                case ECompressionType.Gzip:
                    await using (GZipStream gzipStream = new(memoryStream, CompressionLevel.Optimal, true))
                    {
                        for (int offset = 0; offset < data.Length; offset += ChunkSize)
                        {
                            int count = Math.Min(ChunkSize, data.Length - offset);
                            await gzipStream.WriteAsync(data.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
                case ECompressionType.Deflate:
                    await using (DeflateStream deflateStream = new(memoryStream, CompressionLevel.Optimal, true))
                    {
                        for (int offset = 0; offset < data.Length; offset += ChunkSize)
                        {
                            int count = Math.Min(ChunkSize, data.Length - offset);
                            await deflateStream.WriteAsync(data.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
                case ECompressionType.ZLib:
                    await using (ZLibStream zlibStream = new(memoryStream, CompressionLevel.Optimal, true))
                    {
                        for (int offset = 0; offset < data.Length; offset += ChunkSize)
                        {
                            int count = Math.Min(ChunkSize, data.Length - offset);
                            await zlibStream.WriteAsync(data.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
            }
        }
        return memoryStream.ToArray();
    }

    public static async Task<byte[]> Decompress(this byte[] compressedData, ECompressionType compressionType, CancellationToken cancellationToken = default)
    {
        await using MemoryStream compressedStream = new(compressedData);
        await using MemoryStream decompressedStream = new();

        long maxDecompressedSize = compressedData.LongLength * MaxCompressionRatio;
        if (compressedData.LongLength < ChunkThreshold)
        {
            // Small data: copy all at once
            switch (compressionType)
            {
                case ECompressionType.Brotli:
                    await using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress, true))
                    {
                        await brotliStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case ECompressionType.Gzip:
                    await using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress, true))
                    {
                        await gzipStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case ECompressionType.Deflate:
                    await using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress, true))
                    {
                        await deflateStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case ECompressionType.ZLib:
                    await using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress, true))
                    {
                        await zlibStream.CopyWithLimitAsync(decompressedStream, maxDecompressedSize, cancellationToken).ConfigureAwait(false);
                    }
                    break;
            }
        }
        else
        {
            // Large data: read in chunks
            byte[] buffer = new byte[ChunkSize];
            int bytesRead;
            int totalBytesRead = 0;
            switch (compressionType)
            {
                case ECompressionType.Brotli:
                    await using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress, true))
                    {
                        while ((bytesRead = await brotliStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            totalBytesRead += bytesRead;
                            if (totalBytesRead > maxDecompressedSize)
                            {
                                throw new CompressionLimitExceededException($"Operation would exceed maximum size limit of {maxDecompressedSize} bytes");
                            }
                            await decompressedStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
                case ECompressionType.Gzip:
                    await using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress, true))
                    {
                        while ((bytesRead = await gzipStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            totalBytesRead += bytesRead;
                            if (totalBytesRead > maxDecompressedSize)
                            {
                                throw new CompressionLimitExceededException($"Operation would exceed maximum size limit of {maxDecompressedSize} bytes");
                            }
                            await decompressedStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
                case ECompressionType.Deflate:
                    await using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress, true))
                    {
                        while ((bytesRead = await deflateStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            totalBytesRead += bytesRead;
                            if (totalBytesRead > maxDecompressedSize)
                            {
                                throw new CompressionLimitExceededException($"Operation would exceed maximum size limit of {maxDecompressedSize} bytes");
                            }
                            await decompressedStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
                case ECompressionType.ZLib:
                    await using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress, true))
                    {
                        while ((bytesRead = await zlibStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            totalBytesRead += bytesRead;
                            if (totalBytesRead > maxDecompressedSize)
                            {
                                throw new CompressionLimitExceededException($"Operation would exceed maximum size limit of {maxDecompressedSize} bytes");
                            }
                            await decompressedStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
            }
        }

        return decompressedStream.ToArray();
    }

    /// <summary>
    /// Detect the compression type of a stream based on its header without advancing the stream position.
    /// </summary>
    /// <param name="stream">Stream to analyze</param>
    /// <returns>Detected compression type, or None if not recognized</returns>
    /// <remarks>If the stream is not seekable, there is no way to detect deflate compression</remarks>
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
        byte[] header = new byte[4];
        int bytesRead;

        try
        {
            // Read up to 4 bytes for header detection
            //bytesRead = await stream.ReadAsync(header, 0, 4);
            bytesRead = await stream.ReadAsync(header.AsMemory(0, 4)).ConfigureAwait(false);
            if (bytesRead < 2)
            {
                return ECompressionType.None;
            }

            ECompressionType result = AnalyzeHeader(header, bytesRead);
            if (result != ECompressionType.None)
            {
                return result;
            }

            // For deflate detection, we need more sophisticated approach
            // Reset position first
            stream.Position = originalPosition;

            // Read a larger sample for deflate detection
            byte[] sample = new byte[1024];
            //int sampleBytesRead = await stream.ReadAsync(sample, 0, sample.Length);
            int sampleBytesRead = await stream.ReadAsync(sample).ConfigureAwait(false);

            if (sampleBytesRead > 0 && await IsDeflateCompressed(sample.Take(sampleBytesRead).ToArray()).ConfigureAwait(false))
            {
                return ECompressionType.Deflate;
            }

            return ECompressionType.None;
        }
        finally
        {
            // Reset stream position
            stream.Position = originalPosition;
        }
    }

    internal static async Task<ECompressionType> DetectCompressionTypeNonSeekable(Stream stream)
    {
        // For non-seekable streams, we need to buffer the data
        await using BufferedStream bufferedStream = new(stream, 4096);

        // Try to peek at the first few bytes
        byte[] header = new byte[4];
        //int bytesRead = await bufferedStream.ReadAsync(header, 0, 4);
        int bytesRead = await bufferedStream.ReadAsync(header.AsMemory(0, 4)).ConfigureAwait(false);

        if (bytesRead < 2)
        {
            return ECompressionType.None;
        }

        ECompressionType result = AnalyzeHeader(header, bytesRead);
        if (result != ECompressionType.None)
        {
            return result;
        }

        return ECompressionType.None;
    }

    internal static ECompressionType AnalyzeHeader(byte[] header, int bytesRead)
    {
        // GZIP: 1F 8B
        if (header[0] == 0x1F && header[1] == 0x8B)
        {
            return ECompressionType.Gzip;
        }

        // ZLIB: 78 01 / 78 9C / 78 DA (first byte 0x78, second varies)
        if (header[0] == 0x78 && (header[1] == 0x01 || header[1] == 0x9C || header[1] == 0xDA))
        {
            return ECompressionType.ZLib;
        }

        // Brotli: 0xCE B2 CF 81 (first 4 bytes, but only if at least 4 bytes read)
        if (bytesRead >= 4 && header[0] == 0xCE && header[1] == 0xB2 && header[2] == 0xCF && header[3] == 0x81)
        {
            return ECompressionType.Brotli;
        }

        return ECompressionType.None;
    }

    // Fixed version that doesn't modify the original data
    public static async Task<bool> IsDeflateCompressed(byte[] data)
    {
        try
        {
            await using MemoryStream memoryStream = new(data);
            await using DeflateStream deflateStream = new(memoryStream, CompressionMode.Decompress);

            byte[] buffer = new byte[1];
            //int bytesRead = await deflateStream.ReadAsync(buffer, 0, buffer.Length);
            int bytesRead = await deflateStream.ReadAsync(buffer).ConfigureAwait(false);
            return bytesRead > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Copies data from the source stream to the destination stream, ensuring that the total number of bytes copied
    /// does not exceed the specified limit.
    /// </summary>
    /// <remarks>This method reads data from the <paramref name="source"/> stream in chunks and writes it to
    /// the <paramref name="destination"/> stream. The operation stops if the total number of bytes copied reaches the
    /// specified <paramref name="maxBytes"/> limit.</remarks>
    /// <param name="source">The source stream to read data from.</param>
    /// <param name="destination">The destination stream to write data to.</param>
    /// <param name="maxBytes">The maximum number of bytes to copy. If the limit is exceeded, an exception is thrown.</param>
    /// <param name="cancellationToken">Optional: Cancelltion token for this operation.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="CompressionLimitExceededException">Thrown if the total number of bytes copied exceeds <paramref name="maxBytes"/>.</exception>
    public static async Task CopyWithLimitAsync(this Stream source, Stream destination, long maxBytes, CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[ChunkSize];
        long totalBytes = 0;
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            totalBytes += bytesRead;

            if (totalBytes > maxBytes)
            {
                throw new CompressionLimitExceededException($"Operation would exceed maximum size limit of {maxBytes} bytes");
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
        }
    }

    public static void CopyWithLimit(this Stream source, Stream destination, long maxBytes, CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[ChunkSize];
        long totalBytes = 0;
        int bytesRead;

        while ((bytesRead = source.Read(buffer)) > 0)
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
}
