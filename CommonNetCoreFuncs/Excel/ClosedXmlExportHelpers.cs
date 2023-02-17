using System.Data;
using ClosedXML.Excel;
using CommonNetCoreFuncs.Tools;

namespace CommonNetCoreFuncs.Excel;

/// <summary>
/// Export data to an excel data using ClosedXml 
/// </summary>
public class ClosedXmlExportHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Convert a list of data objects into a MemoryStream containing en excel file with a tabular representation of the data
    /// </summary>
    /// <typeparam name="T">Type of data inside of list to be exported</typeparam>
    /// <param name="dataList">Data to export as a table</param>
    /// <param name="memoryStream">Output memory stream (will be created if one is not provided)</param>
    /// <param name="createTable">If true, will format the exported data into an Excel table</param>
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

    /// <summary>
    /// Convert a list of data objects into a MemoryStream containing en excel file with a tabular representation of the data
    /// </summary>
    /// <param name="datatable">Data to export as a table</param>
    /// <param name="memoryStream">Output memory stream (will be created if one is not provided)</param>
    /// <param name="createTable">If true, will format the exported data into an Excel table</param>
    /// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
    public static async Task<MemoryStream?> GenericExcelExport(DataTable datatable, MemoryStream? memoryStream = null, bool createTable = false)
    {
        try
        {
            memoryStream ??= new();

            using XLWorkbook wb = new();
            IXLWorksheet ws = wb.AddWorksheet("Data");
            if (datatable != null)
            {
                if (!ClosedXmlCommonHelpers.ExportFromTable(wb, ws, datatable, createTable))
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

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <typeparam name="T">Type of data inside of list to be exported</typeparam>
    /// <param name="wb">Workbook to add sheet to</param>
    /// <param name="dataList">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    public static bool AddGenericTable<T>(IXLWorkbook wb, List<T> dataList, string sheetName, bool createTable = false)
    {
        bool success = false;
        try
        {
            int i = 1;
            string actualSheetName = sheetName;
            while (wb.Worksheets.Any() && wb.Worksheets.Where(x => x.Name.StrEq(actualSheetName)).Any())
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

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <param name="wb">Workbook to add sheet to</param>
    /// <param name="dataTable">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    /// <returns></returns>
    public static bool AddGenericTable(IXLWorkbook wb, DataTable dataTable, string sheetName, bool createTable = false)
    {
        bool success = false;
        try
        {
            int i = 1;
            string actualSheetName = sheetName;

            while (wb.Worksheets.Any() && wb.Worksheets.Where(x => x.Name.StrEq(actualSheetName)).Any())
            {
                actualSheetName = sheetName + $" ({i})"; //Get safe new sheet name
                i++;
            }

            IXLWorksheet ws = wb.AddWorksheet(actualSheetName);
            if (dataTable != null)
            {
                success = ClosedXmlCommonHelpers.ExportFromTable(wb, ws, dataTable, createTable);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "AddGenericTable Error");
        }
        return success;
    }
}
