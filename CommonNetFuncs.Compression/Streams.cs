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
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    await brotliStream.CopyToAsync(compressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    await gzipStream.CopyToAsync(compressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    await deflateStream.CopyToAsync(compressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    await zlibStream.CopyToAsync(compressedStream).ConfigureAwait(false);
                }
                break;
        }
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
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                using (BrotliStream brotliStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    brotliStream.CopyTo(compressedStream);
                }
                break;
            case ECompressionType.Gzip:
                using (GZipStream gzipStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    gzipStream.CopyTo(compressedStream);
                }
                break;
            case ECompressionType.Deflate:
                using (DeflateStream deflateStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    deflateStream.CopyTo(compressedStream);
                }
                break;
            case ECompressionType.ZLib:
                using (ZLibStream zlibStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    zlibStream.CopyTo(compressedStream);
                }
                break;
        }
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
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress))
                {
                    await brotliStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress))
                {
                    await gzipStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress))
                {
                    await deflateStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress))
                {
                    await zlibStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                }
                break;
        }
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
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress))
                {
                    brotliStream.CopyTo(decompressedStream);
                }
                break;
            case ECompressionType.Gzip:
                using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(decompressedStream);
                }
                break;
            case ECompressionType.Deflate:
                using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(decompressedStream);
                }
                break;
            case ECompressionType.ZLib:
                using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress))
                {
                    zlibStream.CopyTo(decompressedStream);
                }
                break;
        }
        decompressedStream.Position = 0;
    }

    public static async Task<byte[]> Compress(this byte[] data, ECompressionType compressionType)
    {
        await using MemoryStream memoryStream = new();
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(memoryStream, CompressionLevel.Optimal))
                {
                    await brotliStream.WriteAsync(data).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(memoryStream, CompressionLevel.Optimal))
                {
                    await gzipStream.WriteAsync(data).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(memoryStream, CompressionLevel.Optimal))
                {
                    await deflateStream.WriteAsync(data).ConfigureAwait(false);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(memoryStream, CompressionLevel.Optimal))
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
        await using MemoryStream decompressStream = new();
        switch (compressionType)
        {
            case ECompressionType.Brotli:
                await using (BrotliStream brotliStream = new(compressedStream, CompressionLevel.Optimal))
                {
                    await brotliStream.CopyToAsync(decompressStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress))
                {
                    await gzipStream.CopyToAsync(decompressStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(compressedStream, CompressionLevel.Optimal))
                {
                    await deflateStream.CopyToAsync(decompressStream).ConfigureAwait(false);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(compressedStream, CompressionLevel.Optimal))
                {
                    await zlibStream.CopyToAsync(decompressStream).ConfigureAwait(false);
                }
                break;
        }

        return decompressStream.ToArray();
    }
}
