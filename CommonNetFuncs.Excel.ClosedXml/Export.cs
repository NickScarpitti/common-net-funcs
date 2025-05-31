using System.Data;
using System.Reflection;
using ClosedXML.Excel;
using CommonNetFuncs.Excel.Common;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Excel.ClosedXml.Common;

namespace CommonNetFuncs.Excel.ClosedXml;

///// <summary>
///// Export data to an excel data using ClosedXml
///// </summary>
public static class Export
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Generates a simple excel file containing the passed in data in a tabular format
    /// </summary>
    /// <typeparam name="T">Object to transform into a table</typeparam>
    /// <param name="wb">IXLWorkbook object to place data into</param>
    /// <param name="ws">IXLWorksheet object to place data into</param>
    /// <param name="data">Data to be exported</param>
    /// <param name="createTable">Make the exported data into an Excel table</param>
    /// <returns>True if excel file was created successfully</returns>
    public static bool ExportFromTable<T>(IXLWorkbook wb, IXLWorksheet ws, IEnumerable<T> data, bool createTable = false, CancellationToken cancellationToken = default)
    {
        try
        {
            if (data?.Any() == true)
            {
                IXLStyle? headerStyle = GetStyle(EStyle.Header, wb);
                IXLStyle? bodyStyle = GetStyle(EStyle.Body, wb);

                int x = 1;
                int y = 1;

                PropertyInfo[] props = typeof(T).GetProperties();
                foreach (PropertyInfo prop in props)
                {
                    IXLCell c = ws.Cell(y, x);
                    c.Value = prop.Name;
                    if (!createTable)
                    {
                        c.Style = headerStyle;

                        if (c.Style != null)
                        {
                            c.Style.Fill.BackgroundColor = headerStyle?.Fill.BackgroundColor;
                        }
                    }
                    else
                    {
                        c.Style = bodyStyle; //Use body style since main characteristics will be determined by table style
                    }
                    x++;
                }
                x = 1;
                y++;

                foreach (T item in data)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (PropertyInfo prop in props)
                    {
                        object val = prop.GetValue(item) ?? string.Empty;
                        IXLCell c = ws.Cell(y, x);
                        c.Value = val.ToString();
                        c.Style = bodyStyle;
                        x++;
                    }
                    x = 1;
                    y++;
                }

                if (!createTable)
                {
                    //Not compatible with table
                    ws.Range(1, 1, 1, props.Length - 1).SetAutoFilter();
                }
                else
                {
                    //Based on code found here: https://github.com/ClosedXML/ClosedXML/wiki/Using-Tables
                    IXLTable table = ws.Range(1, 1, y - 1, props.Length).CreateTable();
                    table.ShowTotalsRow = false;
                    table.ShowRowStripes = true;
                    table.Theme = XLTableTheme.TableStyleMedium1;
                    table.ShowAutoFilter = true;
                }

                try
                {
                    for (int i = props.Length - 1; i >= 0; i--)
                    {
                        ws.Column(x).AdjustToContents();
                        x++;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Error using NPOI AutoSizeColumn", ex);
                    logger.Warn("libgdiplus library required to use ClosedXML AutoSizeColumn method");
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
            return false;
        }
    }

    /// <summary>
    /// Generates a simple excel file containing the passed in data in a tabular format
    /// </summary>
    /// <param name="wb">IXLWorkbook object to place data into</param>
    /// <param name="ws">IXLWorksheet object to place data into</param>
    /// <param name="data">Data to be exported</param>
    /// <param name="createTable">Make the exported data into an Excel table</param>
    /// <returns>True if excel file was created successfully</returns>
    public static bool ExportFromTable(IXLWorkbook wb, IXLWorksheet ws, DataTable data, bool createTable = false, CancellationToken cancellationToken = default)
    {
        try
        {
            if (data != null)
            {
                if (data.Rows.Count > 0)
                {
                    IXLStyle? headerStyle = GetStyle(EStyle.Header, wb);
                    IXLStyle? bodyStyle = GetStyle(EStyle.Body, wb);

                    int x = 1;
                    int y = 1;

                    foreach (DataColumn column in data.Columns)
                    {
                        IXLCell? c = ws.Cell(y, x);
                        c.Value = column.ColumnName;
                        if (!createTable)
                        {
                            c.Style = headerStyle;

                            if (c.Style != null)
                            {
                                c.Style.Fill.BackgroundColor = headerStyle?.Fill.BackgroundColor;
                            }
                        }
                        else
                        {
                            c.Style = bodyStyle; //Use body style since main characteristics will be determined by table style
                        }
                        x++;
                    }

                    x = 1;
                    y++;

                    foreach (DataRow row in data.Rows)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        foreach (object? value in row.ItemArray)
                        {
                            string val = value?.ToString() ?? string.Empty;
                            IXLCell c = ws.Cell(y, x);
                            c.Value = val;
                            c.Style = bodyStyle;
                            x++;
                        }
                        x = 1;
                        y++;
                    }

                    if (!createTable)
                    {
                        //Not compatible with table
                        ws.Range(1, 1, 1, data.Columns.Count - 1).SetAutoFilter();
                    }
                    else
                    {
                        //Based on code found here: https://github.com/ClosedXML/ClosedXML/wiki/Using-Tables
                        IXLTable table = ws.Range(1, 1, y - 1, data.Columns.Count).CreateTable();
                        table.ShowTotalsRow = false;
                        table.ShowRowStripes = true;
                        table.Theme = XLTableTheme.TableStyleMedium1;
                        table.ShowAutoFilter = true;
                    }

                    try
                    {
                        for (int i = 0; i < data.Columns.Count; i++)
                        {
                            ws.Column(x).AdjustToContents();
                            x++;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Error using NPOI AutoSizeColumn", ex);
                        logger.Warn("libgdiplus library required to use ClosedXML AutoSizeColumn method");
                    }
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
            return false;
        }
    }
}
