using System.IO.Compression;

namespace CommonNetFuncs.Compression;

public static class Streams
{
    public enum ECompressionType
    {
        Brotli,
        Gzip,
        Deflate,
        ZLib
    }

    /// <summary>
    /// Decompress a stream that was compressed using a supported compression type
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
                    brotliStream.CopyTo(compressedStream);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    gzipStream.CopyTo(compressedStream);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(uncompressedStream, CompressionMode.Compress))
                {
                    deflateStream.CopyTo(compressedStream);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(uncompressedStream, CompressionMode.Compress))
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
                    brotliStream.CopyTo(decompressedStream);
                }
                break;
            case ECompressionType.Gzip:
                await using (GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(decompressedStream);
                }
                break;
            case ECompressionType.Deflate:
                await using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(decompressedStream);
                }
                break;
            case ECompressionType.ZLib:
                await using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress))
                {
                    zlibStream.CopyTo(decompressedStream);
                }
                break;
        }
        decompressedStream.Position = 0;
    }
}
