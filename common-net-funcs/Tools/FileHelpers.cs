using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Common_Net_Funcs.Tools;

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
    /// <param name="startFromZero">Will start incrementing unique value from 0 if true. If false, will start at the integer value present inside of parentheses directly before the extension if such value is present.</param>
    /// <returns></returns>
    public static string GetSafeSaveName(this string originalFullFileName, bool startFromZero = true, bool supressLogging = false)
    {
        //Remove invalid characters from
        string originalFileName = Path.GetFileName(originalFullFileName);
        string oldCleanFileName = originalFileName.Replace(Path.GetFileName(originalFullFileName), Path.GetFileName(originalFullFileName)
            .Replace("/", "-").Replace(@"\", "-").Replace(":", ".").Replace("<", "_").Replace(">", "_").Replace(@"""", "'").Replace("|", "_").Replace("?", "_").Replace("*", "_"));

        string testPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(originalFullFileName) ?? string.Empty, oldCleanFileName));
        if (File.Exists(testPath))
        {
            //Update name
            string ext = Path.GetExtension(originalFullFileName);
            oldCleanFileName = Path.GetFileName(originalFullFileName).Replace(ext, null);
            string incrementingPattern = $@"\([0-9]+\)\{ext}";
            int i = 0;
            string? lastTestPath = null;
            if (!startFromZero)
            {
                i = int.TryParse(Regex.Match(oldCleanFileName, $@"\(([^)]*)\){ext}").Groups[0].Value, out int startNumber) ? startNumber : 0; //Start at number present
            }
            while (File.Exists(testPath))
            {
                if (!supressLogging)
                {
                    logger.Info($"[{testPath}] exists, checking with iterator [{i}]");
                }
                Regex regex = new Regex(incrementingPattern, RegexOptions.IgnoreCase);
                if (regex.IsMatch(oldCleanFileName + ext)) //File already has an iterator
                {
                    testPath = Path.GetFullPath(Regex.Replace(oldCleanFileName + ext, incrementingPattern, $"({i}){ext}"));
                }
                else
                {
                    testPath = Path.GetFullPath(originalFullFileName.Replace(oldCleanFileName + ext, $"{oldCleanFileName} ({i}){ext}"));
                }

                if (!supressLogging)
                {
                    logger.Info($"Checking new testPath [{testPath}] with iterator [{i}]]");
                }

                if (lastTestPath == testPath)
                {
                    logger.Warn($"File name [{testPath}] not changing, breaking out of loop.");
                    break;
                }
                lastTestPath = testPath;
                i++;
            }
        }
        else
        {
            logger.Info($"Original path with cleaned file name [{testPath}] is unique");
        }
        return testPath;
    }

    public static string GetSafeSaveName(string path, string fileName, bool startFromZero = true)
    {
        fileName = fileName.Replace("/", "-").Replace(@"\", "-").Replace(":", ".").Replace("<", "_").Replace(">", "_").Replace(@"""", "'").Replace("|", "_").Replace("?", "_").Replace("*", "_");

        string testPath = Path.GetFullPath(Path.Combine(path, fileName));
        if (File.Exists(testPath))
        {
            int i = 0;
            string ext = Path.GetExtension(fileName);
            string incrementingPattern = $@"\([0-9]+\)\{ext}";
            if (!startFromZero)
            {
                i = int.TryParse(Regex.Match(fileName, $@"\(([^)]*)\){ext}").Groups[0].Value, out int startNumber) ? startNumber : 0; //Start at number present
            }
            while (File.Exists(testPath))
            {
                Regex regex = new Regex(incrementingPattern, RegexOptions.IgnoreCase);
                if (regex.IsMatch(fileName)) //File already has an iterator
                {
                    testPath = Path.GetFullPath(Path.Combine(path, Regex.Replace(fileName, incrementingPattern, $"({i}){ext}")));
                }
                else
                {
                    testPath = Path.GetFullPath(Path.Combine(path, $"{fileName.Replace(ext, string.Empty)} ({i}){ext}"));
                    i++;
                }
            }
        }
        else
        {
            logger.Info($"Original path with cleaned file name [{testPath}] is unique");
        }
        return Path.GetFileName(testPath);
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
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }

    public static async Task ZipFiles(IEnumerable<ZipFile>? zipFiles, MemoryStream? zipFileStream = null)
    {
        try
        {
            zipFileStream ??= new();

            if (zipFiles?.Any() == true)
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
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }
}

public class ZipFile
{
    public Stream? FileData { get; set; }
    public string? FileName { get; set; }
}
