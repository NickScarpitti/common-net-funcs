using System.Data;
using System.Globalization;
using CommonNetFuncs.Csv;

namespace Csv.Tests;

public sealed class CsvReadHelpersTests
{
	private readonly Fixture fixture;
	private readonly string testFilePath;

	public CsvReadHelpersTests()
	{
		fixture = new Fixture();
		testFilePath = Path.GetTempFileName();
	}

	private sealed record TestRecord(string Name, int Age, DateTime BirthDate);

	[Theory]
	[InlineData(true, null)]
	[InlineData(false, null)]
	[InlineData(true, "en-US")]
	public async Task ReadCsv_WithValidFile_ShouldReturnExpectedRecords(bool hasHeader, string? cultureName)
	{
		// Arrange
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		await File.WriteAllTextAsync(testFilePath, csvContent, TestContext.Current.CancellationToken);

		try
		{
			// Act
#pragma warning disable S6966 // Awaitable method should be used
			List<TestRecord> result = CsvReadHelpers.ReadCsv<TestRecord>(testFilePath, hasHeader, cultureInfo);
#pragma warning restore S6966 // Awaitable method should be used

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
			File.Delete(testFilePath);
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
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(csvContent));

		// Act
		List<TestRecord> result = CsvReadHelpers.ReadCsv<TestRecord>(stream, hasHeader, cultureInfo);

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
		Should.Throw<ArgumentNullException>(() => CsvReadHelpers.ReadCsv<TestRecord>((Stream)null!));
	}

	[Fact]
	public void ReadCsvFromStream_WithEmptyStream_ShouldReturnEmptyList()
	{
		// Arrange
		using MemoryStream emptyStream = new();

		// Act
		List<TestRecord> result = CsvReadHelpers.ReadCsv<TestRecord>(emptyStream);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(true, null)]
	[InlineData(false, null)]
	[InlineData(true, "en-US")]
	public async Task ReadCsvAsync_WithValidFile_ShouldReturnExpectedRecords(bool hasHeader, string? cultureName)
	{
		// Arrange
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		await File.WriteAllTextAsync(testFilePath, csvContent, TestContext.Current.CancellationToken);

		try
		{
			// Act
			List<TestRecord> result = await CsvReadHelpers.ReadCsvAsync<TestRecord>(testFilePath, hasHeader, cultureInfo, cancellationToken: TestContext.Current.CancellationToken);

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
			File.Delete(testFilePath);
		}
	}

	[Fact]
	public async Task ReadCsvAsync_WithInvalidFilePath_ShouldThrowFileNotFoundException()
	{
		// Arrange
		string invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		// Act & Assert
		await Should.ThrowAsync<FileNotFoundException>(async () => await CsvReadHelpers.ReadCsvAsync<TestRecord>(invalidPath));
	}

	[Theory]
	[InlineData(true, null)]
	[InlineData(false, null)]
	[InlineData(true, "en-US")]
	public async Task ReadCsvFromStreamAsync_WithValidStream_ShouldReturnExpectedRecords(bool hasHeader, string? cultureName)
	{
		// Arrange
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		await using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(csvContent));

		// Act
		List<TestRecord> result = await CsvReadHelpers.ReadCsvAsync<TestRecord>(stream, hasHeader, cultureInfo, cancellationToken: TestContext.Current.CancellationToken);

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
	public async Task ReadCsvFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		await Should.ThrowAsync<ArgumentNullException>(async () => await CsvReadHelpers.ReadCsvAsync<TestRecord>((Stream)null!));
	}

	[Fact]
	public async Task ReadCsvFromStreamAsync_WithEmptyStream_ShouldReturnEmptyList()
	{
		// Arrange
		await using MemoryStream emptyStream = new();

		// Act
		List<TestRecord> result = await CsvReadHelpers.ReadCsvAsync<TestRecord>(emptyStream, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(true, null)]
	[InlineData(false, null)]
	[InlineData(true, "en-US")]
	public async Task ReadCsvAsyncEnumerable_WithValidFile_ShouldYieldExpectedRecords(bool hasHeader, string? cultureName)
	{
		// Arrange
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		await File.WriteAllTextAsync(testFilePath, csvContent, TestContext.Current.CancellationToken);

		try
		{
			// Act
			List<TestRecord> result = new();
			await foreach (TestRecord record in CsvReadHelpers.ReadCsvAsyncEnumerable<TestRecord>(testFilePath, hasHeader, cultureInfo, cancellationToken: TestContext.Current.CancellationToken))
			{
				result.Add(record);
			}

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
			File.Delete(testFilePath);
		}
	}

	[Fact]
	public async Task ReadCsvAsyncEnumerable_WithInvalidFilePath_ShouldThrowFileNotFoundException()
	{
		// Arrange
		string invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		// Act & Assert
		await Should.ThrowAsync<FileNotFoundException>(async () =>
		{
			await foreach (TestRecord _ in CsvReadHelpers.ReadCsvAsyncEnumerable<TestRecord>(invalidPath))
			{
				// Should not reach here
			}
		});
	}

	[Theory]
	[InlineData(true, null)]
	[InlineData(false, null)]
	[InlineData(true, "en-US")]
	public async Task ReadCsvFromStreamAsyncEnumerable_WithValidStream_ShouldYieldExpectedRecords(bool hasHeader, string? cultureName)
	{
		// Arrange
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		await using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(csvContent));

		// Act
		List<TestRecord> result = new();
		await foreach (TestRecord record in CsvReadHelpers.ReadCsvAsyncEnumerable<TestRecord>(stream, hasHeader, cultureInfo, cancellationToken: TestContext.Current.CancellationToken))
		{
			result.Add(record);
		}

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
	public async Task ReadCsvFromStreamAsyncEnumerable_WithNullStream_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		await Should.ThrowAsync<ArgumentNullException>(async () =>
		{
			await foreach (TestRecord _ in CsvReadHelpers.ReadCsvAsyncEnumerable<TestRecord>((Stream)null!))
			{
				// Should not reach here
			}
		});
	}

	[Fact]
	public async Task ReadCsvFromStreamAsyncEnumerable_WithEmptyStream_ShouldYieldNoRecords()
	{
		// Arrange
		await using MemoryStream emptyStream = new();

		// Act
		List<TestRecord> result = new();
		await foreach (TestRecord record in CsvReadHelpers.ReadCsvAsyncEnumerable<TestRecord>(emptyStream, cancellationToken: TestContext.Current.CancellationToken))
		{
			result.Add(record);
		}

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(true, null)]
	[InlineData(false, null)]
	[InlineData(true, "en-US")]
	public async Task ReadCsvToDataTable_WithValidFile_ShouldReturnExpectedDataTable(bool hasHeader, string? cultureName)
	{
		// Arrange
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		await File.WriteAllTextAsync(testFilePath, csvContent, TestContext.Current.CancellationToken);

		try
		{
			// Act
			using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(testFilePath, hasHeader, cultureInfo);

			// Assert
			dataTable.ShouldNotBeNull();
			dataTable.Rows.Count.ShouldBe(expectedRecords.Count);
			if (hasHeader)
			{
				dataTable.Columns.Contains("Name").ShouldBeTrue();
				dataTable.Columns.Contains("Age").ShouldBeTrue();
				dataTable.Columns.Contains("BirthDate").ShouldBeTrue();
			}
		}
		finally
		{
			File.Delete(testFilePath);
		}
	}

	[Fact]
	public void ReadCsvToDataTable_WithInvalidFilePath_ShouldThrowFileNotFoundException()
	{
		// Arrange
		string invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		// Act & Assert
		Should.Throw<FileNotFoundException>(() => CsvReadHelpers.ReadCsvToDataTable(invalidPath));
	}

	[Theory]
	[InlineData(true, null)]
	[InlineData(false, null)]
	[InlineData(true, "en-US")]
	public async Task ReadCsvToDataTable_WithType_WithValidFile_ShouldReturnExpectedDataTable(bool hasHeader, string? cultureName)
	{
		// Arrange
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		await File.WriteAllTextAsync(testFilePath, csvContent, TestContext.Current.CancellationToken);

		try
		{
			// Act
			using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(testFilePath, typeof(TestRecord), hasHeader, cultureInfo);

			// Assert
			dataTable.ShouldNotBeNull();
			dataTable.Rows.Count.ShouldBe(expectedRecords.Count);
			dataTable.Columns.Contains("Name").ShouldBeTrue();
			dataTable.Columns.Contains("Age").ShouldBeTrue();
			dataTable.Columns.Contains("BirthDate").ShouldBeTrue();
		}
		finally
		{
			File.Delete(testFilePath);
		}
	}

	[Fact]
	public void ReadCsvToDataTable_WithType_WithInvalidFilePath_ShouldThrowFileNotFoundException()
	{
		// Arrange
		string invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		// Act & Assert
		Should.Throw<FileNotFoundException>(() => CsvReadHelpers.ReadCsvToDataTable(invalidPath, typeof(TestRecord)));
	}

	[Theory]
	[InlineData(true, null)]
	[InlineData(false, null)]
	[InlineData(true, "en-US")]
	public void ReadCsvStreamToDataTable_WithValidStream_ShouldReturnExpectedDataTable(bool hasHeader, string? cultureName)
	{
		// Arrange
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(csvContent));

		// Act
		using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(stream, hasHeader, cultureInfo);

		// Assert
		dataTable.ShouldNotBeNull();
		dataTable.Rows.Count.ShouldBe(expectedRecords.Count);
		if (hasHeader)
		{
			dataTable.Columns.Contains("Name").ShouldBeTrue();
			dataTable.Columns.Contains("Age").ShouldBeTrue();
			dataTable.Columns.Contains("BirthDate").ShouldBeTrue();
		}
	}

	[Fact]
	public void ReadCsvStreamToDataTable_WithNullStream_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => CsvReadHelpers.ReadCsvToDataTable((Stream)null!));
	}

	[Fact]
	public void ReadCsvStreamToDataTable_WithEmptyStream_ShouldReturnEmptyDataTable()
	{
		// Arrange
		using MemoryStream emptyStream = new();

		// Act
		using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(emptyStream, false);

		// Assert
		dataTable.ShouldNotBeNull();
		dataTable.Rows.Count.ShouldBe(0);
	}

	[Theory]
	[InlineData(true, null)]
	[InlineData(false, null)]
	[InlineData(true, "en-US")]
	public void ReadCsvStreamToDataTable_WithType_WithValidStream_ShouldReturnExpectedDataTable(bool hasHeader, string? cultureName)
	{
		// Arrange
		List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
		CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

		string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
		using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(csvContent));

		// Act
		using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(stream, typeof(TestRecord), hasHeader, cultureInfo);

		// Assert
		dataTable.ShouldNotBeNull();
		dataTable.Rows.Count.ShouldBe(expectedRecords.Count);
		dataTable.Columns.Contains("Name").ShouldBeTrue();
		dataTable.Columns.Contains("Age").ShouldBeTrue();
		dataTable.Columns.Contains("BirthDate").ShouldBeTrue();
	}

	[Fact]
	public void ReadCsvStreamToDataTable_WithType_WithNullStream_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => CsvReadHelpers.ReadCsvToDataTable((Stream)null!, typeof(TestRecord)));
	}

	[Fact]
	public void ReadCsvStreamToDataTable_WithType_WithEmptyStream_ShouldReturnEmptyDataTable()
	{
		// Arrange
		using MemoryStream emptyStream = new();

		// Act
		using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(emptyStream, typeof(TestRecord), false);

		// Assert
		dataTable.ShouldNotBeNull();
		dataTable.Rows.Count.ShouldBe(0);
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
