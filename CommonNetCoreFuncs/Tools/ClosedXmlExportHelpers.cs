using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Tools
{
    public class ClosedXmlExportHelpers
    {
        private readonly ILogger<ClosedXmlExportHelpers> logger;

        public ClosedXmlExportHelpers(ILogger<ClosedXmlExportHelpers> logger)
        {
            this.logger = logger;
        }

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
                logger.LogError(ex, (ex.InnerException ?? new()).ToString());
            }

            return new MemoryStream();
        }
    }
}