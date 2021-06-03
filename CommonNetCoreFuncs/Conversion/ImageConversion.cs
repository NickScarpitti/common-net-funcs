using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Conversion
{
    public static class ImageConversion
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static string ConvertFileToBase64(this string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
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
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
            
            return null;
        }
    }
}
