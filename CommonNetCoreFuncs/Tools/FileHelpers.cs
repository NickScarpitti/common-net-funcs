using System.IO.Compression;

namespace CommonNetCoreFuncs.Tools;

/// <summary>
/// Helpers for dealing with files
/// </summary>
public static class FileHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Simulates automatic Windows behavior of adding a number after the original file name when a file with the same name exists already
    /// </summary>
    /// <param name="originalFullFileName">Full path and file name</param>
    /// <returns></returns>
    public static string GetSafeSaveName(this string originalFullFileName)
    {
        //Remove invalid characters from 
        originalFullFileName = originalFullFileName.Replace(Path.GetFileName(originalFullFileName), Path.GetFileName(originalFullFileName)
            .Replace("/", "-").Replace(@"\", "-").Replace(":", ".").Replace("<", "_").Replace(">", "_").Replace(@"""", "'").Replace("|", "_").Replace("?", "_").Replace("*", "_"));

        string testPath = Path.GetFullPath(originalFullFileName);
        if (File.Exists(testPath))
        {
            //Update name
            string oldFileName = Path.GetFileName(originalFullFileName);
            string ext = Path.GetExtension(oldFileName);
            oldFileName = oldFileName.Replace(ext, null);
            string newFullFileName = originalFullFileName;
            int i = 0;
            while (File.Exists(testPath))
            {
                newFullFileName = originalFullFileName.Replace(oldFileName + ext, $"{oldFileName} ({i}){ext}");
                testPath = Path.GetFullPath(newFullFileName);
                i++;
            }
            return newFullFileName;
        }
        else
        {
            return originalFullFileName;
        }
    }

    public static async Task AddFileToZip(this ZipArchive archive, ZipFile? zipFile, int fileCount)
    {
        try
        {
            if (zipFile?.FileData != null)
            {
                zipFile.FileData.Position = 0; //Must have this to prevent errors writing data to the attachment
                    var entry = archive.CreateEntry(zipFile.FileName ?? $"File {fileCount}", CompressionLevel.SmallestSize);
                    using var entryStream = entry.Open();
                    await zipFile.FileData.CopyToAsync(entryStream);
                    await entryStream.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "AddFileToZip Error");
        }
    }
    
    public static async Task ZipFiles(IEnumerable<ZipFile>? zipFiles, MemoryStream? zipFileStream = null)
    {
        try
        {
            zipFileStream ??= new();

            if (zipFiles != null && zipFiles.Any())
            {
                int i = 1;
                using MemoryStream memoryStream = new();
                using ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
                foreach (ZipFile file in zipFiles)
                {
                    if (file.FileData != null)
                    {
                        file.FileData.Position = 0; //Must have this to prevent errors writing data to the attachment
                        var entry = archive.CreateEntry(file.FileName ?? $"File {i}", CompressionLevel.SmallestSize);
                        using var entryStream = entry.Open();
                        await file.FileData.CopyToAsync(entryStream);
                        await entryStream.FlushAsync();
                        i++;
                    }
                }
                archive.Dispose();
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(zipFileStream); //Copy to output stream
                zipFileStream.Seek(0, SeekOrigin.Begin);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "ZipFiles Error");
        }
    }
}


public class ZipFile
{
    public Stream? FileData { get; set; }
    public string? FileName { get; set; }
}
