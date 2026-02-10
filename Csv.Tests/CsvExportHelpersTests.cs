using System.Data;
using System.Globalization;
using CommonNetFuncs.Csv;

namespace Csv.Tests;

public sealed class CsvExportHelpersTests
{
	private readonly Fixture fixture;

	public CsvExportHelpersTests()
	{
		fixture = new Fixture();
	}

	private sealed record TestRecord(string Name, int Age, DateTime BirthDate);

	[Fact]
	public async Task ExportListToCsv_Generic_WithValidData_ShouldCreateValidCsvContent()
	{
		// Arrange
		IEnumerable<TestRecord> testData = fixture.CreateMany<TestRecord>(3);
		await using MemoryStream memoryStream = new();

		// Act
		await using MemoryStream result = await testData.ExportToCsv(memoryStream, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		// Verify header
		csvContent.ShouldContain("Name,Age,BirthDate");

		// Verify data presence
		foreach (TestRecord record in testData)
		{
			csvContent.ShouldContain(record.Name);
			csvContent.ShouldContain(record.Age.ToString());
			csvContent.ShouldContain(record.BirthDate.ToString(CultureInfo.InvariantCulture));
		}
	}

	[Fact]
	public async Task ExportListToCsv_Generic_WithNullStream_ShouldCreateNewStream()
	{
		// Arrange
		IEnumerable<TestRecord> testData = fixture.CreateMany<TestRecord>(1);

		// Act
		await using MemoryStream result = await testData.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);
	}

	[Theory]
	[InlineData("Column1,Column2", "Value1,Value2", 2)]
	[InlineData("Name,Age,Location", "John,30,\"New York\"", 3)]
	public async Task ExportListToCsv_DataTable_ShouldCreateValidCsvContent(string headerRow, string dataRow, int columnCount)
	{
		// Arrange
		using DataTable dataTable = new();
		string[] headers = headerRow.Split(',');
		string[] values = dataRow.Split(',');

		for (int i = 0; i < columnCount; i++)
		{
			dataTable.Columns.Add(headers[i]);
		}

		dataTable.Rows.Add(values);

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		// Verify content
		string[] lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		lines.Length.ShouldBe(2); // Header + data row
		lines[0].ShouldBe(headerRow);
		lines[1].ShouldBe(dataRow);
	}

	[Fact]
	public async Task ExportListToCsv_DataTable_WithNullValues_ShouldHandleNullsProperly()
	{
		// Arrange
		using DataTable dataTable = new();
		dataTable.Columns.Add("Column1");
		dataTable.Columns.Add("Column2");

		DataRow row = dataTable.NewRow();
		row["Column1"] = DBNull.Value;
		row["Column2"] = "Value2";
		dataTable.Rows.Add(row);

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		string[] lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		lines[1].ShouldBe(",Value2");
	}

	[Fact]
	public async Task ExportListToCsv_DataTable_WithCommasInData_ShouldQuoteValues()
	{
		// Arrange
		using DataTable dataTable = new();
		dataTable.Columns.Add("Description");

		const string valueWithComma = "First, Second";
		dataTable.Rows.Add(valueWithComma);

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		string[] lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		lines[1].ShouldBe($"\"{valueWithComma}\"");
	}

	[Fact]
	public async Task ExportListToCsv_Generic_WithExistingStream_ShouldUseProvidedStream()
	{
		// Arrange
		IEnumerable<TestRecord> testData = fixture.CreateMany<TestRecord>(2);
		await using MemoryStream providedStream = new();

		// Act
		await using MemoryStream result = await testData.ExportToCsv(providedStream, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeSameAs(providedStream);
		result.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task ExportListToCsv_DataTable_WithExistingStream_ShouldUseProvidedStream()
	{
		// Arrange
		using DataTable dataTable = new();
		dataTable.Columns.Add("Column1");
		dataTable.Rows.Add("Value1");

		await using MemoryStream providedStream = new();

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(providedStream, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeSameAs(providedStream);
		result.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task ExportListToCsv_Generic_WithEmptyList_ShouldCreateHeaderOnly()
	{
		// Arrange
		IEnumerable<TestRecord> emptyData = [];

		// Act
		await using MemoryStream result = await emptyData.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		// Should contain header
		csvContent.ShouldContain("Name,Age,BirthDate");
	}

	[Fact]
	public async Task ExportListToCsv_DataTable_WithEmptyTable_ShouldCreateHeaderOnly()
	{
		// Arrange
		using DataTable dataTable = new();
		dataTable.Columns.Add("Column1");
		dataTable.Columns.Add("Column2");

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		string[] lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		lines.Length.ShouldBe(1); // Header only
		lines[0].ShouldBe("Column1,Column2");
	}

	[Fact]
	public async Task ExportListToCsv_DataTable_WithMultipleRows_ShouldProcessAllRows()
	{
		// Arrange
		using DataTable dataTable = new();
		dataTable.Columns.Add("Name");
		dataTable.Columns.Add("Value");

		for (int i = 0; i < 5; i++)
		{
			dataTable.Rows.Add($"Name{i}", $"Value{i}");
		}

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		string[] lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		lines.Length.ShouldBe(6); // Header + 5 data rows

		for (int i = 0; i < 5; i++)
		{
			lines[i + 1].ShouldBe($"Name{i},Value{i}");
		}
	}

	[Fact]
	public async Task ExportListToCsv_DataTable_WithQuotesAndCommas_ShouldHandleCorrectly()
	{
		// Arrange
		using DataTable dataTable = new();
		dataTable.Columns.Add("Description");
		dataTable.Columns.Add("Notes");

		dataTable.Rows.Add("Test, with comma", "No comma");
		dataTable.Rows.Add("No comma", "Another, with comma");

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		string[] lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		lines[1].ShouldBe("\"Test, with comma\",No comma");
		lines[2].ShouldBe("No comma,\"Another, with comma\"");
	}

	[Fact]
	public async Task ExportListToCsv_DataTable_WithSingleColumn_ShouldNotAddTrailingComma()
	{
		// Arrange
		using DataTable dataTable = new();
		dataTable.Columns.Add("SingleColumn");
		dataTable.Rows.Add("Value1");
		dataTable.Rows.Add("Value2");

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		string[] lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		lines[0].ShouldBe("SingleColumn");
		lines[1].ShouldBe("Value1");
		lines[2].ShouldBe("Value2");
	}

	[Fact]
	public async Task ExportListToCsv_DataTable_WithAllNullValues_ShouldCreateEmptyFields()
	{
		// Arrange
		using DataTable dataTable = new();
		dataTable.Columns.Add("Column1");
		dataTable.Columns.Add("Column2");
		dataTable.Columns.Add("Column3");

		DataRow row = dataTable.NewRow();
		row["Column1"] = DBNull.Value;
		row["Column2"] = DBNull.Value;
		row["Column3"] = DBNull.Value;
		dataTable.Rows.Add(row);

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		string[] lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		lines[1].ShouldBe(",,");
	}

	[Fact]
	public async Task ExportListToCsv_Generic_WithSpecialCharacters_ShouldHandleCorrectly()
	{
		// Arrange
		IEnumerable<TestRecord> testData =
		[
			new TestRecord("Name with, comma", 25, DateTime.Now),
			new TestRecord("Name with \"quotes\"", 30, DateTime.Now),
			new TestRecord("Normal Name", 35, DateTime.Now)
		];

		// Act
		await using MemoryStream result = await testData.ExportToCsv(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBeGreaterThan(0);

		result.Position = 0;
		using StreamReader reader = new(result);
		string csvContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

		// CsvHelper should handle quoting automatically
		csvContent.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task ExportListToCsv_Generic_WithStreamThatThrowsOnWrite_ShouldHandleException()
	{
		// Arrange
		IEnumerable<TestRecord> testData = fixture.CreateMany<TestRecord>(3);
		await using ThrowingStream throwingStream = new();

		// Act
		await using MemoryStream result = await testData.ExportToCsv(throwingStream, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		// The method should handle the exception and return the stream
		result.ShouldNotBeNull();
		// Stream position is reset even after exception
		result.Position.ShouldBe(0);
	}

	[Fact]
	public async Task ExportListToCsv_DataTable_WithStreamThatThrowsOnWrite_ShouldHandleException()
	{
		// Arrange
		DataTable dataTable = new("TestTable");
		dataTable.Columns.Add("Name", typeof(string));
		dataTable.Columns.Add("Age", typeof(int));

		DataRow row = dataTable.NewRow();
		row["Name"] = "John";
		row["Age"] = 30;
		dataTable.Rows.Add(row);

		await using ThrowingStream throwingStream = new();

		// Act
		await using MemoryStream result = await dataTable.ExportToCsv(throwingStream, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		// The method should handle the exception and return the stream
		result.ShouldNotBeNull();
		// Stream position is reset even after exception
		result.Position.ShouldBe(0);
	}

	private sealed class ThrowingStream : MemoryStream
	{
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new IOException("Simulated write error");
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			throw new IOException("Simulated write error");
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			throw new IOException("Simulated write error");
		}
	}
}
