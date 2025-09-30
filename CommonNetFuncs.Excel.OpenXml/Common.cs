using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CommonNetFuncs.Excel.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SixLabors.ImageSharp;
using Color = DocumentFormat.OpenXml.Spreadsheet.Color; //Aliased to prevent issue with DocumentFormat.OpenXml.Spreadsheet.Color
using Dwg = DocumentFormat.OpenXml.Drawing;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace CommonNetFuncs.Excel.OpenXml;

public static partial class Common
{
  private static readonly Lock formatCacheLock = new();
  private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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

  /// <summary>
  /// Adds a new sheet to a SpreadsheetDocument named according to the value passed into sheetName or "Sheet #"
  /// </summary>
  /// <param name="document">SpreadsheetDocument to add sheet to</param>
  /// <param name="sheetName">Optional name for the new sheet that will be created. Default is "Sheet #"</param>
  /// <returns>Id of the sheet that was created</returns>
  public static uint CreateNewSheet(this SpreadsheetDocument document, string? sheetName = null)
  {
    WorkbookPart? workbookPart = document.WorkbookPart;

    workbookPart ??= document.AddWorkbookPart();
    workbookPart.Workbook ??= new();

    // Add a blank WorksheetPart
    WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
    worksheetPart.Worksheet = new Worksheet(new SheetData());

    Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>() ?? workbookPart.Workbook.AppendChild(new Sheets());
    string worksheetPartId = workbookPart.GetIdOfPart(worksheetPart);

    // Get a unique ID for the new worksheet.
    uint sheetId = 1;
    if (sheets.Elements<Sheet>().Any())
    {
      sheetId = sheets.Elements<Sheet>().Max(x => x.SheetId?.Value) + 1 ?? ((uint)sheets.Elements<Sheet>().Count()) + 1;
    }

    // Append the new worksheet and associate it with the workbook.
    Sheet sheet = new()
        {
            Id = worksheetPartId,
            SheetId = sheetId,
            Name = sheetName ?? "Sheet" + sheetId
        };

    sheets.Append(sheet);
    return sheetId;
  }

  /// <summary>
  /// Gets a Worksheet by its name from a SpreadsheetDocument.
  /// </summary>
  /// <param name="document">The SpreadsheetDocument containing the worksheet</param>
  /// <param name="sheetName">The name of the worksheet to retrieve</param>
  /// <returns>The Worksheet corresponding to the given name, or null if not found</returns>
  public static Worksheet? GetWorksheetByName(this SpreadsheetDocument document, string sheetName, bool createIfMissing = true)
  {
    WorkbookPart? workbookPart = document.WorkbookPart;
    if ((workbookPart == null) && createIfMissing)
    {
      workbookPart = document.AddWorkbookPart();
      workbookPart.Workbook = new();
    }
    else if (workbookPart == null)
    {
      throw new ArgumentException("The document does not contain a WorkbookPart.");
    }

    Sheet? sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => string.Equals(s.Name?.Value, sheetName, StringComparison.InvariantCultureIgnoreCase));

    if ((sheet == null) && createIfMissing)
    {
      return document.GetWorksheetById(document.CreateNewSheet(sheetName));
    }
    else if (sheet == null)
    {
      return null;
    }
    else
    {
      WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
      return worksheetPart.Worksheet;
    }
  }

  /// <summary>
  /// Gets a Worksheet by its ID from a SpreadsheetDocument
  /// </summary>
  /// <param name="document">The SpreadsheetDocument containing the worksheet</param>
  /// <param name="sheetId">The ID of the worksheet to retrieve</param>
  /// <returns>The Worksheet corresponding to the given ID, or null if not found</returns>
  public static Worksheet? GetWorksheetById(this SpreadsheetDocument document, uint sheetId)
  {
    WorkbookPart? workbookPart = document.WorkbookPart ?? throw new ArgumentException("The document does not contain a WorkbookPart.");
    Sheet? sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => (s.SheetId != null) && (s.SheetId.Value == sheetId));

    if (sheet == null)
    {
      return null;
    }

    WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
    return worksheetPart.Worksheet;
  }

  /// <summary>
  /// Gets a Worksheet from a Cell object
  /// </summary>
  /// <param name="cell">The Cell object to get the Worksheet from</param>
  /// <returns>The Worksheet containing the Cell, or null if not found</returns>
  public static Worksheet GetWorksheetFromCell(this Cell cell)
  {
    return cell.Ancestors<Worksheet>().FirstOrDefault() ?? throw new InvalidOperationException("Cell is not part of a worksheet.");
  }

  /// <summary>
  /// Gets a Workbook from a Cell object
  /// </summary>
  /// <param name="cell">The Cell object to get the Workbook from</param>
  /// <returns>The Workbook containing the Cell, or null if not found</returns>
  public static Workbook GetWorkbookFromCell(this Cell cell)
  {
    // Get the parent worksheet
    Worksheet worksheet = cell.GetWorksheetFromCell();

    // Get the workbook part
    WorkbookPart? workbookPart = worksheet.WorksheetPart?.GetParentParts().OfType<WorkbookPart>().FirstOrDefault() ?? throw new InvalidOperationException("Worksheet is not part of a workbook.");

    // Return the workbook
    return workbookPart.Workbook;
  }

  /// <summary>
  /// Gets a Workbook from a Worksheet object
  /// </summary>
  /// <param name="worksheet">The Worksheet object to get the Workbook from</param>
  /// <returns>The Workbook containing the Worksheet, or null if not found</returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static Workbook GetWorkbookFromWorksheet(this Worksheet worksheet)
  {
    // Get the workbook part
    WorkbookPart? workbookPart = worksheet.WorksheetPart?.GetParentParts().OfType<WorkbookPart>().FirstOrDefault() ?? throw new InvalidOperationException("Worksheet is not part of a workbook.");

    // Return the workbook
    return workbookPart.Workbook;
  }

  /// <summary>
  /// Gets a Workbook from a WorksheetPart object
  /// </summary>
  /// <param name="worksheetPart">The WorksheetPart object to get the Workbook from</param>
  /// <returns>The Workbook containing the WorksheetPart, or null if not found</returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static Workbook GetWorkbookFromWorksheet(this WorksheetPart worksheetPart)
  {
    // Get the workbook part
    WorkbookPart? workbookPart = worksheetPart.GetParentParts().OfType<WorkbookPart>().FirstOrDefault() ?? throw new InvalidOperationException("Worksheet is not part of a workbook.");

    // Return the workbook
    return workbookPart.Workbook;
  }

  /// <summary>
  /// Gets a WorkbookPart from a CellReference object
  /// </summary>
  /// <param name="workbookPart">The WorkbookPart object that contains the WorksheetPart and CellReference</param>
  /// <param name="cellReference">The CellReference object to get the worksheet WorksheetPart from</param>
  /// <returns>The WorksheetPart containing the CellReference, or null if not found</returns>
  public static WorksheetPart? GetWorksheetPartByCellReference(WorkbookPart workbookPart, CellReference cellReference)
  {
    return workbookPart.WorksheetParts.FirstOrDefault(wp => wp.Worksheet.Descendants<Cell>().Any(c => c.CellReference == cellReference.ToString()));
  }

  /// <summary>
  /// Gets a Sheet by name from a SpreadsheetDocument
  /// </summary>
  /// <param name="document">The SpreadsheetDocument to search in</param>
  /// <param name="sheetName">The name of the sheet to find. If null, returns the first sheet.</param>
  /// <returns>The Sheet object, or null if not found.</returns>
  public static Sheet? GetSheetByName(this SpreadsheetDocument document, string? sheetName = null)
  {
    WorkbookPart? workbookPart = document.WorkbookPart;
    return workbookPart?.Workbook.Descendants<Sheet>().FirstOrDefault(s => (sheetName == null) || (s.Name == sheetName));
  }

  /// <summary>
  /// Gets the SheetData from a SpreadsheetDocument for a specific sheet
  /// </summary>
  /// <param name="document">The SpreadsheetDocument to get the SheetData from</param>
  /// <param name="sheetName">The name of the sheet. If null, uses the first sheet.</param>
  /// <returns>The SheetData object for the specified sheet</returns>
  public static SheetData GetSheetDataFromDocument(this SpreadsheetDocument document, string? sheetName)
  {
    SheetData sheetData = new();

    Sheet? sheet = document.GetSheetByName(sheetName);
    WorkbookPart? workbookPart = document.WorkbookPart;
    if ((sheet != null) && (workbookPart != null))
    {
      WorksheetPart? worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
      Worksheet worksheet = worksheetPart.Worksheet;
      sheetData = worksheet.GetFirstChild<SheetData>() ?? new();
    }
    return sheetData;
  }

  /// <summary>
  /// Checks if a Cell is empty
  /// </summary>
  /// <param name="cell">The Cell to check</param>
  /// <returns><see langword="true"/> if the Cell is empty, false otherwise</returns>
  public static bool IsCellEmpty(this Cell? cell)
  {
    return (cell == null) || string.IsNullOrWhiteSpace(cell.InnerText);
  }

  /// <summary>
  /// Gets a Cell from a Worksheet using a cell reference, creating a new cell if it doesn't already exist
  /// </summary>
  /// <param name="ws">The Worksheet containing the cell</param>
  /// <param name="cellReference">The cell reference to the cell to get</param>
  /// <param name="colOffset">Optional: Column offset from cellReference column</param>
  /// <param name="rowOffset">Optional: Row offset from cellReference row</param>
  /// <returns>The Cell object, or null if not found</returns>
  public static Cell? GetCellFromReference(this Worksheet ws, CellReference cellReference, int colOffset = 0, int rowOffset = 0)
  {
    try
    {
      Row? row = ws.GetRow(cellReference.RowIndex + ((uint)rowOffset));
      if (row == null)
      {
        row = new Row() { RowIndex = (uint)(cellReference.RowIndex + rowOffset) };
        ws.Append(row);
      }
      Cell? cell = row.GetCell(cellReference.ColumnIndex + ((uint)colOffset));
      if (cell == null)
      {
        cell = new Cell() { CellReference = new CellReference(cellReference.ColumnIndex + ((uint)colOffset), cellReference.RowIndex + ((uint)rowOffset)).ToString() };
        row.Append(cell);
      }
      return cell;
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"Error in {nameof(Common)}.{nameof(GetCellFromReference)}");
      return null;
    }
  }

  /// <summary>
  /// Gets a Cell from a Worksheet using a cell reference, creating a new cell if it doesn't already exist
  /// </summary>
  /// <param name="ws">The Worksheet containing the cell</param>
  /// <param name="cellReference">The cell reference (e.g., "A1").</param>
  /// <param name="colOffset">Optional: Column offset from cellReference column</param>
  /// <param name="rowOffset">Optional: Row offset from cellReference row</param>
  /// <returns>The Cell object, or null if not found</returns>
  public static Cell? GetCellFromReference(this Worksheet ws, string cellReference, int colOffset = 0, int rowOffset = 0)
  {
    try
    {
      CellReference cellRef = new(cellReference);
      return ws.GetCellFromReference(cellRef, colOffset, rowOffset);
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"Error in {nameof(Common)}.{nameof(GetCellFromReference)}");
      return null;
    }
  }

  /// <summary>
  /// Gets a Cell offset from a given starting Cell
  /// </summary>
  /// <param name="startCell">The starting Cell</param>
  /// <param name="colOffset">Column offset</param>
  /// <param name="rowOffset">Row offset</param>
  /// <returns>The offset Cell, or null if not found</returns>
  public static Cell? GetCellOffset(this Cell startCell, int colOffset = 0, int rowOffset = 0)
  {
    try
    {
      if ((startCell.Parent is Row row) && (row.Parent is SheetData) && (startCell.CellReference != null))
      {
        Worksheet worksheet = startCell.GetWorksheetFromCell();
        CellReference startCellReference = new(startCell.CellReference!);
        return worksheet.GetCellFromCoordinates(((int)startCellReference.ColumnIndex) + colOffset, ((int)startCellReference.RowIndex) + rowOffset);
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"Error in {nameof(Common)}.{nameof(GetCellOffset)}");
    }
    return null;
  }

  /// <summary>
  /// Gets a Cell from a Worksheet using coordinates
  /// </summary>
  /// <param name="ws">The Worksheet containing the cell</param>
  /// <param name="x">The column index of the cell</param>
  /// <param name="y">The row index of the cell</param>
  /// <param name="colOffset">Optional: Column offset to add to x</param>
  /// <param name="rowOffset">Optional: Row offset to add to y</param>
  /// <returns>The Cell object, or null if not found</returns>
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
      logger.Error(ex, "{msg}", $"Error in {nameof(Common)}.{nameof(GetCellFromCoordinates)}");
      return null;
    }
  }

  /// <summary>
  /// Gets a Cell from a SpreadsheetDocument using a named cell
  /// </summary>
  /// <param name="document">The SpreadsheetDocument containing the cell</param>
  /// <param name="cellName">The named reference of the cell</param>
  /// <param name="colOffset">Optional: Column offset from the named cell</param>
  /// <param name="rowOffset">Optional: Row offset from the named cell</param>
  /// <returns>The Cell object, or null if not found</returns>
  public static Cell? GetCellFromName(this SpreadsheetDocument document, string cellName, int colOffset = 0, int rowOffset = 0)
  {
    try
    {
      WorkbookPart workbookPart = document.WorkbookPart ?? document.AddWorkbookPart();
      return workbookPart.GetCellFromName(cellName, colOffset, rowOffset);
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"Error in {nameof(Common)}.{nameof(GetCellFromName)}");
      return null;
    }
  }

  /// <summary>
  /// Gets a Cell from a WorkbookPart using a named cell
  /// </summary>
  /// <param name="workbookPart">The WorkbookPart containing the cell</param>
  /// <param name="cellName">The named reference of the cell</param>
  /// <param name="colOffset">Optional: Column offset from the named cell</param>
  /// <param name="rowOffset">Optional: Row offset from the named cell</param>
  /// <returns>The Cell object, or null if not found</returns>
  public static Cell? GetCellFromName(this WorkbookPart workbookPart, string cellName, int colOffset = 0, int rowOffset = 0)
  {
    try
    {
      DefinedName? definedName = workbookPart.Workbook.DefinedNames?.Elements<DefinedName>().FirstOrDefault(x => x.Name == cellName) ??
                workbookPart.Workbook.DefinedNames?.Elements<DefinedName>().FirstOrDefault(x => string.Equals(x.Name?.ToString(), cellName, StringComparison.InvariantCultureIgnoreCase)); // Search invariant case if exact fails
      if (definedName != null)
      {
        string reference = definedName.Text;
        string sheetName = reference.Split('!')[0].Trim('\'');
        string cellReference = reference.Split('!')[1].Replace("$", null);

        Sheet sheet = workbookPart.Workbook.Descendants<Sheet>().First(x => x.Name == sheetName);
        WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        return worksheetPart.Worksheet.GetCellFromReference(cellReference, colOffset, rowOffset);
      }
      return null;
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"Error in {nameof(Common)}.{nameof(GetCellFromName)}");
      return null;
    }
  }

  /// <summary>
  /// Gets CellReference of named cell
  /// </summary>
  /// <param name="document">SpreadsheetDocument containing the named cell</param>
  /// <param name="cellName">The name of the cell</param>
  /// <param name="colOffset">Optional: Column offset from the named cell</param>
  /// <param name="rowOffset">Optional: Row offset from the named cell</param>
  /// <returns>The CellReference for the named cell, null if not found</returns>
  public static CellReference? GetCellReferenceFromName(this SpreadsheetDocument document, string cellName, int colOffset = 0, int rowOffset = 0)
  {
    try
    {
      WorkbookPart workbookPart = document.WorkbookPart ?? document.AddWorkbookPart();
      return workbookPart.GetCellReferenceFromName(cellName, colOffset, rowOffset);
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"Error in {nameof(Common)}.{nameof(GetCellReferenceFromName)}");
      return null;
    }
  }

  /// <summary>
  /// Gets CellReference of named cell
  /// </summary>
  /// <param name="workbookPart">WorkbookPart containing the named cell</param>
  /// <param name="cellName">The name of the cell</param>
  /// <param name="colOffset">Optional: Column offset from the named cell</param>
  /// <param name="rowOffset">Optional: Row offset from the named cell</param>
  /// <returns>The CellReference for the named cell, null if not found</returns>
  public static CellReference? GetCellReferenceFromName(this WorkbookPart workbookPart, string cellName, int colOffset = 0, int rowOffset = 0)
  {
    try
    {
      DefinedName? definedName = workbookPart.Workbook.DefinedNames?.Elements<DefinedName>().FirstOrDefault(x => x.Name == cellName) ??
                workbookPart.Workbook.DefinedNames?.Elements<DefinedName>().FirstOrDefault(x => string.Equals(x.Name?.ToString(), cellName, StringComparison.InvariantCultureIgnoreCase)); // Search invariant case if exact fails
      if (definedName != null)
      {
        string reference = definedName.Text;
        string sheetName = reference.Split('!')[0].Trim('\'');
        string cellReference = reference.Split('!')[1].Replace("$", null);

        Sheet sheet = workbookPart.Workbook.Descendants<Sheet>().First(x => x.Name == sheetName);
        WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        Cell? cell = worksheetPart.Worksheet.GetCellFromReference(cellReference, colOffset, rowOffset);
        return (cell?.CellReference != null) ? new(cell.CellReference!) : null;
      }
      return null;
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"Error in {nameof(Common)}.{nameof(GetCellReferenceFromName)}");
      return null;
    }
  }

  private static ConcurrentDictionary<string, Dictionary<string, uint>> WorkbookStandardFormatCache = [];

  /// <summary>
  /// Clears all cached standard formats for all workbooks
  /// </summary>
  public static void ClearStandardFormatCache()
  {
    WorkbookStandardFormatCache = [];
  }

  /// <summary>
  /// Clears standard format cache for specific workbook
  /// </summary>
  /// <param name="document">SpreadsheetDocument to clear in memory style references for</param>
  public static void ClearStandardFormatCacheForWorkbook(SpreadsheetDocument document)
  {
    if (document?.WorkbookPart != null)
    {
      WorkbookStandardFormatCache.TryRemove(GetWorkbookId(document), out _);
    }
  }

  private static ConcurrentDictionary<string, WorkbookStyleCache> WorkbookCustomFormatCaches = new();

  /// <summary>
  /// Clears all cached custom formats for all workbooks
  /// </summary>
  public static void ClearCustomFormatCache()
  {
    WorkbookCustomFormatCaches = [];
  }

  /// <summary>
  /// Clears custom format cache for specific workbook
  /// </summary>
  /// <param name="document">SpreadsheetDocument to clear in memory style references for</param>
  public static void ClearCustomFormatCacheForWorkbook(SpreadsheetDocument document)
  {
    if (document?.WorkbookPart != null)
    {
      WorkbookCustomFormatCaches.TryRemove(GetWorkbookId(document), out _);
    }
  }

  public static Dictionary<string, WorkbookStyleCache> GetWorkbookCustomFormatCaches()
  {
    return new(WorkbookCustomFormatCaches);
  }

  /// <summary>
  /// Gets the Stylesheet from a SpreadsheetDocument
  /// </summary>
  /// <param name="document">The SpreadsheetDocument to get the Stylesheet from</param>
  /// <param name="createIfMissing">If <see langword="true"/>, creates Stylesheet (and parent elements if necessary) if missing.</param>
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
    if ((stylesPart == null) && createIfMissing)
    {
      stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
      stylesPart.Stylesheet = new();
    }
    return stylesPart?.Stylesheet;
  }

  /// <summary>
  /// Gets the Borders from a Stylesheet
  /// </summary>
  /// <param name="stylesheet">The Stylesheet to get the Borders from</param>
  /// <param name="createIfMissing">If <see langword="true"/>, creates Borders if missing</param>
  /// <returns>The Borders object, or null if not found and createIfMissing is false</returns>
  public static Borders? GetBorders(this Stylesheet stylesheet, bool createIfMissing = true)
  {
    Borders? borders = stylesheet.Elements<Borders>().FirstOrDefault();
    if ((borders == null) && createIfMissing)
    {
      stylesheet.AddChild(new Borders());
      borders = stylesheet.Elements<Borders>().First();
      Border defaultBorder = new(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder());
      borders.Append(defaultBorder);
      borders.Count = (uint)borders.Count();
    }
    return borders;
  }

  /// <summary>
  /// Gets the Fills from a Stylesheet
  /// </summary>
  /// <param name="stylesheet">The Stylesheet to get the Fills from</param>
  /// <param name="createIfMissing">If <see langword="true"/>, creates Fills if missing</param>
  /// <returns>The Fills object, or null if not found and createIfMissing is false</returns>
  public static Fills? GetFills(this Stylesheet stylesheet, bool createIfMissing = true)
  {
    Fills? fills = stylesheet.Elements<Fills>().FirstOrDefault();
    if ((fills == null) && createIfMissing)
    {
      stylesheet.AddChild(new Fills());
      fills = stylesheet.Elements<Fills>().First();
      Fill defaultFill1 = new()
            {
                PatternFill = new()
                {
                    PatternType = PatternValues.None
                }
            };
      Fill defaultFill2 = new()
            {
                PatternFill = new()
                {
                    PatternType = PatternValues.Gray125
                }
            };
      fills.Append(defaultFill1);
      fills.Append(defaultFill2);

      // fills.Append(defaultFill3);
      fills.Count = (uint)fills.Count();
    }
    return fills;
  }

  /// <summary>
  /// Gets the Fonts from a Stylesheet
  /// </summary>
  /// <param name="stylesheet">The Stylesheet to get the Fonts from</param>
  /// <param name="createIfMissing">If <see langword="true"/>, creates Fonts if not found</param>
  /// <returns>The Fonts object, or null if not found and createIfMissing is false</returns>
  public static Fonts? GetFonts(this Stylesheet stylesheet, bool createIfMissing = true)
  {
    Fonts? fonts = stylesheet.Elements<Fonts>().FirstOrDefault();
    if ((fonts == null) && createIfMissing)
    {
      stylesheet.AddChild(new Fonts());
      fonts = stylesheet.Elements<Fonts>().First();
      Font defaultFont = new()
            {
                FontSize = new() { Val = 11 },
                Color = new() { Indexed = 8 },
                FontName = new() { Val = nameof(EFontName.Calibri) },
                FontFamilyNumbering = new() { Val = 2 },
                FontScheme = new() { Val = FontSchemeValues.Minor }
            };
      fonts.Append(defaultFont);
      fonts.Count = (uint)fonts.Count();
    }
    return fonts;
  }

  /// <summary>
  /// Gets the CellFormats from a Stylesheet.
  /// </summary>
  /// <param name="stylesheet">The Stylesheet to get the CellFormats from.</param>
  /// <param name="createIfMissing">If <see langword="true"/>, creates CellFormats if not found.</param>
  /// <returns>The CellFormats object, or null if not found and not created.</returns>
  public static CellFormats? GetCellFormats(this Stylesheet stylesheet, bool createIfMissing = true)
  {
    CellFormats? cellFormats = stylesheet.Elements<CellFormats>().FirstOrDefault();
    if ((cellFormats == null) && createIfMissing)
    {
      stylesheet.AddChild(new CellFormats());
      cellFormats = stylesheet.Elements<CellFormats>().First();
      CellFormat defaultCellFormat = new()
            {
                NumberFormatId = 0,
                FontId = 0,
                FillId = 0,
                BorderId = 0
            };
      cellFormats.Append(defaultCellFormat);
      cellFormats.Count = (uint)cellFormats.Count();
    }
    return cellFormats;
  }

  /// <summary>
  /// Creates the style corresponding to the style enum passed in and returns the ID for the style that was created
  /// </summary>
  /// <param name="document">Document to add the standard cell style to</param>
  /// <param name="style">Enum value indicating which style to create</param>
  /// <param name="cellLocked">Whether or not the cells with this style should be locked or not</param>
  /// <returns>The ID of the style that was created</returns>
  public static uint GetStandardCellStyle(this SpreadsheetDocument document, EStyle style, bool cellLocked = false, bool wrapText = false)
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
      case EStyle.Header:
        cellFormat.Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center };
        cellFormat.ApplyAlignment = true;

        border = new(new LeftBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }, new RightBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                    new TopBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }, new BottomBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin });
        borders.Append(border);
        cellFormat.BorderId = ((uint)borders.Count()) - 1;
        cellFormat.ApplyBorder = true;

        fill = new()
                {
                    PatternFill = new()
                    {
                        PatternType = PatternValues.Solid,
                        ForegroundColor = new()
                        {
                            Indexed = (int)EIndexedExcelColors.Grey25Percent
                        }
                    },
                };

        fills.Append(fill);
        cellFormat.FillId = ((uint)fills.Count()) - 1;
        cellFormat.ApplyFill = true;

        cellFormat.FontId = GetFontId(EFont.Header, fonts);
        cellFormat.ApplyFont = true;
        break;

      case EStyle.HeaderThickTop:
        cellFormat.Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center };
        cellFormat.ApplyAlignment = true;

        border = new(new LeftBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }, new RightBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                    new TopBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Medium }, new BottomBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin });
        borders.Append(border);
        cellFormat.BorderId = ((uint)borders.Count()) - 1;
        cellFormat.ApplyBorder = true;

        fill = new()
                {
                    PatternFill = new()
                    {
                        PatternType = PatternValues.Solid,
                        ForegroundColor = new()
                        {
                            Indexed = (int)EIndexedExcelColors.Grey25Percent
                        }
                    },
                };
        fills.Append(fill);
        cellFormat.FillId = ((uint)fills.Count()) - 1;
        cellFormat.ApplyFill = true;

        cellFormat.FontId = GetFontId(EFont.Header, fonts);
        cellFormat.ApplyFont = true;
        break;

      case EStyle.Body:
        cellFormat.Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center };
        cellFormat.ApplyAlignment = true;

        border = new(new LeftBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }, new RightBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                    new BottomBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin });
        borders.Append(border);
        cellFormat.BorderId = ((uint)borders.Count()) - 1;
        cellFormat.ApplyBorder = true;

        cellFormat.FontId = GetFontId(EFont.Default, fonts);
        cellFormat.ApplyFont = true;

        break;

      case EStyle.Error:
        fill = new()
                {
                    PatternFill = new()
                    {
                        PatternType = PatternValues.Solid,
                        ForegroundColor = new()
                        {
                            Indexed = (int)EIndexedExcelColors.Red
                        }
                    },
                };

        fills.Append(fill);
        cellFormat.FillId = ((uint)fills.Count()) - 1;
        cellFormat.ApplyFill = true;
        break;

      case EStyle.Blackout:
        fill = new()
                {
                    PatternFill = new()
                    {
                        PatternType = PatternValues.Solid,
                        ForegroundColor = new()
                        {
                            Indexed = (int)EIndexedExcelColors.Black
                        }
                    },
                };

        fills.Append(fill);
        cellFormat.FillId = ((uint)fills.Count()) - 1;
        cellFormat.ApplyFill = true;

        cellFormat.FontId = GetFontId(EFont.Default, fonts); //Default font is black
        cellFormat.ApplyFont = true;
        break;

      case EStyle.Whiteout:
        fill = new()
                {
                    PatternFill = new()
                    {
                        PatternType = PatternValues.Solid,
                        ForegroundColor = new()
                        {
                            Indexed = (int)EIndexedExcelColors.White
                        }
                    },
                };
        fills.Append(fill);
        cellFormat.FillId = ((uint)fills.Count()) - 1;
        cellFormat.ApplyFill = true;

        cellFormat.FontId = GetFontId(EFont.Whiteout, fonts); // White font
        break;
    }

    if (cellLocked)
    {
      cellFormat.Protection = new Protection { Locked = true };
      cellFormat.ApplyProtection = true;
    }

    if (wrapText)
    {
      if (cellFormat.Alignment != null)
      {
        cellFormat.Alignment.WrapText = true;
      }
      else
      {
        cellFormat.Alignment = new Alignment { WrapText = true };
        cellFormat.ApplyAlignment = true;
      }
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
    // cellFormat.FormatId = (uint)cellFormats.Count() - 1;
    cellFormats.Append(cellFormat);
    uint newFormatId = ((uint)cellFormats.Count()) - 1;

    lock (formatCacheLock)
    {
      formatCache[formatKey] = newFormatId;
    }

    fonts.Count = (uint)fonts.Count();
    fills.Count = (uint)fills.Count();
    borders.Count = (uint)borders.Count();
    cellFormats.Count = (uint)cellFormats.Count();

    return newFormatId;
  }

  /// <summary>
  /// Get font styling based on EFonts option
  /// </summary>
  /// <param name="fontType">Enum for preset fonts</param>
  /// <param name="fonts">Workbook the font will be used in</param>
  /// <returns>IXLFont object containing all of the styling associated with the input EFonts option</returns>
  public static uint GetFontId(EFont fontType, Fonts fonts)
  {
    Font font = new();
    switch (fontType)
    {
      case EFont.Default:
        font.FontSize = new() { Val = 10 };
        font.FontName = new() { Val = nameof(EFontName.Calibri) };
        break;

      case EFont.Header:
        font.FontSize = new() { Val = 10 };
        font.FontName = new() { Val = nameof(EFontName.Calibri) };
        font.Bold = new Bold();
        break;

      case EFont.Whiteout:
        font.FontSize = new() { Val = 10 };
        font.Color = new() { Rgb = "FFFFFF" };
        font.FontName = new() { Val = nameof(EFontName.Calibri) };
        break;
    }
    fonts.Append(font);
    fonts.Count = (uint)fonts.Count();
    return ((uint)fonts.Count()) - 1;
  }

  /// <summary>
  /// Check to see if two CellFormat objects are the same based on Fill, Font, Border, Alignment, and Protection
  /// </summary>
  /// <param name="format1">First CellFormat to compare</param>
  /// <param name="format2">Second CellFormat to compare</param>
  /// <returns><see langword="true"/> if both CellFormats share the same Fill, Font, Border, Alignment, and Protection values, otherwise false</returns>
  public static bool CellFormatsAreEqual(CellFormat format1, CellFormat format2)
  {
    // Compare relevant properties of the CellFormat objects
    return (format1.BorderId == format2.BorderId) &&
            (format1.FillId == format2.FillId) &&
            (format1.FontId == format2.FontId) &&
            (format1.ApplyBorder == format2.ApplyBorder) &&
            (format1.ApplyFill == format2.ApplyFill) &&
            (format1.ApplyFont == format2.ApplyFont) &&
            FormatAlignmentsAreEqual(format1.Alignment, format2.Alignment) &&
            FormatProtectionsAreEqual(format1.Protection, format2.Protection);
  }

  /// <summary>
  /// Check to see if two Alignment objects are identical
  /// </summary>
  /// <param name="alignment1">First Alignment to compare</param>
  /// <param name="alignment2">Second Alignment to compare</param>
  /// <returns><see langword="true"/> if both Alignment objects are the same</returns>
  public static bool FormatAlignmentsAreEqual(Alignment? alignment1, Alignment? alignment2)
  {
    if ((alignment1 == null) && (alignment2 == null))
    {
      return true;
    }

    if ((alignment1 == null) || (alignment2 == null))
    {
      return false;
    }

    return alignment1.Horizontal == alignment2.Horizontal && alignment1.WrapText == alignment2.WrapText;
  }

  /// <summary>
  /// Check to see if two Protection objects are identical
  /// </summary>
  /// <param name="protection1">First Protection to compare</param>
  /// <param name="protection2">Second Protection to compare</param>
  /// <returns><see langword="true"/> if both Protection objects are the same</returns>
  public static bool FormatProtectionsAreEqual(Protection? protection1, Protection? protection2)
  {
    if ((protection1 == null) && (protection2 == null))
    {
      return true;
    }

    if ((protection1 == null) || (protection2 == null))
    {
      return false;
    }

    return protection1.Locked == protection2.Locked;
  }

  public sealed class WorkbookStyleCache
  {
    public Dictionary<int, uint> FontCache { get; } = [];

    public Dictionary<int, uint> FillCache { get; } = [];

    public Dictionary<int, uint> BorderCache { get; } = [];

    public Dictionary<string, uint> CellFormatCache { get; } = [];
  }

  /// <summary>
  /// Create a custom style to apply in a SpreadsheetDocument
  /// </summary>
  /// <param name="document">SpreadsheetDocument to create the custom CellFormat in</param>
  /// <param name="cellLocked">Sets protection value of CellFormat</param>
  /// <param name="font">Sets Font value of CellFormat, creates if no identical font exists yet</param>
  /// <param name="alignment">Sets Alignment value of CellFormat</param>
  /// <param name="fill">Sets Fill value of CellFormat, creates if no identical fill exists yet</param>
  /// <param name="border">Sets Border value of CellFormat, creates if no identical border exists yet</param>
  /// <returns>ID of the new CellFormat</returns>
  public static uint? GetCustomStyle(this SpreadsheetDocument document, bool cellLocked = false, Font? font = null,
        HorizontalAlignmentValues? alignment = null, Fill? fill = null, Border? border = null, bool wrapText = false)
  {
    Stylesheet? stylesheet = document.GetStylesheet();

    if (stylesheet == null)
    {
      return null;
    }

    string workbookId = GetWorkbookId(document);
    WorkbookStyleCache cache = WorkbookCustomFormatCaches.GetOrAdd(workbookId, _ => new WorkbookStyleCache());

    uint fontId = GetOrAddFont(stylesheet, cache, font);
    uint fillId = GetOrAddFill(stylesheet, cache, fill);
    uint borderId = GetOrAddBorder(stylesheet, cache, border);

    // Create a unique key for the cell format
    string cellFormatKey = $"{fontId}|{fillId}|{borderId}|{alignment}|{cellLocked}|{wrapText}";

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

    if (alignment.HasValue || wrapText)
    {
      if (alignment.HasValue)
      {
        cellFormat.Alignment = new() { Horizontal = alignment };
      }

      if (wrapText)
      {
        cellFormat.Alignment ??= new Alignment();
        cellFormat.Alignment.WrapText = true;
      }

      cellFormat.ApplyAlignment = true;
    }

    if (cellLocked)
    {
      cellFormat.Protection = new() { Locked = true };
      cellFormat.ApplyProtection = true;
    }

    CellFormats? cellFormats = stylesheet.Elements<CellFormats>().FirstOrDefault();
    if (cellFormats == null)
    {
      stylesheet.AddChild(new CellFormats());
      cellFormats = stylesheet.Elements<CellFormats>().First();
    }
    cellFormats.Append(cellFormat);
    cellFormats.Count = (uint)cellFormats.Count();
    uint newFormatId = ((uint)cellFormats.Count()) - 1;
    cache.CellFormatCache[cellFormatKey] = newFormatId;

    return newFormatId;
  }

  /// <summary>
  /// Creates a new Font if font does not already exist, otherwise retrieves existing font
  /// </summary>
  /// <param name="stylesheet">Stylesheet containing the font to retrieve</param>
  /// <param name="cache">Cache containing existing styles</param>
  /// <param name="font">Font to get or create</param>
  /// <returns>ID of the new or retrieved Font</returns>
  public static uint GetOrAddFont(this Stylesheet stylesheet, WorkbookStyleCache cache, Font? font)
  {
    if (font == null)
    {
      return 0; // Default font
    }

    int fontHash = font.GetHashCode();
    if (cache.FontCache.TryGetValue(fontHash, out uint existingFontId))
    {
      return existingFontId;
    }
    Fonts? fonts = stylesheet.Elements<Fonts>().FirstOrDefault();
    if (fonts == null)
    {
      stylesheet.AddChild(new Fonts());
      fonts = stylesheet.Elements<Fonts>().First();
    }
    fonts.Append(font);
    fonts.Count = (uint)fonts.Count();
    uint newFontId = ((uint)fonts.Count()) - 1;
    cache.FontCache[fontHash] = newFontId;
    return newFontId;
  }

  /// <summary>
  /// Creates a new Fill if fill does not already exist, otherwise retrieves existing fill
  /// </summary>
  /// <param name="stylesheet">Stylesheet containing the fill to retrieve</param>
  /// <param name="cache">Cache containing existing styles</param>
  /// <param name="fill">Fill to get or create</param>
  /// <returns>ID of the new or retrieved Fill</returns>
  public static uint GetOrAddFill(this Stylesheet stylesheet, WorkbookStyleCache cache, Fill? fill)
  {
    if (fill == null)
    {
      return 0; // Default fill
    }

    int fillHash = fill.GetHashCode();
    if (cache.FillCache.TryGetValue(fillHash, out uint existingFillId))
    {
      return existingFillId;
    }

    Fills? fills = stylesheet.Elements<Fills>().FirstOrDefault();
    if (fills == null)
    {
      stylesheet.AddChild(new Fills());
      fills = stylesheet.Elements<Fills>().First();
    }
    fills.Append(fill);
    fills.Count = (uint)fills.Count();
    uint newFillId = ((uint)fills.Count()) - 1;
    cache.FillCache[fillHash] = newFillId;
    return newFillId;
  }

  /// <summary>
  /// Creates a new Border if border does not already exist, otherwise retrieves existing border
  /// </summary>
  /// <param name="stylesheet">Stylesheet containing the border to retrieve</param>
  /// <param name="cache">Cache containing existing styles</param>
  /// <param name="border">Border to get or create</param>
  /// <returns>ID of the new or retrieved Border</returns>
  public static uint GetOrAddBorder(this Stylesheet stylesheet, WorkbookStyleCache cache, Border? border)
  {
    if (border == null)
    {
      return 0; // Default border
    }

    int borderHash = border.GetHashCode();
    if (cache.BorderCache.TryGetValue(borderHash, out uint existingBorderId))
    {
      return existingBorderId;
    }

    Borders? borders = stylesheet.Elements<Borders>().FirstOrDefault();
    if (borders == null)
    {
      stylesheet.AddChild(new Borders());
      borders = stylesheet.Elements<Borders>().First();
    }
    borders.Append(border);
    borders.Count = (uint)borders.Count();
    uint newBorderId = ((uint)borders.Count()) - 1;
    cache.BorderCache[borderHash] = newBorderId;
    return newBorderId;
  }

  /// <summary>
  /// Gets hash of an OpenXmlElement
  /// </summary>
  /// <param name="element">Element to get hash of</param>
  /// <returns>Hash for the passed in OpenXmlElement</returns>
  public static int GetHashCode(this OpenXmlElement element)
  {
    return element.OuterXml.GetHashCode();
  }

  /// <summary>
  /// Gets a unique identifier for the current SpreadsheetDocument
  /// </summary>
  /// <param name="document">The SpreadsheetDocument to get a unique identifier for</param>
  /// <returns>The unique identifier for SpreadsheetDocument</returns>
  public static string GetWorkbookId(SpreadsheetDocument document)
  {
    // Use a combination of the file path (if available) and creation time
    string filePath = document.PackageProperties.Identifier ?? string.Empty;
    string creationTime = document.PackageProperties.Created?.ToString() ?? string.Empty;
    return $"{filePath}_{creationTime}";
  }

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

  /// <summary>
  /// Creates a cell in the provided sheetData, creating a row to contain it if necessary
  /// </summary>
  /// <param name="sheetData">SheetData to add cell to</param>
  /// <param name="columnIndex">Column index for the cell to create</param>
  /// <param name="rowIndex">Row index for the cell to create</param>
  /// <returns>Cell object that was created or null if sheetData is null</returns>
  [return: NotNullIfNotNull(nameof(sheetData))]
  public static Cell? InsertCell(this SheetData? sheetData, uint columnIndex, uint rowIndex, uint? styleIndex = null)
  {
    Cell? cell = null;
    if (sheetData != null)
    {
      CellReference cellReference = new(columnIndex, rowIndex);
      string cellRef = cellReference.ToString();

      // Check if the row exists, create if not
      Row? row = sheetData?.Elements<Row>().FirstOrDefault(x => (x.RowIndex != null) && (x.RowIndex == rowIndex));
      if (row == null)
      {
        row = new Row { RowIndex = rowIndex };
        sheetData!.Append(row);
      }

      // Check if the cell exists, create if not
      cell = row.Elements<Cell>().FirstOrDefault(c => string.Equals(c.CellReference?.Value, cellRef));
      if (cell == null)
      {
        // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
        Cell? refCell = null;

        foreach (Cell existingCell in row.Elements<Cell>())
        {
          CellReference existingCellReference = new(existingCell.CellReference!);
          if ((existingCellReference.ColumnIndex > cellReference.ColumnIndex) && (existingCellReference.RowIndex > cellReference.RowIndex))
          {
            refCell = existingCell; //existingCell; //cell
            break;
          }

          // Only works until column AA
                    // if (string.Compare(existingCell.CellReference?.Value, cellRef, true) > 0)
                    // {
                    // refCell = existingCell; //existingCell; //cell
                    // break;
                    // }
                }

        Cell newCell = new() { CellReference = cellRef, StyleIndex = styleIndex };
        row.InsertBefore(newCell, refCell);

        return newCell;
      }
    }
    return cell;
  }

  /// <summary>
  /// Creates a cell at the indicated column and row index if it doesn't exist, and then inserts a value into that cell
  /// </summary>
  /// <param name="worksheet">Worksheet to insert the cell and value into</param>
  /// <param name="columnIndex">X coordinate of the cell to be created and value inserted</param>
  /// <param name="rowIndex">Y coordinate of the cell to be created and value inserted</param>
  /// <param name="cellValue">The value to insert into the indicated cell</param>
  /// <param name="cellType">Optional: The type to assign to the cell that determines how it is displayed, defaults to SharedString</param>
  /// <param name="styleIndex">Optional: Index of the style to apply to the cell</param>
  public static void InsertCellValue(this Worksheet worksheet, uint columnIndex, uint rowIndex, CellValue cellValue, CellValues? cellType = null, uint? styleIndex = null)
  {
    SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
    sheetData.InsertCellValue(columnIndex, rowIndex, cellValue, cellType, styleIndex);
  }

  /// <summary>
  /// Creates a cell at the indicated column and row index if it doesn't exist, and then inserts a value into that cell
  /// </summary>
  /// <param name="sheetData">SheetData to insert the cell and value into</param>
  /// <param name="columnIndex">X coordinate of the cell to be created and value inserted</param>
  /// <param name="rowIndex">Y coordinate of the cell to be created and value inserted</param>
  /// <param name="cellValue">The value to insert into the indicated cell</param>
  /// <param name="cellType">Optional: The type to assign to the cell that determines how it is displayed, defaults to SharedString</param>
  /// <param name="styleIndex">Optional: Index of the style to apply to the cell</param>
  public static void InsertCellValue(this SheetData? sheetData, uint columnIndex, uint rowIndex, CellValue cellValue, CellValues? cellType = null, uint? styleIndex = null)
  {
    cellType ??= CellValues.SharedString; //Default to shared string since it is the most compact option
    Cell? cell = sheetData.InsertCell(columnIndex, rowIndex, styleIndex);
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
    }
  }

  /// <summary>
  /// Creates a cell at the indicated column and row index if it doesn't exist, and then inserts a formula into that cell
  /// </summary>
  /// <param name="worksheet">Worksheet to insert the cell and value into</param>
  /// <param name="columnIndex">X coordinate of the cell to be created and value inserted</param>
  /// <param name="rowIndex">Y coordinate of the cell to be created and value inserted</param>
  /// <param name="formulaString">String representation of the formula to insert into the cell</param>
  /// <param name="cellType">Optional: The type to assign to the cell that determines how it is displayed, defaults to String</param>
  /// <param name="styleIndex">Optional: Index of the style to apply to the cell</param>
  public static void InsertCellFormula(this Worksheet worksheet, uint columnIndex, uint rowIndex, string formulaString, CellValues? cellType = null, uint? styleIndex = null)
  {
    SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
    sheetData.InsertCellFormula(columnIndex, rowIndex, formulaString, cellType, styleIndex);
  }

  /// <summary>
  /// Creates a cell at the indicated column and row index if it doesn't exist, and then inserts a formula into that cell
  /// </summary>
  /// <param name="sheetData">SheetData to insert the cell and value into</param>
  /// <param name="columnIndex">X coordinate of the cell to be created and value inserted</param>
  /// <param name="rowIndex">Y coordinate of the cell to be created and value inserted</param>
  /// <param name="formulaString">String representation of the formula to insert into the cell</param>
  /// <param name="cellType">Optional: The type to assign to the cell that determines how it is displayed, defaults to String</param>
  /// <param name="styleIndex">Optional: Index of the style to apply to the cell</param>
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

  /// <summary>
  /// Creates a SharedStringItem with the specified text and inserts it into the SharedStringTablePart. If the item already exists, returns its index.
  /// </summary>
  /// <param name="workbook">Workbook to insert the SharedString into</param>
  /// <param name="text">Text of the SharedString to be created</param>
  /// <returns>Index of the SharedString item</returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static int InsertSharedStringItem(this Workbook workbook, string text)
  {
    // If the part does not contain a SharedStringTable, create one.
    SharedStringTablePart shareStringTablePart = workbook.WorkbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault() ?? workbook.WorkbookPart?.AddNewPart<SharedStringTablePart>() ?? throw new InvalidOperationException("The WorkbookPart is missing.");
    shareStringTablePart.SharedStringTable ??= new();

    int i = 0;

    // Iterate through all the items in the SharedStringTable. If the text already exists, return its index.
    foreach (SharedStringItem item in shareStringTablePart.SharedStringTable.Elements<SharedStringItem>())
    {
      if (string.Equals(item.InnerText, text))
      {
        return i;
      }

      i++;
    }

    // The text does not exist in the part. Create the SharedStringItem and return its index.
    shareStringTablePart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
    shareStringTablePart.SharedStringTable.Save();

    return i;
  }

  /// <summary>
  /// Create a table for the specified sheet in worksheet
  /// </summary>
  /// <param name="worksheet">Worksheet to add table to</param>
  /// <param name="startRow">One based index of the first row of the table.</param>
  /// <param name="startCol">One based index of the first column of the table.</param>
  /// <param name="endRow">One based index of the last row of the table.</param>
  /// <param name="endColumn">One based index of the last column of the table.</param>
  /// <param name="tableName">Name of the table to add</param>
  /// <param name="styleName">Optional: Style to use for table, defaults to TableStyleMedium1</param>
  /// <param name="showRowStripes">Optional: Styles the table to show row stripes or not</param>
  /// <param name="showColStripes">Optional: Styles the table to show column stripes or not</param>
  public static void CreateTable(this Worksheet worksheet, uint startRow, uint startCol, uint endRow, uint endColumn, string tableName, ETableStyle styleName = ETableStyle.TableStyleMedium1,
        bool showRowStripes = true, bool showColStripes = false)
  {
    TableDefinitionPart tableDefinitionPart = worksheet.WorksheetPart!.AddNewPart<TableDefinitionPart>();
    string rId = worksheet.WorksheetPart!.GetIdOfPart(tableDefinitionPart);

    // Check if TableParts element exists, if not create it
    TableParts? tableParts = worksheet.Elements<TableParts>().FirstOrDefault();
    if (tableParts == null)
    {
      tableParts = new TableParts();
      worksheet.Append(tableParts);
    }

    TablePart tablePart = new() { Id = rId };
    tableParts.Append(tablePart);
    tableParts.Count = (uint)tableParts.Count();

    TableColumns tableColumns = new() { Count = (endColumn - startCol) + 1 };
    for (uint i = 0; i < (endColumn - startCol) + 1; i++)
    {
      // Use the content of the first row as column names
      Cell headerCell = worksheet.GetCellFromCoordinates(((int)i) + 1, 1)!;
      string cellValue = headerCell.GetCellValue();
      string columnName = cellValue ?? $"Column{i + 1}";

      tableColumns.Append(new TableColumn { Id = i + 1, Name = columnName });
    }

    string tableRef = $"{new CellReference(startCol, startRow)}:{new CellReference(endColumn, endRow)}";
    tableDefinitionPart.Table = new()
    {
      Id = tableParts.Count,
      Name = tableName,
      DisplayName = tableName,
      Reference = tableRef,
      TotalsRowShown = false,
      HeaderRowCount = 1,
      InsertRow = false,
      InsertRowShift = false,
      Published = false,
      AutoFilter = new AutoFilter() { Reference = tableRef },
      TableColumns = tableColumns,
      TableStyleInfo = new()
            {
                Name = styleName.ToString(),
                ShowFirstColumn = false,
                ShowLastColumn = false,
                ShowRowStripes = showRowStripes,
                ShowColumnStripes = showColStripes
            }
    };
  }

  /// <summary>
  /// Adds auto filter to a range of cells in the worksheet
  /// </summary>
  /// <param name="worksheet">Worksheet to add auto filter to</param>
  /// <param name="startRow">First row of auto filtered range (usually headers)</param>
  /// <param name="startColumn">First column of auto filtered range</param>
  /// <param name="endRow">Last row of auto filtered range</param>
  /// <param name="endColumn">Last column of auto filtered range</param>
  public static void SetAutoFilter(this Worksheet worksheet, uint startRow, uint startColumn, uint endRow, uint endColumn)
  {
    worksheet.Append(new AutoFilter() { Reference = $"{new CellReference(startColumn, startRow)}:{new CellReference(endColumn, endRow)}" });
  }

  /// <summary>
  /// Adds images into a workbook at the designated named ranges
  /// </summary>
  /// <param name="document">SpreadsheetDocument to insert images into</param>
  /// <param name="imageData">Image byte array</param>
  /// <param name="cellName">Named range to insert image at</param>
  public static void AddImage(this SpreadsheetDocument document, byte[] imageData, string cellName)
  {
    document.AddImages([ imageData ], [ cellName ]);
  }

  /// <summary>
  /// Adds images into a workbook at the designated named ranges
  /// </summary>
  /// <param name="document">SpreadsheetDocument to insert images into</param>
  /// <param name="imageData">List of image byte arrays. Must be equal in length to cellNames parameter</param>
  /// <param name="cellNames">List of named ranges to insert images at. Must be equal in length to imageData parameter</param>
  public static void AddImages(this SpreadsheetDocument document, List<byte[]> imageData, List<string> cellNames)
  {
    WorkbookPart? workbookPart = document.WorkbookPart;
    if ((workbookPart != null) && (imageData.Count > 0) && (cellNames.Count > 0) && (imageData.Count == cellNames.Count))
    {
      WorksheetPart? worksheetPart = null;
      DrawingsPart? drawingsPart = null;

      uint? cellStyleIndex = document.GetCustomStyle(font: new() { FontSize = new() { Val = 11 }, FontName = new() { Val = nameof(EFontName.Calibri) } });
      for (int i = 0; i < imageData.Count; i++)
      {
        if ((imageData[i].Length > 0) && !string.IsNullOrWhiteSpace(cellNames[i]))
        {
          CellReference? cellReference = GetCellReferenceFromName(workbookPart, cellNames[i]);
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

            if ((worksheetPart != null) && (drawingsPart != null) && (cellStyleIndex != null))
            {
              worksheetPart.AddImagePart(drawingsPart, GetMergedCellArea(worksheetPart, cellReference), (uint)cellStyleIndex, imageData[i]);
            }
          }
        }
      }
    }
  }

  /// <summary>
  /// Adds images into a workbook at the designated named ranges
  /// </summary>
  /// <param name="document">SpreadsheetDocument to insert images into</param>
  /// <param name="imageData">Image byte array</param>
  /// <param name="mergedCellArea">Tuple defining the first (top left) and last (bottom right) cell of the range to insert the image into</param>
  public static void AddImage(this SpreadsheetDocument document, byte[] imageData, (CellReference FirstCell, CellReference LastCell) mergedCellArea)
  {
    document.AddImages([ imageData ], [ mergedCellArea ]);
  }

  /// <summary>
  /// Adds images into a workbook at the designated named ranges
  /// </summary>
  /// <param name="document">SpreadsheetDocument to insert images into</param>
  /// <param name="imageData">List of image byte arrays. Must be equal in length to cellNames parameter</param>
  /// <param name="mergedCellAreas">
  /// List of tuples defining the first (top left) and last (bottom right) cell of the range to insert the images into. Must be equal in length to imageData parameter
  /// </param>
  public static void AddImages(this SpreadsheetDocument document, List<byte[]> imageData, List<(CellReference FirstCell, CellReference LastCell)> mergedCellAreas)
  {
    WorkbookPart? workbookPart = document.WorkbookPart;
    if ((workbookPart != null) && (imageData.Count > 0) && (mergedCellAreas.Count > 0) && (imageData.Count == mergedCellAreas.Count))
    {
      WorksheetPart? worksheetPart = null;
      DrawingsPart? drawingsPart = null;

      uint? cellStyleIndex = document.GetCustomStyle(font: new() { FontSize = new() { Val = 11 }, FontName = new() { Val = nameof(EFontName.Calibri) } });
      for (int i = 0; i < imageData.Count; i++)
      {
        if (imageData[i].Length > 0)
        {
          (CellReference FirstCell, CellReference LastCell) mergedCellArea = mergedCellAreas[i];
          CellReference? cellReference = mergedCellArea.FirstCell;
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

            if ((worksheetPart != null) && (drawingsPart != null) && (cellStyleIndex != null))
            {
              worksheetPart.AddImagePart(drawingsPart, mergedCellArea, (uint)cellStyleIndex, imageData[i]);
            }
          }
        }
      }
    }
  }

  public static void AddImagePart(this WorksheetPart worksheetPart, DrawingsPart drawingsPart, (CellReference FirstCell, CellReference LastCell) mergedCellArea, uint cellStyleIndex, byte[] imageData)
  {
    // Set cells to standard font to ensure cell sizes are gotten correctly
    Cell? cell = worksheetPart.Worksheet.GetCellFromReference(mergedCellArea.FirstCell);
    if (cell != null)
    {
      cell.StyleIndex = cellStyleIndex;
    }
    using Image image = Image.Load(imageData);
    int imgWidthPx = image.Width;
    int imgHeightPx = image.Height;

    decimal imgAspect = ((decimal)imgWidthPx) / imgHeightPx;

    int rangeWidthPx = GetRangeWidthInPx(worksheetPart, mergedCellArea);
    int rangeHeightPx = GetRangeHeightInPx(worksheetPart, mergedCellArea);
    decimal rangeAspect = ((decimal)rangeWidthPx) / rangeHeightPx;

    decimal scale = (rangeAspect < imgAspect) ? ((rangeWidthPx - 3m) / imgWidthPx) : ((rangeHeightPx - 3m) / imgHeightPx);

    int resizeWidth = (int)Math.Round(imgWidthPx * scale, 0, MidpointRounding.ToZero);
    int resizeHeight = (int)Math.Round(imgHeightPx * scale, 0, MidpointRounding.ToZero);
    int xMargin = (int)Math.Round(((rangeWidthPx - resizeWidth) * 9525) / 2.0m, 0, MidpointRounding.ToZero);
    int yMargin = (int)Math.Round(((rangeHeightPx - resizeHeight) * 9525 * 1.75m) / 2.0m, 0, MidpointRounding.ToZero);

    ImagePart imagePart = drawingsPart.AddImagePart(ImagePartType.Png);
    using (MemoryStream stream = new(imageData))
    {
      imagePart.FeedData(stream);
    }

    AddImageToWorksheet(drawingsPart, drawingsPart.GetIdOfPart(imagePart), mergedCellArea.FirstCell, mergedCellArea.LastCell, xMargin, yMargin, resizeWidth, resizeHeight);
  }

  /// <summary>
  /// Get existing DrawingsPart from WorksheetPart, or create it if it doesn't exist
  /// </summary>
  /// <param name="worksheetPart">WorksheetPart to get or create DrawingsPart in</param>
  /// <returns>The DrawingsPart that was retrieved or created</returns>
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

  /// <summary>
  /// Get the first and last CellReference of a merged cell area
  /// </summary>
  /// <param name="worksheetPart">WorksheetPart containing the merged cell area</param>
  /// <param name="cellReference">Cell reference for the merged cell area</param>
  /// <returns>Cell references for the first (top left) and last (bottom right) cell</returns>
  public static (CellReference FirstCell, CellReference LastCell) GetMergedCellArea(this WorksheetPart worksheetPart, CellReference cellReference)
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
          if ((cellReference.RowIndex >= start.RowIndex) && (cellReference.RowIndex <= end.RowIndex) &&
                        (cellReference.ColumnIndex >= start.ColumnIndex) && (cellReference.ColumnIndex <= end.ColumnIndex))
          {
            return (start, end);
          }
        }
      }
    }

    return (cellReference, cellReference);
  }

  /// <summary>
  /// Get the width of a range in pixels
  /// </summary>
  /// <param name="worksheetPart">WorksheetPart containing the range being measured</param>
  /// <param name="range">The range being measured</param>
  /// <returns>The width of the specified range in EMU</returns>
  public static int GetRangeWidthInPx(WorksheetPart worksheetPart, (CellReference start, CellReference end) range)
  {
    Worksheet worksheet = worksheetPart.Worksheet;
    Columns? columns = worksheet.Elements<Columns>().FirstOrDefault();

    double totalWidthChars = 0;
    for (uint colIndex = range.start.ColumnIndex; colIndex <= range.end.ColumnIndex; colIndex++)
    {
      Column? column = worksheet.GetOrCreateColumn(colIndex, columns: columns);
      double columnWidthChars = column?.Width ?? 8.43; // Default column width (# in characters)
      totalWidthChars += columnWidthChars; //(int)Math.Truncate((columnWidthChars *  7 + 5) / 7 * 256)/ 256;
    }

    return (int)Math.Round(totalWidthChars * 7, 0, MidpointRounding.ToZero);
  }

  /// <summary>
  /// Get the height of a range in pixels
  /// </summary>
  /// <param name="worksheetPart">WorksheetPart containing the range being measured</param>
  /// <param name="range">The range being measured</param>
  /// <returns>The height of the specified range in EMU</returns>
  public static int GetRangeHeightInPx(WorksheetPart worksheetPart, (CellReference start, CellReference end) range)
  {
    Worksheet worksheet = worksheetPart.Worksheet;

    int totalHeight = 0;
    for (uint rowIndex = range.start.RowIndex; rowIndex <= range.end.RowIndex; rowIndex++)
    {
      Row? row = worksheet.GetRow(rowIndex);
      if (row == null)
      {
        SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
        row = new Row { RowIndex = rowIndex };
        sheetData!.Append(row);
      }
      double rowHeight = row?.Height ?? 14.2; // Default row height for Calibri 11pt
      totalHeight += (int)((rowHeight * 12700) / 9525); // Convert to EMUs (1 point = 12700 EMUs), then to pixels (1 pixel = 9525 EMUs)
    }

    return totalHeight;
  }

  /// <summary>
  /// Inserts image into the DrawingsPart of an Excel file
  /// </summary>
  /// <param name="drawingsPart">DrawingsPart of the Excel file to insert an image into</param>
  /// <param name="relationshipId">The relationship ID for the image</param>
  /// <param name="fromCell">CellReference for top left corner of range</param>
  /// <param name="toCell">CellReference for bottom right corner of range</param>
  /// <param name="xMargin">Margin at the right and left of the image</param>
  /// <param name="yMargin">Margin at the top and bottom of the image</param>
  /// <param name="width">Width of the image</param>
  /// <param name="height">Height of the image</param>
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
                ColumnId = new Xdr.ColumnId { Text = (fromCell.ColumnIndex - 1).ToString() },
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
            new Xdr.NonVisualDrawingProperties { Id = imageId, Name = $"Picture {imageId}" },
            new Xdr.NonVisualPictureDrawingProperties(new Dwg.PictureLocks { NoChangeAspect = true }));

    Xdr.BlipFill blipFill = new(
            new Dwg.Blip { Embed = relationshipId },
            new Dwg.Stretch(new Dwg.FillRectangle()));

    Xdr.ShapeProperties spPr = new(
            new Dwg.Transform2D(
                new Dwg.Offset { X = 0, Y = 0 },
                new Dwg.Extents { Cx = width, Cy = height }),
            new Dwg.PresetGeometry { Preset = Dwg.ShapeTypeValues.Rectangle });

    picture.Append(nvPicPr, blipFill, spPr);
    anchor.Append(picture);
    anchor.Append(new Xdr.ClientData());

    worksheetDrawing.Append(anchor);
  }

  /// <summary>
  /// Gets a range of Cells from a Worksheet
  /// </summary>
  /// <param name="sheet">The Worksheet to get the range from</param>
  /// <param name="range">The range in A1:B2 format</param>
  /// <returns>An IEnumerable of Cells in the specified range</returns>
  public static IEnumerable<Cell?> GetRange(this Worksheet sheet, string range)
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

  /// <summary>
  /// Reads tabular data from an unformatted excel sheet to a DataTable object using OpenXML
  /// </summary>
  /// <param name="fileStream">Stream of Excel file being read</param>
  /// <param name="hasHeaders">
  /// Does the data being read have headers. Will be used for data table column names instead of default 'Column0', 'Column1'... if true. If no headers specified, first row of data must have a value for
  /// all columns in order to read all columns correctly.
  /// </param>
  /// <param name="sheetName">Name of sheet to read data from. Will use lowest index sheet if not specified.</param>
  /// <param name="startCellReference">Top left corner containing data to read. Will use A1 if not specified.</param>
  /// <param name="endCellReference">Bottom right cell containing data to read. Will read to first full empty row if not specified.</param>
  /// <returns><see cref="DataTable"/> representation of the data read from the excel file</returns>
  public static DataTable ReadExcelFileToDataTable(this Stream fileStream, bool hasHeaders = true, string? sheetName = null, string? startCellReference = null, string? endCellReference = null)
  {
    DataTable dataTable = new();

    try
    {
      fileStream.Position = 0;
      using SpreadsheetDocument document = SpreadsheetDocument.Open(fileStream, false);
      WorkbookPart? workbookPart = document.WorkbookPart;
      Sheet? sheet = document.GetSheetByName(sheetName);

      if ((sheet != null) && (workbookPart != null))
      {
        WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        Worksheet worksheet = worksheetPart.Worksheet;
        SheetData sheetData = worksheet.GetFirstChild<SheetData>() ?? new();

        // Determine start and end cells
        CellReference startCell = new(startCellReference ?? "A1");
        CellReference endCell = (endCellReference != null) ? new(endCellReference) : sheetData.GetLastPopulatedCell();

        // Add columns to DataTable
        for (uint col = startCell.ColumnIndex; col <= endCell.ColumnIndex; col++)
        {
          string columnName = hasHeaders ? sheetData.GetCellValue(startCell.RowIndex, col) : ($"Column{col - startCell.ColumnIndex}");
          dataTable.Columns.Add(columnName);
        }

        // Add rows to DataTable
        uint dataStartRow = hasHeaders ? (startCell.RowIndex + 1) : startCell.RowIndex;
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

  /// <summary>
  /// Gets the last populated cell from SheetData
  /// </summary>
  /// <param name="sheetData">SheetData to get last populated cell from</param>
  /// <returns>Cell reference of the last populated cell</returns>
  public static CellReference GetLastPopulatedCell(this SheetData sheetData)
  {
    uint maxRow = sheetData.Elements<Row>().Max(x => x.RowIndex?.Value ?? 0);
    uint maxCol = sheetData.Descendants<Cell>().Where(x => x != null).Max(x => new CellReference(x.CellReference!).ColumnIndex);
    return new CellReference(maxCol, maxRow);
  }

  /// <summary>
  /// Gets the string value of a cell
  /// </summary>
  /// <param name="sheetData">SheetData containing cell value to be read</param>
  /// <param name="row">Row index of cell to get value of</param>
  /// <param name="col">Column index of cell to get value of</param>
  /// <returns>String value of the indicated cell</returns>
  public static string GetCellValue(this SheetData sheetData, uint row, uint col)
  {
    CellReference cellRef = new(col, row);
    Cell? cell = sheetData.Elements<Row>().FirstOrDefault(x => (x.RowIndex != null) && (x.RowIndex == row))?
                    .Elements<Cell>().FirstOrDefault(x => (x.CellReference != null) && string.Equals(new CellReference(x.CellReference!).ToString(), cellRef.ToString(), StringComparison.OrdinalIgnoreCase));

    return cell?.GetCellValue() ?? string.Empty;
  }

  /// <summary>
  /// Gets the string value of a cell
  /// </summary>
  /// <param name="sheetData">SheetData containing cell value to be read</param>
  /// <param name="cellReference">CellReference of cell to get value of</param>
  /// <returns>String value of the indicated cell</returns>
  public static string GetCellValue(this SheetData sheetData, CellReference cellReference)
  {
    return sheetData.GetCellValue(cellReference.RowIndex, cellReference.ColumnIndex);
  }

  /// <summary>
  /// Gets the string value of a cell
  /// </summary>
  /// <param name="worksheet">Worksheet that contains the cell to get value of</param>
  /// <param name="cellReference">CellReference to read string value from</param>
  /// <returns>String value of the cell</returns>
  public static string GetCellValue(this Worksheet worksheet, CellReference cellReference)
  {
    Cell? cell = worksheet.GetCellFromCoordinates((int)cellReference.ColumnIndex, (int)cellReference.RowIndex);
    return cell?.GetCellValue() ?? string.Empty;
  }

  /// <summary>
  /// Gets the string value of a cell
  /// </summary>
  /// <param name="cell">Cell to read string value from</param>
  /// <returns>String value of the cell</returns>
  public static string GetCellValue(this Cell? cell)
  {
    if (cell?.CellValue == null)
    {
      return string.Empty;
    }

    string value = cell.CellValue.Text;

    // If this is a shared string, look up the actual value
    if ((cell.DataType != null) && (cell.DataType.Value == CellValues.SharedString))
    {
      SharedStringTablePart? sharedStringPart = cell.GetWorkbookFromCell().WorkbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
      if (sharedStringPart != null)
      {
        return sharedStringPart.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
      }
    }

    return value;
  }

  /// <summary>
  /// Gets the stylized string value of a cell
  /// </summary>
  /// <param name="worksheet">Worksheet that contains the cell to get value of</param>
  /// <param name="cellReference">CellReference to read stylized string value from</param>
  /// <returns>Stylized string value of a cell</returns>
  public static string? GetStringValue(this Worksheet worksheet, CellReference cellReference)
  {
    Cell? cell = worksheet.GetCellFromCoordinates((int)cellReference.ColumnIndex, (int)cellReference.RowIndex);
    return cell?.GetStringValue();
  }

  /// <summary>
  /// Gets the stylized string value of a cell
  /// </summary>
  /// <param name="cell">Cell to read stylized string value from</param>
  /// <returns>Stylized string value of a cell</returns>
  public static string? GetStringValue(this Cell? cell)
  {
    if (cell == null)
    {
      return null;
    }

    if (cell.DataType != null)
    {
      string cellDataType = cell.DataType.Value.ToString();
      if (string.Equals(cellDataType, CellValues.SharedString.ToString()))
      {
        Worksheet worksheet = cell.GetWorksheetFromCell();
        Workbook workbook = worksheet.GetWorkbookFromWorksheet();
        SharedStringTablePart? stringTable = workbook.WorkbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
        if (stringTable != null)
        {
          return stringTable.SharedStringTable.ElementAt(int.Parse(cell.InnerText)).InnerText;
        }
      }
      else if (string.Equals(cellDataType, CellValues.Boolean.ToString()))
      {
        return string.Equals(cell.InnerText, "1") ? "TRUE" : "FALSE";
      }
      else if (string.Equals(cellDataType, CellValues.Error.ToString()))
      {
        return $"ERROR: {cell.InnerText}";
      }
      else // (cellDataType == CellValues.Number.ToString() || cellDataType == CellValues.String.ToString() || cellDataType == CellValues.InlineString.ToString())
      {
        return cell.InnerText;
      }
    }

    return cell.InnerText;
  }

  /// <summary>
  /// Reads an Excel table into a DataTable object using OpenXML
  /// </summary>
  /// <param name="fileStream">Stream of Excel file being read</param>
  /// <param name="tableName">Name of table to read. If not specified, this function will read the first table it finds in the workbook</param>
  /// <returns><see cref="DataTable"/> object containing the data read from Excel stream</returns>
  public static DataTable ReadExcelTableToDataTable(this Stream fileStream, string? tableName = null, CancellationToken cancellationToken = default)
  {
    DataTable dataTable = new();

    try
    {
      fileStream.Position = 0;
      using SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(fileStream, false);
      WorkbookPart? workbookPart = spreadsheetDocument.WorkbookPart ?? throw new InvalidOperationException("The workbook part is missing.");
      Table? table = workbookPart.FindTable(tableName) ?? throw new InvalidOperationException($"Table '{tableName ?? string.Empty}' not found.");
      Sheet? sheet = spreadsheetDocument.GetSheetForTable(table);
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
          dataTable.Columns.Add(headerCell.GetStringValue() ?? $"Column{(col - startCell.ColumnIndex) + 1}");
        }
      }

      // Get body data
      for (uint row = startCell.RowIndex + 1; row <= endCell.RowIndex; row++)
      {
        cancellationToken.ThrowIfCancellationRequested();
        DataRow dataRow = dataTable.NewRow();
        for (uint col = startCell.ColumnIndex; col <= endCell.ColumnIndex; col++)
        {
          Cell? cell = worksheet.GetCellFromCoordinates((int)col, (int)row);
          if (cell != null)
          {
            dataRow[((int)col) - ((int)startCell.ColumnIndex)] = cell.GetStringValue();
          }
        }
        dataTable.Rows.Add(dataRow);
      }
      spreadsheetDocument.Dispose();
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"Unable to read excel table data. Location {nameof(Common)}.{nameof(ReadExcelTableToDataTable)}");
    }

    return dataTable;
  }

  public static Sheet? GetSheetForTable(this SpreadsheetDocument document, Table table)
  {
    WorkbookPart? workbookPart = document.WorkbookPart;
    if (workbookPart == null)
    {
      return null;
    }

    // Find the WorksheetPart containing the Table
    foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
    {
      foreach (TableDefinitionPart tableDefPart in worksheetPart.TableDefinitionParts)
      {
        if (tableDefPart.Table == table)
        {
          // Get the relationship Id for this WorksheetPart
          string relId = workbookPart.GetIdOfPart(worksheetPart);

          // Find the Sheet with this relationship Id
          return workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(x => x.Id != null && x.Id == relId);
        }
      }
    }
    return null;
  }

  /// <summary>
  /// Get table by table name
  /// </summary>
  /// <param name="workbookPart">WorkbookPart containing table.</param>
  /// <param name="tableName">Name of table to retrieve</param>
  /// <returns>Table indicated by tableName, null if not found</returns>
  public static Table? FindTable(this WorkbookPart workbookPart, string? tableName)
  {
    foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
    {
      if (worksheetPart.TableDefinitionParts != null)
      {
        foreach (TableDefinitionPart tableDefinitionPart in worksheetPart.TableDefinitionParts)
        {
          if ((tableName == null) || (tableDefinitionPart.Table.Name == tableName))
          {
            return tableDefinitionPart.Table;
          }
        }
      }
    }
    return null;
  }

  /// <summary>
  /// Gets specified row from worksheet
  /// </summary>
  /// <param name="worksheet">Worksheet to retrieve the row from</param>
  /// <param name="rowIndex">Index of the row to retrieve</param>
  /// <returns>Row from worksheet specified by rowIndex</returns>
  public static Row? GetRow(this Worksheet worksheet, uint rowIndex)
  {
    return worksheet.GetFirstChild<SheetData>()?.Elements<Row>().FirstOrDefault(r => (r.RowIndex != null) && (r.RowIndex == rowIndex));
  }

  /// <summary>
  /// Gets cell at a particular column index from a row
  /// </summary>
  /// <param name="row">Row to get cell from</param>
  /// <param name="columnIndex">Column index of cell in the row</param>
  /// <returns>Cell specified from the row and column index</returns>
  public static Cell? GetCell(this Row row, uint columnIndex)
  {
    if (row.RowIndex == null)
    {
      return null;
    }

    string cellReference = new CellReference(columnIndex, row.RowIndex.Value).ToString();
    return row.Elements<Cell>().FirstOrDefault(c => c.CellReference == cellReference);
  }

  /// <summary>
  /// Automatically adjusts column widths to fit the content in each column
  /// </summary>
  /// <param name="worksheet">The worksheet to adjust columns in</param>
  /// <param name="maxWidth">Optional maximum width limit for columns</param>
  public static void AutoFitColumns(this Worksheet worksheet, double maxWidth = 100)
  {
    SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
    if (sheetData == null)
    {
      return;
    }

    // Dictionary to store maximum width of each column
    ConcurrentDictionary<uint, double> columnWidths = [];

    // Iterate through all rows and cells
    foreach (Row row in sheetData.Elements<Row>())
    {
      foreach (Cell cell in row.Elements<Cell>())
      {
        if (string.IsNullOrEmpty(cell.CellReference?.Value))
        {
          continue;
        }

        CellReference cellRef = new(cell.CellReference!.Value!);
        uint columnIndex = cellRef.ColumnIndex - 1;

        // Calculate the width needed for this cell
        double width = cell.CalculateWidth();

        // Update maximum width for this column if necessary
        if (!columnWidths.TryGetValue(columnIndex, out double value) || (width > value))
        {
          value = Math.Min(width, maxWidth);
          columnWidths[columnIndex] = value;
        }
      }
    }

    // Create or get Columns element
    Columns columns = worksheet.GetColumns();

    // Set the width for each column
    foreach (KeyValuePair<uint, double> kvp in columnWidths)
    {
      Column col = new()
            {
                Min = kvp.Key + 1, // Add 1 because CellReference uses 0-based column indices
                Max = kvp.Key + 1,
                Width = kvp.Value,
                CustomWidth = true
            };

      columns.Append(col);
    }
  }

  /// <summary>
  /// Gets the Columns object from the worksheet, creating it if it does not exist yet
  /// </summary>
  /// <param name="worksheet">Worksheet to get columns from</param>
  /// <returns>The Columns object from the worksheet</returns>
  public static Columns GetColumns(this Worksheet worksheet)
  {
    Columns? columns = worksheet.GetFirstChild<Columns>();
    if (columns == null)
    {
      columns = new Columns();
      worksheet.InsertAt(columns, 0);
    }
    return columns;
  }

  /// <summary>
  /// Fits a single column to the size indicated by columnWidth
  /// </summary>
  /// <param name="worksheet">Worksheet containing the column to size</param>
  /// <param name="colIndex">1 based column Index to fit</param>
  /// <param name="columnWidth">Width to set for the specified column</param>
  /// <param name="columns">Optional: Worksheet Columns object to prevent re-getting it on every call</param>
  public static void SizeColumn(this Worksheet worksheet, uint colIndex, double columnWidth, Columns? columns = null)
  {
    columns ??= worksheet.GetColumns();

    Column? col = columns.Elements<Column>().FirstOrDefault(c => (colIndex >= (c.Min?.Value ?? 0)) && (colIndex <= (c.Max?.Value ?? 0)));
    if (col == null) //Create new column
    {
      worksheet.GetOrCreateColumn(colIndex, columnWidth, columns);
    }
    else
    {
      // Existing column, just need to add width attributes
      col.Width = columnWidth;
      col.CustomWidth = true;
    }
  }

  /// <summary>
  /// Gets or creates a single column
  /// </summary>
  /// <param name="worksheet">Worksheet containing the column to size</param>
  /// <param name="colIndex">1 based column Index to fit</param>
  /// <param name="columnWidth">Optional: Width to set for the specified column</param>
  /// <param name="columns">Optional: Worksheet Columns object to prevent re-getting it on every call</param>
  public static Column? GetOrCreateColumn(this Worksheet worksheet, uint colIndex, double? columnWidth = null, Columns? columns = null)
  {
    columns ??= worksheet.GetColumns();

    Column? col = columns.Elements<Column>().FirstOrDefault(c => (colIndex >= (c.Min?.Value ?? 0)) && (colIndex <= (c.Max?.Value ?? 0)));
    Column? newCol = null;
    if (col == null) //Create new column
    {
      // Columns must be in sequential order according to CellReference. Determine where to insert the new cell.
      Column? refCol = null;

      foreach (Column existingCol in columns.Elements<Column>())
      {
        if (existingCol.Min?.Value > colIndex)
        {
          refCol = existingCol; //col
        }
      }

      newCol = new()
            {
                Min = colIndex,
                Max = colIndex
            };

      if (columnWidth != null)
      {
        newCol.CustomWidth = true;
        newCol.Width = columnWidth;
      }

      columns.InsertBefore(newCol, refCol);
    }
    return col ?? newCol;
  }

  /// <summary>
  /// Calculate the width of a cell based on the provided text
  /// </summary>
  /// <param name="cell">Cell to calculate the width of</param>
  /// <returns>Fitted width of the cell</returns>
  public static double CalculateWidth(this Cell cell)
  {
    return CalculateWidth(cell.GetCellValue(), cell.StyleIndex?.Value);
  }

  /// <summary>
  /// Calculate the width of a cell based on the provided text
  /// </summary>
  /// <param name="text">The text value of the cell</param>
  /// <param name="styleIndex">Style index value associated with the cell</param>
  /// <returns>Fitted width of the cell</returns>
  public static double CalculateWidth(string text, uint? styleIndex = null)
  {
    if (string.IsNullOrEmpty(text))
    {
      return 0;
    }

    const int padding = 1; // Extra padding
    uint[] numberStyles = [ 5, 6, 7, 8 ]; //styles that will add extra chars
    uint[] boldStyles = [ 1, 2, 3, 4, 6, 7, 8 ]; //styles that will bold
    double width = text.Length + padding;

    // Add extra width for numbers to account for digit grouping
    if (double.TryParse(text, out _))
    {
      width++;
    }

    if ((styleIndex != null) && numberStyles.Contains((uint)styleIndex))
    {
      int thousandCount = (int)Math.Truncate(width / 4);

      // add 3 for '.00'
      width += 3 + thousandCount;
    }

    if ((styleIndex != null) && boldStyles.Contains((uint)styleIndex))
    {
      // add an extra char for bold - not 100% acurate but good enough for what i need.
      width++;
    }

    const double maxCharWidth = 5; // Calibri 11pt is 7, but 5 seemed to work ok
    return Math.Truncate((((width * maxCharWidth) + 5) / maxCharWidth) * 256) / 256;
  }

  // Helper classes to deal with cell references more easily
  public partial class CellReference
  {
    [GeneratedRegex(@"([A-Z]+)(\d+)")]
    private static partial Regex CellRefRegex();

    private uint _RowIndex;

    public uint RowIndex
    {
      get => _RowIndex;
      set
      {
        if ((value < 1) || (value > 1048576))
        {
          throw new ArgumentOutOfRangeException(nameof(value), "RowIndex must be between 1 and 1048576");
        }
        _RowIndex = value;
      }
    }

    private uint _ColumnIndex;

    public uint ColumnIndex
    {
      get => _ColumnIndex;
      set
      {
        if ((value < 1) || (value > 16384))
        {
          throw new ArgumentOutOfRangeException(nameof(value), "RowIndex must be between 1 and 16384");
        }
        _ColumnIndex = value;
      }
    }

    public CellReference(string reference)
    {
      Match match = CellRefRegex().Match(reference.ToUpperInvariant());
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

  /// <summary>
      /// Get the 1 based column number for the column name provided (1 = A)
      /// </summary>
      /// <param name="columnName">The column name to get the 1 based (1 = A) column index of</param>
      /// <returns>The 1 based column index (1 = A) corresponding to the value of columnName</returns>
    public static uint ColumnNameToNumber(string columnName)
    {
      uint number = 0;
      for (int i = 0; i < columnName.Length; i++)
      {
        number *= 26;
        number += (uint)((columnName[i] - 'A') + 1);
      }
      return number;// - 1;
    }

  /// <summary>
      /// Get the column name corresponding to the provided 1 based column number (A = 1)
      /// </summary>
      /// <param name="columnNumber">1 based column number (A = 1) to get name of</param>
      /// <returns>Column name corresponding to the value of columnNumber</returns>
    public static string NumberToColumnName(uint columnNumber)
    {
      int number = ((int)columnNumber) - 1; //Make this 1 based to avoid confusion
      string columnName = string.Empty;
      while (number >= 0)
      {
        int remainder = number % 26;
        columnName = $"{Convert.ToChar('A' + remainder)}{columnName}";
        number = (number / 26) - 1;
        if (number < 0)
        {
          break;
        }
      }
      return columnName;
    }
  }
}
