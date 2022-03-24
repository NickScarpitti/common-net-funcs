using System;
using System.IO;
using SixLabors.ImageSharp;

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
        public static string? ConvertImageFileToBase64(this string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    using MemoryStream ms = new (File.ReadAllBytes(filePath));
                    if (ms.Length > 0)
                    {
                        using Image image = Image.Load(ms);
                        if (image != null && image.Height > 0 && image.Width > 0)
                        {
                            byte[] imageBytes = ms.ToArray();
                            string base64 = Convert.ToBase64String(imageBytes);
                            return base64;
                        }
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
