using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Excel
{
    public class NpoiExportHelpers
    {
        private readonly ILogger<NpoiExportHelpers> logger;

        public NpoiExportHelpers(ILogger<NpoiExportHelpers> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Convert a list of data objects into a MemoryStream containing en excel file with a tabular representation of the data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataList"></param>
        /// <param name="memoryStream"></param>
        /// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
        public async Task<MemoryStream?> GenericExcelExport<T>(List<T> dataList, MemoryStream? memoryStream = null)
        {
            try
            {
                if (memoryStream == null)
                {
                    memoryStream = new();
                }

                XSSFWorkbook wb = new();
                ISheet ws = wb.CreateSheet("Data");
                if (dataList != null)
                {
                    if (!NpoiCommonHelpers.ExportFromTable(wb, ws, dataList))
                    {
                        return null;
                    }
                }

                await memoryStream.WriteFileToMemoryStreamAsync(wb);

                return memoryStream;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GenericExcelExport Error");
            }

            return new MemoryStream();
        }

        public bool AddGenericTable<T>(XSSFWorkbook wb, List<T> dataList, string sheetName)
        {
            bool success = false;
            try
            {
                int i = 1;
                string actualSheetName = sheetName;
                while (wb.GetSheet(actualSheetName) != null) 
                {
                    actualSheetName = sheetName + $" ({i})"; //Get safe new sheet name
                    i++;
                }

                ISheet ws = wb.CreateSheet(actualSheetName);
                if (dataList != null)
                {
                    success = NpoiCommonHelpers.ExportFromTable(wb, ws, dataList);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AddGenericTable Error");
            }
            return success;
        }
    }
}