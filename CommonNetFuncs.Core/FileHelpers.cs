using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CommonNetFuncs.Core;

/// <summary>
/// Helpers for dealing with files
/// </summary>
public static partial class FileHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Simulates automatic Windows behavior of adding a number after the original file name when a file with the same name exists already
    /// </summary>
    /// <param name="originalFullFileName">Full path and file name</param>
    /// <param name="startFromZero">
    /// Will start incrementing unique value from 0 if true. If false, will start at the integer value present inside of parentheses directly before the extension if such value is present.
    /// </param>
    /// <param name="suppressLogging">Will prevent this method from emitting logs</param>
    /// <param name="createPathIfMissing">Will create the file path if it does not exist</param>
    /// <returns>Unique file name for the given destination</returns>
    public static string GetSafeSaveName(this string originalFullFileName, bool startFromZero = true, bool suppressLogging = false, bool createPathIfMissing = false)
    {
        // Remove invalid characters from
        string originalFileName = Path.GetFileName(originalFullFileName);
        string cleanFileName = CleanFileName(originalFileName);

        string testPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(originalFullFileName) ?? string.Empty, cleanFileName));

        string? directory = Path.GetDirectoryName(testPath);
        if (!Directory.Exists(directory) && (directory != null))
        {
            if (createPathIfMissing)
            {
                if (!suppressLogging)
                {
                    logger.Warn("{msg}", $"[{directory}] does not exist! Creating new directory...");
                }
                Directory.CreateDirectory(directory);
            }
            else if (!suppressLogging)
            {
                logger.Warn("{msg}", $"[{directory}] does not exist! Unable to continue...");
                return string.Empty;
            }
        }

        if (File.Exists(testPath))
        {
            // Update name
            string ext = Path.GetExtension(originalFullFileName);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFullFileName);
            string incrementingPattern = @$"\(([0-9]+)\)\{ext}";
            int i = 0;
            string? lastTestPath = null;

            if (!startFromZero)
            {
                // Start at number present
                Match match = incrementedFileNameRegex().Match(fileNameWithoutExt);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int startNumber))
                {
                    i = startNumber;
                }
            }

            while (File.Exists(testPath))
            {
                if (!suppressLogging)
                {
                    logger.Info("{msg}", $"[{testPath}] exists, checking with iterator [{i}]");
                }

                // Check if file already has an iterator
                bool hasIterator = Regex.IsMatch(cleanFileName, incrementingPattern);

                if (hasIterator)
                {
                    testPath = Path.GetFullPath(Regex.Replace(cleanFileName, incrementingPattern, $"({i}){ext}"));
                }
                else
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(cleanFileName);
                    testPath = Path.GetFullPath(Path.Combine(
                        Path.GetDirectoryName(originalFullFileName) ?? string.Empty,
                        $"{fileNameWithoutExtension} ({i}){ext}"));
                }

                if (!suppressLogging)
                {
                    logger.Info("{msg}", $"Checking new testPath [{testPath}] with iterator [{i}]]");
                }

                // Prevent infinite loop if file name isn't changing
                if (string.Equals(lastTestPath, testPath, StringComparison.Ordinal))
                {
                    if (!suppressLogging)
                    {
                        logger.Warn("{msg}", $"File name [{testPath}] not changing, breaking out of loop.");
                    }
                    break;
                }

                lastTestPath = testPath;
                i++;
            }
        }
        else if (!suppressLogging)
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
    /// <param name="startFromZero">
    /// Will start incrementing unique value from 0 if true. If false, will start at the integer value present inside of parentheses directly before the extension if such value is present.
    /// </param>
    /// <param name="suppressLogging">Will prevent this method from emitting logs</param>
    /// <param name="createPathIfMissing">Will create the file path if it does not exist</param>
    /// <returns>Unique file name for the given destination</returns>
    public static string GetSafeSaveName(string path, string fileName, bool startFromZero = true, bool suppressLogging = false, bool createPathIfMissing = false)
    {
        fileName = CleanFileName(fileName);
        string testPath = Path.GetFullPath(Path.Combine(path, fileName));

        string? directory = Path.GetDirectoryName(testPath);
        if (!Directory.Exists(directory) && (directory != null))
        {
            if (createPathIfMissing)
            {
                if (!suppressLogging)
                {
                    logger.Warn("{msg}", $"[{directory}] does not exist! Creating new directory...");
                }
                Directory.CreateDirectory(directory);
            }
            else if (!suppressLogging)
            {
                logger.Warn("{msg}", $"[{directory}] does not exist! Unable to continue...");
                return string.Empty;
            }
        }

        if (File.Exists(testPath))
        {
            int i = 0;
            string ext = Path.GetExtension(fileName);
            string incrementingPattern = @$"\(([0-9]+)\)\{ext}";
            string? lastTestPath = null;

            if (!startFromZero)
            {
                // Start at number present
                Match match = Regex.Match(fileName, incrementingPattern);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int startNumber))
                {
                    i = startNumber;
                }
            }

            while (File.Exists(testPath))
            {
                // Check if file already has an iterator
                bool hasIterator = Regex.IsMatch(fileName, incrementingPattern);

                if (hasIterator)
                {
                    testPath = Path.GetFullPath(Path.Combine(path, Regex.Replace(fileName, incrementingPattern, $"({i}){ext}")));
                }
                else
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    testPath = Path.GetFullPath(Path.Combine(
                        path,
                        $"{fileNameWithoutExt} ({i}){ext}"));
                }

                if (!suppressLogging)
                {
                    logger.Info("{msg}", $"Checking new testPath [{testPath}] with iterator [{i}]]");
                }

                // Prevent infinite loop if file name isn't changing
                if (string.Equals(lastTestPath, testPath, StringComparison.Ordinal))
                {
                    if (!suppressLogging)
                    {
                        logger.Warn("{msg}", $"File name [{testPath}] not changing, breaking out of loop.");
                    }
                    break;
                }

                lastTestPath = testPath;
                i++;
            }
        }
        else if (!suppressLogging)
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
    public static bool ValidateFileExtension(this string fileName, string[] validExtensions)
    {
        string extension = Path.GetExtension(fileName);
        return validExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the hash for a file
    /// </summary>
    /// <param name="fileName">Full file name including directory pointing to the file to get the hash of</param>
    /// <param name="algorithm">Algorithm to use to generate the hash</param>
    /// <returns>Hash for the file</returns>
    public static async Task<string> GetHashFromFile(this string fileName, EHashAlgorithm algorithm = EHashAlgorithm.SHA512)
    {
        if (!File.Exists(fileName))
        {
            return string.Empty;
        }

        try
        {
            await using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read);
            return await fileStream.GetHashFromStream(algorithm).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting hash from file!");
            return string.Empty;
        }
    }

    /// <summary>
    /// Generates hash based on the contents of a stream using the designated algorithm
    /// </summary>
    /// <param name="stream">Stream generate hash for</param>
    /// <param name="hashAlgorithm">Algorithm to use to generate the hash</param>
    /// <returns>Hash for the contents of the stream</returns>
    public static async Task<string> GetHashFromStream(this Stream stream, EHashAlgorithm hashAlgorithm = EHashAlgorithm.SHA512)
    {
        HashAlgorithm algorithm = hashAlgorithm switch
        {
            EHashAlgorithm.SHA1 => SHA1.Create(),
            EHashAlgorithm.MD5 => MD5.Create(),
            EHashAlgorithm.SHA256 => SHA256.Create(),
            EHashAlgorithm.SHA384 => SHA384.Create(),
            _ => SHA512.Create()
        };

        try
        {
            stream.Position = 0;
            using (algorithm)
            {
                byte[] hash = await algorithm.ComputeHashAsync(stream).ConfigureAwait(false);
                return Convert.ToHexStringLower(hash);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error computing hash from stream");
            return string.Empty;
        }
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

        if (string.IsNullOrWhiteSpace(startDirectory) || !Directory.Exists(startDirectory))
        {
            return files;
        }

        try
        {
            // Get files in current directory
            files.AddRange(Directory.GetFiles(startDirectory, searchPattern));

            // Get files in all subdirectories
            foreach (string directory in Directory.GetDirectories(startDirectory))
            {
                files.AddRange(GetAllFilesRecursive(directory, searchPattern));
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error getting files from directory: {startDirectory}");
        }

        return files;
    }

    /// <summary>
    /// Cleans a filename by removing invalid characters
    /// </summary>
    /// <param name="fileName">The filename to clean</param>
    /// <returns>A clean filename</returns>
    private static string CleanFileName(string fileName)
    {
        // Replace invalid characters with safe alternatives
        return fileName
            .Replace("/", "-")
            .Replace(@"\", "-")
            .Replace(":", ".")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace("\"", "'")
            .Replace("|", "_")
            .Replace("?", "_")
            .Replace("*", "_");
    }

    [GeneratedRegex(@"\(([^)]*)\)$")]
    private static partial Regex incrementedFileNameRegex();
}
