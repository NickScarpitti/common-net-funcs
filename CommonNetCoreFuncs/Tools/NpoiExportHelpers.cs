using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Tools
{
    public class NpoiExportHelpers
    {
        private readonly ILogger<NpoiExportHelpers> logger;

        public NpoiExportHelpers(ILogger<NpoiExportHelpers> logger)
        {
            this.logger = logger;
        }

        public async Task<MemoryStream> GenericExcelExport<T>(List<T> dataList, MemoryStream memoryStream = null)
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
                logger.LogError(ex, "");
            }

            return new MemoryStream();
        }
    }
}