using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using NPOI.HSSF.UserModel;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.POIFS.FileSystem;
using NPOI.SS;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using SixLabors.ImageSharp;

namespace CommonNetCoreFuncs.Excel;

/// <summary>
/// Methods to make reading and writing to an excel file easier using NPOI  
/// </summary>
public static class NpoiCommonHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public enum EStyles
    {
        Header,
        HeaderThickTop,
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
    public static bool IsCellEmpty(this ICell cell)
    {
        if (string.IsNullOrWhiteSpace(cell.GetStringValue()))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get ICell offset from cellReference
    /// </summary>
    /// <param name="ws">Worksheet that cell is in</param>
    /// <param name="cellReference">Cell reference in A1 notation</param>
    /// <param name="colOffset">X axis offset from the named cell reference</param>
    /// <param name="rowOffset">Y axis offset from the named cell reference</param>
    /// <returns>ICell object of the specified offset of the named cell</returns>
    public static ICell? GetCellFromReference(this ISheet ws, string cellReference, int colOffset = 0, int rowOffset = 0)
    {
        try
        {
            CellReference cr = new(cellReference);
            IRow? row = ws.GetRow(cr.Row + rowOffset);
            row??= ws.CreateRow(cr.Row + rowOffset);
            ICell cell = row.GetCell(cr.Col + colOffset, MissingCellPolicy.CREATE_NULL_AS_BLANK);
            return cell;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "GetCellFromReference Error");
            return null;
        }
    }

    /// <summary>
    /// Get ICell offset from the startCell
    /// </summary>
    /// <param name="startCell">Cell to get offset from</param>
    /// <param name="colOffset">X axis offset from the named cell reference</param>
    /// <param name="rowOffset">Y axis offset from the named cell reference</param>
    /// <returns>ICell object of the specified offset of the startCell</returns>
    public static ICell? GetCellOffset(this ICell startCell, int colOffset = 0, int rowOffset = 0)
    {
        try
        {
            ISheet ws = startCell.Sheet;
            IRow? row = ws.GetRow(startCell.RowIndex + rowOffset);
            row ??= ws.CreateRow(startCell.RowIndex + rowOffset);
            ICell cell = row.GetCell(startCell.ColumnIndex + colOffset, MissingCellPolicy.CREATE_NULL_AS_BLANK);
            return cell;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "GetCellOffset Error");
            return null;
        }
    }

    /// <summary>
    /// Get ICell offset from the cell indicated with the x and y coordinates
    /// </summary>
    /// <param name="ws">Worksheet that cell is in</param>
    /// <param name="x">X coordinate of starting cell</param>
    /// <param name="y">Y coordinate of starting cell</param>
    /// <param name="colOffset">X axis offset from the cell reference</param>
    /// <param name="rowOffset">Y axis offset from the cell reference</param>
    /// <returns>ICell object of the specified offset of the cell indicated with the x and y coordinates</returns>
    public static ICell? GetCellFromCoordinates(this ISheet ws, int x, int y, int colOffset = 0, int rowOffset = 0)
    {
        try
        {
            IRow row = ws.GetRow(y + rowOffset);
            row ??= ws.CreateRow(y + rowOffset);
            ICell cell = row.GetCell(x + colOffset, MissingCellPolicy.CREATE_NULL_AS_BLANK);
            return cell;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "GetCellFromCoordinates Error");
            return null;
        }
    }

    /// <summary>
    /// Get ICell offset from the cell with named reference cellName
    /// </summary>
    /// <param name="wb">Workbook that cell is in</param>
    /// <param name="cellName">Name of cell being looked for</param>
    /// <param name="colOffset">X axis offset from the named cell reference<</param>
    /// <param name="rowOffset">Y axis offset from the named cell reference<</param>
    /// <returns>ICell object of the specified offset of the cell with named reference cellName</returns>
    public static ICell? GetCellFromName(this XSSFWorkbook wb, string cellName, int colOffset = 0, int rowOffset = 0)
    {
        try
        {
            IName name = wb.GetName(cellName);
            CellReference[] crs;
            try
            {
                crs = new AreaReference(name.RefersToFormula, SpreadsheetVersion.EXCEL2007).GetAllReferencedCells();
            }
            catch (Exception ex)
            {
                logger.Warn($"Unable to locate cell with name {cellName}");
                logger.Warn(ex);
                return null;
            }

            ISheet? ws = null;
            int rowNum = -1;
            int colNum = -1;
            for (int i = 0; i < crs.Length; i++)
            {
                ws ??= wb.GetSheet(crs[i].SheetName);

                if (rowNum == -1 || rowNum > crs[i].Row)
                {
                    rowNum = crs[i].Row;
                }

                if (colNum == -1 || colNum > crs[i].Col)
                {
                    colNum = crs[i].Col;
                }
            }

            if (ws != null && colNum > -1 && rowNum > -1)
            {
                IRow row = ws.GetRow(rowNum + rowOffset);
                row ??= ws.CreateRow(rowNum + rowOffset);
                ICell cell = row.GetCell(colNum + colOffset, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                return cell;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "GetCellFromName Error");
            return null;
        }
    }

    /// <summary>
    /// Clear contents from cell with named reference cellName
    /// </summary>
    /// <param name="wb">Workbook that cell is in</param>
    /// <param name="cellName">Name of cell to clear contents from</param>
    public static void ClearAllFromName(this XSSFWorkbook wb, string cellName)
    {
        try
        {
            IName name = wb.GetName(cellName);
            CellReference[] crs;
            try
            {
                crs = new AreaReference(name.RefersToFormula, SpreadsheetVersion.EXCEL2007).GetAllReferencedCells();
            }
            catch (Exception ex)
            {
                logger.Warn($"Unable to locate cell with name {cellName}");
                logger.Warn(ex);
                return;
            }
            ISheet ws = wb.GetSheet(crs[0].SheetName);

            if (ws == null || crs.Length == 0 || name == null)
            {
                return;
            }

            for (int i = 0; i < crs.Length; i++)
            {
                IRow row = ws.GetRow(crs[i].Row);
                if (row != null)
                {
                    ICell cell = row.GetCell(crs[i].Col);
                    if (cell != null)
                    {
                        row.RemoveCell(cell);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "ClearAllFromName Error");
        }
    }

    /// <summary>
    /// Initializes cell at indicated row and column
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns>ICell object of the cell that was created</returns>
    public static ICell CreateCell(this IRow row, int col)
    {
        return row.CreateCell(col);
    }

    /// <summary>
    /// Writes an excel file to the specified path
    /// </summary>
    /// <param name="wb">XSSFWorkbook object to write to a file</param>
    /// <param name="path">Full file path (including file name) to write wb object to</param>
    /// <returns>True if write was successful</returns>
    public static bool WriteExcelFile(XSSFWorkbook wb, string path)
    {
        try
        {
            using (FileStream fs = new(path, FileMode.Create, FileAccess.Write))
            {
                wb.Write(fs);
            }
            wb.Close();
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "WriteExcelFile Error");
            return false;
        }
    }

    /// <exception cref="Exception">Ignore.</exception>
    /// <summary>
    /// Get cell style based on enum EStyle options
    /// </summary>
    /// <param name="style">Name of preset defined styles to use</param>
    /// <param name="wb">Workbook the style will be used in</param>
    /// <param name="cellLocked">True if the cell should be locked / disabled for user input</param>
    /// <param name="htmlColor">Cell background color to be used (only used for custom font)</param>
    /// <param name="font">NPOI.SS.UserModel.IFont object defining the cell font to be used (only used for custom font)</param>
    /// <param name="alignment">NPOI.SS.UserModel.HorizontalAlignment enum indicating text alignment in the cell (only used for custom font)</param>
    /// <returns>IXLStyle object containing all of the styling associated with the input EStyles option</returns>
    public static ICellStyle GetStyle(EStyles style, XSSFWorkbook wb, bool cellLocked = false, string? htmlColor = null, NPOI.SS.UserModel.IFont? font = null, NPOI.SS.UserModel.HorizontalAlignment? alignment = null)
    {
        ICellStyle cellStyle = wb.CreateCellStyle();
        switch (style)
        {
            case EStyles.Header:
                cellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                cellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
                cellStyle.FillPattern = FillPattern.SolidForeground;
                cellStyle.SetFont(GetFont(EFonts.Header, wb));
                break;

            case EStyles.HeaderThickTop:
                cellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                cellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Medium;
                cellStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
                cellStyle.FillPattern = FillPattern.SolidForeground;
                cellStyle.SetFont(GetFont(EFonts.Header, wb));
                break;

            case EStyles.Body:
                cellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                cellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                cellStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.COLOR_NORMAL;
                cellStyle.SetFont(GetFont(EFonts.Default, wb));
                break;

            case EStyles.Error:
                cellStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
                cellStyle.FillPattern = FillPattern.SolidForeground;
                break;

            case EStyles.Custom:
                XSSFCellStyle xStyle = (XSSFCellStyle)wb.CreateCellStyle();
                if (alignment != null) { xStyle.Alignment = (NPOI.SS.UserModel.HorizontalAlignment)alignment; }

                //Old version relies on System.Drawing
                //byte[] rgb = new byte[] { ColorTranslator.FromHtml(htmlColor).R, ColorTranslator.FromHtml(htmlColor).G, ColorTranslator.FromHtml(htmlColor).B };

                Regex regex = new(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
                if (htmlColor != null && htmlColor.Length == 7 && regex.IsMatch(htmlColor))
                {
                    byte[] rgb = new byte[] { Convert.ToByte(htmlColor.Substring(1, 2), 16), Convert.ToByte(htmlColor.Substring(3, 2), 16), Convert.ToByte(htmlColor.Substring(5, 2), 16) };
                    xStyle.SetFillForegroundColor(new XSSFColor(rgb));
                }

                xStyle.FillPattern = FillPattern.SolidForeground;
                if (font != null) { xStyle.SetFont(font); }
                cellStyle = xStyle;
                break;

            default:
                break;
        }
        cellStyle.IsLocked = cellLocked;
        return cellStyle;
    }

    /// <summary>
    /// Get font styling based on EFonts option
    /// </summary>
    /// <param name="font">Enum for preset fonts</param>
    /// <param name="wb">Workbook the font will be used in</param>
    /// <returns>IXLFont object containing all of the styling associated with the input EFonts option</returns>
    public static NPOI.SS.UserModel.IFont GetFont(EFonts font, XSSFWorkbook wb)
    {
        NPOI.SS.UserModel.IFont cellFont = wb.CreateFont();
        switch (font)
        {
            case EFonts.Default:
                cellFont.IsBold = false;
                cellFont.FontHeightInPoints = 10;
                cellFont.FontName = "Calibri";
                break;

            case EFonts.Header:
                cellFont.IsBold = true;
                cellFont.FontHeightInPoints = 10;
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
    /// <typeparam name="T">Type of data inside of list to be inserted into the workbook</typeparam>
    /// <param name="wb">Workbook to insert the data into</param>
    /// <param name="ws">Worksheet to insert the data into</param>
    /// <param name="data">Data to be inserted into the workbook</param>
    /// <param name="createTable">Turn the output into an Excel table (unused)</param>
    /// <returns>True if excel file was created successfully</returns>
    public static bool ExportFromTable<T>(XSSFWorkbook wb, ISheet ws, IEnumerable<T> data, bool createTable = false)
    {
        try
        {
            if (data != null)
            {
                if (data.Any())
                {
                    ICellStyle headerStyle = GetStyle(EStyles.Header, wb);
                    ICellStyle bodyStyle = GetStyle(EStyles.Body, wb);

                    int x = 0;
                    int y = 0;

                    PropertyInfo[] props = typeof(T).GetProperties();
                    foreach (PropertyInfo prop in props)
                    {
                        ICell? c = ws.GetCellFromCoordinates(x, y);
                        if (c != null)
                        {
                            c.SetCellValue(prop.Name.ToString());
                            c.CellStyle = headerStyle;
                        }
                        x++;
                    }
                    x = 0;
                    y++;

                    foreach (T item in data)
                    {
                        foreach (PropertyInfo prop in props)
                        {
                            var val = prop.GetValue(item) ?? string.Empty;
                            ICell? c = ws.GetCellFromCoordinates(x, y);
                            if (c != null)
                            {
                                c.SetCellValue(val.ToString());
                                c.CellStyle = bodyStyle;
                            }
                            x++;
                        }
                        x = 0;
                        y++;
                    }

                    if (!createTable)
                    {
                        ws.SetAutoFilter(new CellRangeAddress(0, 0, 0, props.Length - 1));
                    }
                    else 
                    { 
                        //Based on code found here: https://stackoverflow.com/questions/65178752/format-a-excel-cell-range-as-a-table-using-npoi
                        XSSFTable table = ((XSSFSheet)ws).CreateTable();
                        CT_Table ctTable = table.GetCTTable();
                        AreaReference dataRange = new(new CellReference(0, 0), new CellReference(y -1 , props.Length - 1));
                        
                        ctTable.@ref = dataRange.FormatAsString();
                        ctTable.id = 1;
                        ctTable.name = "Data";
                        ctTable.displayName = "Data";
                        ctTable.autoFilter = new() { @ref = dataRange.FormatAsString() };
                        //ctTable.totalsRowShown = false;
                        ctTable.tableStyleInfo = new() { name = "TableStyleMedium1", showRowStripes = true };
                        ctTable.tableColumns = new() { tableColumn = new() };
                        
                        T tableHeader = data.First();
                        props = tableHeader!.GetType().GetProperties();


                        uint i = 1;
                        foreach (PropertyInfo prop in props)
                        {
                            ctTable.tableColumns.tableColumn.Add(new CT_TableColumn { id = i, name = prop.Name.ToString() });
                            i++;
                        }
                    }

                    try
                    {
                        foreach (PropertyInfo prop in props)
                        {
                            ws.AutoSizeColumn(x, true); //Requires LIBGDI+ to be installed in run environment to work correctly (not true for version 2.6.0+)
                            x++;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Error using NPOI AutoSizeColumn", ex);
                        logger.Warn("libgdiplus library required to use NPOI AutoSizeColumn method");
                    }
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "ExportFromTable Error");
            return false;
        }
    }

    /// <summary>
    /// Generates a simple excel file containing the passed in data in a tabular format
    /// </summary>
    /// <param name="wb">Workbook to insert the data into</param>
    /// <param name="ws">Worksheet to insert the data into</param>
    /// <param name="data">Data as DataTable to be inserted into the workbook</param>
    /// <param name="createTable">Turn the output into an Excel table (unused)</param>
    /// <returns>True if excel file was created successfully</returns>
    public static bool ExportFromTable(XSSFWorkbook wb, ISheet ws, DataTable data, bool createTable = false)
    {
        try
        {
            if (data != null)
            {
                if (data.Rows.Count > 0)
                {
                    ICellStyle headerStyle = GetStyle(EStyles.Header, wb);
                    ICellStyle bodyStyle = GetStyle(EStyles.Body, wb);

                    int x = 0;
                    int y = 0;

                    foreach (DataColumn column in data.Columns)
                    {
                        ICell? c = ws.GetCellFromCoordinates(x, y);
                        if (c != null)
                        {
                            c.SetCellValue(column.ColumnName);
                            c.CellStyle = headerStyle;
                        }
                        x++;
                    }

                    x = 0;
                    y++;

                    foreach (DataRow row in data.Rows)
                    {
                        foreach (object? value in row.ItemArray)
                        {
                            if (value != null)
                            {
                                ICell? c = ws.GetCellFromCoordinates(x, y);
                                if (c != null)
                                {
                                    c.SetCellValue(value.ToString());
                                    c.CellStyle = bodyStyle;
                                }
                            }
                            x++;
                        }
                        x = 0;
                        y++;
                    }

                    if (!createTable)
                    {
                        ws.SetAutoFilter(new CellRangeAddress(0, 0, 0, data.Columns.Count - 1));
                    }
                    else
                    {
                        //Based on code found here: https://stackoverflow.com/questions/65178752/format-a-excel-cell-range-as-a-table-using-npoi
                        XSSFTable table = ((XSSFSheet)ws).CreateTable();
                        CT_Table ctTable = table.GetCTTable();
                        AreaReference dataRange = new(new CellReference(0, 0), new CellReference(y - 1, data.Rows.Count - 1));

                        ctTable.@ref = dataRange.FormatAsString();
                        ctTable.id = 1;
                        ctTable.name = "Data";
                        ctTable.displayName = "Data";
                        ctTable.autoFilter = new() { @ref = dataRange.FormatAsString() };
                        //ctTable.totalsRowShown = false;
                        ctTable.tableStyleInfo = new() { name = "TableStyleMedium1", showRowStripes = true };
                        ctTable.tableColumns = new() { tableColumn = new() };


                        uint i = 1;
                        foreach (DataColumn column in data.Columns)
                        {
                            ctTable.tableColumns.tableColumn.Add(new CT_TableColumn { id = i, name = column.ColumnName });
                            i++;
                        }
                    }

                    try
                    {
                        foreach (DataColumn row in data.Columns)
                        {
                            ws.AutoSizeColumn(x, true); //Requires LIBGDI+ to be installed in run environment to work correctly (not true for version 2.6.0+)
                            x++;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Error using NPOI AutoSizeColumn", ex);
                        logger.Warn("libgdiplus library required to use NPOI AutoSizeColumn method");
                    }
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "ExportFromTable Error");
            return false;
        }
    }

    /// <summary>
    /// Gets string value contained in cell
    /// </summary>
    /// <param name="cell"></param>
    /// <returns>String representation of the value in cell</returns>
    public static string? GetStringValue(this ICell? cell)
    {
        if (cell == null)
        {
            return null;
        }

        return cell.CellType switch
        {
            CellType.Unknown => string.Empty,
            CellType.Numeric => cell.NumericCellValue.ToString(),
            CellType.String => cell.StringCellValue,
            CellType.Formula => cell.CachedFormulaResultType switch
            {
                CellType.Unknown => string.Empty,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.String => cell.StringCellValue,
                CellType.Blank => string.Empty,
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Error => cell.ErrorCellValue.ToString(),
                _ => string.Empty,
            },
            CellType.Blank => string.Empty,
            CellType.Boolean => cell.BooleanCellValue.ToString(),
            CellType.Error => cell.ErrorCellValue.ToString(),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Writes excel file to a MemoryStream object
    /// </summary>
    /// <param name="memoryStream">MemoryStream object to write XSSFWorkbook object to</param>
    /// <param name="wb">XSSFWorkbook object to write into a MemoryStream</param>
    /// <returns></returns>
    public static async Task WriteFileToMemoryStreamAsync(this MemoryStream memoryStream, XSSFWorkbook wb)
    {
        using MemoryStream tempStream = new();
        wb.Write(tempStream, true);
        await tempStream.FlushAsync();
        tempStream.Seek(0, SeekOrigin.Begin);
        await tempStream.CopyToAsync(memoryStream);
        await tempStream.DisposeAsync();
        await memoryStream.FlushAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    /// Adds images into a workbook at the designated named ranges
    /// </summary>
    /// <param name="wb">Workbook to insert images into</param>
    /// <param name="imageData">List of image byte arrays. Must be equal in length to cellNames parameter</param>
    /// <param name="cellNames">List of named ranges to insert images at. Must be equal in length to imageData parameter</param>
    public static void AddImages(this XSSFWorkbook wb, List<byte[]> imageData, List<string> cellNames)
    {
        if (wb != null && imageData.Count > 0 && cellNames.Count > 0 && imageData.Count == cellNames.Count)
        {
            ISheet? ws = null;
            ICreationHelper helper = wb.GetCreationHelper();
            IDrawing? drawing = null;
            for (int i = 0; i < imageData.Count; i++)
            {
                if (imageData[i].Length > 0 && wb != null && cellNames[i] != null)
                {
                    ICell? cell = wb.GetCellFromName(cellNames[i]);
                    CellRangeAddress? area = GetRangeOfMergedCells(cell);

                    if (cell != null && area != null)
                    {
                        if (ws == null)
                        {
                            ws = cell.Sheet;
                            drawing = ws.CreateDrawingPatriarch();
                        }

                        IClientAnchor anchor = helper.CreateClientAnchor();

                        int imgWidth;
                        int imgHeight;

                        //Using old GDI+ System.Drawing
                        //using (MemoryStream ms = new(imageData[i]))
                        //{
                        //    using Image img = Image.FromStream(ms);
                        //    imgWidth = img?.Width ?? 0;
                        //    imgHeight = img?.Height ?? 0;
                        //}

                        using Image image = Image.Load(imageData[i]);
                        imgWidth = image.Width;
                        imgHeight = image.Height;

                        decimal imgAspect = (decimal)imgWidth / imgHeight;

                        int rangeWidth = ws.GetRangeWidthInPx(area.FirstColumn, area.LastColumn);
                        int rangeHeight = ws.GetRangeHeightInPx(area.FirstRow, area.LastRow);
                        decimal rangeAspect = (decimal)rangeWidth / rangeHeight;

                        decimal scale;

                        if (rangeAspect < imgAspect)
                        {
                            scale = (rangeWidth - 3m) / imgWidth; //Set to width of cell -3px
                        }
                        else
                        {
                            scale = (rangeHeight - 3m) / imgHeight; //Set to width of cell -3px
                        }
                        int resizeWidth = (int)Math.Round(imgWidth * scale, 0, MidpointRounding.ToZero);
                        int resizeHeight = (int)Math.Round(imgHeight * scale, 0, MidpointRounding.ToZero);
                        int xMargin = (int)Math.Round((rangeWidth - resizeWidth) * XSSFShape.EMU_PER_PIXEL / 2.0, 0, MidpointRounding.ToZero);
                        int yMargin = (int)Math.Round((rangeHeight - resizeHeight) * XSSFShape.EMU_PER_PIXEL * 1.75 / 2.0, 0, MidpointRounding.ToZero);

                        anchor.AnchorType = AnchorType.DontMoveAndResize;
                        anchor.Col1 = area.FirstColumn;
                        anchor.Row1 = area.FirstRow;
                        anchor.Col2 = area.LastColumn + 1;
                        anchor.Row2 = area.LastRow + 1;
                        anchor.Dx1 = xMargin;
                        anchor.Dy1 = yMargin;
                        anchor.Dx2 = -xMargin;
                        anchor.Dy2 = -yMargin;

                        int pictureIndex = wb.AddPicture(imageData[i], PictureType.PNG);
                        drawing?.CreatePicture(anchor, pictureIndex);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets CellRangeAddress of merged cells
    /// </summary>
    /// <param name="cell"></param>
    /// <returns>CellRangeAddress of merged cells</returns>
    public static CellRangeAddress? GetRangeOfMergedCells(this ICell? cell)
    {
        if (cell != null && cell.IsMergedCell)
        {
            ISheet sheet = cell.Sheet;
            for (int i = 0; i < sheet.NumMergedRegions; i++)
            {
                CellRangeAddress region = sheet.GetMergedRegion(i);
                if (region.ContainsRow(cell.RowIndex) &&
                    region.ContainsColumn(cell.ColumnIndex))
                {
                    return region;
                }
            }
            return null;
        }
        else if (cell != null)
        {
            return CellRangeAddress.ValueOf($"{cell.Address}:{cell.Address}");
        }
        return null;
    }

    /// <summary>
    /// Get the width of a specified range in pixels
    /// </summary>
    /// <param name="ws"></param>
    /// <param name="startCol"></param>
    /// <param name="endCol"></param>
    /// <returns>Double representation of the width of the column range in pixels</returns>
    public static int GetRangeWidthInPx(this ISheet ws, int startCol, int endCol)
    {
        if (startCol > endCol)
        {
            (endCol, startCol) = (startCol, endCol);
        }

        float totalWidth = 0;
        for (int i = startCol; i < endCol + 1; i++)
        {
            float columnWidth = ws.GetColumnWidthInPixels(i);
            if (columnWidth == 0.0)
            {
                logger.Warn($"Width of Column {i} is 0! Check referenced excel sheet: {ws.SheetName}");
            }
            totalWidth += ws.GetColumnWidthInPixels(i);
        }
        int widthInt = (int)Math.Round(totalWidth, 0, MidpointRounding.ToZero);
        return widthInt;
    }

    /// <summary>
    /// Get the height of a specified range in pixels
    /// </summary>
    /// <param name="ws"></param>
    /// <param name="startRow"></param>
    /// <param name="endRow"></param>
    /// <returns>Double representation of the height of the rows range in pixels</returns>
    public static int GetRangeHeightInPx(this ISheet ws, int startRow, int endRow)
    {
        if (startRow > endRow)
        {
            (endRow, startRow) = (startRow, endRow); //Swap values with tuple assignment
        }

        float totaHeight = 0;
        for (int i = startRow; i < endRow + 1; i++)
        {
            totaHeight += ws.GetRow(i).HeightInPoints;
        }
        int heightInt = (int)Math.Round(totaHeight * XSSFShape.EMU_PER_POINT / XSSFShape.EMU_PER_PIXEL, 0, MidpointRounding.ToZero); //Approximation of point to px
        return heightInt;
    }

    /// <summary>
    /// Get cells contained within a range
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="range">String cell reference in A1 notation</param>
    /// <returns>Array of cells contained within the range specified</returns>
    public static ICell[,] GetRange(ISheet sheet, string range)
    {
        string[] cellStartStop = range.Split(':');

        CellReference cellRefStart = new(cellStartStop[0]);
        CellReference cellRefStop = new(cellStartStop[1]);

        ICell[,] cells = new ICell[cellRefStop.Row - cellRefStart.Row + 1, cellRefStop.Col - cellRefStart.Col + 1];

        for (int i = cellRefStart.Row; i < cellRefStop.Row + 1; i++)
        {
            IRow row = sheet.GetRow(i);
            for (int j = cellRefStart.Col; j < cellRefStop.Col + 1; j++)
            {
                cells[i - cellRefStart.Row, j - cellRefStart.Col] = row.GetCell(j);
            }
        }

        return cells;
    }

    /// <summary>
    /// Adds list validation to all cells specified by cellRangeAddressList
    /// </summary>
    /// <param name="ws">ISheet object to add data validation to</param>
    /// <param name="cellRangeAddressList">Cells to add data validation to</param>
    /// <param name="options">Options to be used as the valid choices in the drop down</param>
    public static void AddDataValidation(this ISheet ws, CellRangeAddressList cellRangeAddressList, List<string> options)
    {
        IDataValidationHelper validationHelper = ws.GetDataValidationHelper();
        IDataValidationConstraint constraint = validationHelper.CreateExplicitListConstraint(options.ToArray());
        IDataValidation dataValidation = validationHelper.CreateValidation(constraint, cellRangeAddressList);

        dataValidation.ShowErrorBox = true;
        dataValidation.ErrorStyle = 0;
        dataValidation.CreateErrorBox("InvalidValue", "Selected value must be in list");
        dataValidation.ShowErrorBox = true;
        dataValidation.ShowPromptBox = false;
        //ws.AddValidationData(dataValidation);

        ws.AddValidationData(dataValidation);
    }

    /// <summary>
    /// Reads tabular data from an unformatted excel sheet to a DataTable object similar to Python Pandas
    /// </summary>
    /// <param name="fileStream">Stream of Excel file being read</param>
    /// <param name="hasHeaders">Does the data being read have headers. Will be used for data table column names instead of default 'Column0', 'Column1'... if true. If no headers specified, first row of data must have a value for all columns in order to read all columns correctly./></param>
    /// <param name="sheetName">Name of sheet to read data from. Will use lowest index sheet if not specified.</param>
    /// <param name="startCellReference">Top left corner containing data to read. Will use A1 if not specified.</param>
    /// <param name="endCellReference">Bottom right cell containing data to read. Will read to first full empty row if not specified.</param>
    /// <returns></returns>
    public static DataTable ReadExcelFileToDataTable(this Stream fileStream, bool hasHeaders = true, string? sheetName = null, string? startCellReference = null, string? endCellReference = null)
    {
        DataTable dataTable = new();

        try
        {
            bool isXlsx = DocumentFactoryHelper.HasOOXMLHeader(fileStream);
            IWorkbook? wb = null;
            if (isXlsx) //Only .xlsx files can have tables
            {
                wb = new XSSFWorkbook(fileStream);
            }
            else
            {
                wb = new HSSFWorkbook(fileStream);
            }

            if (wb != null)
            {
                ISheet? ws = null;

                if (!string.IsNullOrWhiteSpace(sheetName))
                {
                    ws = wb.GetSheet(sheetName);
                }
                else
                {
                    ws = wb.GetSheetAt(0); //Get first sheet if not specified
                }

                if (ws != null)
                {
                    int startColIndex = 0;
                    int? endColIndex = null;
                    int startRowIndex = 0;
                    int? endRowIndex = null;
                    ICell? startCell;
                    ICell? endCell;

                    if (string.IsNullOrWhiteSpace(startCellReference))
                    {
                        startCellReference = "A1";
                    }

                    startCell = ws.GetCellFromReference(startCellReference) ?? ws.GetCellFromReference("A1"); //Default to A1 if invalid cell referenced
                    startColIndex = startCell!.ColumnIndex;
                    startRowIndex = startCell!.RowIndex;

                    if (!string.IsNullOrWhiteSpace(endCellReference))
                    {
                        endCell = ws.GetCellFromReference(endCellReference);
                        if (endCell != null)
                        {
                            endColIndex = endCell.ColumnIndex;
                            endRowIndex = endCell.RowIndex;
                        }
                    }

                    //Add headers to table
                    if (hasHeaders)
                    {
                        if ((endColIndex ?? 0) != 0)
                        {
                            for (int colIndex = startColIndex; colIndex < endColIndex + 1; colIndex++)
                            {
                                dataTable.Columns.Add(GetStringValue(GetCellFromCoordinates(ws, colIndex, startRowIndex)));
                            }
                        }
                        else
                        {
                            string? currentCellVal = startCell.GetStringValue();
                            int colIndex = 1;
                            while (!string.IsNullOrWhiteSpace(currentCellVal))
                            {
                                endColIndex = colIndex - 1;
                                dataTable.Columns.Add(currentCellVal);
                                currentCellVal = startCell.GetCellOffset(colIndex, 0).GetStringValue();
                                colIndex++;
                            }
                        }
                    }
                    else
                    {
                        if ((endColIndex ?? 0) != 0)
                        {
                            for (int colIndex = startColIndex; colIndex < endColIndex + 1; colIndex++)
                            {
                                dataTable.Columns.Add($"Column{colIndex - startColIndex}");
                            }
                        }
                        else
                        {
                            string? currentCellVal = startCell.GetStringValue();
                            int colIndex = 1;
                            while (!string.IsNullOrWhiteSpace(currentCellVal))
                            {
                                endColIndex = colIndex - 1;
                                dataTable.Columns.Add($"Column{colIndex - 1}");
                                currentCellVal = startCell.GetCellOffset(colIndex, 0).GetStringValue();
                                colIndex++;
                            }
                        }
                    }

                    //Add rows to table
                    if (dataTable.Columns.Count > 0)
                    {
                        if (endRowIndex != null)
                        {
                            for (int rowIndex = startRowIndex + (hasHeaders ? 1 : 0); rowIndex < endRowIndex + 1; rowIndex++)
                            {
                                string?[] newRowData = new string?[(int)endColIndex! + 1 - startColIndex];

                                for (int colIndex = startColIndex; colIndex < endColIndex + 1; colIndex++)
                                {
                                    newRowData[colIndex - startColIndex] = GetStringValue(GetCellFromCoordinates(ws, colIndex, rowIndex));
                                }
                                dataTable.Rows.Add(newRowData);
                            }
                        }
                        else
                        {
                            int rowIndex = startRowIndex + (hasHeaders ? 1 : 0);
                            bool rowIsNotNull = true;

                            while (rowIsNotNull)
                            {
                                rowIsNotNull = false;

                                string?[] newRowData = new string?[(int)endColIndex! + 1 - startColIndex];

                                for (int colIndex = startColIndex; colIndex < endColIndex + 1; colIndex++)
                                {
                                    string? cellValue = GetStringValue(GetCellFromCoordinates(ws, colIndex, rowIndex));
                                    rowIsNotNull = rowIsNotNull ? rowIsNotNull : !string.IsNullOrWhiteSpace(cellValue);
                                    newRowData[colIndex - startColIndex] = cellValue;
                                }

                                if (rowIsNotNull)
                                {
                                    dataTable.Rows.Add(newRowData);
                                }
                                rowIndex++;
                            }
                        }
                    }
                }
            }

            wb?.Dispose();
        }
        catch(Exception ex)
        {
            logger.Error("Unable to read excel data.", ex);
        }

        return dataTable;
    }

    /// <summary>
    /// Reads an Excel table into a DataTable object similar to Python Pandas
    /// </summary>
    /// <param name="fileStream">Stream of Excel file being read</param>
    /// <param name="tableName">Name of table to read. If not specified, this function will read the first table it finds in the workbook</param>
    /// <returns>DataTable object containing the data read from Excel stream</returns>
    public static DataTable ReadExcelTableToDataTable(this Stream fileStream, string? tableName = null)
    {
        DataTable dataTable = new();

        try
        {
            bool isXlsx = DocumentFactoryHelper.HasOOXMLHeader(fileStream);
            
            if (isXlsx) //Only .xlsx files can have tables
            {
                using XSSFWorkbook wb = new(fileStream);
                ISheet? ws = null;
                ITable? table = null;
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    table = wb.GetTable(tableName);
                }

                //Get first table name if not specified or not found
                if (string.IsNullOrWhiteSpace(tableName) || table == null)
                {
                    int numberOfSheets = wb.NumberOfSheets;
                    for (int sheetIndex = 0; sheetIndex < numberOfSheets; sheetIndex++)
                    {
                        ws = wb.GetSheetAt(sheetIndex);
                        List<XSSFTable> tables = ((XSSFSheet)ws).GetTables();
                        foreach (XSSFTable t in tables)
                        {
                            tableName = t.Name;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(tableName))
                    {
                        table = wb.GetTable(tableName);
                    }
                }

                if (table != null)
                {
                    ws ??= wb.GetSheet(table.SheetName);

                    //Get headers
                    for (int i = table.StartColIndex; i < table.EndColIndex + 1; i++)
                    {
                        dataTable.Columns.Add(GetStringValue(GetCellFromCoordinates(ws, i, table.StartRowIndex)));
                    }

                    //Get body data
                    for (int i = table.StartRowIndex + 1; i < table.EndRowIndex + 1; i++)
                    {
                        string?[] newRowData = new string?[table.EndColIndex + 1 - table.StartColIndex];

                        for (int n = table.StartColIndex; n < table.EndColIndex + 1; n++)
                        {
                            newRowData[n - table.StartColIndex] = GetStringValue(GetCellFromCoordinates(ws, n, i));
                        }

                        dataTable.Rows.Add(newRowData);
                    }
                }
            }           
        }
        catch (Exception ex)
        {
            logger.Error("Unable to read excel table data.", ex);
        }

        return dataTable;
    }
}
