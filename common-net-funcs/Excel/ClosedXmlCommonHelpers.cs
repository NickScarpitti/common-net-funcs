using System.Data;
using System.Reflection;
using ClosedXML.Excel;
using Common_Net_Funcs.Conversion;

namespace Common_Net_Funcs.Excel;

/// <summary>
/// Methods to make reading and writing to an excel file easier using NPOI  
/// </summary>
public static class ClosedXmlCommonHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public enum EStyles
    {
        Header,
        Body,
        Error,
        Custom
    }

    public enum EFonts
    {
        Default,
        Header,
        BigWhiteHeader
    }

    /// <summary>
    /// Checks if cell is empty
    /// </summary>
    /// <param name="cell"></param>
    /// <returns>True if cell is empty</returns>
    public static bool IsCellEmpty(this IXLCell cell)
    {
        if (string.IsNullOrWhiteSpace(cell.Value.ToString()))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Writes an excel file to the specified path
    /// </summary>
    /// <param name="wb"></param>
    /// <param name="path"></param>
    /// <returns>True if write was successful</returns>
    public static bool WriteExcelFile(IXLWorkbook wb, string path)
    {
        try
        {
            using (FileStream fs = new(path, FileMode.Create, FileAccess.Write))
            {
                wb.SaveAs(fs);
            }
            wb.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            return false;
        }
    }

    /// <summary>
    /// Get cell style based on enum EStyle options
    /// </summary>
    /// <param name="style"></param>
    /// <param name="wb"></param>
    /// <param name="cellLocked"></param>
    /// <param name="htmlColor"></param>
    /// <param name="font"></param>
    /// <param name="alignment"></param>
    /// <returns>IXLStyle object containing all of the styling associated with the input EStyles option</returns>
    public static IXLStyle? GetStyle(EStyles style, IXLWorkbook wb, bool cellLocked = false, string? htmlColor = null, IXLFont? font = null, XLAlignmentHorizontalValues? alignment = null)
    {
        IXLStyle? cellStyle = CreateEmptyStyle();
        if (cellStyle == null) { return null; }
        switch (style)
        {
            case EStyles.Header:
                cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; //Center
                cellStyle.Border.BottomBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.LeftBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.RightBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.TopBorder = XLBorderStyleValues.Thin;
                cellStyle.Fill.BackgroundColor = XLColor.LightGray; //XLColor.FromArgb(140, 140, 140);
                cellStyle.Font = GetFont(EFonts.Header, wb);
                break;

            case EStyles.Body:
                cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellStyle.Border.BottomBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.LeftBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.RightBorder = XLBorderStyleValues.Thin;
                cellStyle.Fill.BackgroundColor = XLColor.NoColor; //NPOI.HSSF.Util.HSSFColor.COLOR_NORMAL;
                cellStyle.Font = GetFont(EFonts.Default, wb);
                break;

            case EStyles.Error:
                cellStyle.Fill.BackgroundColor = XLColor.Red; //NPOI.HSSF.Util.HSSFColor.Red.Index;
                cellStyle.Fill.PatternType = XLFillPatternValues.Solid; //FillPattern.SolidForeground;
                break;

            case EStyles.Custom:
                IXLStyle xStyle = wb.Style;
                if (alignment != null) { xStyle.Alignment.Horizontal = (XLAlignmentHorizontalValues)alignment; }
                xStyle.Fill.BackgroundColor = XLColor.FromHtml(htmlColor);
                xStyle.Fill.PatternType = XLFillPatternValues.Solid;
                if (font != null) { xStyle.Font = font; }
                cellStyle = xStyle;
                break;

            default:
                break;
        }
        cellStyle.Protection.Locked = cellLocked;
        return cellStyle;
    }

    /// <summary>
    /// Creates new instance of a IXLStyle object with reflection to avoid using the same reference to the existing workbook style
    /// </summary>
    /// <returns>Empty IXLStyle object</returns>
    private static IXLStyle? CreateEmptyStyle()
    {
        var t = typeof(ClosedXML.Excel.XLConstants).Assembly.GetType("ClosedXML.Excel.XLStyle");
        MethodInfo? m = t?.GetMethod("CreateEmptyStyle", BindingFlags.Static | BindingFlags.NonPublic);
        var o = m?.Invoke(null, null);
        return o as IXLStyle;
    }

    /// <summary>
    /// Get font styling based on EFonts option
    /// </summary>
    /// <param name="font"></param>
    /// <param name="wb"></param>
    /// <returns>IXLFont object containing all of the styling associated with the input EFonts option</returns>
    public static IXLFont GetFont(EFonts font, IXLWorkbook wb)
    {
        IXLFont cellFont = wb.Style.Font;
        switch (font)
        {
            case EFonts.Default:
                cellFont.Bold = false;
                cellFont.FontSize = 10;
                cellFont.FontName = "Calibri";
                break;

            case EFonts.Header:
                cellFont.Bold = true;
                cellFont.FontSize = 10;
                cellFont.FontName = "Calibri";
                break;

            default:
                break;
        }
        return cellFont;
    }

    /// <summary>
    /// Generates a simple excel file containing the passed in data in a tabular format
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="wb">IXLWorkbook object to place data into</param>
    /// <param name="ws">IXLWorksheet object to place data into</param>
    /// <param name="data">Data to be exported</param>
    /// <param name="createTable">Make the exported data into an Excel table</param>
    /// <returns>True if excel file was created successfully</returns>
    public static bool ExportFromTable<T>(IXLWorkbook wb, IXLWorksheet ws, IEnumerable<T> data, bool createTable = false)
    {
        try
        {
            if (data != null)
            {
                if (data.Any())
                {
                    IXLStyle? headerStyle = GetStyle(EStyles.Header, wb);
                    IXLStyle? bodyStyle = GetStyle(EStyles.Body, wb);

                    int x = 1;
                    int y = 1;

                    PropertyInfo[] props = typeof(T).GetProperties();
                    foreach (PropertyInfo prop in props)
                    {
                        IXLCell c = ws.Cell(y, x);
                        c.Value = prop.Name.ToString();
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
                        foreach (PropertyInfo prop in props)
                        {
                            var val = prop.GetValue(item) ?? string.Empty;
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
                        foreach (PropertyInfo prop in props)
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
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            return false;
        }
    }

    /// <summary>
    /// Generates a simple excel file containing the passed in data in a tabular format
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="wb">IXLWorkbook object to place data into</param>
    /// <param name="ws">IXLWorksheet object to place data into</param>
    /// <param name="data">Data to be exported</param>
    /// <param name="createTable">Make the exported data into an Excel table</param>
    /// <returns>True if excel file was created successfully</returns>
    public static bool ExportFromTable(IXLWorkbook wb, IXLWorksheet ws, DataTable data, bool createTable = false)
    {
        try
        {
            if (data != null)
            {
                if (data.Rows.Count > 0)
                {
                    IXLStyle? headerStyle = GetStyle(EStyles.Header, wb);
                    IXLStyle? bodyStyle = GetStyle(EStyles.Body, wb);

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
                        foreach (object? value in row.ItemArray)
                        {
                            string val = value.ToNString() ?? string.Empty;
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
                        foreach (DataColumn column in data.Columns)
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
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            return false;
        }
    }

    /// <summary>
    /// Writes excel file to a MemoryStream object
    /// </summary>
    /// <param name="memoryStream"></param>
    /// <param name="wb"></param>
    /// <returns></returns>
    public static async Task WriteFileToMemoryStreamAsync(this MemoryStream memoryStream, IXLWorkbook wb)
    {
        using MemoryStream tempStream = new();
        SaveOptions options = new()
        {
            EvaluateFormulasBeforeSaving = true,
            ValidatePackage = true,
            GenerateCalculationChain = true
        };

        wb.SaveAs(tempStream, options);
        await tempStream.FlushAsync();
        tempStream.Seek(0, SeekOrigin.Begin);
        await tempStream.CopyToAsync(memoryStream);
        await tempStream.DisposeAsync();
        await memoryStream.FlushAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);
    }

    //Corrupts excel file as is
    //public static void AddImages(this IXLWorkbook wb, List<byte[]> imageData, List<string> cellNames)
    //{
    //    if (wb != null && imageData.Count > 0 && cellNames.Count > 0 && imageData.Count == cellNames.Count)
    //    {
    //        IXLWorksheet ws = null;
    //        for (int i = 0; i < imageData.Count; i++)
    //        {
    //            if (imageData[i].Length > 0 && wb != null && cellNames[i] != null)
    //            {
    //                IXLCell cell = wb.Cell(cellNames[i]);
    //                IXLRange range = wb.Range(cellNames[i]);
    //                if (cell != null && range != null)
    //                {
    //                    if (ws == null)
    //                    {
    //                        ws = cell.Worksheet;
    //                    }

    //                    int imgWidth;
    //                    int imgHeight;
    //                    IXLPicture picture;
    //                    using (MemoryStream ms = new(imageData[i]))
    //                    {
    //                        using Image img = Image.FromStream(ms);
    //                        imgWidth = img.Width;
    //                        imgHeight = img.Height;
    //                        picture = ws.AddPicture(ms);
    //                    }

    //                    double imgAspect = (double)imgWidth / imgHeight;
    //                    double rangeWidth = ws.GetRangeWidthInPx(range.FirstColumn().ColumnNumber(), range.LastColumn().ColumnNumber());
    //                    double rangeHeight = ws.GetRangeHeightInPx(range.FirstRow().RowNumber(), range.LastRow().RowNumber());
    //                    double rangeAspect = (double)rangeWidth / rangeHeight;

    //                    double scale;

    //                    if (rangeAspect < imgAspect)
    //                    {
    //                        scale = (rangeWidth - 3.0) / imgWidth; //Set to width of cell -3px
    //                    }
    //                    else
    //                    {
    //                        scale = (rangeHeight - 3.0) / imgHeight; //Set to width of cell -3px
    //                    }
    //                    int resizeWidth = (int)Math.Round(imgWidth * scale, 0, MidpointRounding.ToZero);
    //                    int resizeHeight = (int)Math.Round(imgHeight * scale, 0, MidpointRounding.ToZero);
    //                    int xMargin = (int)Math.Round((rangeWidth - resizeWidth) / 2.0, 0, MidpointRounding.ToZero);
    //                    int yMargin = (int)Math.Round((rangeHeight - resizeHeight) / 2.0, 0, MidpointRounding.ToZero);

    //                    picture.Scale(scale);
    //                    picture.MoveTo(ws.Cell(range.FirstRow().RowNumber(), range.FirstColumn().ColumnNumber()), new Point(xMargin, yMargin));
    //                }
    //            }
    //        }
    //    }
    //}

    /// <summary>
    /// Get the width of a specified range in pixels
    /// </summary>
    /// <param name="ws"></param>
    /// <param name="startCol"></param>
    /// <param name="endCol"></param>
    /// <returns>Double representation of the width of the column range in pixels</returns>
    //public static double GetRangeWidthInPx(this IXLWorksheet ws, int startCol, int endCol)
    //{
    //    if (startCol > endCol)
    //    {
    //        int endTemp = startCol;
    //        startCol = endCol;
    //        endCol = endTemp;
    //    }

    //    double totalWidth = 0;
    //    for (int i = startCol; i < endCol + 1; i++)
    //    {
    //        totalWidth += (ws.Column(i).Width - 1) * 7 + 12;
    //    }
    //    return totalWidth;
    //}

    /// <summary>
    /// Get the height of a specified range in pixels
    /// </summary>
    /// <param name="ws"></param>
    /// <param name="startCol"></param>
    /// <param name="endCol"></param>
    /// <returns>Double representation of the height of the rows range in pixels</returns>
    //public static double GetRangeHeightInPx(this IXLWorksheet ws, int startRow, int endRow)
    //{
    //    if (startRow > endRow)
    //    {
    //        int endTemp = startRow;
    //        startRow = endRow;
    //        endRow = endTemp;
    //    }

    //    double totaHeight = 0;
    //    for (int i = startRow; i < endRow + 1; i++)
    //    {
    //        totaHeight += ws.Row(i).Height;
    //    }
    //    return totaHeight;
    //}
}
