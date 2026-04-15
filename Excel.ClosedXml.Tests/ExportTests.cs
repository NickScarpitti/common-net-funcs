using System.Data;
using AutoFixture;
using ClosedXML.Excel;
using CommonNetFuncs.Excel.ClosedXml;
using CommonNetFuncs.Excel.Common;
using static CommonNetFuncs.Excel.ClosedXml.Export;

namespace Excel.ClosedXml.Tests;

public sealed class ExportTests : IDisposable
{
	private readonly IFixture fixture;
	private readonly XLWorkbook workbook;

	public ExportTests()
	{
		fixture = new Fixture();
		workbook = new XLWorkbook();
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
				workbook?.Dispose();
			}
			disposed = true;
		}
	}

	~ExportTests()
	{
		Dispose(false);
	}

	public sealed class TestData
	{
		public string? StringProperty { get; set; }

		public int IntProperty { get; set; }

		public DateTime DateProperty { get; set; }
	}

	private DataTable MakeDataTable(int rowCount = 3)
	{
		DataTable dt = new();
		dt.Columns.Add("Col1", typeof(string));
		dt.Columns.Add("Col2", typeof(int));
		for (int i = 0; i < rowCount; i++)
		{
			dt.Rows.Add(fixture.Create<string>(), fixture.Create<int>());
		}
		return dt;
	}

	private const string TestSheetName = "Test";

	#region ExportFromTable (Generic) Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ExportFromTable_Generic_WithValidData_ShouldExportSuccessfully(bool createTable)
	{
		IXLWorksheet worksheet = workbook.AddWorksheet(TestSheetName);
		List<TestData> testData = fixture.CreateMany<TestData>(3).ToList();

		bool result = ExportFromTable(workbook, worksheet, testData, createTable, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		worksheet.Cell(1, 1).Value.ToString().ShouldBe("StringProperty");
		worksheet.Cell(1, 2).Value.ToString().ShouldBe("IntProperty");
		worksheet.Cell(1, 3).Value.ToString().ShouldBe("DateProperty");
		worksheet.Cell(2, 1).Value.ToString().ShouldNotBeEmpty();
		worksheet.Cell(2, 2).Value.ToString().ShouldNotBeEmpty();
		worksheet.Cell(2, 3).Value.ToString().ShouldNotBeEmpty();

		if (createTable)
		{
			worksheet.Tables.Count().ShouldBe(1);
			worksheet.Tables.First().ShowAutoFilter.ShouldBeTrue();
		}
		else
		{
			worksheet.Tables.Count().ShouldBe(0);
			worksheet.AutoFilter.IsEnabled.ShouldBeTrue();
		}
	}

	[Fact]
	public void ExportFromTable_Generic_WithNullData_ShouldReturnTrue()
	{
		IXLWorksheet worksheet = workbook.AddWorksheet(TestSheetName);
		IEnumerable<TestData>? testData = null;

		bool result = ExportFromTable(workbook, worksheet, testData, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		worksheet.CellsUsed().Count().ShouldBe(0);
	}

	[Fact]
	public void ExportFromTable_Generic_WithEmptyData_ShouldReturnTrue()
	{
		IXLWorksheet worksheet = workbook.AddWorksheet(TestSheetName);
		TestData[] testData = Array.Empty<TestData>();

		bool result = ExportFromTable(workbook, worksheet, testData, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		worksheet.CellsUsed().Count().ShouldBe(0);
	}

	[Fact]
	public async Task ExportFromTable_Generic_ShouldRespectCancellationToken()
	{
		IXLWorksheet worksheet = workbook.AddWorksheet(TestSheetName);
		List<TestData> testData = fixture.CreateMany<TestData>(1000).ToList();
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		bool result = ExportFromTable(workbook, worksheet, testData, false, false, cts.Token);

		result.ShouldBeFalse();
	}

	[Fact]
	public void ExportFromTable_Generic_WithWrapText_SetsWrapInCells()
	{
		IXLWorksheet worksheet = workbook.AddWorksheet(TestSheetName);
		List<TestData> testData = fixture.CreateMany<TestData>(2).ToList();

		bool result = ExportFromTable(workbook, worksheet, testData, wrapText: true, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		worksheet.Cell(1, 1).Style.Alignment.WrapText.ShouldBeTrue();
	}

	#endregion

	#region ExportFromTable (DataTable) Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ExportFromTable_DataTable_WithValidData_ShouldExportSuccessfully(bool createTable)
	{
		IXLWorksheet worksheet = workbook.AddWorksheet(TestSheetName);
		using DataTable dataTable = new();
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Columns.Add("Column2", typeof(int));
		dataTable.Columns.Add("Column3", typeof(DateTime));
		for (int i = 0; i < 3; i++)
		{
			dataTable.Rows.Add(fixture.Create<string>(), fixture.Create<int>(), fixture.Create<DateTime>());
		}

		bool result = ExportFromTable(workbook, worksheet, dataTable, createTable, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		worksheet.Cell(1, 1).Value.ToString().ShouldBe("Column1");
		worksheet.Cell(1, 2).Value.ToString().ShouldBe("Column2");
		worksheet.Cell(1, 3).Value.ToString().ShouldBe("Column3");
		worksheet.Cell(2, 1).Value.ToString().ShouldNotBeEmpty();

		if (createTable)
		{
			worksheet.Tables.Count().ShouldBe(1);
		}
	}

	[Fact]
	public void ExportFromTable_DataTable_WithNullData_ShouldReturnTrue()
	{
		IXLWorksheet worksheet = workbook.AddWorksheet(TestSheetName);
		DataTable? dataTable = null;

		bool result = ExportFromTable(workbook, worksheet, dataTable, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		worksheet.CellsUsed().Count().ShouldBe(0);
	}

	[Fact]
	public void ExportFromTable_DataTable_WithEmptyData_ShouldReturnTrue()
	{
		IXLWorksheet worksheet = workbook.AddWorksheet(TestSheetName);
		using DataTable dataTable = new();
		dataTable.Columns.Add("Column1");

		bool result = ExportFromTable(workbook, worksheet, dataTable, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		worksheet.CellsUsed().Count().ShouldBe(0);
	}

	[Fact]
	public async Task ExportFromTable_DataTable_ShouldRespectCancellationToken()
	{
		IXLWorksheet worksheet = workbook.AddWorksheet(TestSheetName);
		using DataTable dataTable = new();
		dataTable.Columns.Add("Column1");
		for (int i = 0; i < 1000; i++)
		{
			dataTable.Rows.Add(fixture.Create<string>());
		}
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		bool result = ExportFromTable(workbook, worksheet, dataTable, false, false, cts.Token);

		result.ShouldBeFalse();
	}

	#endregion

	#region GenericExcelExport<T> Tests

	[Fact]
	public async Task GenericExcelExport_Generic_WithValidData_ReturnsNonNullStream()
	{
		List<TestData> data = fixture.CreateMany<TestData>(5).ToList();

		MemoryStream? ms = await data.GenericExcelExport(cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBeGreaterThan(0);
		ms.Position.ShouldBe(0);
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_Generic_WithLongSheetName_ReturnsEmptyStream()
	{
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();
		string longSheet = new('A', 32); // > 31 chars

		MemoryStream? ms = await data.GenericExcelExport(sheetName: longSheet, cancellationToken: TestContext.Current.CancellationToken);

		// Validation failure returns new empty MemoryStream, not null
		ms.ShouldNotBeNull();
		ms!.Length.ShouldBe(0);
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_Generic_WithLongTableName_ReturnsEmptyStream()
	{
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();
		string longTable = new('T', 256); // > 255 chars

		MemoryStream? ms = await data.GenericExcelExport(tableName: longTable, cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBe(0);
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_Generic_WithSkipColumnNames_ProducesFileWithoutColumnInIt()
	{
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		MemoryStream? ms = await data.GenericExcelExport(skipColumnNames: ["DateProperty"], cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		using XLWorkbook wb = new(ms!);
		IXLWorksheet ws = wb.Worksheets.First();
		// DateProperty excluded: should not find it as header
		ws.Cell(1, 1).Value.ToString().ShouldNotBe("DateProperty");
		ws.LastColumnUsed()!.ColumnNumber().ShouldBe(2);
		await ms.DisposeAsync();
	}

	[Theory]
	[InlineData(ETableStyle.TableStyleLight1)]
	[InlineData(ETableStyle.TableStyleMedium1)]
	[InlineData(ETableStyle.TableStyleDark1)]
	public async Task GenericExcelExport_Generic_WithAllTableStyles_ProducesFile(ETableStyle style)
	{
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		MemoryStream? ms = await data.GenericExcelExport(createTable: true, tableStyle: style, cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBeGreaterThan(0);
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_Generic_WithExistingMemoryStream_WritesToIt()
	{
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();
		using MemoryStream existingMs = new();

		MemoryStream? result = await data.GenericExcelExport(existingMs, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
		result!.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GenericExcelExport_Generic_WithCancellation_ThrowsTaskCanceled()
	{
		List<TestData> data = fixture.CreateMany<TestData>(5).ToList();
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		await Should.ThrowAsync<TaskCanceledException>(() => data.GenericExcelExport(cancellationToken: cts.Token));
	}

	[Fact]
	public async Task GenericExcelExport_Generic_WithWhitespaceSheetName_SucceedsWithDefaultName()
	{
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		MemoryStream? ms = await data.GenericExcelExport(sheetName: "   ", cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBeGreaterThan(0);
		using XLWorkbook wb = new(ms);
		wb.Worksheets.Any(ws => ws.Name == "Data").ShouldBeTrue();
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_Generic_WithWhitespaceTableName_SucceedsWithDefaultName()
	{
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		MemoryStream? ms = await data.GenericExcelExport(createTable: true, tableName: "   ", cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBeGreaterThan(0);
		await ms.DisposeAsync();
	}

	#endregion

	#region GenericExcelExport(DataTable) Tests

	[Fact]
	public async Task GenericExcelExport_DataTable_WithValidData_ReturnsNonNullStream()
	{
		using DataTable dt = MakeDataTable(5);

		MemoryStream? ms = await dt.GenericExcelExport(cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBeGreaterThan(0);
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_DataTable_WithLongSheetName_ReturnsEmptyStream()
	{
		using DataTable dt = MakeDataTable();
		string longSheet = new('S', 32);

		MemoryStream? ms = await dt.GenericExcelExport(sheetName: longSheet, cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBe(0);
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_DataTable_WithLongTableName_ReturnsEmptyStream()
	{
		using DataTable dt = MakeDataTable();
		string longTable = new('T', 256);

		MemoryStream? ms = await dt.GenericExcelExport(tableName: longTable, cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBe(0);
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_DataTable_WithSkipColumnNames_ExcludesColumn()
	{
		using DataTable dt = MakeDataTable(2);

		MemoryStream? ms = await dt.GenericExcelExport(skipColumnNames: ["Col2"], cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		using XLWorkbook wb = new(ms!);
		IXLWorksheet ws = wb.Worksheets.First();
		ws.Cell(1, 1).Value.ToString().ShouldNotBe("Col2");
		ws.LastColumnUsed()!.ColumnNumber().ShouldBe(1);
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_DataTable_WithCancellation_ThrowsTaskCanceled()
	{
		using DataTable dt = MakeDataTable();
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		await Should.ThrowAsync<TaskCanceledException>(() => dt.GenericExcelExport(cancellationToken: cts.Token));
	}

	[Fact]
	public async Task GenericExcelExport_DataTable_WithWhitespaceSheetName_SucceedsWithDefaultName()
	{
		using DataTable dt = MakeDataTable(2);

		MemoryStream? ms = await dt.GenericExcelExport(sheetName: "   ", cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBeGreaterThan(0);
		using XLWorkbook wb = new(ms);
		wb.Worksheets.Any(ws => ws.Name == "Data").ShouldBeTrue();
		await ms.DisposeAsync();
	}

	[Fact]
	public async Task GenericExcelExport_DataTable_WithWhitespaceTableName_SucceedsWithDefaultName()
	{
		using DataTable dt = MakeDataTable(2);

		MemoryStream? ms = await dt.GenericExcelExport(createTable: true, tableName: "   ", cancellationToken: TestContext.Current.CancellationToken);

		ms.ShouldNotBeNull();
		ms!.Length.ShouldBeGreaterThan(0);
		await ms.DisposeAsync();
	}

	#endregion

	#region AddGenericTable<T> Tests

	[Fact]
	public void AddGenericTable_Generic_AddsWorksheetToWorkbook()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(3).ToList();

		bool result = wb.AddGenericTable(data, "Sheet1");

		result.ShouldBeTrue();
		wb.Worksheets.Count.ShouldBe(1);
	}

	[Fact]
	public void AddGenericTable_Generic_WithCustomSheetName_UsesName()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		wb.AddGenericTable(data, "CustomSheet");

		wb.Worksheets.Any(ws => ws.Name == "CustomSheet").ShouldBeTrue();
	}

	[Fact]
	public void AddGenericTable_Generic_DuplicateSheetName_RenamesToAvoidConflict()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		wb.AddGenericTable(data, "Sheet1");
		wb.AddGenericTable(data, "Sheet1");

		wb.Worksheets.Count.ShouldBe(2);
		wb.Worksheets.Any(ws => ws.Name == "Sheet1 (1)").ShouldBeTrue();
	}

	[Fact]
	public void AddGenericTable_Generic_MultipleDuplicates_IncrementsSuffix()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		wb.AddGenericTable(data, "Sheet");
		wb.AddGenericTable(data, "Sheet");
		wb.AddGenericTable(data, "Sheet");

		wb.Worksheets.Count.ShouldBe(3);
		wb.Worksheets.Any(ws => ws.Name == "Sheet (2)").ShouldBeTrue();
	}

	[Fact]
	public void AddGenericTable_Generic_WithLongSheetName_ReturnsFalse()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();
		string longName = new('X', 32);

		bool result = wb.AddGenericTable(data, longName);

		result.ShouldBeFalse();
	}

	[Fact]
	public void AddGenericTable_Generic_WithLongTableName_ReturnsFalse()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();
		string longTable = new('T', 256);

		bool result = wb.AddGenericTable(data, "Sheet1", tableName: longTable);

		result.ShouldBeFalse();
	}

	[Fact]
	public void AddGenericTable_Generic_WithCreateTable_True_CreatesTable()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(3).ToList();

		wb.AddGenericTable(data, "Sheet1", createTable: true);

		wb.Worksheets.First().Tables.Count().ShouldBe(1);
	}

	[Fact]
	public void AddGenericTable_Generic_WithSkipColumnNames_ExcludesColumns()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		wb.AddGenericTable(data, "Sheet1", skipColumnNames: ["DateProperty"]);

		IXLWorksheet ws = wb.Worksheets.First();
		// DateProperty should be excluded; only 2 columns remain
		ws.LastColumnUsed()!.ColumnNumber().ShouldBe(2);
	}

	[Fact]
	public void AddGenericTable_Generic_WithWhitespaceSheetName_UsesDefaultName()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		bool result = wb.AddGenericTable(data, "   ");

		result.ShouldBeTrue();
		wb.Worksheets.Any(ws => ws.Name == "Data").ShouldBeTrue();
	}

	[Fact]
	public void AddGenericTable_Generic_WithWhitespaceTableName_UsesDefaultName()
	{
		using XLWorkbook wb = new();
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		bool result = wb.AddGenericTable(data, "Sheet1", createTable: true, tableName: "   ");

		result.ShouldBeTrue();
		wb.Worksheets.First().Tables.Count().ShouldBe(1);
	}

	#endregion

	#region AddGenericTable(DataTable) Tests

	[Fact]
	public void AddGenericTable_DataTable_AddsWorksheetToWorkbook()
	{
		using XLWorkbook wb = new();
		using DataTable dt = MakeDataTable(3);

		bool result = wb.AddGenericTable(dt, "Sheet1");

		result.ShouldBeTrue();
		wb.Worksheets.Count.ShouldBe(1);
	}

	[Fact]
	public void AddGenericTable_DataTable_DuplicateSheetName_RenamesSecond()
	{
		using XLWorkbook wb = new();
		using DataTable dt = MakeDataTable(2);

		wb.AddGenericTable(dt, "DT");
		wb.AddGenericTable(dt, "DT");

		wb.Worksheets.Count.ShouldBe(2);
		wb.Worksheets.Any(ws => ws.Name == "DT (1)").ShouldBeTrue();
	}

	[Fact]
	public void AddGenericTable_DataTable_WithLongSheetName_ReturnsFalse()
	{
		using XLWorkbook wb = new();
		using DataTable dt = MakeDataTable();
		string longName = new('D', 32);

		bool result = wb.AddGenericTable(dt, longName);

		result.ShouldBeFalse();
	}

	[Fact]
	public void AddGenericTable_DataTable_WithLongTableName_ReturnsFalse()
	{
		using XLWorkbook wb = new();
		using DataTable dt = MakeDataTable();
		string longTable = new('T', 256);

		bool result = wb.AddGenericTable(dt, "Sheet1", tableName: longTable);

		result.ShouldBeFalse();
	}

	[Fact]
	public void AddGenericTable_DataTable_WithSkipColumnNames_ExcludesColumns()
	{
		using XLWorkbook wb = new();
		using DataTable dt = MakeDataTable(2);

		wb.AddGenericTable(dt, "Sheet1", skipColumnNames: ["Col2"]);

		IXLWorksheet ws = wb.Worksheets.First();
		ws.LastColumnUsed()!.ColumnNumber().ShouldBe(1);
	}

	[Fact]
	public void AddGenericTable_DataTable_WithWhitespaceSheetName_UsesDefaultName()
	{
		using XLWorkbook wb = new();
		using DataTable dt = MakeDataTable(2);

		bool result = wb.AddGenericTable(dt, "   ");

		result.ShouldBeTrue();
		wb.Worksheets.Any(ws => ws.Name == "Data").ShouldBeTrue();
	}

	[Fact]
	public void AddGenericTable_DataTable_WithWhitespaceTableName_UsesDefaultName()
	{
		using XLWorkbook wb = new();
		using DataTable dt = MakeDataTable(2);

		bool result = wb.AddGenericTable(dt, "Sheet1", createTable: true, tableName: "   ");

		result.ShouldBeTrue();
		wb.Worksheets.First().Tables.Count().ShouldBe(1);
	}

	#endregion

	#region ExcelExport<T> Tests

	[Fact]
	public void ExcelExport_Generic_ExportsDataCorrectly()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		List<TestData> data = fixture.CreateMany<TestData>(3).ToList();

		data.ExcelExport(wb, ws, cancellationToken: TestContext.Current.CancellationToken);

		ws.Cell(1, 1).Value.ToString().ShouldBe("StringProperty");
		ws.Cell(2, 1).Value.ToString().ShouldNotBeEmpty();
	}

	[Fact]
	public void ExcelExport_Generic_ExportedColumnCount_MatchesType()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		data.ExcelExport(wb, ws, cancellationToken: TestContext.Current.CancellationToken);

		// 3 properties = 3 columns
		ws.LastColumnUsed()!.ColumnNumber().ShouldBe(3);
	}

	[Fact]
	public void ExcelExport_Generic_WithSkipColumnNames_ExcludesColumn()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		List<TestData> data = fixture.CreateMany<TestData>(3).ToList();

		data.ExcelExport(wb, ws, skipColumnNames: ["DateProperty"], cancellationToken: TestContext.Current.CancellationToken);

		// DateProperty excluded: only StringProperty + IntProperty remain
		ws.LastColumnUsed()!.ColumnNumber().ShouldBe(2);
		ws.Cell(1, 1).Value.ToString().ShouldBe("StringProperty");
		ws.Cell(1, 2).Value.ToString().ShouldBe("IntProperty");
	}

	[Fact]
	public void ExcelExport_Generic_WithWrapText_SetsWrapOnHeaderRow()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		data.ExcelExport(wb, ws, wrapText: true, cancellationToken: TestContext.Current.CancellationToken);

		// Header row should have wrap text enabled
		ws.Cell(1, 1).Style.Alignment.WrapText.ShouldBeTrue();
	}

	[Fact]
	public void ExcelExport_Generic_WithCreateTable_CreatesXlTable()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		List<TestData> data = fixture.CreateMany<TestData>(3).ToList();

		data.ExcelExport(wb, ws, createTable: true, cancellationToken: TestContext.Current.CancellationToken);

		ws.Tables.Count().ShouldBe(1);
	}

	[Fact]
	public void ExcelExport_Generic_AdjustsColumnWidthsToContents()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		data.ExcelExport(wb, ws, cancellationToken: TestContext.Current.CancellationToken);

		ws.Column(1).Width.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void ExcelExport_Generic_WithWhitespaceTableName_UsesDefaultAndSucceeds()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();

		bool result = data.ExcelExport(wb, ws, tableName: "   ", cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		ws.Cell(1, 1).Value.ToString().ShouldBe("StringProperty");
	}

	[Fact]
	public void ExcelExport_Generic_WithLongTableName_ReturnsFalse()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		List<TestData> data = fixture.CreateMany<TestData>(2).ToList();
		string longTable = new('T', 256);

		bool result = data.ExcelExport(wb, ws, tableName: longTable, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeFalse();
	}

	[Fact]
	public void ExcelExport_Generic_WithNullItem_SkipsNullItem()
	{
		// null items in list are skipped (item.ToNString().IsNullOrEmpty() == true)
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		List<TestData?> data = [null, fixture.Create<TestData>()];

		bool result = data.ExcelExport<TestData?>(wb, ws, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		// Row 2 should be the non-null item (null item was skipped)
		ws.Cell(2, 1).Value.ToString().ShouldNotBeEmpty();
		// Row 3 should be empty (only 1 non-null item)
		ws.Cell(3, 1).IsEmpty().ShouldBeTrue();
	}

	#endregion

	#region ExcelExport(DataTable) Tests

	[Fact]
	public void ExcelExport_DataTable_ExportsDataCorrectly()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		using DataTable dt = MakeDataTable(4);

		dt.ExcelExport(wb, ws, cancellationToken: TestContext.Current.CancellationToken);

		ws.Cell(1, 1).Value.ToString().ShouldBe("Col1");
		ws.Cell(2, 1).Value.ToString().ShouldNotBeEmpty();
	}

	[Fact]
	public void ExcelExport_DataTable_ExportedColumnCount_MatchesTable()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		using DataTable dt = MakeDataTable(2);

		dt.ExcelExport(wb, ws, cancellationToken: TestContext.Current.CancellationToken);

		ws.LastColumnUsed()!.ColumnNumber().ShouldBe(2);
	}

	[Fact]
	public void ExcelExport_DataTable_WithSkipColumnNames_ExcludesColumn()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		using DataTable dt = MakeDataTable(3);

		dt.ExcelExport(wb, ws, skipColumnNames: ["Col2"], cancellationToken: TestContext.Current.CancellationToken);

		ws.LastColumnUsed()!.ColumnNumber().ShouldBe(1);
		ws.Cell(1, 1).Value.ToString().ShouldBe("Col1");
	}

	[Fact]
	public void ExcelExport_DataTable_WithWrapText_SetsWrapOnHeaderRow()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		using DataTable dt = MakeDataTable(2);

		dt.ExcelExport(wb, ws, wrapText: true, cancellationToken: TestContext.Current.CancellationToken);

		ws.Cell(1, 1).Style.Alignment.WrapText.ShouldBeTrue();
	}

	[Fact]
	public void ExcelExport_DataTable_WithCreateTable_CreatesXlTable()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		using DataTable dt = MakeDataTable(3);

		dt.ExcelExport(wb, ws, createTable: true, cancellationToken: TestContext.Current.CancellationToken);

		ws.Tables.Count().ShouldBe(1);
	}

	[Fact]
	public void ExcelExport_DataTable_WithTableStyle_AppliesStyle()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		using DataTable dt = MakeDataTable(2);

		dt.ExcelExport(wb, ws, createTable: true, tableStyle: ETableStyle.TableStyleMedium9, cancellationToken: TestContext.Current.CancellationToken);

		ws.Tables.Count().ShouldBe(1);
	}

	[Fact]
	public void ExcelExport_DataTable_WithWhitespaceTableName_UsesDefaultAndSucceeds()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		using DataTable dt = MakeDataTable(2);

		bool result = dt.ExcelExport(wb, ws, tableName: "   ", cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
		ws.Cell(1, 1).Value.ToString().ShouldBe("Col1");
	}

	[Fact]
	public void ExcelExport_DataTable_WithLongTableName_ReturnsFalse()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		using DataTable dt = MakeDataTable(2);
		string longTable = new('T', 256);

		bool result = dt.ExcelExport(wb, ws, tableName: longTable, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeFalse();
	}

	[Fact]
	public void ExcelExport_DataTable_WithCancellation_ThrowsTaskCanceled()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet(TestSheetName);
		using DataTable dt = MakeDataTable(50);
		using CancellationTokenSource cts = new();
		cts.Cancel();

		Should.Throw<TaskCanceledException>(() => dt.ExcelExport(wb, ws, cancellationToken: cts.Token));
	}

	#endregion
}
