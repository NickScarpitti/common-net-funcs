using CommonNetFuncs.Excel.Common;
using CommonNetFuncs.Excel.OpenXml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data;
using static CommonNetFuncs.Excel.OpenXml.Common;

namespace Excel.OpenXml.Tests;

public sealed class CommonTests
{
    [Fact]
    public void InitializeExcelFile_ShouldCreateNewSheet()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        // Act
        uint sheetId = document.InitializeExcelFile("Test Sheet");

        // Assert
        sheetId.ShouldBeGreaterThan(0u);
        document.WorkbookPart.ShouldNotBeNull();
        document.WorkbookPart!.Workbook.Sheets?.Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Custom Sheet")]
    public void CreateNewSheet_ShouldCreateSheetWithCorrectName(string? sheetName)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        // Act
        uint sheetId = document.CreateNewSheet(sheetName);

        // Assert
        sheetId.ShouldBeGreaterThan(0u);
        Sheet sheet = document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>().First();
        sheet.Name!.Value.ShouldBe(sheetName ?? $"Sheet{sheetId}");
    }

    [Fact]
    public void GetWorksheetByName_ShouldReturnCorrectWorksheet()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        const string sheetName = "Test Sheet";
        document.CreateNewSheet(sheetName);

        // Act
        Worksheet? worksheet = document.GetWorksheetByName(sheetName);

        // Assert
        worksheet.ShouldNotBeNull();
        Sheet sheet = document.WorkbookPart!.Workbook.Descendants<Sheet>()
            .First(s => s.Name == sheetName);
        sheet.ShouldNotBeNull();
    }

    [Fact]
    public void GetWorksheetById_ShouldReturnCorrectWorksheet()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        uint sheetId = document.CreateNewSheet("Test Sheet");

        // Act
        Worksheet? worksheet = document.GetWorksheetById(sheetId);

        // Assert
        worksheet.ShouldNotBeNull();
    }

    [Fact]
    public void GetWorksheetFromCell_ShouldReturnCorrectWorksheet()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        Cell cell = worksheet.InsertCell(1, 1)!;

        // Act
        Worksheet result = cell.GetWorksheetFromCell();

        // Assert
        result.ShouldBe(worksheet);
    }

    [Fact]
    public void InsertCell_ShouldCreateNewCell()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Act
        Cell? cell = worksheet.InsertCell(1, 1);

        // Assert
        cell.ShouldNotBeNull();
        cell.CellReference!.Value.ShouldBe("A1");
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("B2")]
    [InlineData("Z10")]
    public void CellReference_ShouldParseCorrectly(string reference)
    {
        // Act
        CellReference cellRef = new(reference);

        // Assert
        cellRef.ToString().ShouldBe(reference);
    }

    [Theory]
    [InlineData(1u, "A")]
    [InlineData(26u, "Z")]
    [InlineData(27u, "AA")]
    [InlineData(52u, "AZ")]
    [InlineData(53u, "BA")]
    public void NumberToColumnName_ShouldConvertCorrectly(uint columnNumber, string expectedName)
    {
        // Act
        string result = CellReference.NumberToColumnName(columnNumber);

        // Assert
        result.ShouldBe(expectedName);
    }

    [Theory]
    [InlineData("A", 1u)]
    [InlineData("Z", 26u)]
    [InlineData("AA", 27u)]
    [InlineData("AZ", 52u)]
    [InlineData("BA", 53u)]
    public void ColumnNameToNumber_ShouldConvertCorrectly(string columnName, uint expectedNumber)
    {
        // Act
        uint result = CellReference.ColumnNameToNumber(columnName);

        // Assert
        result.ShouldBe(expectedNumber);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetCustomStyle_ShouldCreateAndReturnStyleIndex(bool repeatStyle)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        Font font = new() { FontSize = new() { Val = 11 } };
        Fill fill = new() { PatternFill = new() { PatternType = PatternValues.Solid } };
        Border border = new() { LeftBorder = new() { Style = BorderStyleValues.Thin } };

        // Act
        uint? styleIndex = document.GetCustomStyle(cellLocked: true, font: font, alignment: HorizontalAlignmentValues.Center, fill: fill, border: border);

        uint? style2Index = repeatStyle ? document.GetCustomStyle(cellLocked: true, font: font, alignment: HorizontalAlignmentValues.Center, fill: fill, border: border) :
            document.GetCustomStyle(cellLocked: true, font: font, alignment: HorizontalAlignmentValues.General, fill: fill, border: new() { LeftBorder = new() { Style = BorderStyleValues.Thick } });

        // Assert
        styleIndex.ShouldNotBeNull();
        style2Index.ShouldNotBeNull();

        styleIndex.Value.ShouldBe(0u);

        if (repeatStyle)
        {
            style2Index.ShouldBe(styleIndex);
        }
        else
        {
            style2Index.ShouldNotBe(styleIndex);
            ((uint)style2Index).ShouldBeGreaterThan(0u);
        }
    }

    [Fact]
    public void CreateTable_ShouldCreateTableInWorksheet()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Act
        worksheet.CreateTable(1, 1, 2, 2, "TestTable");

        // Assert
        TableParts? tableParts = worksheet.Elements<TableParts>().FirstOrDefault();
        tableParts.ShouldNotBeNull();
        tableParts.Count?.Value.ShouldBe(1u);
    }

    [Fact]
    public void AutoFitColumns_ShouldAdjustColumnWidths()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        worksheet.InsertCellValue(1, 1, new CellValue("Test"), CellValues.String);

        // Act
        worksheet.AutoFitColumns();

        // Assert
        Columns? columns = worksheet.Elements<Columns>().FirstOrDefault();
        columns.ShouldNotBeNull();
        columns!.Elements<Column>().Count().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ReadExcelFileToDataTable_ShouldReturnPopulatedDataTable()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        worksheet.InsertCellValue(1, 1, new CellValue("Header"), CellValues.String);
        worksheet.InsertCellValue(1, 2, new CellValue("Data"), CellValues.String);
        document.Save();
        memoryStream.Flush();
        memoryStream.Position = 0;

        // Act
        DataTable result = memoryStream.ReadExcelFileToDataTable(true);

        // Assert
        result.Columns.Count.ShouldBe(1);
        result.Rows.Count.ShouldBe(1);
        result.Rows[0][0].ShouldBe("Data");
    }

    [Fact]
    public void GetSheetDataFromDocument_ShouldReturnEmptySheetData_WhenSheetNotFound()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        // Act
        SheetData result = document.GetSheetDataFromDocument("NonExistentSheet");

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(0);
    }

    [Theory]
    [InlineData("A1", "B2", true)]
    [InlineData("A1", "A1", true)]
    [InlineData("Z10", "AA15", true)]
    public void GetRange_ShouldReturnCorrectCells(string start, string end, bool createCells)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        if (createCells)
        {
            worksheet.InsertCellValue(1, 1, new CellValue("Test"), CellValues.String);
        }

        // Act
        IEnumerable<Cell?> cells = worksheet.GetRange($"{start}:{end}");

        // Assert
        cells.ShouldNotBeNull();
        cells.Count().ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(EStyle.Header, true)]
    [InlineData(EStyle.HeaderThickTop, false)]
    [InlineData(EStyle.Body, true)]
    [InlineData(EStyle.Error, false)]
    [InlineData(EStyle.Blackout, true)]
    [InlineData(EStyle.Whiteout, false)]
    public void GetStandardCellStyle_ShouldCreateCorrectStyle(EStyle style, bool cellLocked)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        // Act
        uint styleId = document.GetStandardCellStyle(style, cellLocked);

        // Assert
        styleId.ShouldBeGreaterThanOrEqualTo(0u);
    }

    [Theory]
    [InlineData(EFont.Default)]
    [InlineData(EFont.Header)]
    [InlineData(EFont.Whiteout)]
    public void GetFontId_ShouldReturnCorrectId(EFont fontType)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        Stylesheet stylesheet = document.GetStylesheet()!;
        Fonts fonts = stylesheet.GetFonts()!;

        // Act
        uint fontId = GetFontId(fontType, fonts);

        // Assert
        fontId.ShouldBeGreaterThanOrEqualTo(0u);
    }

    [Fact]
    public void ClearFormatCaches_ShouldClearAllCaches()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.GetStandardCellStyle(EStyle.Header, false);
        document.GetCustomStyle(font: new Font { FontSize = new FontSize { Val = 12 } });

        // Act
        ClearStandardFormatCache();
        ClearCustomFormatCache();

        // Assert
        // Verify by creating new styles - they should start from beginning
        uint newStyleId = document.GetStandardCellStyle(EStyle.Header, false);
        newStyleId.ShouldBe(3u);
    }

    [Fact]
    public void FormatProtectionsAreEqual_ShouldCompareCorrectly()
    {
        // Arrange
        Protection? protection1 = new() { Locked = true };
        Protection? protection2 = new() { Locked = true };
        Protection? protection3 = new() { Locked = false };

        // Act & Assert
        FormatProtectionsAreEqual(protection1, protection2).ShouldBeTrue();
        FormatProtectionsAreEqual(protection1, protection3).ShouldBeFalse();
        FormatProtectionsAreEqual(null, null).ShouldBeTrue();
        FormatProtectionsAreEqual(protection1, null).ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetWorksheetByName_ShouldHandleNonExistentSheet(bool createMissingSheet)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        if (!createMissingSheet)
        {
            // Create a workbook part without any sheets to prevent error being thrown
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new();
        }

        const string nonExistentSheet = "NonExistentSheet";

        // Act
        Worksheet? worksheet = document.GetWorksheetByName(nonExistentSheet, createMissingSheet);

        // Assert
        if (!createMissingSheet)
        {
            worksheet.ShouldBeNull();
        }
        else
        {
            worksheet.ShouldNotBeNull();
        }
    }

    [Fact]
    public void GetWorksheetByName_ShouldThrowWhewnNonExistentWorkbookPart()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        const string nonExistentSheet = "NonExistentSheet";

        // Act / Assert
        Should.Throw<ArgumentException>(() => document.GetWorksheetByName(nonExistentSheet, false));
    }

    [Fact]
    public void InsertCellFormula_ShouldCreateFormulaCell()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Act
        worksheet.InsertCellFormula(1, 1, "SUM(A2:A10)");

        // Assert
        Cell? cell = worksheet.GetCellFromCoordinates(1, 1);
        cell.ShouldNotBeNull();
        cell!.CellFormula.ShouldNotBeNull();
        cell.CellFormula!.Text.ShouldBe("SUM(A2:A10)");
    }

    [Fact]
    public void CalculateWidth_ShouldHandleVariousInputs()
    {
        // Arrange
        const string text1 = "Short";
        const string text2 = "This is a much longer text that needs more width";
        const string text3 = "123456.78"; // Numeric text

        // Act
        double width1 = CalculateWidth(text1);
        double width2 = CalculateWidth(text2);
        double width3 = CalculateWidth(text3, 5); // Style 5 is numeric

        // Assert
        width2.ShouldBeGreaterThan(width1);
        width3.ShouldBeGreaterThan(width1);
    }

    [Fact]
    public void SetAutoFilter_ShouldCreateAutoFilter()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Act
        worksheet.SetAutoFilter(1, 1, 10, 5);

        // Assert
        AutoFilter? autoFilter = worksheet.Elements<AutoFilter>().FirstOrDefault();
        autoFilter.ShouldNotBeNull();
        autoFilter!.Reference!.Value.ShouldBe("A1:E10");
    }

    [Fact]
    public void GetMergedCellArea_ShouldHandleMergedAndUnmergedCells()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Create a merged cell range
        MergeCells mergeCells = new();
        worksheet.Append(mergeCells);
        mergeCells.Append(new MergeCell { Reference = "A1:B2" });

        CellReference cellInMerge = new("A1");
        CellReference cellNotInMerge = new("C3");

        // Act
        (CellReference firstCell, CellReference lastCell) = worksheet.WorksheetPart!.GetMergedCellArea(cellInMerge);
        (CellReference unMergedFirstCell, CellReference unMergedLastCell) = worksheet.WorksheetPart!.GetMergedCellArea(cellNotInMerge);

        // Assert
        firstCell.ToString().ShouldBe("A1");
        lastCell.ToString().ShouldBe("B2");
        unMergedFirstCell.ToString().ShouldBe(cellNotInMerge.ToString());
        unMergedLastCell.ToString().ShouldBe(cellNotInMerge.ToString());
    }

    [Fact]
    public void CellReference_ShouldHandleInvalidValues()
    {
        // Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new CellReference(0, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => new CellReference(16385, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => new CellReference(1, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => new CellReference(1, 1048577));
    }

    [Fact]
    public void GetStylesheet_ShouldCreateStylesheetIfMissing()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        // Act
        Stylesheet? stylesheet = document.GetStylesheet(true);

        // Assert
        stylesheet.ShouldNotBeNull();
        document.WorkbookPart!.WorkbookStylesPart.ShouldNotBeNull();
    }

    [Fact]
    public void InsertSharedStringItem_ShouldHandleDuplicates()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

        document.CreateNewSheet("Test Sheet");
        Workbook workbook = document.WorkbookPart!.Workbook;
        const string text = "Duplicate Text";

        // Act
        int firstIndex = workbook.InsertSharedStringItem(text);
        int secondIndex = workbook.InsertSharedStringItem(text);

        // Assert
        firstIndex.ShouldBe(secondIndex);
    }

    [Fact]
    public void GetWorkbookFromCell_ShouldReturnWorkbook()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        Cell cell = worksheet.InsertCell(1, 1)!;

        // Act
        Workbook workbook = cell.GetWorkbookFromCell();

        // Assert
        workbook.ShouldNotBeNull();
        workbook.ShouldBe(document.WorkbookPart!.Workbook);
    }

    [Fact]
    public void GetWorkbookFromWorksheet_ShouldReturnWorkbook()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Act
        Workbook workbook = worksheet.GetWorkbookFromWorksheet();

        // Assert
        workbook.ShouldNotBeNull();
        workbook.ShouldBe(document.WorkbookPart!.Workbook);
    }

    [Fact]
    public void GetWorksheetPartByCellReference_ShouldReturnCorrectPart()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        CellReference cellRef = new("A1");

        // Act
        WorksheetPart? result = GetWorksheetPartByCellReference(document.WorkbookPart!, cellRef);

        // Assert
        result.ShouldNotBeNull();
        result.Worksheet.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void IsCellEmpty_ShouldReturnTrue_ForEmptyCells(string? value)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        Cell cell = worksheet.InsertCell(1, 1)!;
        if (value != null)
        {
            cell.CellValue = new CellValue(value);
        }

        // Act
        bool isEmpty = cell.IsCellEmpty();

        // Assert
        isEmpty.ShouldBeTrue();
    }

    [Fact]
    public void GetSheetDataFromDocument_ShouldReturnSheetData_WhenSheetExists()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        const string sheetName = "Test Sheet";
        document.CreateNewSheet(sheetName);

        // Act
        SheetData sheetData = document.GetSheetDataFromDocument(sheetName);

        // Assert
        sheetData.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("A1", 0, 0)]
    [InlineData("B2", 1, 1)]
    [InlineData("Z10", -1, -1)]
    public void GetCellFromReference_ShouldHandleOffsets(string reference, int colOffset, int rowOffset)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Act
        Cell? cell = worksheet.GetCellFromReference(reference, colOffset, rowOffset);

        // Assert
        cell.ShouldNotBeNull();
        CellReference cellRef = new(cell.CellReference!);
        CellReference originalRef = new(reference);
        cellRef.ColumnIndex.ShouldBe((uint)(originalRef.ColumnIndex + colOffset));
        cellRef.RowIndex.ShouldBe((uint)(originalRef.RowIndex + rowOffset));
    }

    [Theory]
    [InlineData(1, 1, 0, 1)]
    [InlineData(1, 1, 1, 0)]
    [InlineData(2, 2, -1, -1)]
    public void GetCellOffset_ShouldReturnOffsetCell(uint startCol, uint startRow, int colOffset, int rowOffset)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        Cell startCell = worksheet.InsertCell(startCol, startRow)!;

        // Act
        Cell? offsetCell = startCell.GetCellOffset(colOffset, rowOffset);

        // Assert
        offsetCell.ShouldNotBeNull();
        CellReference cellRef = new(offsetCell.CellReference!);
        cellRef.ColumnIndex.ShouldBe((uint)(startCol + colOffset));
        cellRef.RowIndex.ShouldBe((uint)(startRow + rowOffset));
    }

    [Fact]
    public void GetCellFromName_ShouldReturnCell_WhenNameExists()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        const string cellName = "TestName";
        document.WorkbookPart!.Workbook.DefinedNames = new DefinedNames(new DefinedName { Name = cellName, Text = "Test Sheet!A1" });

        // Act
        Cell? cell = document.GetCellFromName(cellName);

        // Assert
        cell.ShouldNotBeNull();
        cell.CellReference!.Value.ShouldBe("A1");
    }

    [Fact]
    public void GetCellReferenceFromName_ShouldReturnReference_WhenNameExists()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        const string cellName = "TestName";
        document.WorkbookPart!.Workbook.DefinedNames = new DefinedNames(new DefinedName { Name = cellName, Text = "Test Sheet!A1" });

        // Act
        CellReference? cellRef = document.GetCellReferenceFromName(cellName);

        // Assert
        cellRef.ShouldNotBeNull();
        cellRef.ToString().ShouldBe("A1");
    }

    [Fact]
    public void ClearStandardFormatCacheForWorkbook_ShouldClearCache()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.GetStandardCellStyle(EStyle.Header, false);
        string workbookId = GetWorkbookId(document);

        // Act
        ClearStandardFormatCacheForWorkbook(document);

        // Assert
        GetWorkbookCustomFormatCaches().ContainsKey(workbookId).ShouldBeFalse();
    }

    [Fact]
    public void ClearCustomFormatCacheForWorkbook_ShouldClearCache()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.GetCustomStyle(font: new Font { FontSize = new FontSize { Val = 12 } });
        string workbookId = GetWorkbookId(document);

        // Act
        ClearCustomFormatCacheForWorkbook(document);

        // Assert
        GetWorkbookCustomFormatCaches().ContainsKey(workbookId).ShouldBeFalse();
    }

    [Fact]
    public void AddImage_WithByteArray_ShouldAddImageToWorkbook()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        const string cellName = "TestName";
        document.WorkbookPart!.Workbook.DefinedNames = new DefinedNames(new DefinedName { Name = cellName, Text = "Test Sheet!A1" });

        byte[] imageData = File.ReadAllBytes("TestData/test.png");

        // Act
        document.AddImage(imageData, cellName);

        // Assert
        WorksheetPart worksheetPart = document.WorkbookPart!.WorksheetParts.First();
        worksheetPart.DrawingsPart.ShouldNotBeNull();
        worksheetPart.DrawingsPart!.ImageParts.Count().ShouldBe(1);
    }

    [Fact]
    public void AddImages_WithByteArrayList_ShouldAddImagesToWorkbook()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        const string cellName = "TestName";
        document.WorkbookPart!.Workbook.DefinedNames = new DefinedNames(new DefinedName { Name = cellName, Text = "Test Sheet!A1" });
        List<byte[]> imageData = [File.ReadAllBytes("TestData/test.png")];
        List<string> cellNames = [cellName];

        // Act
        document.AddImages(imageData, cellNames);

        // Assert
        WorksheetPart worksheetPart = document.WorkbookPart!.WorksheetParts.First();
        worksheetPart.DrawingsPart.ShouldNotBeNull();
        worksheetPart.DrawingsPart!.ImageParts.Count().ShouldBe(1);
    }

    [Theory]
    [InlineData("Test")]
    [InlineData("123")]
    [InlineData("")]
    public void GetCellValue_ShouldReturnCorrectValue(string value)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        Cell cell = worksheet.InsertCell(1, 1)!;
        cell.CellValue = new CellValue(value);

        // Act
        string result = cell.GetCellValue();

        // Assert
        result.ShouldBe(value);
    }

    [Theory]
    [InlineData("Test", nameof(CellValues.String))]
    [InlineData("TRUE", nameof(CellValues.Boolean))]
    [InlineData("ERROR", nameof(CellValues.Error))]
    [InlineData("Test")]
    public void GetStringValue_ShouldReturnFormattedValue(string value, string? cellType = null)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        Cell cell = worksheet.InsertCell(1, 1)!;
        cell.CellValue = new CellValue(value);
        if (!string.IsNullOrEmpty(cellType))
        {
            cell.DataType = new EnumValue<CellValues>(Enum.Parse<CellValues>(cellType));
        }

        // Act
        string? result = cell.GetStringValue();

        // Assert
        result.ShouldNotBeNull();
        if (cellType == CellValues.Boolean.ToString())
        {
            result.ShouldBe((value == "1") ? "TRUE" : "FALSE");
        }
        else if (cellType == CellValues.Error.ToString())
        {
            result.ShouldStartWith("ERROR:");
        }
        else
        {
            result.ShouldBe(value);
        }
    }

    [Fact]
    public void ReadExcelTableToDataTable_ShouldReadTableData()
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Create a table
        worksheet.InsertCellValue(1, 1, new CellValue("Header1"), CellValues.String);
        worksheet.InsertCellValue(1, 2, new CellValue("Data1"), CellValues.String);
        worksheet.CreateTable(1, 1, 2, 1, "TestTable");

        // Act
        DataTable result = memoryStream.ReadExcelTableToDataTable("TestTable");

        // Assert
        result.Columns.Count.ShouldBe(1);
        result.Rows.Count.ShouldBe(1);
        result.Rows[0][0].ToString().ShouldBe("Data1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("TestTable")]
    public void FindTable_ShouldReturnCorrectTable(string? tableName)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;
        worksheet.CreateTable(1, 1, 2, 1, tableName ?? "DefaultTable");

        // Act
        Table? table = document.WorkbookPart!.FindTable(tableName);

        // Assert
        table.ShouldNotBeNull();
        if (tableName != null)
        {
            table.Name!.Value.ShouldBe(tableName);
        }
    }

    [Theory]
    [InlineData(1u, 100.0)]
    [InlineData(2u, 50.0)]
    public void SizeColumn_ShouldSetColumnWidth(uint colIndex, double width)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Act
        worksheet.SizeColumn(colIndex, width);

        // Assert
        Column? column = worksheet.GetOrCreateColumn(colIndex);
        column.ShouldNotBeNull();
        column.Width!.Value.ShouldBe(width);
        column.CustomWidth!.Value.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    public void GetOrCreateColumn_ShouldCreateNewColumn(uint colIndex)
    {
        // Arrange
        using MemoryStream memoryStream = new();
        using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
        document.CreateNewSheet("Test Sheet");
        Worksheet worksheet = document.GetWorksheetByName("Test Sheet")!;

        // Act
        Column? column = worksheet.GetOrCreateColumn(colIndex);

        // Assert
        column.ShouldNotBeNull();
        column.Min!.Value.ShouldBe(colIndex);
        column.Max!.Value.ShouldBe(colIndex);
    }
}
