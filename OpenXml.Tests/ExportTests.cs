using System.Data;
using CommonNetFuncs.Excel.OpenXml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Excel.OpenXml.Tests;

public sealed class ExportTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly MemoryStream _memoryStream;

    public ExportTests()
    {
        _fixture = new Fixture();
        _memoryStream = new MemoryStream();
    }

    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _memoryStream.Dispose();
            }
            disposed = true;
        }
    }

    ~ExportTests()
    {
        Dispose(false);
    }

    public class TestModel
    {
        public required string Name { get; set; }

        public required int Age { get; set; }

        public string? Description { get; set; }
    }

    [Fact]
    public void GenericExcelExport_WithValidList_ShouldCreateExcelFile()
    {
        // Arrange
        List<TestModel> testData = _fixture.CreateMany<TestModel>(3).ToList();

        // Act
        MemoryStream? result = testData.GenericExcelExport();

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
        WorkbookPart? workbookPart = doc.WorkbookPart;
        workbookPart.ShouldNotBeNull();

        WorksheetPart? worksheetPart = workbookPart.WorksheetParts.FirstOrDefault();
        worksheetPart.ShouldNotBeNull();

        Worksheet worksheet = worksheetPart.Worksheet;
        SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
        sheetData.ShouldNotBeNull();

        // Verify headers
        Row firstRow = sheetData.Elements<Row>().First();
        firstRow.Elements<Cell>().Count().ShouldBe(3); // Name, Age, Description
    }

    [Theory]
    [InlineData(true, "CustomTable")]
    [InlineData(false, "Data")]
    public void GenericExcelExport_WithTableFormatting_ShouldRespectTableSettings(bool createTable, string tableName)
    {
        // Arrange
        List<TestModel> testData = _fixture.CreateMany<TestModel>(2).ToList();

        // Act
        MemoryStream? result = testData.GenericExcelExport(
            createTable: createTable,
            tableName: tableName);

        // Assert
        result.ShouldNotBeNull();
        using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
        WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
        worksheetPart.ShouldNotBeNull();

        if (createTable)
        {
            TableDefinitionPart? tableDefinitionPart = worksheetPart.TableDefinitionParts.FirstOrDefault();
            tableDefinitionPart.ShouldNotBeNull();
            tableDefinitionPart.Table.Name?.Value.ShouldBe(tableName);
        }
    }

    [Fact]
    public void GenericExcelExport_WithDataTable_ShouldCreateExcelFile()
    {
        // Arrange
        DataTable dataTable = new("TestTable");
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Value", typeof(int));

        dataTable.Rows.Add("Test1", 1);
        dataTable.Rows.Add("Test2", 2);

        // Act
        MemoryStream? result = dataTable.GenericExcelExport();

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
        WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
        worksheetPart.ShouldNotBeNull();

        SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        sheetData.ShouldNotBeNull();
        sheetData.Elements<Row>().Count().ShouldBe(3); // Header + 2 data rows
    }

    //[Fact]
    //public void AddGenericTable_WithValidData_ShouldAddNewSheet()
    //{
    //    // Arrange
    //    List<TestModel> testData = _fixture.CreateMany<TestModel>(2).ToList();
    //    using SpreadsheetDocument doc = SpreadsheetDocument.Create(_memoryStream, SpreadsheetDocumentType.Workbook);
    //    doc.AddWorkbookPart();

    //    // Act
    //    bool result = doc.AddGenericTable(testData, "TestSheet");

    //    // Assert
    //    result.ShouldBeTrue();
    //    doc.WorkbookPart.ShouldNotBeNull();
    //    doc.WorkbookPart.Workbook.Sheets?.Elements<Sheet>().Count().ShouldBe(1);
    //}

    [Fact]
    public void ExportFromTable_WithSkipColumns_ShouldOmitSpecifiedColumns()
    {
        // Arrange
        List<TestModel> testData = _fixture.CreateMany<TestModel>(2).ToList();
        List<string> skipColumns = new() { "Description" };

        using SpreadsheetDocument doc = SpreadsheetDocument.Create(_memoryStream, SpreadsheetDocumentType.Workbook);
        doc.AddWorkbookPart();
        uint sheetId = doc.InitializeExcelFile("TestSheet");
        Worksheet? worksheet = doc.GetWorksheetById(sheetId);
        worksheet.ShouldNotBeNull();

        // Act
        bool result = Export.ExportFromTable(doc, worksheet, testData, skipColumnNames: skipColumns);

        // Assert
        result.ShouldBeTrue();
        SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
        sheetData.ShouldNotBeNull();

        Row headerRow = sheetData.Elements<Row>().First();
        headerRow.Elements<Cell>().Count().ShouldBe(2); // Only Name and Age
    }

    [Fact]
    public void ExportFromTable_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        List<TestModel> testData = _fixture.CreateMany<TestModel>(100).ToList();
        using CancellationTokenSource cts = new();
        using SpreadsheetDocument doc = SpreadsheetDocument.Create(_memoryStream, SpreadsheetDocumentType.Workbook);
        doc.AddWorkbookPart();
        uint sheetId = doc.InitializeExcelFile("TestSheet");
        Worksheet? worksheet = doc.GetWorksheetById(sheetId);
        worksheet.ShouldNotBeNull();

        // Act & Assert
        cts.Cancel();
        Should.Throw<OperationCanceledException>(() => Export.ExportFromTable(doc, worksheet, testData, cancellationToken: cts.Token));
    }
}
