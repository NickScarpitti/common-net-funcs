using System.Data;
using System.Globalization;
using CommonNetFuncs.Csv;

namespace Csv.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly
public sealed class CsvExportHelpersTests
{
    private readonly Fixture _fixture;

    public CsvExportHelpersTests()
    {
        _fixture = new Fixture();
    }

    private sealed record TestRecord(string Name, int Age, DateTime BirthDate);

    [Fact]
    public async Task ExportListToCsv_Generic_WithValidData_ShouldCreateValidCsvContent()
    {
        // Arrange
        IEnumerable<TestRecord> testData = _fixture.CreateMany<TestRecord>(3);
        await using MemoryStream memoryStream = new();

        // Act
        await using MemoryStream result = await testData.ExportToCsv(memoryStream);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        result.Position = 0;
        using StreamReader reader = new(result);
        string csvContent = await reader.ReadToEndAsync();

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
        IEnumerable<TestRecord> testData = _fixture.CreateMany<TestRecord>(1);

        // Act
        await using MemoryStream result = await testData.ExportToCsv();

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
        await using MemoryStream result = await dataTable.ExportToCsv();

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        result.Position = 0;
        using StreamReader reader = new(result);
        string csvContent = await reader.ReadToEndAsync();

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
        await using MemoryStream result = await dataTable.ExportToCsv();

        // Assert
        result.ShouldNotBeNull();
        result.Position = 0;
        using StreamReader reader = new(result);
        string csvContent = await reader.ReadToEndAsync();

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
        await using MemoryStream result = await dataTable.ExportToCsv();

        // Assert
        result.ShouldNotBeNull();
        result.Position = 0;
        using StreamReader reader = new(result);
        string csvContent = await reader.ReadToEndAsync();

        string[] lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines[1].ShouldBe($"\"{valueWithComma}\"");
    }
}

#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
