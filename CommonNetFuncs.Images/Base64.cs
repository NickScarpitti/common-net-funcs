using SixLabors.ImageSharp;
using static System.Convert;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Core.ExceptionLocation;
using CommonNetFuncs.Core;

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
        try
        {
            if (File.Exists(filePath))
            {
                using MemoryStream ms = new(File.ReadAllBytes(filePath));
                if (ms.Length > 0)
                {
                    using Image image = Image.Load(ms);
                    if (image?.Height > 0 && image.Width > 0)
                    {
                        byte[] imageBytes = ms.ToArray();
                        return ToBase64String(imageBytes);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        return null;
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
                    byte[] imageBytes = ms.ToArray();
                    return ToBase64String(imageBytes);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        return null;
    }

    private const string B64 = "base64";
    public static string? CleanImageValue(this string? imgValue)
    {
        if (!string.IsNullOrEmpty(imgValue))
        {
            if (imgValue?.Contains(',') == true)
            {
                imgValue = imgValue.Right(imgValue.Length - imgValue.IndexOf(',') - 1);
            }
            else if (imgValue?.Contains(B64) == true && imgValue.Length > B64.Length)
            {
                imgValue = imgValue.Right(imgValue.Length - imgValue.IndexOf(B64) - B64.Length);
            }
            else
            {
                imgValue = null;
            }
        }
        return imgValue;
    }

    public static bool ImageSaveToFile(this string imageBase64, string savePath)
    {
        try
        {
            byte[] bytes = FromBase64String(imageBase64);

            using Image image = Image.Load(bytes);
            if (image?.Width > 0 && image.Height > 0)
            {
                image.Save(savePath);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\nSave Path: {savePath}");
            return false;
        }
    }
}
