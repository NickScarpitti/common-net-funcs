using System.Data;
using System.IO.Packaging;
using CommonNetFuncs.Core;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Excel.OpenXml.Common;

namespace CommonNetFuncs.Excel.OpenXml;

/// <summary>
/// Export data to an excel data using NPOI
/// </summary>
public static class Export
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
    public static MemoryStream? GenericExcelExport<T>(this IEnumerable<T> dataList, MemoryStream? memoryStream = null, bool createTable = false,
        string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null)
    {
        try
        {
            memoryStream ??= new();

            using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook, true);
            document.CompressionOption = CompressionOption.Maximum;
            uint newSheetId = document.InitializeExcelFile(sheetName);
            Worksheet? worksheet = document.GetWorksheetById(newSheetId);

            if (worksheet != null && !ExportFromTable(document, worksheet, dataList, createTable, tableName, skipColumnNames))
            {
                return null;
            }

            document.Save();
            document.Dispose();

            return memoryStream;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        return new();
    }

    /// <summary>
    /// Convert a list of data objects into a MemoryStream containing en excel file with a tabular representation of the data
    /// </summary>
    /// <param name="datatable">Data to export as a table</param>
    /// <param name="memoryStream">Output memory stream (will be created if one is not provided)</param>
    /// <param name="createTable">If true, will format the exported data into an Excel table</param>
    /// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
    public static MemoryStream? GenericExcelExport(this DataTable datatable, MemoryStream? memoryStream = null, bool createTable = false,
        string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null)
    {
        try
        {
            memoryStream ??= new();
            using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook, true);
            document.CompressionOption = CompressionOption.Maximum;
            uint newSheetId = document.InitializeExcelFile(sheetName);
            Worksheet? worksheet = document.GetWorksheetById(newSheetId);

            if (worksheet != null && !ExportFromTable(document, worksheet, datatable, createTable, tableName, skipColumnNames))
            {
                return null;
            }

            document.Save();
            document.Dispose();

            return memoryStream;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        return new();
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <typeparam name="T">Type of data inside of list to be exported</typeparam>
    /// <param name="document">Workbook to add table to</param>
    /// <param name="data">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <returns>True if data was successfully added to the workbook</returns>
    public static bool AddGenericTable<T>(this SpreadsheetDocument document, IEnumerable<T> data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null)
    {
        return document.AddGenericTableInternal<T>(data, typeof(IEnumerable<T>), sheetName, createTable, tableName, skipColumnNames);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <param name="document">Workbook to add table to</param>
    /// <param name="data">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <returns>True if data was successfully added to the workbook</returns>
    public static bool AddGenericTable(this SpreadsheetDocument document, DataTable data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null)
    {
        return document.AddGenericTableInternal<char>(data, typeof(DataTable), sheetName, createTable, tableName, skipColumnNames);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="document">Workbook to add sheet table to</param>
    /// <param name="data">Data to populate table with (only accepts IEnumerable </param>
    /// <param name="dataType">Type of the data parameter</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <returns>True if data was successfully added to the workbook</returns>
    private static bool AddGenericTableInternal<T>(this SpreadsheetDocument document, object? data, Type dataType, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null)
    {
        bool success = false;
        try
        {
            int i = 1;
            string actualSheetName = sheetName;
            while (document.GetWorksheetByName(actualSheetName) != null)
            {
                actualSheetName = $"{sheetName} ({i})"; //Get safe new sheet name
                i++;
            }

            Worksheet? worksheet = document.GetWorksheetById(document.CreateNewSheet(actualSheetName));
            if (worksheet != null && data != null)
            {
                if (dataType == typeof(IEnumerable<T>))
                {
                    success = ExportFromTable(document, worksheet, (IEnumerable<T>)data, createTable, tableName, skipColumnNames);
                }
                else if (dataType == typeof(DataTable))
                {
                    success = ExportFromTable(document, worksheet, (DataTable)data, createTable, tableName, skipColumnNames);
                }
                else
                {
                    throw new("Invalid type for data parameter. Parameter must be either an IEnumerable or DataTable class");
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return success;
    }
}
