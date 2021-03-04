using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Tools
{
    public class ExportHelpers
    {
        private readonly ILogger<ExportHelpers> logger;

        public ExportHelpers(ILogger<ExportHelpers> logger)
        {
            this.logger = logger;
        }
        /// <summary>
        /// GenericExcelExport
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="memoryStream"></param>
        /// <param name="tempLocation"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">Ignore.</exception>
        /// <exception cref="IOException">Ignore.</exception>
        /// <exception cref="System.Security.SecurityException">Ignore.</exception>
        /// <exception cref="DirectoryNotFoundException">Ignore.</exception>
        /// <exception cref="UnauthorizedAccessException">Ignore.</exception>
        /// <exception cref="PathTooLongException">Ignore.</exception>
        /// <exception cref="ObjectDisposedException">Ignore.</exception>
        public async Task<MemoryStream> GenericExcelExport<T>(List<T> dataList, MemoryStream memoryStream, string tempLocation)
        {
            try
            {
                //string tempNameHash = DateTime.Now.GetHashCode().ToString() + ".xlsx";
                //using (FileStream fileStream = new FileStream(Path.Combine(tempLocation, tempNameHash), FileMode.Create, FileAccess.Write))
                //{
                    XSSFWorkbook wb = new XSSFWorkbook();
                    ISheet ws = wb.CreateSheet("Data");
                    if (dataList != null)
                    {
                        logger.LogInformation("Step 3");
                        if (!NPOIHelpers.ExportFromTable(wb, ws, dataList))
                        {
                            return null;
                        }
                    }
                    
                    //wb.Write(fileStream);
                    MemoryStream tempStream = new MemoryStream();
                    wb.Write(tempStream, true);
                    await tempStream.FlushAsync();
                    tempStream.Position = 0;
                    await tempStream.CopyToAsync(memoryStream);                   
                    await tempStream.DisposeAsync();
                    await memoryStream.FlushAsync();
                    memoryStream.Position = 0;
                //}
                
                return memoryStream;

                //using (FileStream fileStream1 = new FileStream(Path.Combine(tempLocation, tempNameHash), FileMode.Open))
                //{
                //    await fileStream1.CopyToAsync(memoryStream);
                //}
                //memoryStream.Position = 0;

                //if (File.Exists(Path.Combine(tempLocation, tempNameHash)))
                //{
                //    File.Delete(Path.Combine(tempLocation, tempNameHash));
                //}

                //return memoryStream;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "");
            }
            
            return new MemoryStream();
        }
    }
}
