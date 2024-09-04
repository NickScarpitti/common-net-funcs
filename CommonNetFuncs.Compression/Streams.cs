using System.IO.Compression;

namespace CommonNetFuncs.Compression;

public static class Streams
{
    public enum ECompressionType
    {
        Brotli,
        Gzip
    }

    /// <summary>
    /// Decompress a stream that was compressed using a supported compression type
    /// </summary>
    /// <param name="compressedStream">Stream with compressed data</param>
    /// <param name="decompressedStream">Stream to receive decompressed form of compressedStream</param>
    /// <param name="compressionType">Type of compression used in stream (GZip and Brotli compression methods currently supported</param>
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
            default:
                throw new NotSupportedException();
        }
        decompressedStream.Position = 0;
    }

    ///// <summary>
    ///// Decompress a stream that was compressed using GZip compression
    ///// </summary>
    ///// <param name="compressedStream">Stream compressed with GZip</param>
    ///// <param name="decompressedStream">Memory stream to receive decompressed form of compressedStream</param>
    //public static async Task DecompressGzipSteam(this Stream compressedStream, MemoryStream decompressedStream)
    //{
    //    await using GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress); //Decompressed data will be written to this stream
    //    gzipStream.CopyTo(decompressedStream);
    //    decompressedStream.Position = 0;
    //}

    ///// <summary>
    ///// Decompress a stream that was compressed using Brotli compression
    ///// </summary>
    ///// <param name="compressedStream">Stream compressed with Brotli</param>
    ///// <param name="decompressedStream">Memory stream to receive decompressed form of compressedStream</param>
    //public static async Task DecompressBrotliStream(this Stream compressedStream, MemoryStream decompressedStream)
    //{
    //    decompressedStream.Position = 0;
    //}
}
