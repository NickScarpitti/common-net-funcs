using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonNetFuncs.Core;

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
                    logger.Warn("{msg}", $"[{directory}] does not exist! Creating new directory...");
                }
                Directory.CreateDirectory(directory);
            }
            else if (!supressLogging)
            {
                logger.Warn("{msg}", $"[{directory}] does not exist! Unable to continue...");
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
                    logger.Info("{msg}", $"[{testPath}] exists, checking with iterator [{i}]");
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
                    logger.Info("{msg}", $"Checking new testPath [{testPath}] with iterator [{i}]]");
                }

                if (lastTestPath == testPath)
                {
                    logger.Warn("{msg}", $"File name [{testPath}] not changing, breaking out of loop.");
                    break;
                }
                lastTestPath = testPath;
                i++;
            }
        }
        else
        {
            logger.Info("{msg}", $"Original path with cleaned file name [{testPath}] is unique");
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
                    logger.Warn("{msg}", $"[{directory}] does not exist! Creating new directory...");
                }
                Directory.CreateDirectory(directory);
            }
            else if (!supressLogging)
            {
                logger.Warn("{msg}", $"[{directory}] does not exist! Unable to continue...");
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
                    logger.Info("{msg}", $"Checking new testPath [{testPath}] with iterator [{i}]]");
                }

                if (lastTestPath == testPath)
                {
                    logger.Warn("{msg}", $"File name [{testPath}] not changing, breaking out of loop.");
                    break;
                }
                lastTestPath = testPath;
                i++;
            }
        }
        else
        {
            logger.Info("{msg}", $"Original path with cleaned file name [{testPath}] is unique");
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

    /// <summary>
    /// Gets the hash for a file
    /// </summary>
    /// <param name="fileName">Full file name including directory pointing to the file to get the hash of</param>
    /// <returns>Hash for the file</returns>
    public static async Task<string> GetHashFromFile(this string fileName, EHashAlgorithm algorithm = EHashAlgorithm.SHA512)
    {
        string? hash = null;
        await using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read);
        try
        {
            hash = await fileStream.GetHashFromStream(algorithm);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting hash from file!");
        }
        finally
        {
            fileStream.Close();
            await fileStream.DisposeAsync();
        }
        return hash ?? string.Empty;
    }

    /// <summary>
    /// Generates hash based on the contents of a stream using the designated algorithm
    /// </summary>
    /// <param name="stream">Stream generate hash for</param>
    /// <param name="hashAlgorithm">Algorithm to use to generate the hash</param>
    /// <returns>Hash for the contents of the stream</returns>
    public static async Task<string> GetHashFromStream(this Stream stream, EHashAlgorithm hashAlgorithm = EHashAlgorithm.SHA512)
    {
        byte[] data;
        if (hashAlgorithm == EHashAlgorithm.SHA1)
        {
            using HashAlgorithm algorithm = SHA1.Create();
            data = await algorithm.ComputeHashAsync(stream);
        }
        else if (hashAlgorithm == EHashAlgorithm.MD5)
        {
            using HashAlgorithm algorithm = MD5.Create();
            data = await algorithm.ComputeHashAsync(stream);
        }
        else if (hashAlgorithm == EHashAlgorithm.SHA256)
        {
            using HashAlgorithm algorithm = SHA256.Create();
            data = await algorithm.ComputeHashAsync(stream);
        }
        else if (hashAlgorithm == EHashAlgorithm.SHA384)
        {
            using HashAlgorithm algorithm = SHA384.Create();
            data = await algorithm.ComputeHashAsync(stream);
        }
        else
        {
            using HashAlgorithm algorithm = SHA512.Create();
            data = await algorithm.ComputeHashAsync(stream);
        }

        StringBuilder stringBuilder = new();

        for (int i = 0; i < data.Length; i++)
        {
            stringBuilder.Append(data[i].ToString("x2"));
        }
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Returns the full file path of all files contained under the directory startDirectory
    /// </summary>
    /// <param name="startDirectory">Top most directory to get files from</param>
    /// <param name="searchPattern">Optional: Search pattern value to be used for the searchPattern parameter in Directory.GetDirectories</param>
    /// <returns>List of all files contained within startDirectory and matching searchPattern</returns>
    public static List<string> GetAllFilesRecursive(string? startDirectory, string searchPattern = "*")
    {
        List<string> files = [];
        if (!startDirectory.IsNullOrWhiteSpace() && Directory.Exists(startDirectory))
        {
            foreach (string subDirectory in Directory.GetDirectories(startDirectory, searchPattern, SearchOption.AllDirectories))
            {
                files.AddRange(Directory.GetFiles(subDirectory));
            }
        }
        return files;
    }
}
