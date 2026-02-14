using System.Data;
using CommonNetFuncs.Excel.Npoi;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using xRetry.v3;

namespace Excel.Npoi.Tests;

public class ExportTests
{
	private readonly IFixture fixture;

	public ExportTests() { fixture = new Fixture(); }

	public class TestData
	{
		public string StringProperty { get; set; } = string.Empty;

		public int IntProperty { get; set; }

		public DateTime DateProperty { get; set; }
	}

	[RetryTheory(3)]
	[InlineData(true, "TestSheet", "TestTable")]
	[InlineData(false, "Data", "Data")]
	public async Task GenericExcelExport_WithValidList_ShouldReturnMemoryStream(bool createTable, string sheetName, string tableName)
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();
		List<string> skipColumnNames = new() { "DateProperty" };

		// Act
		MemoryStream? result = await testData.GenericExcelExport(memoryStream: null, createTable: createTable, sheetName: sheetName, tableName: tableName, skipColumnNames: skipColumnNames);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
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

	[RetryFact(3)]
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

	[RetryFact(3)]
	public void AddGenericTable_WithGenericList_ShouldAddDataToWorkbook()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = workbook.AddGenericTable(testData, "TestSheet", createTable: true, tableName: "TestTable");

		// Assert
		result.ShouldBeTrue();
		workbook.GetSheet("TestSheet").ShouldNotBeNull();
	}

	[RetryFact(3)]
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

	[RetryFact(3)]
	public void ExcelExport_WithGenericList_ShouldExportDataCorrectly()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = testData.ExcelExport(workbook, sheet, createTable: true, tableName: "TestTable");

		// Assert
		result.ShouldBeTrue();
		sheet.LastRowNum.ShouldBe(3); // Header row + 3 data rows
	}

	[RetryFact(3)]
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

	[RetryFact(3)]
	public void AddGenericTable_WithDuplicateSheetNames_ShouldCreateUniqueNames()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(2).ToList();

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

	[RetryFact(3)]
	public async Task GenericExcelExport_WithCancellation_ShouldHandleCancellation()
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(100).ToList();
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(async () => await testData.GenericExcelExport(cancellationToken: cts.Token));
	}

	// Add these test methods to the existing ExportTests class

	[RetryFact(3)]
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

	[RetryFact(3)]
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

	[RetryFact(3)]
	public void ExcelExport_WithAutoFilter_ShouldApplyFilterCorrectly()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet("TestSheet");
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

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

	[RetryTheory(3)]
	[InlineData(true, new[] { "StringProperty" })]
	[InlineData(false, new[] { "IntProperty", "DateProperty" })]
	public void ExcelExport_WithSkippedColumns_ShouldExcludeSpecifiedColumns(bool createTable, string[] columnsToSkip)
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

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

	[RetryFact(3)]
	public void AddGenericTable_WithInvalidTableName_ShouldHandleError()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();
		string invalidTableName = new('X', 257); // Excel table names have a length limit

		// Act
		bool result = workbook.AddGenericTable(testData, "TestSheet", createTable: true, tableName: invalidTableName);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void GenericExcelExport_WithLargeDataSet_ShouldHandleMemoryEfficiently()
	{
		// Arrange
		List<TestData> largeDataSet = fixture.CreateMany<TestData>(10000).ToList();

		// Act
		Task<MemoryStream?> Export()
		{ return largeDataSet.GenericExcelExport(createTable: true, sheetName: "LargeData", tableName: "LargeTable"); }

		// Assert
		Should.NotThrow(Export);
	}

	[RetryTheory(3)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	public async Task GenericExcelExport_WithInvalidSheetName_ShouldUseDefaultName(string? sheetName)
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		await using MemoryStream? result = await testData.GenericExcelExport(sheetName: sheetName!, tableName: "TestTable");

		// Assert
		result.ShouldNotBeNull();
		using XSSFWorkbook workbook = new(result);
		workbook.GetSheet("Data").ShouldNotBeNull(); // Should use default sheet name
	}

	[RetryFact(3)]
	public void ExcelExport_WithCustomStyles_ShouldApplyCorrectFormatting()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = testData.ExcelExport(workbook, sheet, createTable: true, tableName: "StyledTable");

		// Assert
		result.ShouldBeTrue();
		IRow headerRow = sheet.GetRow(0);
		headerRow.GetCell(0).CellStyle.FillForegroundColor.ShouldBe(NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index);
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_WithDisposedMemoryStream_ShouldHandleError()
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();
		await using MemoryStream memoryStream = new();
		await memoryStream.DisposeAsync();

		// Act
		await using MemoryStream? resultStream = await testData.GenericExcelExport(memoryStream: memoryStream);

		// Assert
		resultStream.ShouldNotBeNull();
		resultStream.Length.ShouldBe(0); // Should return an empty stream since the original was disposed
	}

	#region Additional Coverage Tests

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithNullSheetName_ShouldUseDefaultName()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");

		// Act
		await using MemoryStream? result = await dataTable.GenericExcelExport(sheetName: null!);

		// Assert
		result.ShouldNotBeNull();
		using XSSFWorkbook workbook = new(result);
		workbook.GetSheet("Data").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithEmptySheetName_ShouldUseDefaultName()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");

		// Act
		await using MemoryStream? result = await dataTable.GenericExcelExport(sheetName: "");

		// Assert
		result.ShouldNotBeNull();
		using XSSFWorkbook workbook = new(result);
		workbook.GetSheet("Data").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithWhitespaceSheetName_ShouldUseDefaultName()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");

		// Act
		await using MemoryStream? result = await dataTable.GenericExcelExport(sheetName: "   ");

		// Assert
		result.ShouldNotBeNull();
		using XSSFWorkbook workbook = new(result);
		workbook.GetSheet("Data").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithNullTableName_ShouldUseDefaultName()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");

		// Act
		await using MemoryStream? result = await dataTable.GenericExcelExport(createTable: true, tableName: null!);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithLongSheetName_ShouldReturnEmptyStream()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");
		string longSheetName = new('X', 32);

		// Act
		await using MemoryStream? result = await dataTable.GenericExcelExport(sheetName: longSheetName);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithLongTableName_ShouldReturnEmptyStream()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");
		string longTableName = new('X', 32);

		// Act
		await using MemoryStream? result = await dataTable.GenericExcelExport(createTable: true, tableName: longTableName);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_List_WithLongSheetName_ShouldReturnEmptyStream()
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();
		string longSheetName = new('X', 32);

		// Act
		await using MemoryStream? result = await testData.GenericExcelExport(sheetName: longSheetName);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_List_WithLongTableName_ShouldReturnEmptyStream()
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();
		string longTableName = new('X', 32);

		// Act
		await using MemoryStream? result = await testData.GenericExcelExport(createTable: true, tableName: longTableName);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBe(0);
	}

	[RetryFact(3)]
	public void AddGenericTable_XSSFWorkbook_WithDataTable_ShouldAddDataToWorkbook()
	{
		// Arrange
		using XSSFWorkbook workbook = new();
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

	[RetryFact(3)]
	public void AddGenericTable_XSSFWorkbook_WithGenericList_ShouldAddDataToWorkbook()
	{
		// Arrange
		using XSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = workbook.AddGenericTable(testData, "TestSheet", createTable: true, tableName: "TestTable");

		// Assert
		result.ShouldBeTrue();
		workbook.GetSheet("TestSheet").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void AddGenericTable_WithNullData_ShouldHandleGracefully()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData>? nullData = null;

		// Act
		bool result = workbook.AddGenericTable(nullData!, "TestSheet");

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void AddGenericTable_WithLongSheetName_ShouldReturnFalse()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();
		string longSheetName = new('X', 32);

		// Act
		bool result = workbook.AddGenericTable(testData, longSheetName);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void AddGenericTable_WithLongTableName_ShouldReturnFalse()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();
		string longTableName = new('X', 32);

		// Act
		bool result = workbook.AddGenericTable(testData, "TestSheet", createTable: true, tableName: longTableName);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithLongSheetName_ShouldReturnFalse()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");
		string longSheetName = new('X', 32);

		// Act
		bool result = workbook.AddGenericTable(dataTable, longSheetName);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithLongTableName_ShouldReturnFalse()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");
		string longTableName = new('X', 32);

		// Act
		bool result = workbook.AddGenericTable(dataTable, "TestSheet", createTable: true, tableName: longTableName);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void AddGenericTable_WithNullSheetName_ShouldUseDefaultName()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = workbook.AddGenericTable(testData, null!);

		// Assert
		result.ShouldBeTrue();
		workbook.GetSheet("Data").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void AddGenericTable_WithEmptySheetName_ShouldUseDefaultName()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = workbook.AddGenericTable(testData, "");

		// Assert
		result.ShouldBeTrue();
		workbook.GetSheet("Data").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void AddGenericTable_WithWhitespaceSheetName_ShouldUseDefaultName()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = workbook.AddGenericTable(testData, "   ");

		// Assert
		result.ShouldBeTrue();
		workbook.GetSheet("Data").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void AddGenericTable_WithNullTableName_ShouldUseDefaultName()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = workbook.AddGenericTable(testData, "TestSheet", createTable: true, tableName: null!);

		// Assert
		result.ShouldBeTrue();
		workbook.GetSheet("TestSheet").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithNullTableName_ShouldUseDefaultName()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");

		// Act
		bool result = workbook.AddGenericTable(dataTable, "TestSheet", createTable: true, tableName: null!);

		// Assert
		result.ShouldBeTrue();
		workbook.GetSheet("TestSheet").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void ExcelExport_DataTable_WithSkippedColumns_ShouldExcludeSpecifiedColumns()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Columns.Add("Column2", typeof(int));
		dataTable.Columns.Add("Column3", typeof(DateTime));
		dataTable.Rows.Add("Value1", 1, DateTime.Now);
		List<string> skipColumns = ["Column2"];

		// Act
		bool result = dataTable.ExcelExport(workbook, sheet, skipColumnNames: skipColumns);

		// Assert
		result.ShouldBeTrue();
		IRow headerRow = sheet.GetRow(0);
		List<string?> headerValues = Enumerable.Range(0, headerRow.LastCellNum)
				.Select(i => headerRow.GetCell(i)?.StringCellValue)
				.ToList();
		headerValues.ShouldNotContain("Column2");
		headerValues.ShouldContain("Column1");
		headerValues.ShouldContain("Column3");
	}

	[RetryFact(3)]
	public void ExcelExport_DataTable_WithNullTableName_ShouldUseDefaultName()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");

		// Act
		bool result = dataTable.ExcelExport(workbook, sheet, createTable: true, tableName: null!);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void ExcelExport_DataTable_WithLongTableName_ShouldReturnFalse()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");
		string longTableName = new('X', 32);

		// Act
		bool result = dataTable.ExcelExport(workbook, sheet, createTable: true, tableName: longTableName);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ExcelExport_List_WithNullTableName_ShouldUseDefaultName()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = testData.ExcelExport(workbook, sheet, createTable: true, tableName: null!);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void ExcelExport_List_WithLongTableName_ShouldReturnFalse()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();
		string longTableName = new('X', 32);

		// Act
		bool result = testData.ExcelExport(workbook, sheet, createTable: true, tableName: longTableName);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ExcelExport_DataTable_WithEmptyDataTable_ShouldReturnTrue()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		DataTable emptyDataTable = new();

		// Act
		bool result = emptyDataTable.ExcelExport(workbook, sheet);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void ExcelExport_DataTable_WithNullSkipColumnNames_ShouldNotSkipAnyColumns()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Columns.Add("Column2", typeof(int));
		dataTable.Rows.Add("Value1", 1);

		// Act
		bool result = dataTable.ExcelExport(workbook, sheet, skipColumnNames: null);

		// Assert
		result.ShouldBeTrue();
		IRow headerRow = sheet.GetRow(0);
		headerRow.LastCellNum.ShouldBe((short)2);
	}

	[RetryFact(3)]
	public void ExcelExport_List_WithNullSkipColumnNames_ShouldNotSkipAnyColumns()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = testData.ExcelExport(workbook, sheet, skipColumnNames: null);

		// Assert
		result.ShouldBeTrue();
		IRow headerRow = sheet.GetRow(0);
		((int)headerRow.LastCellNum).ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ExcelExport_DataTable_WithWrapText_ShouldApplyWrapText()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Long text value");

		// Act
		bool result = dataTable.ExcelExport(workbook, sheet, wrapText: true);

		// Assert
		result.ShouldBeTrue();
		IRow dataRow = sheet.GetRow(1);
		dataRow.GetCell(0).CellStyle.WrapText.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void ExcelExport_List_WithWrapText_ShouldApplyWrapText()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = testData.ExcelExport(workbook, sheet, wrapText: true);

		// Assert
		result.ShouldBeTrue();
		IRow dataRow = sheet.GetRow(1);
		dataRow.GetCell(0).CellStyle.WrapText.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithWrapText_ShouldApplyWrapText()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Long text value");

		// Act
		bool result = workbook.AddGenericTable(dataTable, "TestSheet", wrapText: true);

		// Assert
		result.ShouldBeTrue();
		ISheet sheet = workbook.GetSheet("TestSheet");
		IRow dataRow = sheet.GetRow(1);
		dataRow.GetCell(0).CellStyle.WrapText.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void AddGenericTable_List_WithWrapText_ShouldApplyWrapText()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		bool result = workbook.AddGenericTable(testData, "TestSheet", wrapText: true);

		// Assert
		result.ShouldBeTrue();
		ISheet sheet = workbook.GetSheet("TestSheet");
		IRow dataRow = sheet.GetRow(1);
		dataRow.GetCell(0).CellStyle.WrapText.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithWrapText_ShouldApplyWrapText()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Long text value");

		// Act
		await using MemoryStream? result = await dataTable.GenericExcelExport(wrapText: true);

		// Assert
		result.ShouldNotBeNull();
		using XSSFWorkbook workbook = new(result);
		ISheet sheet = workbook.GetSheetAt(0);
		IRow dataRow = sheet.GetRow(1);
		dataRow.GetCell(0).CellStyle.WrapText.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_List_WithWrapText_ShouldApplyWrapText()
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		await using MemoryStream? result = await testData.GenericExcelExport(wrapText: true);

		// Assert
		result.ShouldNotBeNull();
		using XSSFWorkbook workbook = new(result);
		ISheet sheet = workbook.GetSheetAt(0);
		IRow dataRow = sheet.GetRow(1);
		dataRow.GetCell(0).CellStyle.WrapText.ShouldBeTrue();
	}

	#endregion

	#region Cancellation Tests

	[RetryFact(3)]
	public async Task GenericExcelExport_List_WithCancelledToken_ShouldThrowTaskCanceledException()
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(10000).ToList(); // Large dataset to ensure cancellation is possible
		using CancellationTokenSource cts = new();
		await cts.CancelAsync(); // Cancel immediately

		// Act & Assert
		await Should.ThrowAsync<TaskCanceledException>(async () =>
			await testData.GenericExcelExport(cancellationToken: cts.Token));
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithCancelledToken_ShouldThrowTaskCanceledException()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		for (int i = 0; i < 10000; i++)
		{
			dataTable.Rows.Add($"Value{i}");
		}
		using CancellationTokenSource cts = new();
		await cts.CancelAsync(); // Cancel immediately

		// Act & Assert
		await Should.ThrowAsync<TaskCanceledException>(async () =>
			await dataTable.GenericExcelExport(cancellationToken: cts.Token));
	}

	[RetryFact(3)]
	public void ExcelExport_List_WithCancelledToken_ShouldThrowTaskCanceledException()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData> testData = fixture.CreateMany<TestData>(10000).ToList(); // Large dataset
		using CancellationTokenSource cts = new();
		cts.Cancel(); // Cancel immediately

		// Act & Assert
		Should.Throw<TaskCanceledException>(() =>
			testData.ExcelExport(workbook, sheet, cancellationToken: cts.Token));
	}

	[RetryFact(3)]
	public void ExcelExport_DataTable_WithCancelledToken_ShouldThrowTaskCanceledException()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		for (int i = 0; i < 10000; i++)
		{
			dataTable.Rows.Add($"Value{i}");
		}
		using CancellationTokenSource cts = new();
		cts.Cancel(); // Cancel immediately

		// Act & Assert
		Should.Throw<TaskCanceledException>(() =>
			dataTable.ExcelExport(workbook, sheet, cancellationToken: cts.Token));
	}

	#endregion

	#region Null/Empty Data Tests

	[RetryFact(3)]
	public async Task GenericExcelExport_List_WithNullData_ShouldReturnEmptyStream()
	{
		// Arrange
		List<TestData>? nullData = null;

		// Act
		await using MemoryStream? result = await nullData!.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void ExcelExport_List_WithNullData_ShouldReturnTrue()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData>? nullData = null;

		// Act
		bool result = nullData!.ExcelExport(workbook, sheet);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void ExcelExport_DataTable_WithNullData_ShouldReturnTrue()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		DataTable? nullData = null;

		// Act
		bool result = nullData!.ExcelExport(workbook, sheet);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithNullData_ShouldReturnFalse()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		DataTable? nullData = null;

		// Act
		bool result = workbook.AddGenericTable(nullData!, "TestSheet");

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region Edge Cases

	[RetryFact(3)]
	public void ExcelExport_List_WithNullItemInList_ShouldSkipNullItems()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		ISheet sheet = workbook.CreateSheet();
		List<TestData?> testDataWithNull = new()
		{
			new TestData { StringProperty = "Test1", IntProperty = 1 },
			null,
			new TestData { StringProperty = "Test2", IntProperty = 2 }
		};

		// Act
		bool result = testDataWithNull.ExcelExport(workbook, sheet);

		// Assert
		result.ShouldBeTrue();
		// Should have header + 2 data rows (null item skipped)
		sheet.LastRowNum.ShouldBe(2);
	}

	[RetryFact(3)]
	public void AddGenericTable_WithDuplicateSheetName_ShouldCreateUniqueSheetName()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		List<TestData> testData1 = fixture.CreateMany<TestData>(2).ToList();
		List<TestData> testData2 = fixture.CreateMany<TestData>(2).ToList();

		// Act
		bool result1 = workbook.AddGenericTable(testData1, "TestSheet");
		bool result2 = workbook.AddGenericTable(testData2, "TestSheet");

		// Assert
		result1.ShouldBeTrue();
		result2.ShouldBeTrue();
		workbook.NumberOfSheets.ShouldBe(2);
		workbook.GetSheet("TestSheet").ShouldNotBeNull();
		workbook.GetSheet("TestSheet (1)").ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithDuplicateSheetName_ShouldCreateUniqueSheetName()
	{
		// Arrange
		using SXSSFWorkbook workbook = new();
		DataTable dataTable1 = new();
		dataTable1.Columns.Add("Column1", typeof(string));
		dataTable1.Rows.Add("Value1");

		DataTable dataTable2 = new();
		dataTable2.Columns.Add("Column1", typeof(string));
		dataTable2.Rows.Add("Value2");

		// Act
		bool result1 = workbook.AddGenericTable(dataTable1, "TestSheet");
		bool result2 = workbook.AddGenericTable(dataTable2, "TestSheet");

		// Assert
		result1.ShouldBeTrue();
		result2.ShouldBeTrue();
		workbook.NumberOfSheets.ShouldBe(2);
		workbook.GetSheet("TestSheet").ShouldNotBeNull();
		workbook.GetSheet("TestSheet (1)").ShouldNotBeNull();
	}

	#endregion

	#region Additional Edge Cases for GenericExcelExport

	[RetryFact(3)]
	public async Task GenericExcelExport_List_WithEmptyTableName_ShouldUseDefaultName()
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		await using MemoryStream? result = await testData.GenericExcelExport(createTable: true, tableName: "");

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_List_WithWhitespaceTableName_ShouldUseDefaultName()
	{
		// Arrange
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		// Act
		await using MemoryStream? result = await testData.GenericExcelExport(createTable: true, tableName: "   ");

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithEmptyTableName_ShouldUseDefaultName()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");

		// Act
		await using MemoryStream? result = await dataTable.GenericExcelExport(createTable: true, tableName: "");

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExport_DataTable_WithWhitespaceTableName_ShouldUseDefaultName()
	{
		// Arrange
		DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Rows.Add("Value1");

		// Act
		await using MemoryStream? result = await dataTable.GenericExcelExport(createTable: true, tableName: "   ");

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	#endregion
}
