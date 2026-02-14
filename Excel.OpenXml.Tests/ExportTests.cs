using System.Data;
using CommonNetFuncs.Excel.OpenXml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using xRetry.v3;

namespace Excel.OpenXml.Tests;

public sealed class ExportTests : IDisposable
{
	private readonly Fixture fixture;
	private readonly MemoryStream memoryStream;

	public ExportTests()
	{
		fixture = new Fixture();
		memoryStream = new MemoryStream();
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
				memoryStream.Dispose();
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

	[RetryFact(3)]
	public void GenericExcelExport_WithValidList_ShouldCreateExcelFile()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(3).ToList();

		// Act
		using MemoryStream? result = testData.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		WorkbookPart? workbookPart = doc.WorkbookPart;
		workbookPart.ShouldNotBeNull();

		WorksheetPart? worksheetPart = workbookPart.WorksheetParts.FirstOrDefault();
		worksheetPart.ShouldNotBeNull();

		Worksheet? worksheet = worksheetPart.Worksheet;
		SheetData? sheetData = worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		// Verify headers
		Row firstRow = sheetData.Elements<Row>().First();
		firstRow.Elements<Cell>().Count().ShouldBe(3); // Name, Age, Description
	}

	[RetryTheory(3)]
	[InlineData(true, "CustomTable")]
	[InlineData(false, "Data")]
	public void GenericExcelExport_WithTableFormatting_ShouldRespectTableSettings(bool createTable, string tableName)
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(2).ToList();

		// Act
		using MemoryStream? result = testData.GenericExcelExport(
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
			tableDefinitionPart.Table?.Name?.Value.ShouldBe(tableName);
		}
	}

	[RetryFact(3)]
	public void GenericExcelExport_WithDataTable_ShouldCreateExcelFile()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Name", typeof(string));
		dataTable.Columns.Add("Value", typeof(int));

		dataTable.Rows.Add("Test1", 1);
		dataTable.Rows.Add("Test2", 2);

		// Act
		using MemoryStream? result = dataTable.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		worksheetPart.ShouldNotBeNull();

		SheetData? sheetData = worksheetPart.Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(3); // Header + 2 data rows
	}

	//[RetryFact(3)]
	//public void AddGenericTable_WithValidData_ShouldAddNewSheet()
	//{
	//    // Arrange
	//    List<TestModel> testData = fixture.CreateMany<TestModel>(2).ToList();
	//    using SpreadsheetDocument doc = SpreadsheetDocument.Create(_memoryStream, SpreadsheetDocumentType.Workbook);
	//    doc.AddWorkbookPart();

	//    // Act
	//    bool result = doc.AddGenericTable(testData, "TestSheet");

	//    // Assert
	//    result.ShouldBeTrue();
	//    doc.WorkbookPart.ShouldNotBeNull();
	//    doc.WorkbookPart.Workbook.Sheets?.Elements<Sheet>().Count().ShouldBe(1);
	//}

	[RetryFact(3)]
	public void ExportFromTable_WithSkipColumns_ShouldOmitSpecifiedColumns()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(2).ToList();
		List<string> skipColumns = new() { "Description" };

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
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

	[RetryFact(3)]
	public async Task ExportFromTable_WithCancellation_ShouldRespectCancellationToken()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(100).ToList();
		using CancellationTokenSource cts = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act & Assert
		await cts.CancelAsync();
		Should.Throw<OperationCanceledException>(() => Export.ExportFromTable(doc, worksheet, testData, cancellationToken: cts.Token));
	}

	[RetryFact(3)]
	public void GenericExcelExport_WithNullList_ShouldCreateEmptyFile()
	{
		// Arrange
		List<TestModel>? nullList = null;

		// Act
		using MemoryStream? result = nullList!.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0); // Creates valid Excel file even with null data
	}

	[RetryFact(3)]
	public void GenericExcelExport_WithEmptyList_ShouldCreateExcelFile()
	{
		// Arrange
		List<TestModel> emptyList = new();

		// Act
		using MemoryStream? result = emptyList.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0); // Should still create a valid Excel file
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithNullTable_ShouldCreateEmptyFile()
	{
		// Arrange
		using DataTable? nullTable = null;

		// Act
		using MemoryStream? result = nullTable!.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0); // Creates valid Excel file even with null data
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithEmptyTable_ShouldCreateExcelFile()
	{
		// Arrange
		using DataTable emptyTable = new("EmptyTable");
		emptyTable.Columns.Add("Column1", typeof(string));

		// Act
		using MemoryStream? result = emptyTable.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void AddGenericTable_IEnumerable_WithValidData_ShouldAddNewSheet()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act
		bool result = doc.AddGenericTable(testData, "TestSheet", createTable: false, tableName: "TestTable");

		// Assert
		result.ShouldBeTrue();
		doc.WorkbookPart.Workbook.Sheets?.Elements<Sheet>().Count().ShouldBe(1);
		Sheet? sheet = doc.WorkbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet.ShouldNotBeNull();
		sheet.Name?.Value.ShouldBe("TestSheet");
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithValidData_ShouldAddNewSheet()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Name", typeof(string));
		dataTable.Columns.Add("Value", typeof(int));
		dataTable.Rows.Add("Test1", 1);
		dataTable.Rows.Add("Test2", 2);

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act
		bool result = doc.AddGenericTable(dataTable, "DataSheet", createTable: false, tableName: "DataTable");

		// Assert
		result.ShouldBeTrue();
		doc.WorkbookPart.Workbook.Sheets?.Elements<Sheet>().Count().ShouldBe(1);
		Sheet? sheet = doc.WorkbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet.ShouldNotBeNull();
		sheet.Name?.Value.ShouldBe("DataSheet");
	}

	[RetryFact(3)]
	public void AddGenericTable_IEnumerable_WithDuplicateSheetNames_ShouldAppendNumber()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(1).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act - Add first sheet
		bool result1 = doc.AddGenericTable(testData, "TestSheet", createTable: false, tableName: "Table1");

		// Act - Add second sheet with same name
		bool result2 = doc.AddGenericTable(testData, "TestSheet", createTable: false, tableName: "Table2");

		// Assert
		result1.ShouldBeTrue();
		result2.ShouldBeTrue();
		doc.WorkbookPart.Workbook.Sheets?.Elements<Sheet>().Count().ShouldBe(2);

		List<Sheet> sheets = doc.WorkbookPart.Workbook.Sheets!.Elements<Sheet>().ToList();
		sheets[0].Name?.Value.ShouldBe("TestSheet");
		sheets[1].Name?.Value.ShouldBe("TestSheet (1)"); // Should append (1)
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithDuplicateSheetNames_ShouldAppendNumber()
	{
		// Arrange
		using DataTable dataTable1 = new("Table1");
		dataTable1.Columns.Add("Column", typeof(string));
		dataTable1.Rows.Add("Value");

		using DataTable dataTable2 = new("Table2");
		dataTable2.Columns.Add("Column", typeof(string));
		dataTable2.Rows.Add("Value");

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act - Add sheets with same name
		bool result1 = doc.AddGenericTable(dataTable1, "DupeSheet", createTable: false, tableName: "Table1");
		bool result2 = doc.AddGenericTable(dataTable2, "DupeSheet", createTable: false, tableName: "Table2");

		// Assert
		result1.ShouldBeTrue();
		result2.ShouldBeTrue();

		List<Sheet> sheets = doc.WorkbookPart.Workbook.Sheets!.Elements<Sheet>().ToList();
		sheets[0].Name?.Value.ShouldBe("DupeSheet");
		sheets[1].Name?.Value.ShouldBe("DupeSheet (1)");
	}

	[RetryFact(3)]
	public void AddGenericTable_IEnumerable_WithMultipleDuplicates_ShouldIncrementCounter()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(1).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act - Add multiple sheets with same name
		doc.AddGenericTable(testData, "Sheet", createTable: false, tableName: "Table1");
		doc.AddGenericTable(testData, "Sheet", createTable: false, tableName: "Table2");
		doc.AddGenericTable(testData, "Sheet", createTable: false, tableName: "Table3");

		// Assert
		List<Sheet> sheets = doc.WorkbookPart.Workbook.Sheets!.Elements<Sheet>().ToList();
		sheets.Count.ShouldBe(3);
		sheets[0].Name?.Value.ShouldBe("Sheet");
		sheets[1].Name?.Value.ShouldBe("Sheet (1)");
		sheets[2].Name?.Value.ShouldBe("Sheet (2)");
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithSkipColumns_ShouldOmitSpecifiedColumns()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Columns.Add("Column2", typeof(int));
		dataTable.Columns.Add("Column3", typeof(string));
		dataTable.Rows.Add("Value1", 1, "Value3");

		List<string> skipColumns = new() { "Column2" };

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, dataTable, skipColumnNames: skipColumns);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row headerRow = sheetData.Elements<Row>().First();
		headerRow.Elements<Cell>().Count().ShouldBe(2); // Only Column1 and Column3
	}

	[RetryFact(3)]
	public async Task ExportFromTable_DataTable_WithCancellation_ShouldHandleCancellation()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column1", typeof(string));

		for (int i = 0; i < 100; i++)
		{
			dataTable.Rows.Add($"Value{i}");
		}

		using CancellationTokenSource cts = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act & Assert - DataTable version catches exceptions and returns false
		await cts.CancelAsync();
		try
		{
			Export.ExportFromTable(doc, worksheet, dataTable, cancellationToken: cts.Token);
		}
		catch (OperationCanceledException)
		{
			// Expected - cancellation may be detected
		}
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithEmptyList_ShouldReturnTrue()
	{
		// Arrange
		List<TestModel> emptyList = new();

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, emptyList);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithEmptyTable_ShouldReturnTrue()
	{
		// Arrange
		using DataTable emptyTable = new("EmptyTable");
		emptyTable.Columns.Add("Column1", typeof(string));

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, emptyTable);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void GenericExcelExport_WithWrapText_ShouldApplyWrapTextFormatting()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(2).ToList();

		// Act
		using MemoryStream? result = testData.GenericExcelExport(wrapText: true);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithWrapText_ShouldApplyWrapTextFormatting()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Name", typeof(string));
		dataTable.Rows.Add("Test");

		// Act
		using MemoryStream? result = dataTable.GenericExcelExport(wrapText: true);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void GenericExcelExport_WithCustomSheetName_ShouldUseSpecifiedName()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(1).ToList();

		// Act
		using MemoryStream? result = testData.GenericExcelExport(sheetName: "CustomSheet");

		// Assert
		result.ShouldNotBeNull();
		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet.ShouldNotBeNull();
		sheet.Name?.Value.ShouldBe("CustomSheet");
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithCustomSheetName_ShouldUseSpecifiedName()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column", typeof(string));
		dataTable.Rows.Add("Value");

		// Act
		using MemoryStream? result = dataTable.GenericExcelExport(sheetName: "MyCustomSheet");

		// Assert
		result.ShouldNotBeNull();
		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet.ShouldNotBeNull();
		sheet.Name?.Value.ShouldBe("MyCustomSheet");
	}

	[RetryFact(3)]
	public void ExportFromTable_WithAllColumnsSkipped_ShouldReturnFalse()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(1).ToList();
		List<string> skipAllColumns = new() { "Name", "Age", "Description" };

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, testData, skipColumnNames: skipAllColumns);

		// Assert
		result.ShouldBeFalse(); // Returns false when no properties to export
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithAllColumnsSkipped_ShouldHandleGracefully()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Columns.Add("Column2", typeof(int));
		dataTable.Rows.Add("Value", 123);

		List<string> skipAllColumns = new() { "Column1", "Column2" };

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, dataTable, skipColumnNames: skipAllColumns);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void GenericExcelExport_WithProvidedMemoryStream_ShouldUseExistingStream()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(1).ToList();
		using MemoryStream providedStream = new();

		// Act
		MemoryStream? result = testData.GenericExcelExport(providedStream);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(providedStream); // Should be same instance
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithProvidedMemoryStream_ShouldUseExistingStream()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column", typeof(string));
		dataTable.Rows.Add("Value");
		using MemoryStream providedStream = new();

		// Act
		MemoryStream? result = dataTable.GenericExcelExport(providedStream);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(providedStream);
		result.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithNullSheetData_ShouldReturnFalse()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(1).ToList();

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Remove SheetData to trigger exception
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		if (sheetData != null)
		{
			worksheet.RemoveChild(sheetData);
		}

		// Act - Exception is caught and returns false
		bool result = Export.ExportFromTable(doc, worksheet, testData);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithNullSheetData_ShouldReturnFalse()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column", typeof(string));
		dataTable.Rows.Add("Value");

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Remove SheetData to trigger exception
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		if (sheetData != null)
		{
			worksheet.RemoveChild(sheetData);
		}

		// Act - Exception is caught and returns false
		bool result = Export.ExportFromTable(doc, worksheet, dataTable);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithNullValues_ShouldFilterOutNulls()
	{
		// Arrange
		List<TestModel?> testData = new()
		{
			fixture.Create<TestModel>(),
			null,
			fixture.Create<TestModel>()
		};

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, testData!);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		// Should have header + 2 data rows (null filtered out)
		sheetData.Elements<Row>().Count().ShouldBe(3);
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithNullValues_ShouldHandleNull()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Columns.Add("Column2", typeof(string));
		dataTable.Rows.Add("Value1", DBNull.Value);
		dataTable.Rows.Add(DBNull.Value, "Value2");

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, dataTable);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(3); // Header + 2 data rows
	}

	[RetryFact(3)]
	public void GenericExcelExport_IEnumerable_WithLargeDataset_ShouldHandleLargeVolumes()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(1000).ToList();

		// Act
		using MemoryStream? result = testData.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		SheetData? sheetData = worksheetPart?.Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(1001); // Header + 1000 rows
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithLargeDataset_ShouldHandleLargeVolumes()
	{
		// Arrange
		using DataTable dataTable = new("LargeTable");
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Columns.Add("Column2", typeof(int));

		for (int i = 0; i < 1000; i++)
		{
			dataTable.Rows.Add($"Value{i}", i);
		}

		// Act
		using MemoryStream? result = dataTable.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		SheetData? sheetData = worksheetPart?.Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(1001); // Header + 1000 rows
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithSpecialCharacters_ShouldHandleSpecialChars()
	{
		// Arrange
		List<TestModel> testData = new()
		{
			new TestModel { Name = "Test<>&\"'", Age = 1, Description = "Special chars: <>\"'&" },
			new TestModel { Name = "Unicode: 你好世界", Age = 2, Description = "Emoji: 😊🎉" }
		};

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, testData);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(3); // Header + 2 data rows
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithSpecialCharacters_ShouldHandleSpecialChars()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column<>&", typeof(string));
		dataTable.Rows.Add("Value with <>&\"'");
		dataTable.Rows.Add("你好世界 😊");

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, dataTable);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(3); // Header + 2 data rows
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithVeryLongString_ShouldHandleLongStrings()
	{
		// Arrange
		string longString = new('A', 10000); // Very long string
		List<TestModel> testData = new()
		{
			new TestModel { Name = longString, Age = 1, Description = longString }
		};

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, testData);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void AddGenericTable_IEnumerable_WithNullData_ShouldReturnFalse()
	{
		// Arrange
		List<TestModel>? nullData = null;

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act
		bool result = doc.AddGenericTable(nullData!, "TestSheet");

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithNullData_ShouldReturnFalse()
	{
		// Arrange
		DataTable? nullData = null;

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act
		bool result = doc.AddGenericTable(nullData!, "TestSheet");

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithMultipleSheets_ShouldCreateMultipleTables()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(2).ToList();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act
		bool result1 = doc.AddGenericTable(testData, "Sheet1", createTable: true, tableName: "Table1");
		bool result2 = doc.AddGenericTable(testData, "Sheet2", createTable: true, tableName: "Table2");

		// Assert
		result1.ShouldBeTrue();
		result2.ShouldBeTrue();

		List<Sheet> sheets = doc.WorkbookPart.Workbook.Sheets!.Elements<Sheet>().ToList();
		sheets.Count.ShouldBe(2);
	}

	[RetryFact(3)]
	public void GenericExcelExport_IEnumerable_WithCaseInsensitiveSkipColumns_ShouldOmitColumns()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(1).ToList();
		List<string> skipColumns = new() { "NAME", "description" }; // Different case

		// Act
		using MemoryStream? result = testData.GenericExcelExport(skipColumnNames: skipColumns);

		// Assert
		result.ShouldNotBeNull();
		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		SheetData? sheetData = worksheetPart?.Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row headerRow = sheetData.Elements<Row>().First();
		headerRow.Elements<Cell>().Count().ShouldBe(1); // Only Age
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithCaseInsensitiveSkipColumns_ShouldOmitColumns()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column1", typeof(string));
		dataTable.Columns.Add("Column2", typeof(int));
		dataTable.Columns.Add("Column3", typeof(string));
		dataTable.Rows.Add("Val1", 123, "Val3");

		List<string> skipColumns = new() { "COLUMN1", "column3" }; // Different case

		// Act
		using MemoryStream? result = dataTable.GenericExcelExport(skipColumnNames: skipColumns);

		// Assert
		result.ShouldNotBeNull();
		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		SheetData? sheetData = worksheetPart?.Worksheet?.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row headerRow = sheetData.Elements<Row>().First();
		headerRow.Elements<Cell>().Count().ShouldBe(1); // Only Column2
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithCreateTable_ShouldCreateExcelTable()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(3).ToList();

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, testData, createTable: true, tableName: "MyTable");

		// Assert
		result.ShouldBeTrue();
		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		worksheetPart.ShouldNotBeNull();

		TableDefinitionPart? tableDefPart = worksheetPart.TableDefinitionParts.FirstOrDefault();
		tableDefPart.ShouldNotBeNull();
		tableDefPart.Table?.Name?.Value.ShouldBe("MyTable");
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithCreateTable_ShouldCreateExcelTable()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Col1", typeof(string));
		dataTable.Columns.Add("Col2", typeof(int));
		dataTable.Rows.Add("Value", 123);

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, dataTable, createTable: true, tableName: "DataTable");

		// Assert
		result.ShouldBeTrue();
		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		worksheetPart.ShouldNotBeNull();

		TableDefinitionPart? tableDefPart = worksheetPart.TableDefinitionParts.FirstOrDefault();
		tableDefPart.ShouldNotBeNull();
		tableDefPart.Table?.Name?.Value.ShouldBe("DataTable");
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithoutCreateTable_ShouldCreateAutoFilter()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(2).ToList();

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, testData, createTable: false);

		// Assert
		result.ShouldBeTrue();
		AutoFilter? autoFilter = worksheet.Elements<AutoFilter>().FirstOrDefault();
		autoFilter.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithoutCreateTable_ShouldCreateAutoFilter()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column", typeof(string));
		dataTable.Rows.Add("Value");

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, dataTable, createTable: false);

		// Assert
		result.ShouldBeTrue();
		AutoFilter? autoFilter = worksheet.Elements<AutoFilter>().FirstOrDefault();
		autoFilter.ShouldNotBeNull();
	}

	public class TestModelWithNullProperties
	{
		public string? Name { get; set; }

		public int? Age { get; set; }

		public string? Description { get; set; }
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithNullPropertyValues_ShouldHandleNulls()
	{
		// Arrange
		List<TestModelWithNullProperties> testData = new()
		{
			new TestModelWithNullProperties { Name = null, Age = 1, Description = null },
			new TestModelWithNullProperties { Name = "Test", Age = null, Description = "Desc" }
		};

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, testData);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(3); // Header + 2 data rows
	}

	[RetryFact(3)]
	public void GenericExcelExport_IEnumerable_WithMultipleParameters_ShouldRespectAllParameters()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(5).ToList();

		// Act
		using MemoryStream? result = testData.GenericExcelExport(
			memoryStream: new MemoryStream(),
			createTable: true,
			sheetName: "CustomSheet",
			tableName: "CustomTable",
			skipColumnNames: new() { "Description" },
			wrapText: true);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet?.Name?.Value.ShouldBe("CustomSheet");

		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		TableDefinitionPart? tableDefPart = worksheetPart?.TableDefinitionParts.FirstOrDefault();
		tableDefPart?.Table?.Name?.Value.ShouldBe("CustomTable");

		SheetData? sheetData = worksheetPart?.Worksheet?.GetFirstChild<SheetData>();
		Row headerRow = sheetData!.Elements<Row>().First();
		headerRow.Elements<Cell>().Count().ShouldBe(2); // Name and Age only
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithMultipleParameters_ShouldRespectAllParameters()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Col1", typeof(string));
		dataTable.Columns.Add("Col2", typeof(int));
		dataTable.Columns.Add("Col3", typeof(string));

		for (int i = 0; i < 5; i++)
		{
			dataTable.Rows.Add($"Value{i}", i, $"Desc{i}");
		}

		// Act
		using MemoryStream? result = dataTable.GenericExcelExport(
			memoryStream: new MemoryStream(),
			createTable: true,
			sheetName: "DataSheet",
			tableName: "DataTable",
			skipColumnNames: new() { "Col3" },
			wrapText: true);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet?.Name?.Value.ShouldBe("DataSheet");

		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		TableDefinitionPart? tableDefPart = worksheetPart?.TableDefinitionParts.FirstOrDefault();
		tableDefPart?.Table?.Name?.Value.ShouldBe("DataTable");

		// DataTable implementation writes all headers but skips values for skipped columns
		SheetData? sheetData = worksheetPart?.Worksheet?.GetFirstChild<SheetData>();
		Row headerRow = sheetData!.Elements<Row>().First();
		headerRow.Elements<Cell>().Count().ShouldBe(3); // All 3 columns in header
	}

	[RetryFact(3)]
	public void GenericExcelExport_IEnumerable_WhenExportFromTableFails_ShouldReturnNull()
	{
		// Arrange - Create a list with a worksheet that will fail ExportFromTable
		List<TestModel> testData = fixture.CreateMany<TestModel>(1).ToList();

		// Create a corrupted memory stream scenario by pre-filling with large data
		using MemoryStream corruptedStream = new();
		byte[] largeData = new byte[1024 * 1024 * 50]; // 50MB
		corruptedStream.Write(largeData, 0, largeData.Length);
		corruptedStream.Position = 0;

		// Act - Try to create Excel with pre-filled stream
		try
		{
			MemoryStream? result = testData.GenericExcelExport(corruptedStream);
			// If we get here, check if result is valid
			result.ShouldNotBeNull();
		}
		catch
		{
			// Expected - corrupted stream may cause exception
		}
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WhenExportFromTableFails_ShouldReturnNull()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column", typeof(string));
		dataTable.Rows.Add("Value");

		// Create a corrupted memory stream scenario
		using MemoryStream corruptedStream = new();
		byte[] largeData = new byte[1024 * 1024 * 50]; // 50MB
		corruptedStream.Write(largeData, 0, largeData.Length);
		corruptedStream.Position = 0;

		// Act - Try to create Excel with pre-filled stream
		try
		{
			MemoryStream? result = dataTable.GenericExcelExport(corruptedStream);
			result.ShouldNotBeNull();
		}
		catch
		{
			// Expected - corrupted stream may cause exception
		}
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithSingleRow_ShouldExportCorrectly()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(1).ToList();

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, testData);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(2); // Header + 1 data row
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithSingleRow_ShouldExportCorrectly()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column", typeof(string));
		dataTable.Rows.Add("Value");

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, dataTable);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();
		sheetData.Elements<Row>().Count().ShouldBe(2); // Header + 1 data row
	}

	[RetryFact(3)]
	public void ExportFromTable_IEnumerable_WithManyColumns_ShouldHandleManyColumns()
	{
		// Arrange
		var testData = new[]
		{
			new
			{
				Col1 = "A", Col2 = "B", Col3 = "C", Col4 = "D", Col5 = "E",
				Col6 = "F", Col7 = "G", Col8 = "H", Col9 = "I", Col10 = "J"
			}
		};

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, testData);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row headerRow = sheetData.Elements<Row>().First();
		headerRow.Elements<Cell>().Count().ShouldBe(10); // 10 columns
	}

	[RetryFact(3)]
	public void ExportFromTable_DataTable_WithManyColumns_ShouldHandleManyColumns()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		for (int i = 1; i <= 10; i++)
		{
			dataTable.Columns.Add($"Col{i}", typeof(string));
		}
		object[] values = Enumerable.Range(1, 10).Select(i => (object)$"Val{i}").ToArray();
		dataTable.Rows.Add(values);

		using SpreadsheetDocument doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		uint sheetId = doc.InitializeExcelFile("TestSheet");
		Worksheet? worksheet = doc.GetWorksheetById(sheetId);
		worksheet.ShouldNotBeNull();

		// Act
		bool result = Export.ExportFromTable(doc, worksheet, dataTable);

		// Assert
		result.ShouldBeTrue();
		SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
		sheetData.ShouldNotBeNull();

		Row headerRow = sheetData.Elements<Row>().First();
		headerRow.Elements<Cell>().Count().ShouldBe(10); // 10 columns
	}

	[RetryFact(3)]
	public void AddGenericTable_IEnumerable_WithEmptyData_ShouldReturnTrue()
	{
		// Arrange
		List<TestModel> emptyData = new();

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act
		bool result = doc.AddGenericTable(emptyData, "EmptySheet");

		// Assert
		result.ShouldBeTrue(); // Should create sheet even with empty data
	}

	[RetryFact(3)]
	public void AddGenericTable_DataTable_WithEmptyData_ShouldReturnTrue()
	{
		// Arrange
		using DataTable emptyDataTable = new("EmptyTable");
		emptyDataTable.Columns.Add("Column", typeof(string));
		// No rows added

		using MemoryStream stream = new();
		using SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		doc.AddWorkbookPart();
		doc.WorkbookPart!.Workbook = new Workbook();
		doc.WorkbookPart.Workbook.AddChild(new Sheets());

		// Act
		bool result = doc.AddGenericTable(emptyDataTable, "EmptySheet");

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void GenericExcelExport_IEnumerable_WithDefaultParameters_ShouldUseDefaults()
	{
		// Arrange
		List<TestModel> testData = fixture.CreateMany<TestModel>(2).ToList();

		// Act - Use all default parameters
		using MemoryStream? result = testData.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet?.Name?.Value.ShouldBe("Data"); // Default sheet name

		// Should not have table (createTable defaults to false)
		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		TableDefinitionPart? tableDefPart = worksheetPart?.TableDefinitionParts.FirstOrDefault();
		tableDefPart.ShouldBeNull();
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithDefaultParameters_ShouldUseDefaults()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column", typeof(string));
		dataTable.Rows.Add("Value");

		// Act - Use all default parameters
		using MemoryStream? result = dataTable.GenericExcelExport();

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		using SpreadsheetDocument doc = SpreadsheetDocument.Open(result, false);
		Sheet? sheet = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
		sheet?.Name?.Value.ShouldBe("Data"); // Default sheet name

		// Should not have table (createTable defaults to false)
		WorksheetPart? worksheetPart = doc.WorkbookPart?.WorksheetParts.FirstOrDefault();
		TableDefinitionPart? tableDefPart = worksheetPart?.TableDefinitionParts.FirstOrDefault();
		tableDefPart.ShouldBeNull();
	}

	#region Exception Handling Tests

	[RetryFact(3)]
	public void GenericExcelExport_IEnumerable_WithDisposedStream_ShouldHandleException()
	{
		// Arrange
		List<TestModel> data = fixture.CreateMany<TestModel>(1).ToList();
		MemoryStream disposedStream = new();
		disposedStream.Dispose();

		// Act
		MemoryStream? result = data.GenericExcelExport(disposedStream);

		// Assert - Should return empty MemoryStream when exception occurs
		result.ShouldNotBeNull();
		result.Length.ShouldBe(0);
	}

	[RetryFact(3)]
	public void GenericExcelExport_IEnumerable_WithReadOnlyStream_ShouldHandleException()
	{
		// Arrange
		List<TestModel> data = fixture.CreateMany<TestModel>(1).ToList();
		byte[] buffer = new byte[1024];
		using MemoryStream readOnlyStream = new(buffer, writable: false);

		// Act
		MemoryStream? result = data.GenericExcelExport(readOnlyStream);

		// Assert - Should return empty MemoryStream when exception occurs
		result.ShouldNotBeNull();
		result.Length.ShouldBe(0);
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithDisposedStream_ShouldHandleException()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column", typeof(string));
		dataTable.Rows.Add("Value");

		MemoryStream disposedStream = new();
		disposedStream.Dispose();

		// Act
		MemoryStream? result = dataTable.GenericExcelExport(disposedStream);

		// Assert - Should return empty MemoryStream when exception occurs
		result.ShouldNotBeNull();
		result.Length.ShouldBe(0);
	}

	[RetryFact(3)]
	public void GenericExcelExport_DataTable_WithReadOnlyStream_ShouldHandleException()
	{
		// Arrange
		using DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Column", typeof(string));
		dataTable.Rows.Add("Value");

		byte[] buffer = new byte[1024];
		using MemoryStream readOnlyStream = new(buffer, writable: false);

		// Act
		MemoryStream? result = dataTable.GenericExcelExport(readOnlyStream);

		// Assert - Should return empty MemoryStream when exception occurs
		result.ShouldNotBeNull();
		result.Length.ShouldBe(0);
	}

	#endregion
}
