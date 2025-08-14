using System.Data;
using System.Reflection;
using CommonNetFuncs.Excel.Common;
using NPOI.SS.UserModel;
using NPOI.Util;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Excel.Npoi;

/// <summary>
/// Export data to an excel data using NPOI
/// </summary>
public static class Export
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public const int MaxCellWidthInExcelUnits = 65280;

    /// <summary>
    /// Convert a list of data objects into a MemoryStream containing en excel file with a tabular representation of the
    /// data
    /// </summary>
    /// <typeparam name="T">Type of data inside of list to be exported</typeparam>
    /// <param name="dataList">Data to export as a table.</param>
    /// <param name="memoryStream">Output memory stream (will be created if one is not provided)</param>
    /// <param name="createTable">If <see langword="true"/>, will format the exported data into an Excel table.</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
    public static async Task<MemoryStream?> GenericExcelExport<T>(this IEnumerable<T> dataList, MemoryStream? memoryStream = null, bool createTable = false,
        string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                sheetName = "Data";
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = "Data";
            }

            if (sheetName.Length > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(sheetName), "Sheet name cannot be longer than 31 characters");
            }

            if (tableName.Length > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(tableName), "Table name cannot be longer than 255 characters");
            }

            memoryStream ??= new();

            using SXSSFWorkbook wb = new();
            ISheet ws = wb.CreateSheet(sheetName);
            if (!dataList.ExcelExport(wb, ws, createTable, tableName, skipColumnNames, wrapText, cancellationToken))
            {
                return null;
            }

            await memoryStream.WriteFileToMemoryStreamAsync(wb, cancellationToken).ConfigureAwait(false);
            wb.Close();

            return memoryStream;
        }
        catch (OperationCanceledException)
        {
            throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExport)} was canceled");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{nameof(Export)}.{nameof(GenericExcelExport)} Error");
        }

        return new();
    }

    /// <summary>
    /// Convert a list of data objects into a MemoryStream containing en excel file with a tabular representation of the
    /// data
    /// </summary>
    /// <param name="datatable">Data to export as a table.</param>
    /// <param name="memoryStream">Output memory stream (will be created if one is not provided)</param>
    /// <param name="createTable">If <see langword="true"/>, will format the exported data into an Excel table.</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
    public static async Task<MemoryStream?> GenericExcelExport(this DataTable datatable, MemoryStream? memoryStream = null, bool createTable = false,
        string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                sheetName = "Data";
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = "Data";
            }

            if (sheetName.Length > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(sheetName), "Sheet name cannot be longer than 31 characters");
            }

            if (tableName.Length > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(tableName), "Table name cannot be longer than 255 characters");
            }

            memoryStream ??= new();

            using SXSSFWorkbook wb = new();
            ISheet ws = wb.CreateSheet(sheetName);
            if (!datatable.ExcelExport(wb, ws, createTable, tableName, skipColumnNames, wrapText, cancellationToken))
            {
                return null;
            }

            await memoryStream.WriteFileToMemoryStreamAsync(wb, cancellationToken).ConfigureAwait(false);
            wb.Close();

            return memoryStream;
        }
        catch (OperationCanceledException)
        {
            throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExport)} was canceled");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{nameof(Export)}.{nameof(GenericExcelExport)} Error");
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
    /// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
    public static bool AddGenericTable<T>(this SXSSFWorkbook wb, IEnumerable<T> data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
    {
        return wb.AddGenericTableInternal<T>(data, typeof(IEnumerable<T>), sheetName, createTable, tableName, skipColumnNames, wrapText);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <param name="wb">Workbook to add table to</param>
    /// <param name="data">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
    public static bool AddGenericTable(this SXSSFWorkbook wb, DataTable data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
    {
        return wb.AddGenericTableInternal<char>(data, typeof(DataTable), sheetName, createTable, tableName, skipColumnNames, wrapText);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <param name="wb">Workbook to add table to</param>
    /// <param name="data">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
    public static bool AddGenericTable(this XSSFWorkbook wb, DataTable data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
    {
        using SXSSFWorkbook workbook = new(wb);
        return workbook.AddGenericTable(data, sheetName, createTable, tableName, skipColumnNames, wrapText);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <typeparam name="T">Type of data inside of list to be exported</typeparam>
    /// <param name="wb">Workbook to add table to</param>
    /// <param name="data">Data to insert into workbook</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
    public static bool AddGenericTable<T>(this XSSFWorkbook wb, IEnumerable<T> data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
    {
        using SXSSFWorkbook workbook = new(wb);
        return workbook.AddGenericTable(data, sheetName, createTable, tableName, skipColumnNames, wrapText);
    }

    /// <summary>
    /// Add data to a new sheet in a workbook
    /// </summary>
    /// <typeparam name="T">The type that contains the data for the exported table. Supported types are IEnumerable and DataTable</typeparam>
    /// <param name="wb">Workbook to add sheet table to</param>
    /// <param name="data">Data to populate table with (only accepts IEnumerable and DataTable)</param>
    /// <param name="dataType">Type of the data parameter</param>
    /// <param name="sheetName">Name of sheet to add data into</param>
    /// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
    /// <param name="tableName">Name of the table in Excel</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
    private static bool AddGenericTableInternal<T>(this SXSSFWorkbook wb, object? data, Type dataType, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
    {
        bool success = false;
        try
        {
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                sheetName = "Data";
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = "Data";
            }

            if (sheetName.Length > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(sheetName), "Sheet name cannot be longer than 31 characters");
            }

            if (tableName.Length > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(tableName), "Table name cannot be longer than 255 characters");
            }

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
                    success = ((IEnumerable<T>)data).ExcelExport(wb, ws, createTable, tableName, skipColumnNames, wrapText);
                }
                else if (dataType == typeof(DataTable))
                {
                    success = ((DataTable)data).ExcelExport(wb, ws, createTable, tableName, skipColumnNames, wrapText);
                }
                else
                {
                    throw new("Invalid type for data parameter. Parameter must be either an IEnumerable or DataTable class");
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExport)} was canceled");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{nameof(Export)}.{nameof(AddGenericTableInternal)} Error");
        }
        return success;
    }

    /// <summary>
    /// Generates a simple excel file containing the passed in data in a tabular format
    /// </summary>
    /// <typeparam name="T">Type of data inside of list to be inserted into the workbook</typeparam>
    /// <param name="data">Data to be inserted into the workbook</param>
    /// <param name="wb">Workbook to insert the data into</param>
    /// <param name="ws">Worksheet to insert the data into</param>
    /// <param name="createTable">Turn the output into an Excel table.</param>
    /// <param name="tableName">Name of the table when createTable is true</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns><see langword="true"/> if excel file was created successfully</returns>
    public static bool ExcelExport<T>(this IEnumerable<T> data, SXSSFWorkbook wb, ISheet ws, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false, CancellationToken cancellationToken = default)
    {
        skipColumnNames ??= [];
        try
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = "Data";
            }

            if (tableName.Length > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(tableName), "Table name cannot be longer than 255 characters");
            }

            if (data?.Any() == true)
            {
                ICellStyle headerStyle = wb.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
                ICellStyle bodyStyle = wb.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);
                int x = 0;
                int y = 0;

                Dictionary<int, int> maxColumnWidths = [];
                List<string> columnNames = [];

                PropertyInfo[] props = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => (skipColumnNames.Count == 0) || !skipColumnNames.Contains(x.Name, StringComparer.InvariantCultureIgnoreCase)).ToArray();

                IRow currentRow = ws.GetRow(y) ?? ws.CreateRow(y);
                foreach (PropertyInfo prop in props)
                {
                    //ICell? c = ws.GetCellFromCoordinates(x, y);
                    ICell? c = currentRow.GetCell(x) ?? currentRow.CreateCell(x);
                    if (c != null)
                    {
                        c.SetCellValue(prop.Name);
                        c.CellStyle = headerStyle;
                        columnNames.Add(prop.Name);
                    }
                    maxColumnWidths[x] = (prop.Name.Length + 6) * 256;
                    x++;
                }
                x = 0;
                y++;

                foreach (T item in data)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    currentRow = ws.GetRow(y) ?? ws.CreateRow(y);
                    foreach (PropertyInfo prop in props)
                    {
                        object value = prop.GetValue(item) ?? string.Empty;

                        //ICell? c = ws.GetCellFromCoordinates(x, y);
                        ICell? c = currentRow.GetCell(x) ?? currentRow.CreateCell(x);
                        if (c != null)
                        {
                            string? valueString = value.ToString();
                            c.SetCellValue(valueString);
                            c.CellStyle = bodyStyle;
                            int newVal = (valueString?.Length ?? 1 + 6) * 256;
                            if (maxColumnWidths[x] < newVal)
                            {
                                maxColumnWidths[x] = newVal;
                            }
                        }
                        x++;
                    }
                    x = 0;
                    y++;
                }

                if (!createTable)
                {
                    ws.SetAutoFilter(new(0, 0, 0, props.Length - 1));
                }
                else
                {
                    wb.XssfWorkbook.CreateTable(ws.SheetName, tableName, 0, props.Length - 1, 0, y - 1, columnNames);
                }

                try
                {
                    for (int i = 0; i < props.Length; i++)
                    {
                        // ws.AutoSizeColumn(x, true);
                        ws.SetColumnWidth(x, (maxColumnWidths[x] <= MaxCellWidthInExcelUnits) ? maxColumnWidths[x] : MaxCellWidthInExcelUnits);
                        x++;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{msg}", $"Error using NPOI AutoSizeColumn in {nameof(Export)}.{nameof(ExcelExport)}");
                    logger.Warn("Ensure that either the liberation-fonts-common or mscorefonts2 package (which can be found here: https://mscorefonts2.sourceforge.net/) is installed when using Linux containers");
                }
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExport)} was canceled");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{nameof(Export)}.{nameof(ExcelExport)} Error");
            return false;
        }
    }

    /// <summary>
    /// Generates a simple excel file containing the passed in data in a tabular format
    /// </summary>
    /// <param name="data">Data as DataTable to be inserted into the workbook</param>
    /// <param name="wb">Workbook to insert the data into</param>
    /// <param name="ws">Worksheet to insert the data into</param>
    /// <param name="createTable">Turn the output into an Excel table.</param>
    /// <param name="tableName">Name of the table when createTable is true</param>
    /// <param name="skipColumnNames">List of columns to not include in export</param>
    /// <returns><see langword="true"/> if excel file was created successfully</returns>
    public static bool ExcelExport(this DataTable data, SXSSFWorkbook wb, ISheet ws, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false, CancellationToken cancellationToken = default)
    {
        skipColumnNames ??= [];
        try
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = "Data";
            }

            if (tableName.Length > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(tableName), "Table name cannot be longer than 255 characters");
            }

            if (data?.Rows.Count > 0)
            {
                ICellStyle headerStyle = wb.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
                ICellStyle bodyStyle = wb.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

                int x = 0;
                int y = 0;

                HashSet<int> skipColumns = [];
                Dictionary<int, int> maxColumnWidths = [];
                List<string> columnNames = [];
                IRow currentRow = ws.GetRow(y) ?? ws.CreateRow(y);
                foreach (DataColumn column in data.Columns)
                {
                    if (!skipColumnNames.Contains(column.ColumnName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        //ICell? c = ws.GetCellFromCoordinates(x, y);
                        ICell? c = currentRow.GetCell(x) ?? currentRow.CreateCell(x);
                        if (c != null)
                        {
                            c.SetCellValue(column.ColumnName);
                            c.CellStyle = headerStyle;
                            columnNames.Add(column.ColumnName);
                        }
                        maxColumnWidths.Add(x, (column.ColumnName.Length + 6) * 256);
                    }
                    else
                    {
                        skipColumns.Add(x);
                    }
                    x++;
                }

                x = 0;
                y++;

                foreach (DataRow row in data.Rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentRow = ws.GetRow(y) ?? ws.CreateRow(y);
                    foreach (object? value in row.ItemArray)
                    {
                        if ((value != null) && !skipColumns.Contains(x))
                        {
                            //ICell? c = ws.GetCellFromCoordinates(x, y);
                            ICell? c = currentRow.GetCell(x) ?? currentRow.CreateCell(x);
                            if (c != null)
                            {
                                string? valueString = value.ToString();
                                c.SetCellValue(valueString);
                                c.CellStyle = bodyStyle;
                                int newVal = (valueString?.Length ?? 1 + 6) * 256;
                                if (maxColumnWidths[x] < newVal)
                                {
                                    maxColumnWidths[x] = newVal;
                                }
                            }
                        }
                        x++;
                    }
                    x = 0;
                    y++;
                }

                if (!createTable)
                {
                    ws.SetAutoFilter(new(0, 0, 0, data.Columns.Count - 1));
                }
                else
                {
                    wb.XssfWorkbook.CreateTable(ws.SheetName, tableName, 0, data.Columns.Count - 1, 0, y - 1, columnNames);
                }

                try
                {
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        // ws.AutoSizeColumn(x, true);
                        ws.SetColumnWidth(x, (maxColumnWidths[x] + (Units.EMU_PER_PIXEL * 3) <= MaxCellWidthInExcelUnits) ? (maxColumnWidths[x] + (Units.EMU_PER_PIXEL * 3)) : MaxCellWidthInExcelUnits);
                        x++;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{msg}", $"Error using NPOI AutoSizeColumn in {nameof(Export)}.{nameof(ExcelExport)}");
                    logger.Warn("Ensure that either the liberation-fonts-common or mscorefonts2 package (which can be found here: https://mscorefonts2.sourceforge.net/) is installed when using Linux containers");
                }
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExport)} was canceled");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{nameof(Export)}.{nameof(ExcelExport)} Error");
            return false;
        }
    }
}
