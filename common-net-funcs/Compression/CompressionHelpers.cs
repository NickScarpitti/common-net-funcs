using System.IO.Compression;

namespace Common_Net_Funcs.Compression;

public static class CompressionHelpers
{
    public static async Task AddToZipArchive(this IEnumerable<(Stream? fileStream, string? fileName)> files, ZipArchive archive, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        foreach ((Stream fileStream, string? fileName) in files)
        {
            await fileStream.AddToZipArchive(archive, fileName, compressionLevel);
        }
    }

    public static async Task AddToZipArchive(this Stream? fileStream, ZipArchive archive, string? fileName, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        if (fileStream != null)
        {
            fileStream.Position = 0; //Must have this to prevent errors writing data to the attachment
            ZipArchiveEntry entry = archive.CreateEntry(fileName ?? $"File {archive.Entries.Count}", compressionLevel);
            await using Stream entryStream = entry.Open();
            await fileStream.CopyToAsync(entryStream);
            await entryStream.FlushAsync();
        }
    }

    public static async Task DecompressGzipSteam(this Stream compressedStream, MemoryStream decompressedStream)
    {
        await using GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress); //Decompressed data will be written to this stream
        gzipStream.CopyTo(decompressedStream);
        decompressedStream.Position = 0;
    }

    public static async Task DecompressBrotliStream(this Stream compressedStream, MemoryStream decompressedStream)
    {
        await using BrotliStream brotliStream = new(compressedStream, CompressionMode.Decompress);
        brotliStream.CopyTo(decompressedStream);
        decompressedStream.Position = 0;
    }
}
