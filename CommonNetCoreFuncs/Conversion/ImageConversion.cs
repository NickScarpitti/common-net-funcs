using System;
using System.Drawing;
using System.IO;

namespace CommonNetCoreFuncs.Conversion
{
    public static class ImageConversion
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Read local image file and convert it to a base 64 string
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Base 64 string representation of image file</returns>
        public static string ConvertImageFileToBase64(this string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    //TODO:: Update to using ImageSharp, SkiaSharp, or Microsoft.Maui.Graphics (https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only)
                    using Image image = Image.FromFile(filePath);
                    if (image != null)
                    {
                        using MemoryStream ms = new();
                        image.Save(ms, image.RawFormat);
                        byte[] imageBytes = ms.ToArray();

                        string base64 = Convert.ToBase64String(imageBytes);
                        return base64;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ConvertImageFileToBase64 Error");
            }
            
            return null;
        }
    }
}
