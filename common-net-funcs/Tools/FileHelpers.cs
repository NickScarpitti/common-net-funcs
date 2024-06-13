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
            string incrementingPattern = @$"\([0-9]+\)\{ext}";
            int i = 0;
            string? lastTestPath = null;
            if (!startFromZero)
            {
                i = int.TryParse(Regex.Match(oldCleanFileName, @$"\(([^)]*)\){ext}").Groups[0].Value, out int startNumber) ? startNumber : 0; //Start at number present
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
            string incrementingPattern = @$"\([0-9]+\)\{ext}";
            string? lastTestPath = null;

            if (!startFromZero)
            {
                i = int.TryParse(Regex.Match(fileName, @$"\(([^)]*)\){ext}").Groups[0].Value, out int startNumber) ? startNumber : 0; //Start at number present
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
    /// Validates file extension based on list of valid extensions
    /// </summary>
    /// <param name="fileName">Full file name (with extension) to check for a valid file extension</param>
    /// <param name="validExtensions">Array of valid file extensions</param>
    /// <returns>True if the file has a valid extension</returns>
    public static bool ValidateFileExtention(this string fileName, string[] validExtensions)
    {
        string extension = Path.GetExtension(fileName);
        return validExtensions.ContainsInvariant(extension);
    }
}
