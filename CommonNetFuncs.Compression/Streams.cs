using System.IO.Compression;

namespace CommonNetFuncs.Compression;

public static class Streams
{
    public enum ECompressionType : short
    {
        Brotli = 1,
        Gzip = 2,
        Deflate = 3,
        ZLib = 4
    }

    /// <summary>
    /// Compress a stream that was compressed using a supported compression type
    /// </summary>
    /// <param name="uncompressedStream">Stream to compress</param>
    /// <param name="compressedStream">Stream to receive compressed form of uncompressedStream</param>
    /// <param name="compressionType">Type of compression to use on stream (GZip, Brotli, Deflate, or ZLib)</param>
    /// <exception cref="NotSupportedException"></exception>
    public static async Task CompressStream(this Stream uncompressedStream, Stream compressedStream, ECompressionType compressionType)
    {
        if (!compressedStream.CanWrite)
        {
            throw new NotSupportedException("Compressed stream does not support writing.");
        }

        if (!uncompressedStream.CanRead)
        {
            throw new NotSupportedException("Uncompressed stream does not support reading.");
        }

        await uncompressedStream.FlushAsync().ConfigureAwait(false); //Ensure the stream is flushed before compressing
        uncompressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    await uncompressedStream.CopyToAsync(brotliStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    await uncompressedStream.CopyToAsync(gzipStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    await uncompressedStream.CopyToAsync(deflateStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(compressedStream, CompressionLevel.Optimal, true))
                {
                    await uncompressedStream.CopyToAsync(zlibStream).ConfigureAwait(false);
                }
                break;
        }
        await compressedStream.FlushAsync().ConfigureAwait(false);
        compressedStream.Position = 0;
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
        uncompressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning

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
        compressedStream.Position = 0;
    }

    /// <summary>
    /// Decompress a stream that was compressed using a supported compression type
    /// </summary>
    /// <param name="compressedStream">Stream with compressed data</param>
    /// <param name="decompressedStream">Stream to receive decompressed form of compressedStream</param>
    /// <param name="compressionType">Type of compression used in stream (GZip, Brotli, Deflate, or ZLib)</param>
    /// <exception cref="NotSupportedException"></exception>
    public static async Task DecompressStream(this Stream compressedStream, Stream decompressedStream, ECompressionType compressionType)
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
        compressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning

        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await brotliStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await gzipStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await deflateStream.CopyToAsync(decompressedStream).ConfigureAwait(true);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await zlibStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
        }
        await decompressedStream.FlushAsync().ConfigureAwait(false);
        decompressedStream.Position = 0;
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
        compressedStream.Position = 0; //Reset the position of the uncompressed stream to the beginning

        switch (compressionType)
        {
            case ECompressionType.Brotli:
                using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    brotliStream.CopyTo(decompressedStream);
                }
                break;
            case ECompressionType.Gzip:
                using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    gzipStream.CopyTo(decompressedStream);
                }
                break;
            case ECompressionType.Deflate:
                using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    deflateStream.CopyTo(decompressedStream);
                }
                break;
            case ECompressionType.ZLib:
                using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    zlibStream.CopyTo(decompressedStream);
                }
                break;
        }
        decompressedStream.Flush();
        decompressedStream.Position = 0;
    }

    public static async Task<byte[]> Compress(this byte[] data, ECompressionType compressionType)
    {
        await using MemoryStream memoryStream = new();
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(memoryStream, CompressionLevel.Optimal, true))
                {
                    await brotliStream.WriteAsync(data).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(memoryStream, CompressionLevel.Optimal, true))
                {
                    await gzipStream.WriteAsync(data).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(memoryStream, CompressionLevel.Optimal, true))
                {
                    await deflateStream.WriteAsync(data).ConfigureAwait(false);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(memoryStream, CompressionLevel.Optimal, true))
                {
                    await zlibStream.WriteAsync(data).ConfigureAwait(false);
                }
                break;
        }
        return memoryStream.ToArray();
    }

    public static async Task<byte[]> Decompress(this byte[] compressedData, ECompressionType compressionType)
    {
        await using MemoryStream compressedStream = new(compressedData);
        await using MemoryStream decompressedStream = new();
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await brotliStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await gzipStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await deflateStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress, true))
                {
                    await zlibStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
        }

        return decompressedStream.ToArray();
    }
}
