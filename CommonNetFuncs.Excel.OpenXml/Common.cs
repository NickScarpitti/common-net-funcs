using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using CommonNetFuncs.Core;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SixLabors.ImageSharp;
using A = DocumentFormat.OpenXml.Drawing;
using Color = DocumentFormat.OpenXml.Spreadsheet.Color; //Aliased to prevent issue with DocumentFormat.OpenXml.Spreadsheet.Color
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace CommonNetFuncs.Excel.OpenXml;

public static class ExcelHelper
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public enum EStyles
    {
        Header,
        HeaderThickTop,
        Body,
        Error,
        Blackout,
        Whiteout,
        Custom
    }

    public enum EFonts
    {
        Default,
        Header,
        Whiteout
    }

    //private const int MaxCellWidthInExcelUnits = 65280;

    // OpenXML doesn't have a direct equivalent for ICell, so we'll work with Cell objects
    public static bool IsCellEmpty(this Cell cell)
    {
        return string.IsNullOrWhiteSpace(cell.InnerText);
    }

    public static Cell? GetCellFromReference(this Worksheet ws, string cellReference, int colOffset = 0, int rowOffset = 0)
    {
        try
        {
            CellReference cellRef = new(cellReference);
            Row? row = ws.GetRow(cellRef.RowIndex + (uint)rowOffset);
            if (row == null)
            {
                row = new Row() { RowIndex = (uint)(cellRef.RowIndex + rowOffset) };
                ws.Append(row);
            }
            Cell? cell = row.GetCell(cellRef.ColumnIndex + (uint)colOffset);
            if (cell == null)
            {
                cell = new Cell() { CellReference = new CellReference(cellRef.ColumnIndex + (uint)colOffset, cellRef.RowIndex + (uint)rowOffset).ToString() };
                row.Append(cell);
            }
            return cell;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {ex.GetLocationOfException()}");
            return null;
        }
    }

    public static Cell? GetCellOffset(this Cell startCell, int colOffset = 0, int rowOffset = 0)
    {
        try
        {
            if (startCell.Parent is Row row && row.Parent is SheetData sheetData && startCell.CellReference != null)
            {
                Worksheet? worksheet = startCell.GetWorksheetFromCell();
                if (worksheet != null)
                {
                    CellReference startCellReference = new(startCell.CellReference!);
                    return worksheet.GetCellFromCoordinates((int)startCellReference.ColumnIndex + colOffset, (int)startCellReference.RowIndex + rowOffset);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {ex.GetLocationOfException()}");
        }
        return null;
    }

    public static Cell? GetCellFromCoordinates(this Worksheet ws, int x, int y, int colOffset = 0, int rowOffset = 0)
    {
        try
        {
            Row? row = ws.GetRow((uint)(y + rowOffset));
            if (row == null)
            {
                row = new Row() { RowIndex = (uint)(y + rowOffset) };
                ws.Append(row);
            }
            Cell? cell = row.GetCell((uint)(x + colOffset));
            if (cell == null)
            {
                cell = new Cell() { CellReference = new CellReference((uint)(x + colOffset), (uint)(y + rowOffset)).ToString() };
                row.Append(cell);
            }
            return cell;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {ex.GetLocationOfException()}");
            return null;
        }
    }

    public static Cell? GetCellFromName(this SpreadsheetDocument document, string cellName, int colOffset = 0, int rowOffset = 0)
    {
        try
        {
            WorkbookPart workbookPart = document.WorkbookPart ?? document.AddWorkbookPart();
            DefinedName? definedName = workbookPart.Workbook.DefinedNames?.Elements<DefinedName>().FirstOrDefault(x => x.Name == cellName);
            if (definedName != null)
            {
                string reference = definedName.Text;
                string sheetName = reference.Split('!')[0].Trim('\'');
                string cellReference = reference.Split('!')[1];

                Sheet sheet = workbookPart.Workbook.Descendants<Sheet>().First(s => s.Name == sheetName);
                WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                return worksheetPart.Worksheet.GetCellFromReference(cellReference, colOffset, rowOffset);
            }
            return null;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {ex.GetLocationOfException()}");
            return null;
        }
    }

    public static Worksheet? GetWorksheetFromCell(this Cell cell)
    {
        // Get the parent elements
        if (cell.Parent is not Row row || row.Parent is not SheetData sheetData)
        {
            return null;
        }

        return sheetData.Parent as Worksheet;
    }

    public static Workbook? GetWorkbookFromCell(this Cell cell)
    {
        // Get the parent elements
        if (cell.Parent is not Row row || row.Parent is not SheetData sheetData || sheetData.Parent is not Worksheet worksheet)
        {
            return null;
        }

        return worksheet.Parent as Workbook;
    }

    public static bool SaveExcelFile(SpreadsheetDocument document)
    {
        try
        {
            document.Save();
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {ex.GetLocationOfException()}");
            return false;
        }
    }

    private static ConcurrentDictionary<string, Dictionary<string, uint>> WorkbookStandardFormatCache = [];

    public static void ClearFormatCache()
    {
        WorkbookStandardFormatCache = [];
    }

    public static void ClearCacheForWorkbook(SpreadsheetDocument document)
    {
        if (document?.WorkbookPart != null)
        {
            WorkbookStandardFormatCache.TryRemove(GetWorkbookId(document), out _);
        }
    }

    public static Stylesheet GetStylesheet(this SpreadsheetDocument document)
    {
        WorkbookPart workbookPart = document.WorkbookPart ?? document.AddWorkbookPart();
        WorkbookStylesPart stylesPart = workbookPart.WorkbookStylesPart ?? workbookPart.AddNewPart<WorkbookStylesPart>();
        return stylesPart.Stylesheet ?? new();
    }

    public static uint GetStandardCellStyle(EStyles style, SpreadsheetDocument document, bool cellLocked = false)
    {
        Stylesheet stylesheet = document.GetStylesheet();

        Dictionary<string, uint> formatCache = WorkbookStandardFormatCache.GetOrAdd(GetWorkbookId(document), _ => []);
        string formatKey = $"{style}_{cellLocked}";

        if (formatCache.TryGetValue(formatKey, out uint existingFormatId))
        {
            return existingFormatId;
        }

        Borders borders = stylesheet.Elements<Borders>().First();
        Fills fills = stylesheet.Elements<Fills>().First();
        Fonts fonts = stylesheet.Elements<Fonts>().First();
        CellFormat cellFormat = new();

        Border border;
        Fill fill;
        switch (style)
        {
            case EStyles.Header:
                cellFormat.Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center };

                border = new(new LeftBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }, new RightBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                    new TopBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }, new BottomBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin });
                borders.Append(border);
                cellFormat.BorderId = (uint)borders.Count() - 1;
                cellFormat.ApplyBorder = true;

                fill = new(
                    new PatternFill(
                        new ForegroundColor { Rgb = "D9D9D9" },
                        new BackgroundColor { Indexed = 64 }
                    )
                    { PatternType = PatternValues.Solid }
                );
                fills.Append(fill);
                cellFormat.FillId = (uint)fills.Count() - 1;
                cellFormat.ApplyFill = true;

                cellFormat.FontId = GetFontId(EFonts.Header, fonts);
                cellFormat.ApplyFont = true;
                break;

            case EStyles.HeaderThickTop:
                cellFormat.Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center };

                border = new(new LeftBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }, new RightBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                    new TopBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Medium }, new BottomBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin });
                borders.Append(border);
                cellFormat.BorderId = (uint)borders.Count() - 1;
                cellFormat.ApplyBorder = true;

                fill = new(
                    new PatternFill(
                        new ForegroundColor { Rgb = "D9D9D9" },
                        new BackgroundColor { Indexed = 64 }
                    )
                    { PatternType = PatternValues.Solid }
                );
                fills.Append(fill);
                cellFormat.FillId = (uint)fills.Count() - 1;
                cellFormat.ApplyFill = true;

                cellFormat.FontId = GetFontId(EFonts.Header, fonts);
                cellFormat.ApplyFont = true;
                break;

            case EStyles.Body:
                cellFormat.Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center };

                border = new(new LeftBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }, new RightBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                    new BottomBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin });
                borders.Append(border);
                cellFormat.BorderId = (uint)borders.Count() - 1;
                cellFormat.ApplyBorder = true;

                cellFormat.FontId = GetFontId(EFonts.Default, fonts);
                cellFormat.ApplyFont = true;
                break;

            case EStyles.Error:
                fill = new(
                    new PatternFill(
                        new ForegroundColor { Rgb = "FF0000" }, // Red
                        new BackgroundColor { Indexed = 64 }
                    )
                    { PatternType = PatternValues.Solid }
                );
                fills.Append(fill);
                cellFormat.FillId = (uint)fills.Count() - 1;
                cellFormat.ApplyFill = true;
                break;

            case EStyles.Blackout:
                fill = new(
                    new PatternFill(
                        new ForegroundColor { Rgb = "000000" }, // Black
                        new BackgroundColor { Indexed = 64 }
                    )
                    { PatternType = PatternValues.Solid }
                );
                fills.Append(fill);
                cellFormat.FillId = (uint)fills.Count() - 1;
                cellFormat.ApplyFill = true;

                cellFormat.FontId = GetFontId(EFonts.Default, fonts); //Default font is black
                cellFormat.ApplyFont = true;
                break;

            case EStyles.Whiteout:
                fill = new(
                    new PatternFill(
                        new ForegroundColor { Rgb = "FFFFFF" }, // White
                        new BackgroundColor { Indexed = 64 }
                    )
                    { PatternType = PatternValues.Solid }
                );
                fills.Append(fill);
                cellFormat.FillId = (uint)fills.Count() - 1;
                cellFormat.ApplyFill = true;

                cellFormat.FontId = GetFontId(EFonts.Whiteout, fonts); // White font
                break;
        }

        if (cellLocked)
        {
            cellFormat.Protection = new Protection { Locked = true };
            cellFormat.ApplyProtection = true;
        }

        // Check if an identical CellFormat already exists
        CellFormats cellFormats = stylesheet.Elements<CellFormats>().First();
        for (uint i = 0; i < (cellFormats.Count ?? 0); i++)
        {
            if (CellFormatsAreEqual(cellFormat, cellFormats.Elements<CellFormat>().ElementAt((int)i)))
            {
                formatCache[formatKey] = i;
                return i;
            }
        }

        // If no matching format found, add the new one
        cellFormats.Append(cellFormat);
        uint newFormatId = (cellFormats.Count ?? 0) - 1;
        formatCache[formatKey] = newFormatId;
        return newFormatId;
    }

    /// <summary>
    /// Get font styling based on EFonts option
    /// </summary>
    /// <param name="fontType">Enum for preset fonts</param>
    /// <param name="fonts">Workbook the font will be used in</param>
    /// <returns>IXLFont object containing all of the styling associated with the input EFonts option</returns>
    public static uint GetFontId(EFonts fontType, Fonts fonts)
    {
        Font font = new();
        switch (fontType)
        {
            case EFonts.Default:
                font.FontSize = new() { Val = 10 };
                font.Color = new() { Rgb = new HexBinaryValue() { Value = "000000" } };
                font.FontName = new() { Val = "Calibri" };
                break;

            case EFonts.Header:
                font.FontSize = new() { Val = 10 };
                font.Color = new() { Rgb = new HexBinaryValue() { Value = "000000" } };
                font.FontName = new() { Val = "Calibri" };
                font.Bold = new Bold();
                break;

            case EFonts.Whiteout:
                font.FontSize = new() { Val = 10 };
                font.Color = new() { Rgb = new HexBinaryValue() { Value = "FFFFFF" } };
                font.FontName = new() { Val = "Calibri" };
                break;
        }
        fonts.Append(font);
        return (uint)fonts.Count() - 1;
    }

    private static bool CellFormatsAreEqual(CellFormat format1, CellFormat format2)
    {
        // Compare relevant properties of the CellFormat objects
        return format1.BorderId == format2.BorderId &&
            format1.FillId == format2.FillId &&
            format1.FontId == format2.FontId &&
            format1.ApplyBorder == format2.ApplyBorder &&
            format1.ApplyFill == format2.ApplyFill &&
            format1.ApplyFont == format2.ApplyFont &&
            FormatAlignmentsAreEqual(format1.Alignment, format2.Alignment) &&
            FormatProtectionsAreEqual(format1.Protection, format2.Protection);
    }

    private static bool FormatAlignmentsAreEqual(Alignment? alignment1, Alignment? alignment2)
    {
        if (alignment1 == null && alignment2 == null) return true;
        if (alignment1 == null || alignment2 == null) return false;
        return alignment1.Horizontal == alignment2.Horizontal;
    }

    private static bool FormatProtectionsAreEqual(Protection? protection1, Protection? protection2)
    {
        if (protection1 == null && protection2 == null) return true;
        if (protection1 == null || protection2 == null) return false;
        return protection1.Locked == protection2.Locked;
    }

    private static readonly ConcurrentDictionary<string, WorkbookStyleCache> WorkbookCaches = new();

    private class WorkbookStyleCache
    {
        public Dictionary<int, uint> FontCache { get; } = [];
        public Dictionary<int, uint> FillCache { get; } = [];
        public Dictionary<int, uint> BorderCache { get; } = [];
        public Dictionary<string, uint> CellFormatCache { get; } = [];
    }

    public static uint? GetCustomStyle(SpreadsheetDocument document, bool cellLocked = false, Font? font = null,
        HorizontalAlignmentValues? alignment = null, Fill? fill = null, Border? border = null)
    {
        Stylesheet stylesheet = document.GetStylesheet();

        if (stylesheet == null) return null;

        string workbookId = GetWorkbookId(document);
        WorkbookStyleCache cache = WorkbookCaches.GetOrAdd(workbookId, _ => new WorkbookStyleCache());

        uint fontId = GetOrAddFont(stylesheet, cache, font);
        uint fillId = GetOrAddFill(stylesheet, cache, fill);
        uint borderId = GetOrAddBorder(stylesheet, cache, border);

        // Create a unique key for the cell format
        string cellFormatKey = $"{fontId}|{fillId}|{borderId}|{alignment}|{cellLocked}";

        if (cache.CellFormatCache.TryGetValue(cellFormatKey, out uint existingFormatId))
        {
            return existingFormatId;
        }

        CellFormat cellFormat = new()
        {
            FontId = fontId,
            FillId = fillId,
            BorderId = borderId,
            ApplyFont = font != null,
            ApplyFill = fill != null,
            ApplyBorder = border != null
        };

        if (alignment.HasValue)
        {
            cellFormat.Alignment = new() { Horizontal = alignment };
            cellFormat.ApplyAlignment = true;
        }

        if (cellLocked)
        {
            cellFormat.Protection = new() { Locked = true };
            cellFormat.ApplyProtection = true;
        }

        CellFormats cellFormats = stylesheet.Elements<CellFormats>().First();
        cellFormats.Append(cellFormat);
        uint newFormatId = (cellFormats.Count ?? 1) - 1;
        cache.CellFormatCache[cellFormatKey] = newFormatId;

        return newFormatId;
    }

    private static uint GetOrAddFont(Stylesheet stylesheet, WorkbookStyleCache cache, Font? font)
    {
        if (font == null) return 0; // Default font

        int fontHash = GetHashCode(font);
        if (cache.FontCache.TryGetValue(fontHash, out uint existingFontId))
        {
            return existingFontId;
        }

        Fonts fonts = stylesheet.Elements<Fonts>().First();
        fonts.Append(font);
        uint newFontId = (fonts.Count ?? 1) - 1;
        cache.FontCache[fontHash] = newFontId;
        return newFontId;
    }

    private static uint GetOrAddFill(Stylesheet stylesheet, WorkbookStyleCache cache, Fill? fill)
    {
        if (fill == null) return 0; // Default fill

        int fillHash = GetHashCode(fill);
        if (cache.FillCache.TryGetValue(fillHash, out uint existingFillId))
        {
            return existingFillId;
        }

        Fills fills = stylesheet.Elements<Fills>().First();
        fills.Append(fill);
        uint newFillId = (fills.Count ?? 1) - 1;
        cache.FillCache[fillHash] = newFillId;
        return newFillId;
    }

    private static uint GetOrAddBorder(Stylesheet stylesheet, WorkbookStyleCache cache, Border? border)
    {
        if (border == null) return 0; // Default border

        int borderHash = GetHashCode(border);
        if (cache.BorderCache.TryGetValue(borderHash, out uint existingBorderId))
        {
            return existingBorderId;
        }

        Borders borders = stylesheet.Elements<Borders>().First();
        borders.Append(border);
        uint newBorderId = (borders.Count ?? 1) - 1;
        cache.BorderCache[borderHash] = newBorderId;
        return newBorderId;
    }

    private static int GetHashCode(OpenXmlElement element)
    {
        return element.OuterXml.GetHashCode();
    }

    private static string GetWorkbookId(SpreadsheetDocument document)
    {
        // Use a combination of the file path (if available) and creation time
        string filePath = document.PackageProperties.Identifier ?? "";
        string creationTime = document.PackageProperties.Created?.ToString() ?? "";
        return $"{filePath}_{creationTime}";
    }

    // Call this method when you're done with a workbook to free up memory
    public static void ClearCache(SpreadsheetDocument document)
    {
        string workbookId = GetWorkbookId(document);
        WorkbookCaches.TryRemove(workbookId, out _);
    }

    public static bool ExportFromTable<T>(SpreadsheetDocument document, Worksheet worksheet, IEnumerable<T> data, bool createTable = false, string tableName = "Data")
    {
        try
        {
            if (data?.Any() == true)
            {
                uint headerStyleId = GetStandardCellStyle(EStyles.Header, document);
                uint bodyStyleId = GetStandardCellStyle(EStyles.Body, document);

                uint rowIndex = 1;
                uint columnIndex = 1;

                PropertyInfo[] properties = typeof(T).GetProperties();

                // Write headers
                foreach (PropertyInfo prop in properties)
                {
                    Cell cell = InsertCellInWorksheet(worksheet, columnIndex, rowIndex);
                    cell.CellValue = new CellValue(prop.Name);
                    cell.StyleIndex = headerStyleId;
                    columnIndex++;
                }

                rowIndex++;

                // Write data
                foreach (T item in data)
                {
                    columnIndex = 1;
                    foreach (PropertyInfo prop in properties)
                    {
                        Cell cell = InsertCellInWorksheet(worksheet, columnIndex, rowIndex);
                        cell.CellValue = new CellValue(prop.GetValue(item)?.ToString() ?? string.Empty);
                        cell.StyleIndex = bodyStyleId;
                        columnIndex++;
                    }
                    rowIndex++;
                }

                if (createTable)
                {
                    CreateTable(worksheet, 1, 1, rowIndex - 1, (uint)properties.Length, tableName);
                }
                else
                {
                    SetAutoFilter(worksheet, 1, 1, rowIndex - 1, (uint)properties.Length);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {ex.GetLocationOfException()}");
            return false;
        }
    }

    public static bool ExportFromTable(SpreadsheetDocument document, Worksheet worksheet, DataTable data, bool createTable = false, string tableName = "Data")
    {
        try
        {
            if (data?.Rows.Count > 0)
            {
                uint headerStyleId = GetStandardCellStyle(EStyles.Header, document);
                uint bodyStyleId = GetStandardCellStyle(EStyles.Body, document);

                uint y = 0;
                uint x = 0;

                Dictionary<uint, int> maxColumnWidths = [];

                foreach (DataColumn column in data.Columns)
                {
                    Cell cell = InsertCellInWorksheet(worksheet, x, y);
                    cell.CellValue = new CellValue(column.ColumnName);
                    cell.StyleIndex = headerStyleId;
                    maxColumnWidths.Add(x, (column.ColumnName.Length + 5) * 256);
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
                            Cell cell = InsertCellInWorksheet(worksheet, x, y);
                            cell.CellValue = new(value.ToString() ?? string.Empty);
                            cell.StyleIndex = bodyStyleId;
                            x++;
                            int newVal = (value.ToString()?.Length ?? 1) * 256;
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

                if (createTable)
                {
                    CreateTable(worksheet, 1, 1, y - 1, (uint)data.Columns.Count, tableName);
                }
                else
                {
                    SetAutoFilter(worksheet, 1, 1, y - 1, (uint)data.Columns.Count);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {ex.GetLocationOfException()}");
            return false;
        }
    }

    private static Cell InsertCellInWorksheet(Worksheet worksheet, uint columnIndex, uint rowIndex)
    {
        SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
        CellReference cellReference = new(columnIndex, rowIndex);

        // Check if the row exists, create if not
        Row? row = sheetData?.Elements<Row>().FirstOrDefault(x => x.RowIndex != null && x.RowIndex == rowIndex);
        if (row == null)
        {
            row = new Row { RowIndex = rowIndex };
            sheetData!.Append(row);
        }

        // Check if the cell exists, create if not
        Cell? cell = row.Elements<Cell>().FirstOrDefault(c => c.CellReference?.Value == cellReference.ToString());
        if (cell == null)
        {
            cell = new Cell { CellReference = cellReference.ToString() };
            row.Append(cell);
        }
        return cell;
    }

    private static void CreateTable(Worksheet worksheet, uint startRow, uint startColumn, uint endRow, uint endColumn, string tableName)
    {
        TableDefinitionPart tableDefinitionPart = worksheet.WorksheetPart!.AddNewPart<TableDefinitionPart>();
        Table table = new()
        {
            Id = 1,
            Name = tableName,
            DisplayName = tableName,
            Reference = new CellReference(startColumn, startRow) + ":" + new CellReference(endColumn, endRow)
        };

        AutoFilter autoFilter = new() { Reference = table.Reference };

        TableColumns tableColumns = new() { Count = endColumn - startColumn + 1 };
        for (uint i = 0; i < endColumn - startColumn + 1; i++)
        {
            tableColumns.Append(new TableColumn { Id = (uint)(i + 1), Name = $"Column{i + 1}" });
        }

        TableStyleInfo tableStyleInfo = new()
        {
            Name = "TableStyleMedium2",
            ShowFirstColumn = false,
            ShowLastColumn = false,
            ShowRowStripes = true,
            ShowColumnStripes = false
        };

        table.Append(autoFilter);
        table.Append(tableColumns);
        table.Append(tableStyleInfo);

        tableDefinitionPart.Table = table;

        TablePart tablePart = new() { Id = worksheet.WorksheetPart.GetIdOfPart(tableDefinitionPart) };

        // Check if TableParts element exists, if not create it
        TableParts? tableParts = worksheet.Elements<TableParts>().FirstOrDefault();
        if (tableParts == null)
        {
            tableParts = new();
            worksheet.Append(tableParts);
        }

        tableParts.Append(tablePart);
        tableParts.Count = (uint)tableParts.ChildElements.Count;
    }

    private static void SetAutoFilter(Worksheet worksheet, uint startRow, uint startColumn, uint endRow, uint endColumn)
    {
        worksheet.Append(new AutoFilter() { Reference = new CellReference(startColumn, startRow) + ":" + new CellReference(endColumn, endRow) });
    }

    public static string? GetStringValue(this Cell cell)
    {
        if (cell == null) return null;

        if (cell.DataType != null)
        {
            string cellDataType = cell.DataType.Value.ToString();
            if (cellDataType == CellValues.SharedString.ToString())
            {
                Worksheet? worksheet = cell.GetWorksheetFromCell();
                SharedStringTablePart? stringTable = worksheet?.WorksheetPart?.GetParentParts().OfType<SharedStringTablePart>().FirstOrDefault();
                if (stringTable != null)
                {
                    return stringTable.SharedStringTable.ElementAt(int.Parse(cell.InnerText)).InnerText;
                }
            }
            else if (cellDataType == CellValues.Boolean.ToString())
            {
                return cell.InnerText == "1" ? "TRUE" : "FALSE";
            }
            else if (cellDataType == CellValues.Error.ToString())
            {
                return "ERROR: " + cell.InnerText;
            }
            else // (cellDataType == CellValues.Number.ToString() || cellDataType == CellValues.String.ToString() || cellDataType == CellValues.InlineString.ToString())
            {
                return cell.InnerText;
            }
        }

        return cell.InnerText;
    }

    /// <summary>
    /// Adds images into a workbook at the designated named ranges
    /// </summary>
    /// <param name="workbookPart">WorkbookPart to insert images into</param>
    /// <param name="imageData">List of image byte arrays. Must be equal in length to cellNames parameter</param>
    /// <param name="cellNames">List of named ranges to insert images at. Must be equal in length to imageData parameter</param>
    public static void AddImages(this WorkbookPart workbookPart, List<byte[]> imageData, List<string> cellNames)
    {
        if (workbookPart != null && imageData.Count > 0 && cellNames.Count > 0 && imageData.Count == cellNames.Count)
        {
            WorksheetPart? worksheetPart = null;
            DrawingsPart? drawingsPart = null;

            for (int i = 0; i < imageData.Count; i++)
            {
                if (imageData[i].Length > 0 && cellNames[i] != null)
                {
                    CellReference? cellReference = GetCellFromName(workbookPart, cellNames[i]);
                    if (cellReference != null)
                    {
                        if (worksheetPart == null)
                        {
                            worksheetPart = GetWorksheetPartByCellReference(workbookPart, cellReference);
                            if (worksheetPart != null)
                            {
                                drawingsPart = GetOrCreateDrawingsPart(worksheetPart);
                            }
                        }

                        if (worksheetPart != null && drawingsPart != null)
                        {
                            (CellReference, CellReference) mergedCellArea = GetMergedCellArea(worksheetPart, cellReference);

                            using Image image = Image.Load(imageData[i]);
                            int imgWidth = image.Width;
                            int imgHeight = image.Height;

                            decimal imgAspect = (decimal)imgWidth / imgHeight;

                            int rangeWidth = GetRangeWidthInEmu(worksheetPart, mergedCellArea);
                            int rangeHeight = GetRangeHeightInEmu(worksheetPart, mergedCellArea);
                            decimal rangeAspect = (decimal)rangeWidth / rangeHeight;

                            decimal scale = rangeAspect < imgAspect
                                ? (rangeWidth - 3 * 9525m) / (imgWidth * 9525m)  // 1px = 9525 EMUs
                                : (rangeHeight - 3 * 9525m) / (imgHeight * 9525m);

                            int resizeWidth = (int)Math.Round(imgWidth * scale * 9525m, 0, MidpointRounding.ToZero);
                            int resizeHeight = (int)Math.Round(imgHeight * scale * 9525m, 0, MidpointRounding.ToZero);
                            int xMargin = (int)Math.Round((rangeWidth - resizeWidth) / 2.0m, 0, MidpointRounding.ToZero);
                            int yMargin = (int)Math.Round((rangeHeight - resizeHeight) * 1.75m / 2.0m, 0, MidpointRounding.ToZero);

                            ImagePart imagePart = drawingsPart.AddImagePart(ImagePartType.Png);
                            using (MemoryStream stream = new(imageData[i]))
                            {
                                imagePart.FeedData(stream);
                            }

                            AddImageToWorksheet(drawingsPart, imagePart.GetIdOfPart(imagePart),
                                mergedCellArea.Item1, mergedCellArea.Item2,
                                xMargin, yMargin, resizeWidth, resizeHeight);
                        }
                    }
                }
            }
        }
    }

    private static CellReference? GetCellFromName(WorkbookPart workbookPart, string name)
    {
        DefinedName? definedName = workbookPart.Workbook.DefinedNames?.Elements<DefinedName>().FirstOrDefault(dn => dn.Name == name);

        if (definedName != null)
        {
            string reference = definedName.Text;
            // Assuming the reference is in the format "SheetName!A1"
            string[] parts = reference.Split('!');
            if (parts.Length == 2)
            {
                return new CellReference(parts[1]);
            }
        }

        return null;
    }

    private static WorksheetPart? GetWorksheetPartByCellReference(WorkbookPart workbookPart, CellReference cellReference)
    {
        return workbookPart.WorksheetParts.FirstOrDefault(wp => wp.Worksheet.Descendants<Cell>().Any(c => c.CellReference == cellReference.ToString()));
    }

    private static DrawingsPart? GetOrCreateDrawingsPart(WorksheetPart worksheetPart)
    {
        if (worksheetPart.DrawingsPart == null)
        {
            DrawingsPart drawingsPart = worksheetPart.AddNewPart<DrawingsPart>();
            drawingsPart.WorksheetDrawing = new Xdr.WorksheetDrawing();

            Drawing drawing = new() { Id = worksheetPart.GetIdOfPart(drawingsPart) };
            worksheetPart.Worksheet.Append(drawing);
        }

        return worksheetPart.DrawingsPart;
    }

    private static (CellReference, CellReference) GetMergedCellArea(WorksheetPart worksheetPart, CellReference cellReference)
    {
        MergeCells? mergedCells = worksheetPart.Worksheet.Elements<MergeCells>().FirstOrDefault();
        if (mergedCells != null)
        {
            foreach (MergeCell mergedCell in mergedCells.Elements<MergeCell>())
            {
                if (mergedCell.Reference?.Value != null)
                {
                    string[] range = mergedCell.Reference.Value.Split(':');
                    CellReference start = new(range[0]);
                    CellReference end = new(range[1]);
                    if (cellReference.RowIndex >= start.RowIndex && cellReference.RowIndex <= end.RowIndex &&
                        cellReference.ColumnIndex >= start.ColumnIndex && cellReference.ColumnIndex <= end.ColumnIndex)
                    {
                        return (start, end);
                    }
                }
            }
        }

        return (cellReference, cellReference);
    }

    private static int GetRangeWidthInEmu(WorksheetPart worksheetPart, (CellReference start, CellReference end) range)
    {
        Worksheet worksheet = worksheetPart.Worksheet;
        Columns? cols = worksheet.Elements<Columns>().FirstOrDefault();

        int totalWidth = 0;
        for (uint col = range.start.ColumnIndex; col <= range.end.ColumnIndex; col++)
        {
            Column? column = cols?.Elements<Column>().FirstOrDefault(x => x.Min != null && x.Max != null && x.Min <= col + 1 && col + 1 <= x.Max);
            double columnWidth = column?.Width ?? 8.43; // Default column width
            totalWidth += (int)(columnWidth * 9525); // Convert to EMUs (1 character width = 9525 EMUs)
        }

        return totalWidth;
    }

    private static int GetRangeHeightInEmu(WorksheetPart worksheetPart, (CellReference start, CellReference end) range)
    {
        Worksheet worksheet = worksheetPart.Worksheet;

        int totalHeight = 0;
        for (uint row = range.start.RowIndex; row <= range.end.RowIndex; row++)
        {
            Row? r = worksheet.Elements<Row>().FirstOrDefault(x => x.RowIndex != null && x.RowIndex == row);
            double rowHeight = r?.Height ?? 15; // Default row height
            totalHeight += (int)(rowHeight * 9525); // Convert to EMUs (1 point = 9525 EMUs)
        }

        return totalHeight;
    }

    private static void AddImageToWorksheet(DrawingsPart drawingsPart, string relationshipId, CellReference fromCell, CellReference toCell, int xMargin, int yMargin, int width, int height)
    {
        Xdr.WorksheetDrawing worksheetDrawing = drawingsPart.WorksheetDrawing;

        uint imageId = 1;
        if (worksheetDrawing.Elements<Xdr.TwoCellAnchor>().Any())
        {
            imageId = worksheetDrawing.Elements<Xdr.TwoCellAnchor>().Where(x => x != null)
                .Max(x => uint.Parse((x.Elements<Xdr.Picture>().FirstOrDefault()?.NonVisualPictureProperties?.NonVisualDrawingProperties?.Id ?? '0')!)) + 1;
        }

        Xdr.TwoCellAnchor anchor = new()
        {
            FromMarker = new Xdr.FromMarker
            {
                ColumnId = new Xdr.ColumnId { Text = fromCell.ColumnIndex.ToString() },
                RowId = new Xdr.RowId { Text = (fromCell.RowIndex - 1).ToString() },
                ColumnOffset = new Xdr.ColumnOffset { Text = xMargin.ToString() },
                RowOffset = new Xdr.RowOffset { Text = yMargin.ToString() }
            },

            ToMarker = new Xdr.ToMarker
            {
                ColumnId = new Xdr.ColumnId { Text = toCell.ColumnIndex.ToString() },
                RowId = new Xdr.RowId { Text = toCell.RowIndex.ToString() },
                ColumnOffset = new Xdr.ColumnOffset { Text = (-xMargin).ToString() },
                RowOffset = new Xdr.RowOffset { Text = (-yMargin).ToString() }
            }
        };

        Xdr.Picture picture = new();
        Xdr.NonVisualPictureProperties nvPicPr = new(
            new Xdr.NonVisualDrawingProperties { Id = imageId, Name = "Picture " + imageId },
            new Xdr.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = true })
        );

        Xdr.BlipFill blipFill = new(
            new A.Blip { Embed = relationshipId },
            new A.Stretch(new A.FillRectangle())
        );

        Xdr.ShapeProperties spPr = new(
            new A.Transform2D(
                new A.Offset { X = 0, Y = 0 },
                new A.Extents { Cx = width, Cy = height }
            ),
            new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle }
        );

        picture.Append(nvPicPr, blipFill, spPr);
        anchor.Append(picture);
        anchor.Append(new Xdr.ClientData());

        worksheetDrawing.Append(anchor);
    }

    public static IEnumerable<Cell?> GetRange(Worksheet sheet, string range)
    {
        string[] addresses = range.Split(':');
        CellReference startAddress = new(addresses[0]);
        CellReference endAddress = new(addresses[1]);

        for (uint row = startAddress.RowIndex; row <= endAddress.RowIndex; row++)
        {
            for (uint col = startAddress.ColumnIndex; col <= endAddress.ColumnIndex; col++)
            {
                yield return sheet.GetCellFromCoordinates((int)col, (int)row);
            }
        }
    }

    // The AddDataValidation method doesn't have a direct equivalent in OpenXML.
    // Data validation in OpenXML requires a different approach and would need
    // a more complex implementation.

    public static Sheet? GetSheetByName(this SpreadsheetDocument document, string? sheetName = null)
    {
        WorkbookPart? workbookPart = document.WorkbookPart;
        return workbookPart?.Workbook.Descendants<Sheet>().FirstOrDefault(s => sheetName == null || s.Name == sheetName);
    }

    public static SheetData GetSheetDataFromDocument(this SpreadsheetDocument document, string? sheetName)
    {
        SheetData sheetData = new();

        Sheet? sheet = document.GetSheetByName(sheetName);
        WorkbookPart? workbookPart = document.WorkbookPart;
        if (sheet != null && workbookPart != null)
        {
            WorksheetPart? worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            Worksheet worksheet = worksheetPart.Worksheet;
            sheetData = worksheet.GetFirstChild<SheetData>() ?? new();
        }
        return sheetData;
    }

    /// <summary>
    /// Reads tabular data from an unformatted excel sheet to a DataTable object using OpenXML
    /// </summary>
    /// <param name="fileStream">Stream of Excel file being read</param>
    /// <param name="hasHeaders">Does the data being read have headers. Will be used for data table column names instead of default 'Column0', 'Column1'... if true. If no headers specified, first row of data must have a value for all columns in order to read all columns correctly.</param>
    /// <param name="sheetName">Name of sheet to read data from. Will use lowest index sheet if not specified.</param>
    /// <param name="startCellReference">Top left corner containing data to read. Will use A1 if not specified.</param>
    /// <param name="endCellReference">Bottom right cell containing data to read. Will read to first full empty row if not specified.</param>
    /// <returns>DataTable representation of the data read from the excel file</returns>
    public static DataTable ReadExcelFileToDataTable(this Stream fileStream, bool hasHeaders = true, string? sheetName = null, string? startCellReference = null, string? endCellReference = null)
    {
        DataTable dataTable = new();

        try
        {
            using SpreadsheetDocument document = SpreadsheetDocument.Open(fileStream, false);
            WorkbookPart? workbookPart = document.WorkbookPart;
            Sheet? sheet = document.GetSheetByName(sheetName);

            if (sheet != null && workbookPart != null)
            {
                WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                Worksheet worksheet = worksheetPart.Worksheet;
                SheetData sheetData = worksheet.GetFirstChild<SheetData>() ?? new();

                // Determine start and end cells
                CellReference startCell = new(startCellReference ?? "A1");
                CellReference endCell = endCellReference != null ? new(endCellReference) : GetLastPopulatedCell(sheetData);

                // Add columns to DataTable
                for (uint col = startCell.ColumnIndex; col <= endCell.ColumnIndex; col++)
                {
                    string columnName = hasHeaders ? sheetData.GetCellValue(startCell.RowIndex, col) : $"Column{col - startCell.ColumnIndex}";
                    dataTable.Columns.Add(columnName);
                }

                // Add rows to DataTable
                uint dataStartRow = hasHeaders ? startCell.RowIndex + 1 : startCell.RowIndex;
                for (uint row = dataStartRow; row <= endCell.RowIndex; row++)
                {
                    DataRow dataRow = dataTable.NewRow();
                    bool rowHasData = false;

                    for (uint col = startCell.ColumnIndex; col <= endCell.ColumnIndex; col++)
                    {
                        string cellValue = sheetData.GetCellValue(row, col);
                        dataRow[(int)(col - startCell.ColumnIndex)] = cellValue;
                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            rowHasData = true;
                        }
                    }

                    if (rowHasData)
                    {
                        dataTable.Rows.Add(dataRow);
                    }
                    else
                    {
                        break; // Stop at the first empty row
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error reading Excel file: {ex.Message}");
        }

        return dataTable;
    }

    private static CellReference GetLastPopulatedCell(SheetData sheetData)
    {
        uint maxRow = sheetData.Elements<Row>().Max(x => x.RowIndex?.Value ?? 0);
        uint maxCol = sheetData.Descendants<Cell>().Where(x => x != null).Max(x => new CellReference(x.CellReference!).ColumnIndex);
        return new CellReference(maxCol, maxRow);
    }

    private static string GetCellValue(this SheetData sheetData, uint row, uint col)
    {
        CellReference cellRef = new(col, row);
        Cell? cell = sheetData.Elements<Row>().FirstOrDefault(x => x.RowIndex != null && x.RowIndex == row)?
                    .Elements<Cell>().FirstOrDefault(x => x.CellReference != null && string.Equals(new CellReference(x.CellReference!).ToString(), cellRef.ToString(), StringComparison.OrdinalIgnoreCase));

        return cell?.GetCellValue() ?? string.Empty;
    }

    private static string GetCellValue(this Cell cell)
    {
        if (cell.DataType != null && cell.DataType == CellValues.SharedString)
        {
            SharedStringTablePart? sharedStringPart = cell.GetWorkbookFromCell()?.WorkbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
            if (sharedStringPart != null)
            {
                int ssid = int.Parse(cell.InnerText);
                return sharedStringPart.SharedStringTable.Elements<SharedStringItem>().ElementAt(ssid).InnerText;
            }
        }
        else if (cell.CellValue != null)
        {
            return cell.CellValue.Text;
        }
        return string.Empty;
    }

    /// <summary>
    /// Reads an Excel table into a DataTable object using OpenXML
    /// </summary>
    /// <param name="fileStream">Stream of Excel file being read</param>
    /// <param name="tableName">Name of table to read. If not specified, this function will read the first table it finds in the workbook</param>
    /// <returns>DataTable object containing the data read from Excel stream</returns>
    public static DataTable ReadExcelTableToDataTable(this Stream fileStream, string? tableName = null)
    {
        DataTable dataTable = new();

        try
        {
            using SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(fileStream, false);
            WorkbookPart? workbookPart = spreadsheetDocument.WorkbookPart ?? throw new InvalidOperationException("The workbook part is missing.");
            Table? table = FindTable(workbookPart, tableName) ?? throw new InvalidOperationException($"Table '{tableName ?? ""}' not found.");
            Sheet? sheet = spreadsheetDocument.GetSheetByName(table.Name);
            if (sheet?.Id?.Value == null)
            {
                throw new InvalidOperationException("Sheet not found for the table.");
            }

            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
            Worksheet worksheet = worksheetPart.Worksheet;

            // Get table range
            string[] tableRange = table.Reference?.Value?.Split(':') ?? [];
            if (tableRange.Length != 2)
            {
                throw new InvalidOperationException("Invalid table range.");
            }

            CellReference startCell = new(tableRange[0]);
            CellReference endCell = new(tableRange[1]);

            // Get headers
            for (uint col = startCell.ColumnIndex; col <= endCell.ColumnIndex; col++)
            {
                Cell? headerCell = worksheet.GetCellFromCoordinates((int)col, (int)startCell.RowIndex);
                if (headerCell != null)
                {
                    dataTable.Columns.Add(headerCell.GetStringValue() ?? $"Column{col - startCell.ColumnIndex + 1}");
                }
            }

            // Get body data
            for (uint row = startCell.RowIndex + 1; row <= endCell.RowIndex; row++)
            {
                DataRow dataRow = dataTable.NewRow();
                for (uint col = startCell.ColumnIndex; col <= endCell.ColumnIndex; col++)
                {
                    Cell? cell = worksheet.GetCellFromCoordinates((int)col, (int)row);
                    if (cell != null)
                    {
                        dataRow[(int)col - (int)startCell.ColumnIndex] = cell.GetStringValue();
                    }
                }
                dataTable.Rows.Add(dataRow);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Unable to read excel table data. Location {ex.GetLocationOfException()}");
        }

        return dataTable;
    }

    private static Table? FindTable(WorkbookPart workbookPart, string? tableName)
    {
        foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
        {
            if (worksheetPart.TableDefinitionParts != null)
            {
                foreach (TableDefinitionPart tableDefinitionPart in worksheetPart.TableDefinitionParts)
                {
                    if (tableName == null || tableDefinitionPart.Table.Name == tableName)
                    {
                        return tableDefinitionPart.Table;
                    }
                }
            }
        }
        return null;
    }

    // The GetClosestHssfColor method is not applicable to OpenXML as it
    // uses a different color system. You would need to implement a similar
    // functionality using the color representations in OpenXML.

    public static Row? GetRow(this Worksheet worksheet, uint rowIndex)
    {
        return worksheet.GetFirstChild<SheetData>()?.Elements<Row>().FirstOrDefault(r => r.RowIndex != null && r.RowIndex == rowIndex);
    }

    public static Cell? GetCell(this Row row, uint columnIndex)
    {
        if (row.RowIndex == null) return null;
        string cellReference = new CellReference(columnIndex, row.RowIndex.Value).ToString();
        return row.Elements<Cell>().FirstOrDefault(c => c.CellReference == cellReference);
    }
}

// Helper classes and methods
public partial class CellReference
{
    [GeneratedRegex(@"([A-Z]+)(\d+)")]
    private static partial Regex CellRefRegex();

    public uint RowIndex { get; set; }
    public uint ColumnIndex { get; set; }

    public CellReference(string reference)
    {
        Match match = CellRefRegex().Match(reference);
        ColumnIndex = ColumnNameToNumber(match.Groups[1].Value);
        RowIndex = uint.Parse(match.Groups[2].Value);
    }

    public CellReference(uint col, uint row)
    {
        ColumnIndex = col;
        RowIndex = row;
    }

    public override string ToString()
    {
        return $"{NumberToColumnName(ColumnIndex)}{RowIndex}";
    }

    private static uint ColumnNameToNumber(string columnName)
    {
        uint number = 0;
        for (int i = 0; i < columnName.Length; i++)
        {
            number *= 26;
            number += (uint)(columnName[i] - 'A' + 1);
        }
        return number - 1;
    }

    private static string NumberToColumnName(uint columnNumber)
    {
        int number = (int)columnNumber;
        string columnName = "";
        while (number >= 0)
        {
            int remainder = number % 26;
            columnName = Convert.ToChar('A' + remainder) + columnName;
            number = (number / 26) - 1;
            if (number < 0) break;
        }
        return columnName;
    }
}
