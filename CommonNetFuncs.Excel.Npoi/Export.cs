﻿using System.Data;
using CommonNetFuncs.Core;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.Excel.Npoi;

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
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
    public static async Task<MemoryStream?> GenericExcelExport<T>(this IEnumerable<T> dataList, MemoryStream? memoryStream = null, bool createTable = false,
        string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null)
    {
        try
        {
            memoryStream ??= new();

            using SXSSFWorkbook wb = new();
            ISheet ws = wb.CreateSheet(sheetName);
            if (!dataList.ExcelExport(wb, ws, createTable, tableName, skipColumnNames))
            {
                return null;
            }

            await memoryStream.WriteFileToMemoryStreamAsync(wb).ConfigureAwait(false);
            wb.Close();

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
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
    public static async Task<MemoryStream?> GenericExcelExport(this DataTable datatable, MemoryStream? memoryStream = null, bool createTable = false,
        string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null)
    {
        try
        {
            memoryStream ??= new();

            using SXSSFWorkbook wb = new();
            ISheet ws = wb.CreateSheet(sheetName);
            if (!datatable.ExcelExport(wb, ws, createTable, tableName, skipColumnNames))
            {
                return null;
            }

            await memoryStream.WriteFileToMemoryStreamAsync(wb).ConfigureAwait(false);
            wb.Close();

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
    /// <param name="wb">Workbook to add table to</param>
    /// <param name="data">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns>True if data was successfully added to the workbook</returns>
    public static bool AddGenericTable<T>(this SXSSFWorkbook wb, IEnumerable<T> data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null)
    {
        return wb.AddGenericTableInternal<T>(data, typeof(IEnumerable<T>), sheetName, createTable, tableName, skipColumnNames);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <param name="wb">Workbook to add table to</param>
    /// <param name="data">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns>True if data was successfully added to the workbook</returns>
    public static bool AddGenericTable(this SXSSFWorkbook wb, DataTable data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null)
    {
        return wb.AddGenericTableInternal<char>(data, typeof(DataTable), sheetName, createTable, tableName, skipColumnNames);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <param name="wb">Workbook to add table to</param>
    /// <param name="data">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns>True if data was successfully added to the workbook</returns>
    public static bool AddGenericTable(this XSSFWorkbook wb, DataTable data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null)
    {
        using SXSSFWorkbook workbook = new(wb);
        return workbook.AddGenericTable(data, sheetName, createTable, tableName, skipColumnNames);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <typeparam name="T">Type of data inside of list to be exported</typeparam>
    /// <param name="wb">Workbook to add table to</param>
    /// <param name="data">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns>True if data was successfully added to the workbook</returns>
    public static bool AddGenericTable<T>(this XSSFWorkbook wb, IEnumerable<T> data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null)
    {
        using SXSSFWorkbook workbook = new(wb);
        return workbook.AddGenericTable(data, sheetName, createTable, tableName, skipColumnNames);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <typeparam name="T">The type that contains the data for the exported table. Supported types are IEnumerable and DataTable</typeparam>
    /// <param name="wb">Workbook to add sheet table to</param>
    /// <param name="data">Data to populate table with (only accepts IEnumerable and DataTable)</param>
    /// <param name="dataType">Type of the data parameter</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If true, will format the inserted data into an Excel table</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns>True if data was successfully added to the workbook</returns>
    private static bool AddGenericTableInternal<T>(this SXSSFWorkbook wb, object? data, Type dataType, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null)
    {
        bool success = false;
        try
        {
            int i = 1;
            string actualSheetName = sheetName;
            while (wb.GetSheet(actualSheetName) != null)
            {
                actualSheetName = $"{sheetName} ({i})"; //Get safe new sheet name
                i++;
            }

            ISheet ws = wb.CreateSheet(actualSheetName);
            if (data != null)
            {
                if (dataType == typeof(IEnumerable<T>))
                {
                    success = ((IEnumerable<T>)data).ExcelExport(wb, ws, createTable, tableName, skipColumnNames);
                }
                else if (dataType == typeof(DataTable))
                {
                    success = ((DataTable)data).ExcelExport(wb, ws, createTable, tableName, skipColumnNames);
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
