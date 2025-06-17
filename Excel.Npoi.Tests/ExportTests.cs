using CommonNetFuncs.Excel.Npoi;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using System.Data;

namespace Excel.Npoi.Tests;

public class ExportTests
{
    private readonly IFixture _fixture;

    public ExportTests() { _fixture = new Fixture(); }

    public class TestData
    {
        public string StringProperty { get; set; } = string.Empty;

        public int IntProperty { get; set; }

        public DateTime DateProperty { get; set; }
    }

    [Theory]
    [InlineData(true, "TestSheet", "TestTable")]
    [InlineData(false, "Data", "Data")]
    public async Task GenericExcelExport_WithValidList_ShouldReturnMemoryStream(bool createTable, string sheetName, string tableName)
    {
        // Arrange
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();
        List<string> skipColumnNames = new() { "DateProperty" };

        // Act
        MemoryStream? result = await testData.GenericExcelExport(memoryStream: null, createTable: createTable, sheetName: sheetName, tableName: tableName, skipColumnNames: skipColumnNames);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GenericExcelExport_WithEmptyList_ShouldReturnEmptyMemoryStream()
    {
        // Arrange
        List<TestData> emptyList = new();

        // Act
        await using MemoryStream? result = await emptyList.GenericExcelExport();

        // Assert
        result.ShouldNotBeNull();
        using XSSFWorkbook wb = new(result);
        wb.NumberOfSheets.ShouldBe(1);
        ISheet sheet = wb.GetSheetAt(0);
        sheet.LastRowNum.ShouldBe(0); // No data rows, only header
        sheet.SheetName.ShouldBe("Data");
    }

    [Fact]
    public async Task GenericExcelExport_WithDataTable_ShouldReturnMemoryStream()
    {
        // Arrange
        DataTable dataTable = new();
        dataTable.Columns.Add("Column1", typeof(string));
        dataTable.Columns.Add("Column2", typeof(int));
        dataTable.Rows.Add("Value1", 1);
        dataTable.Rows.Add("Value2", 2);

        // Act
        MemoryStream? result = await dataTable.GenericExcelExport(createTable: true, sheetName: "TestSheet", tableName: "TestTable");

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AddGenericTable_WithGenericList_ShouldAddDataToWorkbook()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();

        // Act
        bool result = workbook.AddGenericTable(testData, "TestSheet", createTable: true, tableName: "TestTable");

        // Assert
        result.ShouldBeTrue();
        workbook.GetSheet("TestSheet").ShouldNotBeNull();
    }

    [Fact]
    public void AddGenericTable_WithDataTable_ShouldAddDataToWorkbook()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        DataTable dataTable = new();
        dataTable.Columns.Add("Column1", typeof(string));
        dataTable.Columns.Add("Column2", typeof(int));
        dataTable.Rows.Add("Value1", 1);

        // Act
        bool result = workbook.AddGenericTable(dataTable, "TestSheet", createTable: true, tableName: "TestTable");

        // Assert
        result.ShouldBeTrue();
        workbook.GetSheet("TestSheet").ShouldNotBeNull();
    }

    [Fact]
    public void ExcelExport_WithGenericList_ShouldExportDataCorrectly()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        ISheet sheet = workbook.CreateSheet();
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();

        // Act
        bool result = testData.ExcelExport(workbook, sheet, createTable: true, tableName: "TestTable");

        // Assert
        result.ShouldBeTrue();
        sheet.LastRowNum.ShouldBe(3); // Header row + 3 data rows
    }

    [Fact]
    public void ExcelExport_WithDataTable_ShouldExportDataCorrectly()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        ISheet sheet = workbook.CreateSheet();
        DataTable dataTable = new();
        dataTable.Columns.Add("Column1", typeof(string));
        dataTable.Columns.Add("Column2", typeof(int));
        dataTable.Rows.Add("Value1", 1);
        dataTable.Rows.Add("Value2", 2);

        // Act
        bool result = dataTable.ExcelExport(workbook, sheet, createTable: true, tableName: "TestTable");

        // Assert
        result.ShouldBeTrue();
        sheet.LastRowNum.ShouldBe(2); // Header row + 2 data rows
    }

    [Fact]
    public void AddGenericTable_WithDuplicateSheetNames_ShouldCreateUniqueNames()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        List<TestData> testData = _fixture.CreateMany<TestData>(2).ToList();

        // Act
        bool result1 = workbook.AddGenericTable(testData, "TestSheet");
        bool result2 = workbook.AddGenericTable(testData, "TestSheet");
        bool result3 = workbook.AddGenericTable(testData, "TestSheet");

        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeTrue();
        result3.ShouldBeTrue();
        workbook.GetSheet("TestSheet").ShouldNotBeNull();
        workbook.GetSheet("TestSheet (1)").ShouldNotBeNull();
        workbook.GetSheet("TestSheet (2)").ShouldNotBeNull();
    }

    [Fact]
    public async Task GenericExcelExport_WithCancellation_ShouldHandleCancellation()
    {
        // Arrange
        List<TestData> testData = _fixture.CreateMany<TestData>(100).ToList();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await testData.GenericExcelExport(cancellationToken: cts.Token));
    }

    // Add these test methods to the existing ExportTests class

    [Fact]
    public void ExcelExport_WithMaximumColumnWidth_ShouldHandleWidthLimits()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        ISheet sheet = workbook.CreateSheet();
        List<TestData> testData = new()
        {
            new() { StringProperty = new string('X', 1000), IntProperty = 1, DateProperty = DateTime.Now }
        };

        // Act
        bool result = testData.ExcelExport(
            workbook,
            sheet,
            createTable: false);

        // Assert
        result.ShouldBeTrue();
        sheet.GetColumnWidth(0).ShouldBeLessThanOrEqualTo(Export.MaxCellWidthInExcelUnits);
    }

    [Fact]
    public void ExcelExport_WithNullValues_ShouldHandleNullsGracefully()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        ISheet sheet = workbook.CreateSheet();
        List<TestData?> testData = new()
        {
            null,
            new() { StringProperty = null!, IntProperty = 1, DateProperty = DateTime.Now },
            new() { StringProperty = "Test", IntProperty = 2, DateProperty = DateTime.Now }
        };

        // Act
        bool result = testData.ExcelExport(workbook, sheet, createTable: true, tableName: "TestTable");

        // Assert
        result.ShouldBeTrue();
        sheet.LastRowNum.ShouldBe(2); // Header + 2 data rows (null is skipped)
    }

    [Fact]
    public void ExcelExport_WithAutoFilter_ShouldApplyFilterCorrectly()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        ISheet sheet = workbook.CreateSheet("TestSheet");
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();

        // Act
        bool result = testData.ExcelExport(workbook, sheet, createTable: false); // This will use auto-filter instead of table

        // Assert
        result.ShouldBeTrue();
        workbook.NumberOfSheets.ShouldBe(1);
        sheet.SheetName.ShouldBe("TestSheet");
        sheet.GetLastPopulatedRowInColumn(0).ShouldBe(3); // Header + 2 data rows
        sheet.GetLastPopulatedRowInColumn(1).ShouldBe(3); // Header + 2 data rows
        sheet.GetLastPopulatedRowInColumn(2).ShouldBe(3); // Header + 2 data rows
    }

    [Theory]
    [InlineData(true, new[] { "StringProperty" })]
    [InlineData(false, new[] { "IntProperty", "DateProperty" })]
    public void ExcelExport_WithSkippedColumns_ShouldExcludeSpecifiedColumns(bool createTable, string[] columnsToSkip)
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        ISheet sheet = workbook.CreateSheet();
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();

        // Act
        bool result = testData.ExcelExport(workbook, sheet, createTable: createTable, tableName: "TestTable", skipColumnNames: columnsToSkip.ToList());

        // Assert
        result.ShouldBeTrue();
        IRow headerRow = sheet.GetRow(0);
        foreach (string columnName in columnsToSkip)
        {
            List<string?> headerValues = Enumerable.Range(0, headerRow.LastCellNum)
                .Select(i => headerRow.GetCell(i)?.StringCellValue)
                .ToList();
            headerValues.ShouldNotContain(columnName);
        }
    }

    [Fact]
    public void AddGenericTable_WithInvalidTableName_ShouldHandleError()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();
        string invalidTableName = new('X', 257); // Excel table names have a length limit

        // Act
        bool result = workbook.AddGenericTable(testData, "TestSheet", createTable: true, tableName: invalidTableName);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GenericExcelExport_WithLargeDataSet_ShouldHandleMemoryEfficiently()
    {
        // Arrange
        List<TestData> largeDataSet = _fixture.CreateMany<TestData>(10000).ToList();

        // Act
        Task<MemoryStream?> Export()
        { return largeDataSet.GenericExcelExport(createTable: true, sheetName: "LargeData", tableName: "LargeTable"); }

        // Assert
        Should.NotThrow(Export);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GenericExcelExport_WithInvalidSheetName_ShouldUseDefaultName(string? sheetName)
    {
        // Arrange
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();

        // Act
        await using MemoryStream? result = await testData.GenericExcelExport(sheetName: sheetName!, tableName: "TestTable");

        // Assert
        result.ShouldNotBeNull();
        using XSSFWorkbook workbook = new(result);
        workbook.GetSheet("Data").ShouldNotBeNull(); // Should use default sheet name
    }

    [Fact]
    public void ExcelExport_WithCustomStyles_ShouldApplyCorrectFormatting()
    {
        // Arrange
        using SXSSFWorkbook workbook = new();
        ISheet sheet = workbook.CreateSheet();
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();

        // Act
        bool result = testData.ExcelExport(workbook, sheet, createTable: true, tableName: "StyledTable");

        // Assert
        result.ShouldBeTrue();
        IRow headerRow = sheet.GetRow(0);
        headerRow.GetCell(0).CellStyle.FillForegroundColor.ShouldBe(NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index);
    }

    [Fact]
    public async Task GenericExcelExport_WithDisposedMemoryStream_ShouldHandleError()
    {
        // Arrange
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();
        await using MemoryStream memoryStream = new();
        memoryStream.Dispose();

        // Act
        await using MemoryStream? resultStream = await testData.GenericExcelExport(memoryStream: memoryStream);

        // Assert
        resultStream.ShouldNotBeNull();
        resultStream.Length.ShouldBe(0); // Should return an empty stream since the original was disposed
    }
}
