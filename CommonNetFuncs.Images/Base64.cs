using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using static System.Convert;

namespace CommonNetFuncs.Images;

/// <summary>
/// Helper methods for converting images
/// </summary>
public static partial class Base64
{
    //[GeneratedRegex(@"data:image\/([^;]+);base64,([^'"")\s]+)")]
    [GeneratedRegex(@"data:image\/([^;]+);base64,([^'"")\s]+)|base64([^'"")\s]+)")]
    private static partial Regex ExtractBase64Regex();

    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Read local image file and convert it to a base 64 string
    /// </summary>
    /// <param name="filePath">Path to image file to convert into a base 64 string</param>
    /// <returns>Base 64 string representation of image</returns>
    public static string? ConvertImageFileToBase64(this string filePath)
    {
        if (File.Exists(filePath))
        {
            using MemoryStream ms = new(File.ReadAllBytes(filePath));
            return ms.ConvertImageFileToBase64();
        }
        else
        {
            throw new FileNotFoundException("The file was not found", filePath);
        }
    }

    /// <summary>
    /// Convert memory stream of an image to a base 64 string
    /// </summary>
    /// <param name="ms">Memory stream containing image data to convert into a base 64 string</param>
    /// <returns>Base 64 string representation of image</returns>
    public static string? ConvertImageFileToBase64(this MemoryStream ms)
    {
        try
        {
            if (ms.Length > 0)
            {
                using Image image = Image.Load(ms);
                if (image?.Height > 0 && image.Width > 0)
                {
                    ReadOnlySpan<byte> imageBytes = ms.ToArray();
                    return ToBase64String(imageBytes);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{nameof(Base64)}.{nameof(ConvertImageFileToBase64)} Error");
        }

        return null;
    }

    /// <summary>
    /// Clean up a base 64 image value by removing any prefix or unwanted characters
    /// </summary>
    /// <param name="imgValue">Base 64 string from web element to clean up</param>
    /// <returns>CLean base 64 image string, or null if invalid</returns>
    [Obsolete("Please use ExtractBase64 instead.")]
    public static string? CleanImageValue(this string? imgValue)
    {
        if (!string.IsNullOrWhiteSpace(imgValue))
        {
            const string B64 = "base64";
            if (imgValue?.Contains(',') == true)
            {
                int numChars = imgValue.Length - imgValue.IndexOf(',') - 1;
                imgValue = imgValue.Substring(imgValue.Length - numChars, numChars);
            }
            else if (imgValue?.Contains(B64) == true && imgValue.Length > B64.Length)
            {
                int numChars = imgValue.Length - imgValue.IndexOf(B64) - B64.Length;
                imgValue = imgValue.Substring(imgValue.Length - numChars, numChars);
            }
            return imgValue.IsValidBase64Image() ? imgValue : null;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Extract a base 64 image value from a typical CSS background image string.
    /// </summary>
    /// <param name="imgValue">Background image string from web element CSS containing a base 64 image to extract.</param>
    /// <remarks>Will also remove "base64" from the beginning of the imgValue string if the CSS format is not matched</remarks>
    /// <returns>Base 64 image string, or null if invalid base64 image or "none"</returns>
    public static string? ExtractBase64(this string? imgValue)
    {
        string? resultValue = null;
        if (!string.IsNullOrWhiteSpace(imgValue))
        {
            Regex regex = ExtractBase64Regex();
            MatchCollection matches = regex.Matches(imgValue);

            if (matches.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(matches[0].Groups[2].Value))
                {
                    resultValue = matches[0].Groups[2].Value; // Get the base64 part
                }
                else if (!string.IsNullOrWhiteSpace(matches[0].Groups[3].Value))
                {
                    resultValue = matches[0].Groups[3].Value; // Get the base64 part
                }

                if (resultValue?.Length > 0)
                {
                    return resultValue.IsValidBase64Image() ? resultValue : null;
                }
            }

            // If no match, check if imgValue is a valid base64 image already
            if (imgValue.Length > 0 && !string.Equals(imgValue, "none", StringComparison.InvariantCultureIgnoreCase) && !string.Equals(imgValue[..4], "http", StringComparison.InvariantCultureIgnoreCase))
            {
                return imgValue.IsValidBase64Image() ? imgValue : null;
            }
        }
        return resultValue.IsValidBase64Image() ? resultValue : null;
    }

    /// <summary>
    /// Removes the version query parameter from the specified image source URL if present.
    /// </summary>
    /// <remarks>This method identifies the version query parameter by the pattern "?v=" and removes it along with any subsequent characters. The comparison is case-insensitive.</remarks>
    /// <param name="imgSrc">The image source URL to process.</param>
    /// <returns>The image source URL without the version query parameter. If the input does not contain a version query parameter, the original string is returned.</returns>
    [return: NotNullIfNotNull(nameof(imgSrc))]
    public static string? RemoveImageVersionQuery(this string? imgSrc)
    {
        const string ImgVersionStart = "?v=";
        string? result = imgSrc;
        if (!string.IsNullOrWhiteSpace(imgSrc) && imgSrc.Contains(ImgVersionStart, StringComparison.InvariantCultureIgnoreCase) && imgSrc.Length > ImgVersionStart.Length)
        {
            result = imgSrc[..imgSrc.IndexOf(ImgVersionStart, StringComparison.InvariantCultureIgnoreCase)];
        }
        return result;
    }

    /// <summary>
    /// Save a base 64 image string to a file
    /// </summary>
    /// <param name="imageBase64">Base 64 string representation of an image</param>
    /// <param name="savePath">Path (including file name) to save image to</param>
    /// <returns>True if the image was saved successfully, otherwise false</returns>
    public static bool ImageSaveToFile(this string imageBase64, string savePath)
    {
        try
        {
            ReadOnlySpan<byte> bytes = FromBase64String(imageBase64);

            using Image image = Image.Load(bytes);
            if (image?.Width > 0 && image.Height > 0)
            {
                image.Save(savePath);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{nameof(Base64)}.{nameof(ImageSaveToFile)} Error\nSave Path: {savePath}");
            return false;
        }
    }

    /// <summary>
    /// Check if a base 64 string is a valid image
    /// </summary>
    /// <param name="imageBase64">String to confirm if is valid Base64 image</param>
    /// <returns>True if string is a valid base 64 image, otherwise, false</returns>
    public static bool IsValidBase64Image(this string? imageBase64)
    {
        if (string.IsNullOrWhiteSpace(imageBase64))
        {
            return false;
        }

        try
        {
            ReadOnlySpan<byte> bytes = FromBase64String(imageBase64);
            using Image image = Image.Load(bytes);
            return image?.Width > 0 && image.Height > 0;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{nameof(Base64)}.{nameof(IsValidBase64Image)} Error");
            return false;
        }
    }
}
