using SixLabors.ImageSharp;
using static System.Convert;
using static Common_Net_Funcs.Tools.DebugHelpers;

namespace Common_Net_Funcs.Conversion;

/// <summary>
/// Helper methods for converting images
/// </summary>
public static class ImageConversion
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
}
