using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Excel
{
    public class ClosedXmlExportHelpers
    {
        private readonly ILogger<ClosedXmlExportHelpers> logger;

        public ClosedXmlExportHelpers(ILogger<ClosedXmlExportHelpers> logger)
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
        public async Task<MemoryStream> GenericExcelExport<T>(List<T> dataList, MemoryStream memoryStream)
        {
            try
            {
                using XLWorkbook wb = new();
                IXLWorksheet ws = wb.AddWorksheet("Data");
                if (dataList != null)
                {
                    if (!ClosedXmlCommonHelpers.ExportFromTable(wb, ws, dataList))
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
    }
}