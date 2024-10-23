using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
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

    /// OK
    /// <summary>
    /// Populates SpreadsheetDocument with all components needed for a new Excel file including a single new sheet
    /// </summary>
    /// <param name="document">SpreadsheetDocument to add components to</param>
    /// <param name="sheetName">Optional name for the new sheet that will be created</param>
    /// <returns>Id of the sheet that was created during initialization</returns>
    public static uint InitializeExcelFile(this SpreadsheetDocument document, string? sheetName = null)
    {
        return document.CreateNewSheet(sheetName);
    }

    /// OK
    /// <summary>
    /// Adds a new sheet to a SpreadsheetDocument named according to the value passed into sheetName or "Sheet #"
    /// </summary>
    /// <param name="document">SpreadsheetDocument to add sheet to</param>
    /// <param name="sheetName">Optional name for the new sheet that will be created. Default is "Sheet #"</param>
    /// <returns>Id of the sheet that was created</returns>
    public static uint CreateNewSheet(this SpreadsheetDocument document, string? sheetName = null)
    {
        WorkbookPart? workbookPart = document.WorkbookPart;
        if (workbookPart == null)
        {
            workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new();
        }
        // Add a blank WorksheetPart
        WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(new SheetData());

        Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>() ?? workbookPart.Workbook.AppendChild(new Sheets());
        string worksheetPartId = workbookPart.GetIdOfPart(worksheetPart);

        // Get a unique ID for the new worksheet.
        uint sheetId = 1;
        if (sheets.Elements<Sheet>().Any())
        {
            sheetId = (sheets.Elements<Sheet>().Max(s => s.SheetId?.Value) + 1) ?? (uint)sheets.Elements<Sheet>().Count() + 1;
        }

        // Append the new worksheet and associate it with the workbook.
        Sheet sheet = new() {
            Id = worksheetPartId,
            SheetId = sheetId,
            Name = sheetName ?? "Sheet" + sheetId
        };

        sheets.Append(sheet);
        return sheetId;
    }

    /// OK
    /// <summary>
    /// Gets a Worksheet by its name from a SpreadsheetDocument.
    /// </summary>
    /// <param name="document">The SpreadsheetDocument containing the worksheet</param>
    /// <param name="sheetName">The name of the worksheet to retrieve</param>
    /// <returns>The Worksheet corresponding to the given name, or null if not found</returns>
    public static Worksheet? GetWorksheetByName(this SpreadsheetDocument document, string sheetName, bool createIfMissing = true)
    {
        WorkbookPart? workbookPart = document.WorkbookPart;
        if (workbookPart == null && createIfMissing)
        {
            workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new();
        }
        else if(workbookPart == null)
        {
            throw new ArgumentException("The document does not contain a WorkbookPart.");
        }

        Sheet? sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name?.Value.StrEq(sheetName) == true);

        if (sheet == null && createIfMissing)
        {
            return document.GetWorksheetById(document.CreateNewSheet(sheetName));
        }
        else if(sheet == null)
        {
            return null;
        }
        else
        {
            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            return worksheetPart.Worksheet;
        }
    }

    /// OK
    /// <summary>
    /// Gets a Worksheet by its ID from a SpreadsheetDocument
    /// </summary>
    /// <param name="document">The SpreadsheetDocument containing the worksheet</param>
    /// <param name="sheetId">The ID of the worksheet to retrieve</param>
    /// <returns>The Worksheet corresponding to the given ID, or null if not found</returns>
    public static Worksheet? GetWorksheetById(this SpreadsheetDocument document, uint sheetId)
    {
        WorkbookPart? workbookPart = document.WorkbookPart ?? throw new ArgumentException("The document does not contain a WorkbookPart.");
        Sheet? sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.SheetId != null && s.SheetId.Value == sheetId);

        if (sheet == null) return null;

        WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        return worksheetPart.Worksheet;
    }

    public static Worksheet GetWorksheetFromCell(this Cell cell)
    {
        return cell.Ancestors<Worksheet>().FirstOrDefault() ?? throw new InvalidOperationException("Cell is not part of a worksheet.");
    }

    public static Workbook GetWorkbookFromCell(this Cell cell)
    {
        // Get the parent worksheet
        Worksheet worksheet = cell.GetWorksheetFromCell();

        // Get the workbook part
        WorkbookPart? workbookPart = (worksheet.WorksheetPart?.GetParentParts().OfType<WorkbookPart>().FirstOrDefault()) ?? throw new InvalidOperationException("Worksheet is not part of a workbook.");

        // Return the workbook
        return workbookPart.Workbook;
    }

    public static Workbook GetWorkbookFromWorksheet(this Worksheet worksheet)
    {
        // Get the workbook part
        WorkbookPart? workbookPart = (worksheet.WorksheetPart?.GetParentParts().OfType<WorkbookPart>().FirstOrDefault()) ?? throw new InvalidOperationException("Worksheet is not part of a workbook.");

        // Return the workbook
        return workbookPart.Workbook;
    }

    public static Workbook GetWorkbookFromWorksheet(this WorksheetPart worksheetPart)
    {
        // Get the workbook part
        WorkbookPart? workbookPart = (worksheetPart.GetParentParts().OfType<WorkbookPart>().FirstOrDefault()) ?? throw new InvalidOperationException("Worksheet is not part of a workbook.");

        // Return the workbook
        return workbookPart.Workbook;
    }


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
                Worksheet worksheet = startCell.GetWorksheetFromCell();
                CellReference startCellReference = new(startCell.CellReference!);
                return worksheet.GetCellFromCoordinates((int)startCellReference.ColumnIndex + colOffset, (int)startCellReference.RowIndex + rowOffset);
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

    /// OK
    /// <summary>
    /// Gets the Stylesheet from a SpreadsheetDocument
    /// </summary>
    /// <param name="document">The SpreadsheetDocument to get the Stylesheet from</param>
    /// <param name="createIfMissing">If true, creates Stylesheet (and parent elements if necessary) if missing.</param>
    /// <returns>The Stylesheet from the document or null if not found and createIfMissing is false</returns>
    public static Stylesheet? GetStylesheet(this SpreadsheetDocument document, bool createIfMissing = true)
    {
        WorkbookPart? workbookPart = document.WorkbookPart;
        if (workbookPart == null)
        {
            if (createIfMissing)
            {
                workbookPart = document.AddWorkbookPart();
            }
            else
            {
                return null;
            }
        }

        WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
        if (stylesPart == null && createIfMissing)
        {
            stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = new();
        }
        return stylesPart?.Stylesheet;
    }

    /// OK
    /// <summary>
    /// Gets the Borders from a Stylesheet
    /// </summary>
    /// <param name="stylesheet">The Stylesheet to get the Borders from</param>
    /// <param name="createIfMissing">If true, creates Borders if missing</param>
    /// <returns>The Borders object, or null if not found and createIfMissing is false</returns>
    public static Borders? GetBorders(this Stylesheet stylesheet, bool createIfMissing = true)
    {
        Borders? borders = stylesheet.Elements<Borders>().FirstOrDefault();
        if (borders == null && createIfMissing)
        {
            stylesheet.AddChild(new Borders());
            borders = stylesheet.Elements<Borders>().First();
        }
        return borders;
    }

    /// OK
    /// <summary>
    /// Gets the Fills from a Stylesheet
    /// </summary>
    /// <param name="stylesheet">The Stylesheet to get the Fills from</param>
    /// <param name="createIfMissing">If true, creates Fills if missing</param>
    /// <returns>The Fills object, or null if not found and createIfMissing is false</returns>
    public static Fills? GetFills(this Stylesheet stylesheet, bool createIfMissing = true)
    {
        Fills? fills = stylesheet.Elements<Fills>().FirstOrDefault();
        if (fills == null && createIfMissing)
        {
            stylesheet.AddChild(new Fills());
            fills = stylesheet.Elements<Fills>().First();
        }
        return fills;
    }

    /// OK
    /// <summary>
    /// Gets the Fonts from a Stylesheet
    /// </summary>
    /// <param name="stylesheet">The Stylesheet to get the Fonts from</param>
    /// <param name="createIfMissing">If true, creates Fonts if not found</param>
    /// <returns>The Fonts object, or null if not found and createIfMissing is false</returns>
    public static Fonts? GetFonts(this Stylesheet stylesheet, bool createIfMissing = true)
    {
        Fonts? fonts = stylesheet.Elements<Fonts>().FirstOrDefault();
        if (fonts == null && createIfMissing)
        {
            stylesheet.AddChild(new Fonts());
            fonts = stylesheet.Elements<Fonts>().First();
        }
        return fonts;
    }

    /// OK
    /// <summary>
    /// Gets the CellFormats from a Stylesheet.
    /// </summary>
    /// <param name="stylesheet">The Stylesheet to get the CellFormats from.</param>
    /// <param name="createIfMissing">If true, creates CellFormats if not found.</param>
    /// <returns>The CellFormats object, or null if not found and not created.</returns>
    public static CellFormats? GetCellFormats(this Stylesheet stylesheet, bool createIfMissing = true)
    {
        CellFormats? cellFormats = stylesheet.Elements<CellFormats>().FirstOrDefault();
        if (cellFormats == null && createIfMissing)
        {
            stylesheet.AddChild(new CellFormats());
            cellFormats = stylesheet.Elements<CellFormats>().First();
        }
        return cellFormats;
    }

    /// OK
    /// <summary>
    /// Creates the style corresponding to the style enum passed in and returns the ID for the style that was created
    /// </summary>
    /// <param name="style">Enum value indicating which style to create</param>
    /// <param name="document">Document to add the standard cell style to</param>
    /// <param name="cellLocked">Whether or not the cells with this style should be locked or not</param>
    /// <returns>The ID of the style that was created</returns>
    public static uint GetStandardCellStyle(EStyles style, SpreadsheetDocument document, bool cellLocked = false)
    {
        Stylesheet stylesheet = document.GetStylesheet()!;

        Dictionary<string, uint> formatCache = WorkbookStandardFormatCache.GetOrAdd(GetWorkbookId(document), _ => []);
        string formatKey = $"{style}_{cellLocked}";

        if (formatCache.TryGetValue(formatKey, out uint existingFormatId))
        {
            return existingFormatId;
        }

        Borders borders = stylesheet.GetBorders()!;
        Fills fills = stylesheet.GetFills()!;
        Fonts fonts = stylesheet.GetFonts()!;
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
        CellFormats cellFormats = stylesheet.GetCellFormats()!;
        for (uint i = 0; i < (uint)cellFormats.Count(); i++)
        {
            if (CellFormatsAreEqual(cellFormat, cellFormats.Elements<CellFormat>().ElementAt((int)i)))
            {
                formatCache[formatKey] = i;
                return i;
            }
        }

        // If no matching format found, add the new one
        cellFormats.Append(cellFormat);
        uint newFormatId = (uint)cellFormats.Count() - 1;
        formatCache[formatKey] = newFormatId;
        return newFormatId;
    }

    /// OK
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

    public static bool CellFormatsAreEqual(CellFormat format1, CellFormat format2)
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

    public static bool FormatAlignmentsAreEqual(Alignment? alignment1, Alignment? alignment2)
    {
        if (alignment1 == null && alignment2 == null) return true;
        if (alignment1 == null || alignment2 == null) return false;
        return alignment1.Horizontal == alignment2.Horizontal;
    }

    public static bool FormatProtectionsAreEqual(Protection? protection1, Protection? protection2)
    {
        if (protection1 == null && protection2 == null) return true;
        if (protection1 == null || protection2 == null) return false;
        return protection1.Locked == protection2.Locked;
    }

    public static readonly ConcurrentDictionary<string, WorkbookStyleCache> WorkbookCaches = new();

    public class WorkbookStyleCache
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
        uint newFormatId = (uint)cellFormats.Count() - 1;
        cache.CellFormatCache[cellFormatKey] = newFormatId;

        return newFormatId;
    }

    public static uint GetOrAddFont(Stylesheet stylesheet, WorkbookStyleCache cache, Font? font)
    {
        if (font == null) return 0; // Default font

        int fontHash = GetHashCode(font);
        if (cache.FontCache.TryGetValue(fontHash, out uint existingFontId))
        {
            return existingFontId;
        }

        Fonts fonts = stylesheet.Elements<Fonts>().First();
        fonts.Append(font);
        uint newFontId = (uint)fonts.Count() - 1;
        cache.FontCache[fontHash] = newFontId;
        return newFontId;
    }

    public static uint GetOrAddFill(Stylesheet stylesheet, WorkbookStyleCache cache, Fill? fill)
    {
        if (fill == null) return 0; // Default fill

        int fillHash = GetHashCode(fill);
        if (cache.FillCache.TryGetValue(fillHash, out uint existingFillId))
        {
            return existingFillId;
        }

        Fills fills = stylesheet.Elements<Fills>().First();
        fills.Append(fill);
        uint newFillId = (uint)fills.Count() - 1;
        cache.FillCache[fillHash] = newFillId;
        return newFillId;
    }

    public static uint GetOrAddBorder(Stylesheet stylesheet, WorkbookStyleCache cache, Border? border)
    {
        if (border == null) return 0; // Default border

        int borderHash = GetHashCode(border);
        if (cache.BorderCache.TryGetValue(borderHash, out uint existingBorderId))
        {
            return existingBorderId;
        }

        Borders borders = stylesheet.Elements<Borders>().First();
        borders.Append(border);
        uint newBorderId = (uint)borders.Count() - 1;
        cache.BorderCache[borderHash] = newBorderId;
        return newBorderId;
    }

    public static int GetHashCode(OpenXmlElement element)
    {
        return element.OuterXml.GetHashCode();
    }

    public static string GetWorkbookId(SpreadsheetDocument document)
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
                SheetData? sheetData = worksheet.GetFirstChild<SheetData>() ?? throw new ArgumentException("The worksheet does not contain sheetData, which is required for this operation.");

                uint headerStyleId = GetStandardCellStyle(EStyles.Header, document);
                uint bodyStyleId = GetStandardCellStyle(EStyles.Body, document);

                uint x = 1;
                uint y = 1;

                PropertyInfo[] properties = typeof(T).GetProperties();

                // Write headers
                foreach (PropertyInfo prop in properties)
                {
                    sheetData.InsertCellValue(x, y, new CellValue(prop.Name), CellValues.SharedString, headerStyleId);
                    x++;
                }
                x = 1;
                y++;

                // Write data
                foreach (T item in data)
                {
                    foreach (PropertyInfo prop in properties)
                    {
                        sheetData.InsertCellValue(x, y, new CellValue(prop.GetValue(item)?.ToString() ?? string.Empty), CellValues.SharedString, bodyStyleId);
                        x++;
                    }
                    x = 1;
                    y++;
                }

                if (createTable)
                {
                    CreateTable(worksheet, 1, 1, y - 1, (uint)properties.Length, tableName);
                }
                else
                {
                    SetAutoFilter(worksheet, 1, 1, y - 1, (uint)properties.Length);
                }
            }
            ClearCacheForWorkbook(document);
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
                SheetData? sheetData = worksheet.GetFirstChild<SheetData>() ?? throw new ArgumentException("The worksheet does not contain sheetData, which is required for this operation.");

                uint headerStyleId = GetStandardCellStyle(EStyles.Header, document);
                uint bodyStyleId = GetStandardCellStyle(EStyles.Body, document);

                uint y = 0;
                uint x = 0;

                Dictionary<uint, int> maxColumnWidths = [];

                foreach (DataColumn column in data.Columns)
                {
                    sheetData.InsertCellValue(x, y, new(column.ColumnName), CellValues.SharedString, headerStyleId);
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
                            sheetData.InsertCellValue(x, y, new(value.ToString() ?? string.Empty), CellValues.SharedString, bodyStyleId);
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

    /// OK
    /// <summary>
    /// Creates a cell in the provided worksheet, creating a row to contain it if necessary
    /// </summary>
    /// <param name="worksheet">Worksheet to add cell to</param>
    /// <param name="columnIndex">Column index for the cell to create</param>
    /// <param name="rowIndex">Row index for the cell to create</param>
    /// <returns>Cell object that was created or null if worksheet sheetData element is null</returns>
    public static Cell? InsertCell(this Worksheet worksheet, uint columnIndex, uint rowIndex)
    {
        SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
        return sheetData.InsertCell(columnIndex, rowIndex);
    }

    /// OK
    /// <summary>
    /// Creates a cell in the provided sheetData, creating a row to contain it if necessary
    /// </summary>
    /// <param name="sheetData">SheetData to add cell to</param>
    /// <param name="columnIndex">Column index for the cell to create</param>
    /// <param name="rowIndex">Row index for the cell to create</param>
    /// <returns>Cell object that was created or null if sheetData is null</returns>
    [return:NotNullIfNotNull(nameof(sheetData))]
    public static Cell? InsertCell(this SheetData? sheetData, uint columnIndex, uint rowIndex)
    {
        Cell? cell = null;
        if (sheetData != null)
        {
            CellReference cellReference = new(columnIndex, rowIndex);
            string cellRef = cellReference.ToString();
            // Check if the row exists, create if not
            Row? row = sheetData?.Elements<Row>().FirstOrDefault(x => x.RowIndex != null && x.RowIndex == rowIndex);
            if (row == null)
            {
                row = new Row { RowIndex = rowIndex };
                sheetData!.Append(row);
            }

            // Check if the cell exists, create if not
            cell = row.Elements<Cell>().FirstOrDefault(c => c.CellReference?.Value == cellRef);
            if (cell == null)
            {
                //cell = new Cell { CellReference = cellReference.ToString() };
                //row.Append(cell);

                // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
                Cell? refCell = null;

                foreach (Cell existingCell in row.Elements<Cell>())
                {
                    if (string.Compare(existingCell.CellReference?.Value, cellRef, true) > 0)
                    {
                        refCell = cell;
                        break;
                    }
                }

                Cell newCell = new() { CellReference = cellRef };
                row.InsertBefore(newCell, refCell);

                return newCell;
            }
        }
        return cell;
    }

    /// OK
    /// <summary>
    ///
    /// </summary>
    /// <param name="worksheet"></param>
    /// <param name="columnIndex"></param>
    /// <param name="rowIndex"></param>
    /// <param name="cellValue"></param>
    /// <param name="cellType"></param>
    /// <param name="styleIndex"></param>
    public static void InsertCellValue(this Worksheet worksheet, uint columnIndex, uint rowIndex, CellValue cellValue, CellValues? cellType = null, uint? styleIndex = null)
    {
        SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
        sheetData.InsertCellValue(columnIndex, rowIndex, cellValue, cellType, styleIndex);
    }

    /// OK
    /// <summary>
    ///
    /// </summary>
    /// <param name="sheetData"></param>
    /// <param name="columnIndex"></param>
    /// <param name="rowIndex"></param>
    /// <param name="cellValue"></param>
    /// <param name="cellType"></param>
    /// <param name="styleIndex"></param>
    public static void InsertCellValue(this SheetData? sheetData, uint columnIndex, uint rowIndex, CellValue cellValue, CellValues? cellType = null, uint? styleIndex = null)
    {
        cellType ??= CellValues.SharedString; //Default to shared string since it is the most compact option
        Cell? cell = sheetData.InsertCell(columnIndex, rowIndex);
        if (cell != null)
        {
            if (cellType == CellValues.SharedString)
            {
                Workbook workbook = cell.GetWorkbookFromCell();
                int index = workbook.InsertSharedStringItem(cellValue.InnerText);
                cell.CellValue = new CellValue(index.ToString());
                cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
            }
            else
            {
                cell.CellValue = cellValue;
                cell.DataType = cellType ?? new EnumValue<CellValues>(cellType!);
            }
            if (styleIndex != null)
            {
                cell.StyleIndex = styleIndex;
            }
        }
    }

    /// OK
    /// <summary>
    ///
    /// </summary>
    /// <param name="worksheet"></param>
    /// <param name="columnIndex"></param>
    /// <param name="rowIndex"></param>
    /// <param name="formulaString"></param>
    /// <param name="cellType"></param>
    /// <param name="styleIndex"></param>
    public static void InsertCellFormula(this Worksheet worksheet, uint columnIndex, uint rowIndex, string formulaString, CellValues? cellType = null, uint? styleIndex = null)
    {
        SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
        sheetData.InsertCellFormula(columnIndex, rowIndex, formulaString, cellType, styleIndex);
    }

    /// OK
    /// <summary>
    ///
    /// </summary>
    /// <param name="sheetData"></param>
    /// <param name="columnIndex"></param>
    /// <param name="rowIndex"></param>
    /// <param name="formulaString"></param>
    /// <param name="cellType"></param>
    /// <param name="styleIndex"></param>
    public static void InsertCellFormula(this SheetData? sheetData, uint columnIndex, uint rowIndex, string formulaString, CellValues? cellType = null, uint? styleIndex = null)
    {
        cellType ??= CellValues.String;
        Cell? cell = sheetData.InsertCell(columnIndex, rowIndex);
        if (cell != null)
        {
            if (cellType == CellValues.SharedString)
            {
                cellType = CellValues.String;
            }
            cell.CellFormula = new(formulaString);
            cell.DataType = cellType;
            if (styleIndex != null)
            {
                cell.StyleIndex = styleIndex;
            }
        }
    }

    // Given text and a SharedStringTablePart, creates a SharedStringItem with the specified text and inserts it into the SharedStringTablePart. If the item already exists, returns its index.
    public static int InsertSharedStringItem(this Workbook workbook, string text)
    {
        // If the part does not contain a SharedStringTable, create one.
        SharedStringTablePart shareStringTablePart = workbook.WorkbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault() ?? workbook.WorkbookPart?.AddNewPart<SharedStringTablePart>() ?? throw new InvalidOperationException("The WorkbookPart is missing.");
        shareStringTablePart.SharedStringTable ??= new();

        int index = shareStringTablePart.SharedStringTable.Elements<SharedStringItem>().Select(x => x.InnerText).IndexOf(text);

        int i = 0;
        // Iterate through all the items in the SharedStringTable. If the text already exists, return its index.
        foreach (SharedStringItem item in shareStringTablePart.SharedStringTable.Elements<SharedStringItem>())
        {
            if (item.InnerText == text) return i;
            i++;
        }

        // The text does not exist in the part. Create the SharedStringItem and return its index.
        shareStringTablePart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
        shareStringTablePart.SharedStringTable.Save();

        return i;
    }

public static void CreateTable(Worksheet worksheet, uint startRow, uint startColumn, uint endRow, uint endColumn, string tableName)
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
            tableColumns.Append(new TableColumn { Id = i + 1, Name = $"Column{i + 1}" });
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

    /// OK
    /// <summary>
    /// Adds auto filter to a range of cells in the worksheet
    /// </summary>
    /// <param name="worksheet">Worksheet to add auto filter to</param>
    /// <param name="startRow">First row of auto filtered range (usually headers)</param>
    /// <param name="startColumn">First column of auto filtered range</param>
    /// <param name="endRow">Last row of auto filtered range</param>
    /// <param name="endColumn">Last column of auto filtered range</param>
    public static void SetAutoFilter(Worksheet worksheet, uint startRow, uint startColumn, uint endRow, uint endColumn)
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
                Worksheet worksheet = cell.GetWorksheetFromCell();
                SharedStringTablePart? stringTable = worksheet.WorksheetPart?.GetParentParts().OfType<SharedStringTablePart>().FirstOrDefault();
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

    public static CellReference? GetCellFromName(WorkbookPart workbookPart, string name)
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

    public static WorksheetPart? GetWorksheetPartByCellReference(WorkbookPart workbookPart, CellReference cellReference)
    {
        return workbookPart.WorksheetParts.FirstOrDefault(wp => wp.Worksheet.Descendants<Cell>().Any(c => c.CellReference == cellReference.ToString()));
    }

    public static DrawingsPart? GetOrCreateDrawingsPart(WorksheetPart worksheetPart)
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

    public static (CellReference, CellReference) GetMergedCellArea(WorksheetPart worksheetPart, CellReference cellReference)
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

    public static int GetRangeWidthInEmu(WorksheetPart worksheetPart, (CellReference start, CellReference end) range)
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

    public static int GetRangeHeightInEmu(WorksheetPart worksheetPart, (CellReference start, CellReference end) range)
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

    public static void AddImageToWorksheet(DrawingsPart drawingsPart, string relationshipId, CellReference fromCell, CellReference toCell, int xMargin, int yMargin, int width, int height)
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

    public static CellReference GetLastPopulatedCell(SheetData sheetData)
    {
        uint maxRow = sheetData.Elements<Row>().Max(x => x.RowIndex?.Value ?? 0);
        uint maxCol = sheetData.Descendants<Cell>().Where(x => x != null).Max(x => new CellReference(x.CellReference!).ColumnIndex);
        return new CellReference(maxCol, maxRow);
    }

    public static string GetCellValue(this SheetData sheetData, uint row, uint col)
    {
        CellReference cellRef = new(col, row);
        Cell? cell = sheetData.Elements<Row>().FirstOrDefault(x => x.RowIndex != null && x.RowIndex == row)?
                    .Elements<Cell>().FirstOrDefault(x => x.CellReference != null && string.Equals(new CellReference(x.CellReference!).ToString(), cellRef.ToString(), StringComparison.OrdinalIgnoreCase));

        return cell?.GetCellValue() ?? string.Empty;
    }

    public static string GetCellValue(this Cell cell)
    {
        if (cell.DataType != null && cell.DataType == CellValues.SharedString)
        {
            SharedStringTablePart? sharedStringPart = cell.GetWorkbookFromCell().WorkbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
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

    public static Table? FindTable(WorkbookPart workbookPart, string? tableName)
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

    private uint _RowIndex;

    public uint RowIndex
    {
        get { return _RowIndex; }
        set
        {
            if (value < 1 || value > 1048576)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "RowIndex must be between 1 and 1048576");
            }
            _RowIndex = value;
        }
    }

    private uint _ColumnIndex;

    public uint ColumnIndex {
        get { return _ColumnIndex; }
        set
        {
            if (value < 1 || value > 16384)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "RowIndex must be between 1 and 16384");
            }
            _ColumnIndex = value;
        }
    }

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

    public static uint ColumnNameToNumber(string columnName)
    {
        uint number = 0;
        for (int i = 0; i < columnName.Length; i++)
        {
            number *= 26;
            number += (uint)(columnName[i] - 'A' + 1);
        }
        return number - 1;
    }

    public static string NumberToColumnName(uint columnNumber)
    {
        int number = (int)columnNumber - 1; //Make this 1 based to avoid confusion
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
