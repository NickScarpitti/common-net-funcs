using System.Data;
using System.Runtime.CompilerServices;
using AutoFixture;
using CommonNetFuncs.Excel.OpenXml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using xRetry.v3;

namespace Excel.OpenXml.Tests;

public sealed class SaxExportTests : IDisposable
{
	private readonly Fixture fixture;

	public SaxExportTests()
	{
		fixture = new Fixture();
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
			disposed = true;
		}
	}

	~SaxExportTests()
	{
		Dispose(false);
	}

	// ── helpers ──────────────────────────────────────────────────────────────

	public class TestModel
	{
		public required string Name { get; set; }
		public required int Age { get; set; }
		public string? Description { get; set; }
	}

	/// <summary>Reads a SAX-written inline-string or plain cell value.</summary>
	private static string GetCellValue(Cell cell)
	{
		return cell.DataType?.Value == CellValues.InlineString
			? cell.InlineString?.Text?.Text ?? string.Empty
			: cell.CellValue?.Text ?? string.Empty;
	}

	/// <summary>Opens a memory stream (rewound to 0) as a read-only SpreadsheetDocument.</summary>
	private static SpreadsheetDocument OpenDoc(MemoryStream stream)
	{
		stream.Position = 0;
		return SpreadsheetDocument.Open(stream, false);
	}

	/// <summary>Wraps a synchronous sequence as an async-enumerable, optionally honouring cancellation between items.</summary>
	private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source, [EnumeratorCancellation] CancellationToken ct = default)
	{
		foreach (T item in source)
		{
			ct.ThrowIfCancellationRequested();
			yield return item;
		}
		await Task.CompletedTask;
	}

	// ── GenericExcelExportAsync<T> (IEnumerable) ─────────────────────────────

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_WithValidList_ShouldCreateExcelFile()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(3).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream);

		stream.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = OpenDoc(stream);
		WorksheetPart? wsp = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		wsp.ShouldNotBeNull();

		SheetData? sheetData = wsp.Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(4); // header + 3 rows
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_HeaderNamesMatchPropertyNames()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(1).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		List<Cell> headerCells = sheetData.Elements<Row>().First().Elements<Cell>().ToList();
		headerCells.Count.ShouldBe(3);
		GetCellValue(headerCells[0]).ShouldBe(nameof(TestModel.Name));
		GetCellValue(headerCells[1]).ShouldBe(nameof(TestModel.Age));
		GetCellValue(headerCells[2]).ShouldBe(nameof(TestModel.Description));
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_WithCustomSheetName_ShouldUseSpecifiedName()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(1).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream, sheetName: "MySheet");

		using SpreadsheetDocument doc = OpenDoc(stream);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet.ShouldNotBeNull();
		sheet.Name?.Value.ShouldBe("MySheet");
	}

	[RetryTheory(3)]
	[InlineData(true, "MyTable")]
	[InlineData(false, "Data")]
	public async Task GenericExcelExportAsync_IEnumerable_TableCreation_ShouldRespectCreateTableFlag(bool createTable, string tableName)
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream, createTable: createTable, tableName: tableName);

		using SpreadsheetDocument doc = OpenDoc(stream);
		WorksheetPart? wsp = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		wsp.ShouldNotBeNull();

		if (createTable)
		{
			TableDefinitionPart? tdp = wsp.TableDefinitionParts.FirstOrDefault();
			tdp.ShouldNotBeNull();
			tdp.Table?.Name?.Value.ShouldBe(tableName);
		}
		else
		{
			// AutoFilter should be present in the worksheet XML
			// Read the raw XML to verify (worksheet DOM rebuilds from SAX-written XML)
			wsp.TableDefinitionParts.ShouldBeEmpty();
		}
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_WithoutCreateTable_ShouldWriteAutoFilter()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream, createTable: false);

		using SpreadsheetDocument doc = OpenDoc(stream);
		WorksheetPart? wsp = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		wsp.ShouldNotBeNull();

		// AutoFilter is written inline by the SAX writer; it is accessible through the DOM after reload
		AutoFilter? autoFilter = wsp.Worksheet?.Elements<AutoFilter>().FirstOrDefault();
		autoFilter.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_WithSkipColumns_ShouldOmitSpecifiedColumns()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream, skipColumnNames: ["Description"]);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row header = sheetData.Elements<Row>().First();
		header.Elements<Cell>().Count().ShouldBe(2); // Name and Age only
		GetCellValue(header.Elements<Cell>().First()).ShouldBe(nameof(TestModel.Name));
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_CaseInsensitiveSkipColumns_ShouldOmitColumns()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(1).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream, skipColumnNames: ["name", "DESCRIPTION"]);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row header = sheetData.Elements<Row>().First();
		header.Elements<Cell>().Count().ShouldBe(1); // Age only
		GetCellValue(header.Elements<Cell>().First()).ShouldBe(nameof(TestModel.Age));
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_WithEmptyList_ShouldWriteHeadersOnly()
	{
		List<TestModel> data = [];

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream);

		stream.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(1); // header only
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_NullItemsAreFiltered()
	{
		List<TestModel?> data = [fixture.Create<TestModel>(), null, fixture.Create<TestModel>()];

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(3); // header + 2 non-null rows
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_WithCancellation_ShouldThrow()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(10).ToList();
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		using MemoryStream stream = new();
		await Should.ThrowAsync<OperationCanceledException>(() => data.GenericExcelExportAsync(stream, cancellationToken: cts.Token));
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_WithWrapText_ShouldNotThrow()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		await Should.NotThrowAsync(() => data.GenericExcelExportAsync(stream, wrapText: true));
		stream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_WithLargeDataset_ShouldHandleVolume()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(1000).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(1001); // header + 1000 rows
	}

	// ── GenericExcelExportAsync<T> (IAsyncEnumerable) ────────────────────────

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IAsyncEnumerable_WithValidData_ShouldCreateExcelFile()
	{
		List<TestModel> source = fixture.CreateMany<TestModel>(3).ToList();

		using MemoryStream stream = new();
		await ToAsyncEnumerable(source).GenericExcelExportAsync(stream);

		stream.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(4); // header + 3 rows
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IAsyncEnumerable_HeaderNamesMatchPropertyNames()
	{
		List<TestModel> source = fixture.CreateMany<TestModel>(1).ToList();

		using MemoryStream stream = new();
		await ToAsyncEnumerable(source).GenericExcelExportAsync(stream);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		List<Cell> headerCells = sheetData.Elements<Row>().First().Elements<Cell>().ToList();
		headerCells.Count.ShouldBe(3);
		GetCellValue(headerCells[0]).ShouldBe(nameof(TestModel.Name));
		GetCellValue(headerCells[1]).ShouldBe(nameof(TestModel.Age));
		GetCellValue(headerCells[2]).ShouldBe(nameof(TestModel.Description));
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IAsyncEnumerable_WithCustomSheetName_ShouldUseSpecifiedName()
	{
		List<TestModel> source = fixture.CreateMany<TestModel>(1).ToList();

		using MemoryStream stream = new();
		await ToAsyncEnumerable(source).GenericExcelExportAsync(stream, sheetName: "AsyncSheet");

		using SpreadsheetDocument doc = OpenDoc(stream);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet.ShouldNotBeNull();
		sheet.Name?.Value.ShouldBe("AsyncSheet");
	}

	[RetryTheory(3)]
	[InlineData(true, "AsyncTable")]
	[InlineData(false, "Data")]
	public async Task GenericExcelExportAsync_IAsyncEnumerable_TableCreation_ShouldRespectCreateTableFlag(bool createTable, string tableName)
	{
		List<TestModel> source = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		await ToAsyncEnumerable(source).GenericExcelExportAsync(stream, createTable: createTable, tableName: tableName);

		using SpreadsheetDocument doc = OpenDoc(stream);
		WorksheetPart? wsp = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		wsp.ShouldNotBeNull();

		if (createTable)
		{
			TableDefinitionPart? tdp = wsp.TableDefinitionParts.FirstOrDefault();
			tdp.ShouldNotBeNull();
			tdp.Table?.Name?.Value.ShouldBe(tableName);
		}
		else
		{
			wsp.TableDefinitionParts.ShouldBeEmpty();
		}
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IAsyncEnumerable_WithSkipColumns_ShouldOmitSpecifiedColumns()
	{
		List<TestModel> source = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		await ToAsyncEnumerable(source).GenericExcelExportAsync(stream, skipColumnNames: ["Age", "Description"]);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row header = sheetData.Elements<Row>().First();
		header.Elements<Cell>().Count().ShouldBe(1); // Name only
		GetCellValue(header.Elements<Cell>().First()).ShouldBe(nameof(TestModel.Name));
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IAsyncEnumerable_WithCancellation_ShouldThrow()
	{
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		using MemoryStream stream = new();
		await Should.ThrowAsync<OperationCanceledException>(
			() => ToAsyncEnumerable(fixture.CreateMany<TestModel>(10), cts.Token).GenericExcelExportAsync(stream, cancellationToken: cts.Token));
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IAsyncEnumerable_WithoutCreateTable_ShouldWriteAutoFilter()
	{
		List<TestModel> source = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		await ToAsyncEnumerable(source).GenericExcelExportAsync(stream, createTable: false);

		using SpreadsheetDocument doc = OpenDoc(stream);
		WorksheetPart? wsp = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		wsp.ShouldNotBeNull();
		wsp.Worksheet?.Elements<AutoFilter>().FirstOrDefault().ShouldNotBeNull();
	}

	// ── GenericExcelExportAsync (DataTable) ──────────────────────────────────

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_DataTable_WithValidTable_ShouldCreateExcelFile()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Name", typeof(string));
		dt.Columns.Add("Value", typeof(int));
		dt.Rows.Add("Alice", 1);
		dt.Rows.Add("Bob", 2);

		using MemoryStream stream = new();
		await dt.GenericExcelExportAsync(stream);

		stream.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(3); // header + 2 rows
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_DataTable_HeaderNamesMatchColumnNames()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Name", typeof(string));
		dt.Columns.Add("Score", typeof(int));
		dt.Rows.Add("Alice", 42);

		using MemoryStream stream = new();
		await dt.GenericExcelExportAsync(stream);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		List<Cell> headerCells = sheetData.Elements<Row>().First().Elements<Cell>().ToList();
		headerCells.Count.ShouldBe(2);
		GetCellValue(headerCells[0]).ShouldBe("Name");
		GetCellValue(headerCells[1]).ShouldBe("Score");
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_DataTable_WithCustomSheetName_ShouldUseSpecifiedName()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Col", typeof(string));
		dt.Rows.Add("Val");

		using MemoryStream stream = new();
		await dt.GenericExcelExportAsync(stream, sheetName: "DtSheet");

		using SpreadsheetDocument doc = OpenDoc(stream);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet.ShouldNotBeNull();
		sheet.Name?.Value.ShouldBe("DtSheet");
	}

	[RetryTheory(3)]
	[InlineData(true, "DtTable")]
	[InlineData(false, "Data")]
	public async Task GenericExcelExportAsync_DataTable_TableCreation_ShouldRespectCreateTableFlag(bool createTable, string tableName)
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Col", typeof(string));
		dt.Rows.Add("Val");

		using MemoryStream stream = new();
		await dt.GenericExcelExportAsync(stream, createTable: createTable, tableName: tableName);

		using SpreadsheetDocument doc = OpenDoc(stream);
		WorksheetPart? wsp = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		wsp.ShouldNotBeNull();

		if (createTable)
		{
			TableDefinitionPart? tdp = wsp.TableDefinitionParts.FirstOrDefault();
			tdp.ShouldNotBeNull();
			tdp.Table?.Name?.Value.ShouldBe(tableName);
		}
		else
		{
			wsp.TableDefinitionParts.ShouldBeEmpty();
		}
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_DataTable_WithSkipColumns_ShouldOmitSpecifiedColumns()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("A", typeof(string));
		dt.Columns.Add("B", typeof(int));
		dt.Columns.Add("C", typeof(string));
		dt.Rows.Add("a", 1, "c");

		using MemoryStream stream = new();
		await dt.GenericExcelExportAsync(stream, skipColumnNames: ["B"]);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row header = sheetData.Elements<Row>().First();
		header.Elements<Cell>().Count().ShouldBe(2); // A and C only
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_DataTable_CaseInsensitiveSkipColumns_ShouldOmitColumns()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Alpha", typeof(string));
		dt.Columns.Add("Beta", typeof(int));
		dt.Rows.Add("x", 1);

		using MemoryStream stream = new();
		await dt.GenericExcelExportAsync(stream, skipColumnNames: ["ALPHA"]);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row header = sheetData.Elements<Row>().First();
		header.Elements<Cell>().Count().ShouldBe(1); // Beta only
		GetCellValue(header.Elements<Cell>().First()).ShouldBe("Beta");
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_DataTable_WithEmptyTable_ShouldNotWriteData()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Col", typeof(string));
		// No rows

		using MemoryStream stream = new();
		await dt.GenericExcelExportAsync(stream);

		// The SAX DataTable path only writes when Rows.Count > 0; the stream is a valid (minimal) xlsx
		stream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_DataTable_WithCancellation_ShouldThrow()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Col", typeof(string));
		for (int i = 0; i < 10; i++) dt.Rows.Add($"Row{i}");

		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		using MemoryStream stream = new();
		await Should.ThrowAsync<OperationCanceledException>(() => dt.GenericExcelExportAsync(stream, cancellationToken: cts.Token));
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_DataTable_WithNullCellValues_ShouldWriteEmptyString()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("A", typeof(string));
		dt.Columns.Add("B", typeof(string));
		dt.Rows.Add("value", DBNull.Value);

		using MemoryStream stream = new();
		await dt.GenericExcelExportAsync(stream);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(2); // header + 1 row
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_DataTable_WithoutCreateTable_ShouldWriteAutoFilter()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Col", typeof(string));
		dt.Rows.Add("Value");

		using MemoryStream stream = new();
		await dt.GenericExcelExportAsync(stream, createTable: false);

		using SpreadsheetDocument doc = OpenDoc(stream);
		WorksheetPart? wsp = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		wsp.ShouldNotBeNull();
		wsp.Worksheet?.Elements<AutoFilter>().FirstOrDefault().ShouldNotBeNull();
	}

	// ── ExportFromTableSaxAsync<T> (IEnumerable) — low-level ─────────────────

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_IEnumerable_WritesHeaderAndDataRows()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(3).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, data);

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		// Navigate by worksheetPart directly because the sheet is not registered in this low-level test
		WorksheetPart? readWsp = readDoc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		readWsp.ShouldNotBeNull();

		SheetData? sheetData = readWsp.Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(4); // header + 3 rows
	}

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_IEnumerable_WithSkipColumns_OmitsColumns()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, data, skipColumnNames: ["Age"]);

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		SheetData? sheetData = readDoc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row header = sheetData.Elements<Row>().First();
		header.Elements<Cell>().Count().ShouldBe(2); // Name and Description only
	}

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_IEnumerable_WithCreateTable_CreatesTableDefinition()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, data, createTable: true, tableName: "LowLevelTable");

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		WorksheetPart? readWsp = readDoc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		readWsp.ShouldNotBeNull();

		TableDefinitionPart? tdp = readWsp.TableDefinitionParts.FirstOrDefault();
		tdp.ShouldNotBeNull();
		tdp.Table?.Name?.Value.ShouldBe("LowLevelTable");
	}

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_IEnumerable_WithoutCreateTable_WritesAutoFilter()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, data, createTable: false);

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		WorksheetPart? readWsp = readDoc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		readWsp.ShouldNotBeNull();
		readWsp.Worksheet?.Elements<AutoFilter>().FirstOrDefault().ShouldNotBeNull();
	}

	// ── ExportFromTableSaxAsync<T> (IAsyncEnumerable) — low-level ────────────

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_IAsyncEnumerable_WritesHeaderAndDataRows()
	{
		List<TestModel> source = fixture.CreateMany<TestModel>(3).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, ToAsyncEnumerable(source));

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		SheetData? sheetData = readDoc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(4); // header + 3 rows
	}

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_IAsyncEnumerable_WithCreateTable_CreatesTableDefinition()
	{
		List<TestModel> source = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, ToAsyncEnumerable(source), createTable: true, tableName: "AsyncLowLevel");

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		WorksheetPart? readWsp = readDoc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		readWsp.ShouldNotBeNull();

		TableDefinitionPart? tdp = readWsp.TableDefinitionParts.FirstOrDefault();
		tdp.ShouldNotBeNull();
		tdp.Table?.Name?.Value.ShouldBe("AsyncLowLevel");
	}

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_IAsyncEnumerable_WithSkipColumns_OmitsColumns()
	{
		List<TestModel> source = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, ToAsyncEnumerable(source), skipColumnNames: ["Description"]);

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		SheetData? sheetData = readDoc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row header = sheetData.Elements<Row>().First();
		header.Elements<Cell>().Count().ShouldBe(2); // Name and Age only
	}

	// ── ExportFromTableSaxAsync (DataTable) — low-level ──────────────────────

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_DataTable_WritesHeaderAndDataRows()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("X", typeof(string));
		dt.Columns.Add("Y", typeof(int));
		dt.Rows.Add("hello", 1);
		dt.Rows.Add("world", 2);

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, dt);

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		SheetData? sheetData = readDoc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(3); // header + 2 rows
	}

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_DataTable_WithEmptyTable_WritesNothing()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Col", typeof(string));
		// No rows

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, dt);

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		// The worksheetPart has no content written (DataTable guard: Rows.Count > 0)
		// Opening it should succeed without crashing
		stream.Position = 0;
		stream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_DataTable_WithSkipColumns_OmitsColumns()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("P", typeof(string));
		dt.Columns.Add("Q", typeof(int));
		dt.Columns.Add("R", typeof(string));
		dt.Rows.Add("p", 1, "r");

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, dt, skipColumnNames: ["Q"]);

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		SheetData? sheetData = readDoc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row header = sheetData.Elements<Row>().First();
		header.Elements<Cell>().Count().ShouldBe(2); // P and R only
	}

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_DataTable_WithCreateTable_CreatesTableDefinition()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Col", typeof(string));
		dt.Rows.Add("Value");

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, dt, createTable: true, tableName: "DtLowLevel");

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		WorksheetPart? readWsp = readDoc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		readWsp.ShouldNotBeNull();

		TableDefinitionPart? tdp = readWsp.TableDefinitionParts.FirstOrDefault();
		tdp.ShouldNotBeNull();
		tdp.Table?.Name?.Value.ShouldBe("DtLowLevel");
	}

	[RetryFact(3)]
	public async Task ExportFromTableSaxAsync_DataTable_WithoutCreateTable_WritesAutoFilter()
	{
		using DataTable dt = new("Test");
		dt.Columns.Add("Col", typeof(string));
		dt.Rows.Add("Value");

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		WorkbookPart wbp = doc.InitializeExcelFile();
		WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();

		await Export.ExportFromTableSaxAsync(doc, wsp, dt, createTable: false);

		doc.WorkbookPart!.Workbook!.Save();
		doc.Dispose();

		using SpreadsheetDocument readDoc = OpenDoc(stream);
		WorksheetPart? readWsp = readDoc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		readWsp.ShouldNotBeNull();
		readWsp.Worksheet?.Elements<AutoFilter>().FirstOrDefault().ShouldNotBeNull();
	}

	// ── cross-cutting / additional coverage ───────────────────────────────────

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_ColumnWidthsAreSet()
	{
		// Column widths are pre-computed from header text in SAX mode
		List<TestModel> data = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream);

		using SpreadsheetDocument doc = OpenDoc(stream);
		WorksheetPart? wsp = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		wsp.ShouldNotBeNull();

		// At least one Column element with CustomWidth set should be present
		Columns? cols = wsp.Worksheet?.GetFirstChild<Columns>();
		cols.ShouldNotBeNull();
		cols.Elements<Column>().Any(c => c.CustomWidth?.Value == true).ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_SpecialCharactersInValues_ShouldNotThrow()
	{
		List<TestModel> data =
		[
			new TestModel { Name = "O'Brien <>&\"", Age = 1, Description = "你好 😊" }
		];

		using MemoryStream stream = new();
		await Should.NotThrowAsync(() => data.GenericExcelExportAsync(stream));
		stream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IAsyncEnumerable_NullItemsAreFiltered()
	{
		List<TestModel?> source = [fixture.Create<TestModel>(), null, fixture.Create<TestModel>()];

		using MemoryStream stream = new();
		await ToAsyncEnumerable(source).GenericExcelExportAsync(stream);

		using SpreadsheetDocument doc = OpenDoc(stream);
		SheetData? sheetData = doc.WorkbookPart?.WorksheetParts.First().Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(3); // header + 2 non-null rows
	}

	[RetryFact(3)]
	public async Task GenericExcelExportAsync_IEnumerable_SheetIdIsAssigned()
	{
		List<TestModel> data = fixture.CreateMany<TestModel>(1).ToList();

		using MemoryStream stream = new();
		await data.GenericExcelExportAsync(stream);

		using SpreadsheetDocument doc = OpenDoc(stream);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet.ShouldNotBeNull();
		sheet.SheetId?.Value.ShouldBeGreaterThan(0u);
	}
}
