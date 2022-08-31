using ClosedXML.Excel;

namespace CommonNetCoreFuncs.Excel;

public class ClosedXmlExportHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Convert a list of data objects into a MemoryStream containing en excel file with a tabular representation of the data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dataList"></param>
    /// <param name="memoryStream"></param>
    /// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
    public static async Task<MemoryStream?> GenericExcelExport<T>(List<T> dataList, MemoryStream? memoryStream = null, bool createTable = false)
    {
        try
        {
            memoryStream ??= new();

            using XLWorkbook wb = new();
            IXLWorksheet ws = wb.AddWorksheet("Data");
            if (dataList != null)
            {
                if (!ClosedXmlCommonHelpers.ExportFromTable(wb, ws, dataList, createTable))
                {
                    return null;
                }
            }

            await memoryStream.WriteFileToMemoryStreamAsync(wb);

            return memoryStream;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "GenericExcelExport Error");
        }

        return new MemoryStream();
    }

    public static bool AddGenericTable<T>(IXLWorkbook wb, List<T> dataList, string sheetName, bool createTable = false)
    {
        bool success = false;
        try
        {
            int i = 1;
            string actualSheetName = sheetName;
            while (wb.Worksheet(actualSheetName) != null)
            {
                actualSheetName = sheetName + $" ({i})"; //Get safe new sheet name
                i++;
            }

            IXLWorksheet ws = wb.AddWorksheet(actualSheetName);
            if (dataList != null)
            {
                success = ClosedXmlCommonHelpers.ExportFromTable(wb, ws, dataList, createTable);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "AddGenericTable Error");
        }
        return success;
    }
}
