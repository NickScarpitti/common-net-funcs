using System.IO.Compression;
using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.Compression;

public static class Files
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Compress a file in the form of a stream into a memory stream
    /// </summary>
    /// <param name="file">Stream and file name to compress into a zipped file</param>
    /// <param name="zipFileStream">Memory stream to receive the zipped file</param>
    /// <param name="compressionLevel">Configure compression preference</param>
    public static async Task ZipFile(this (Stream? fileStream, string fileName) file, MemoryStream? zipFileStream = null, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        List<(Stream? fileStream, string fileName)> files = [file];
        await files.ZipFiles(zipFileStream, compressionLevel).ConfigureAwait(false);
    }

    /// <summary>
    /// Compress multiple files in the form of a stream into a memory stream
    /// </summary>
    /// <param name="files">Streams and associated file names to compress into a zipped file</param>
    /// <param name="zipFileStream">Memory stream to receive the zipped files</param>
    /// <param name="compressionLevel">Configure compression preference</param>
    public static async Task ZipFiles(this IEnumerable<(Stream? fileStream, string fileName)> files, MemoryStream? zipFileStream = null, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        try
        {
            zipFileStream ??= new();

            if (files.Any())
            {
                await using MemoryStream memoryStream = new();
                using ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true);
                await files.AddFilesToZip(archive, compressionLevel).ConfigureAwait(false);
                archive.Dispose();
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(zipFileStream).ConfigureAwait(false); //Copy to output stream
                zipFileStream.Position = 0;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Compress multiple files in the form of a stream into a ZipArchive
    /// </summary>
    /// <param name="files">Streams and associated file names to compress into a zipped file</param>
    /// <param name="archive">ZipArchive to add zipped files to</param>
    /// <param name="compressionLevel">Configure compression preference</param>
    public static async Task AddFilesToZip(this IEnumerable<(Stream? fileStream, string fileName)> files, ZipArchive archive, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        foreach ((Stream? fileStream, string fileName) in files)
        {
            await fileStream.AddFileToZip(archive, fileName, compressionLevel).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Compress a file in the form of a stream into a ZipArchive
    /// </summary>
    /// <param name="fileStream">Stream to compress into a zipped file</param>
    /// <param name="archive">ZipArchive to add zipped files to</param>
    /// <param name="fileName">Name to use for zipped file</param>
    /// <param name="compressionLevel">Configure compression preference</param>
    public static async Task AddFileToZip(this Stream? fileStream, ZipArchive archive, string fileName, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        try
        {
            if (fileStream != null)
            {
                fileStream.Position = 0; //Must have this to prevent errors writing data to the attachment
                ZipArchiveEntry entry = archive.CreateEntry(fileName ?? $"File {archive.Entries.Count}", compressionLevel);
                await using Stream entryStream = entry.Open();
                await fileStream.CopyToAsync(entryStream).ConfigureAwait(false);
                await entryStream.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }
}
