using SixLabors.ImageSharp;
using static System.Convert;

namespace CommonNetFuncs.Images;

/// <summary>
/// Helper methods for converting images
/// </summary>
public static class Base64
{
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
            throw new FileNotFoundException($"The file was not found", filePath);
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

    private const string B64 = "base64";

    /// <summary>
    /// Clean up a base 64 image value by removing any prefix or unwanted characters
    /// </summary>
    /// <param name="imgValue">Base 64 string from web element to clean up</param>
    /// <returns>CLean base 64 image string, or null if invalid</returns>
    public static string? CleanImageValue(this string? imgValue)
    {
        if (!string.IsNullOrWhiteSpace(imgValue))
        {
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
