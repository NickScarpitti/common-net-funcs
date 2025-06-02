using System.Globalization;
using AutoFixture;
using CommonNetFuncs.Csv;
using Shouldly;

namespace Csv.Tests;

public sealed class CsvReadHelpersTests
{
    private readonly Fixture _fixture;
    private readonly string _testFilePath;

    public CsvReadHelpersTests()
    {
        _fixture = new Fixture();
        _testFilePath = Path.GetTempFileName();
    }

    private sealed record TestRecord(string Name, int Age, DateTime BirthDate);

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, null)]
    [InlineData(true, "en-US")]
    public void ReadCsv_WithValidFile_ShouldReturnExpectedRecords(bool hasHeader, string? cultureName)
    {
        // Arrange
        List<TestRecord> expectedRecords = _fixture.CreateMany<TestRecord>(3).ToList();
        CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

        string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
        File.WriteAllText(_testFilePath, csvContent);

        try
        {
            // Act
            List<TestRecord> result = CsvReadHelpers.ReadCsv<TestRecord>(_testFilePath, hasHeader, cultureInfo);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(expectedRecords.Count);
            for (int i = 0; i < result.Count; i++)
            {
                result[i].Name.ShouldBe(expectedRecords[i].Name);
                result[i].Age.ShouldBe(expectedRecords[i].Age);
                result[i].BirthDate.ShouldBe(expectedRecords[i].BirthDate);
            }
        }
        finally
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public void ReadCsv_WithInvalidFilePath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        string invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => CsvReadHelpers.ReadCsv<TestRecord>(invalidPath));
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, null)]
    [InlineData(true, "en-US")]
    public void ReadCsvFromStream_WithValidStream_ShouldReturnExpectedRecords(bool hasHeader, string? cultureName)
    {
        // Arrange
        List<TestRecord> expectedRecords = _fixture.CreateMany<TestRecord>(3).ToList();
        CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

        string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        List<TestRecord> result = CsvReadHelpers.ReadCsvFromStream<TestRecord>(stream, hasHeader, cultureInfo);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(expectedRecords.Count);
        for (int i = 0; i < result.Count; i++)
        {
            result[i].Name.ShouldBe(expectedRecords[i].Name);
            result[i].Age.ShouldBe(expectedRecords[i].Age);
            result[i].BirthDate.ShouldBe(expectedRecords[i].BirthDate);
        }
    }

    [Fact]
    public void ReadCsvFromStream_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => CsvReadHelpers.ReadCsvFromStream<TestRecord>(null!));
    }

    [Fact]
    public void ReadCsvFromStream_WithEmptyStream_ShouldReturnEmptyList()
    {
        // Arrange
        using MemoryStream emptyStream = new();

        // Act
        List<TestRecord> result = CsvReadHelpers.ReadCsvFromStream<TestRecord>(emptyStream);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    private static string GenerateCsvContent(IEnumerable<TestRecord> records, bool includeHeader)
    {
        using StringWriter writer = new();

        if (includeHeader)
        {
            writer.WriteLine("Name,Age,BirthDate");
        }

        foreach (TestRecord record in records)
        {
            writer.WriteLine($"{record.Name},{record.Age},{record.BirthDate:O}");
        }

        return writer.ToString();
    }
}
