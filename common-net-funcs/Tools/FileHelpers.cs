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
    /// <param name="supressLogging">Will prevent this method from emitting logs</param>
    /// <param name="createPathIfMissing">Will create the file path if it does not exist</param>
    /// <returns>Unique file name for the given destination</returns>
    public static string GetSafeSaveName(this string originalFullFileName, bool startFromZero = true, bool supressLogging = false, bool createPathIfMissing = false)
    {
        //Remove invalid characters from
        string originalFileName = Path.GetFileName(originalFullFileName);
        string oldCleanFileName = originalFileName.Replace(Path.GetFileName(originalFullFileName), Path.GetFileName(originalFullFileName)
            .Replace("/", "-").Replace(@"\", "-").Replace(":", ".").Replace("<", "_").Replace(">", "_").Replace(@"""", "'").Replace("|", "_").Replace("?", "_").Replace("*", "_"));

        string testPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(originalFullFileName) ?? string.Empty, oldCleanFileName));

        string? directory = Path.GetDirectoryName(testPath);
        if (!Directory.Exists(directory) && directory != null)
        {
            if (createPathIfMissing)
            {
                if (!supressLogging)
                {
                    logger.Warn($"[{directory}] does not exist! Creating new directory...");
                }
                Directory.CreateDirectory(directory);
            }
            else if (!supressLogging)
            {
                logger.Warn($"[{directory}] does not exist! Unable to continue...");
                return string.Empty;
            }
        }

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
                Regex regex = new(incrementingPattern, RegexOptions.IgnoreCase);
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

    /// <summary>
    /// Simulates automatic Windows behavior of adding a number after the original file name when a file with the same name exists already
    /// </summary>
    /// <param name="path">Full path to look in for duplicated file names</param>
    /// <param name="fileName">The file name to check for uniqueness with in the given file path</param>
    /// <param name="startFromZero">Will start incrementing unique value from 0 if true. If false, will start at the integer value present inside of parentheses directly before the extension if such value is present.</param>
    /// <param name="supressLogging">Will prevent this method from emitting logs</param>
    /// <param name="createPathIfMissing">Will create the file path if it does not exist</param>
    /// <returns>Unique file name for the given destination</returns>
    public static string GetSafeSaveName(string path, string fileName, bool startFromZero = true, bool supressLogging = false, bool createPathIfMissing = false)
    {
        fileName = fileName.Replace("/", "-").Replace(@"\", "-").Replace(":", ".").Replace("<", "_").Replace(">", "_").Replace(@"""", "'").Replace("|", "_").Replace("?", "_").Replace("*", "_");

        string testPath = Path.GetFullPath(Path.Combine(path, fileName));

        string? directory = Path.GetDirectoryName(testPath);
        if (!Directory.Exists(directory) && directory != null)
        {
            if (createPathIfMissing)
            {
                if (!supressLogging)
                {
                    logger.Warn($"[{directory}] does not exist! Creating new directory...");
                }
                Directory.CreateDirectory(directory);
            }
            else if (!supressLogging)
            {
                logger.Warn($"[{directory}] does not exist! Unable to continue...");
                return string.Empty;
            }
        }

        if (File.Exists(testPath))
        {
            int i = 0;
            string ext = Path.GetExtension(fileName);
            string incrementingPattern = $@"\([0-9]+\)\{ext}";
            string? lastTestPath = null;

            if (!startFromZero)
            {
                i = int.TryParse(Regex.Match(fileName, $@"\(([^)]*)\){ext}").Groups[0].Value, out int startNumber) ? startNumber : 0; //Start at number present
            }
            while (File.Exists(testPath))
            {
                Regex regex = new(incrementingPattern, RegexOptions.IgnoreCase);
                if (regex.IsMatch(fileName)) //File already has an iterator
                {
                    testPath = Path.GetFullPath(Path.Combine(path, Regex.Replace(fileName, incrementingPattern, $"({i}){ext}")));
                }
                else
                {
                    testPath = Path.GetFullPath(Path.Combine(path, $"{fileName.Replace(ext, string.Empty)} ({i}){ext}"));
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
        return Path.GetFileName(testPath);
    }

    /// <summary>
    /// Adds a zipped file to an existing ZipArchive
    /// </summary>
    /// <param name="archive">ZipArchive to add zipped file to</param>
    /// <param name="zipFile">File to add to archive</param>
    /// <param name="fileCount">Number to use as part of the default file name "File {fileCount}"</param>
    public static async Task AddFileToZip(this ZipArchive archive, ZipFile? zipFile, int fileCount)
    {
        try
        {
            if (zipFile?.FileData != null)
            {
                zipFile.FileData.Position = 0; //Must have this to prevent errors writing data to the attachment
                ZipArchiveEntry entry = archive.CreateEntry(zipFile.FileName ?? $"File {fileCount}", CompressionLevel.SmallestSize);
                await using Stream entryStream = entry.Open();
                await zipFile.FileData.CopyToAsync(entryStream);
                await entryStream.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Zips passed in files directly to a stream object
    /// </summary>
    /// <param name="zipFiles">Files to zip</param>
    /// <param name="zipFileStream">Stream to contain resulting zip archive</param>
    public static async Task ZipFiles(IEnumerable<ZipFile>? zipFiles, MemoryStream? zipFileStream = null)
    {
        try
        {
            zipFileStream ??= new();

            if (zipFiles?.Any() == true)
            {
                int i = 1;
                await using MemoryStream memoryStream = new();
                using ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true);
                foreach (ZipFile file in zipFiles)
                {
                    if (file.FileData != null)
                    {
                        file.FileData.Position = 0; //Must have this to prevent errors writing data to the attachment
                        ZipArchiveEntry entry = archive.CreateEntry(file.FileName ?? $"File {i}", CompressionLevel.SmallestSize);
                        await using Stream entryStream = entry.Open();
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
