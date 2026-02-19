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

	public enum CsvReadMethodType
	{
		Sync,
		Async,
		AsyncEnumerable,
		DataTable,
		DataTableWithType
	}

	//[Theory]
	//[InlineData(true, null)]
	//[InlineData(false, null)]
	//[InlineData(true, "en-US")]
	//public void ReadCsv_WithValidFile_ShouldReturnExpectedRecords(bool hasHeader, string? cultureName)
	//{
	//    // Arrange
	//    List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
	//    CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

	//    string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
	//    await File.WriteAllTextAsync(_testFilePath, csvContent);

	//    try
	//    {
	//        // Act
	//        List<TestRecord> result = CsvReadHelpers.ReadCsv<TestRecord>(_testFilePath, hasHeader, cultureInfo);

	//        // Assert
	//        result.ShouldNotBeNull();
	//        result.Count.ShouldBe(expectedRecords.Count);
	//        for (int i = 0; i < result.Count; i++)
	//        {
	//            result[i].Name.ShouldBe(expectedRecords[i].Name);
	//            result[i].Age.ShouldBe(expectedRecords[i].Age);
	//            result[i].BirthDate.ShouldBe(expectedRecords[i].BirthDate);
	//        }
	//    }
	//    finally
	//    {
	//        File.Delete(_testFilePath);
	//    }
	//}

	//[Fact]
	//public void ReadCsv_WithInvalidFilePath_ShouldThrowFileNotFoundException()
	//{
	//    // Arrange
	//    string invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

	//    // Act & Assert
	//    Should.Throw<FileNotFoundException>(() => CsvReadHelpers.ReadCsv<TestRecord>(invalidPath));
	//}

	//[Theory]
	//[InlineData(true, null)]
	//[InlineData(false, null)]
	//[InlineData(true, "en-US")]
	//public void ReadCsvFromStream_WithValidStream_ShouldReturnExpectedRecords(bool hasHeader, string? cultureName)
	//{
	//    // Arrange
	//    List<TestRecord> expectedRecords = fixture.CreateMany<TestRecord>(3).ToList();
	//    CultureInfo? cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

	//    string csvContent = GenerateCsvContent(expectedRecords, hasHeader);
	//    using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(csvContent));

	//    // Act
	//    List<TestRecord> result = CsvReadHelpers.ReadCsv<TestRecord>(stream, hasHeader, cultureInfo);

	//    // Assert
	//    result.ShouldNotBeNull();
	//    result.Count.ShouldBe(expectedRecords.Count);
	//    for (int i = 0; i < result.Count; i++)
	//    {
	//        result[i].Name.ShouldBe(expectedRecords[i].Name);
	//        result[i].Age.ShouldBe(expectedRecords[i].Age);
	//        result[i].BirthDate.ShouldBe(expectedRecords[i].BirthDate);
	//    }
	//}

	//[Fact]
	//public void ReadCsvFromStream_WithNullStream_ShouldThrowArgumentNullException()
	//{
	//    // Act & Assert
	//    Should.Throw<ArgumentNullException>(() => CsvReadHelpers.ReadCsv<TestRecord>(null!));
	//}

	//[Fact]
	//public void ReadCsvFromStream_WithEmptyStream_ShouldReturnEmptyList()
	//{
	//    // Arrange
	//    using MemoryStream emptyStream = new();

	//    // Act
	//    List<TestRecord> result = CsvReadHelpers.ReadCsv<TestRecord>(emptyStream);

	//    // Assert
	//    result.ShouldNotBeNull();
	//    result.ShouldBeEmpty();
	//}

	//private static string GenerateCsvContent(IEnumerable<TestRecord> records, bool includeHeader)
	//{
	//    using StringWriter writer = new();

	//    if (includeHeader)
	//    {
	//        writer.WriteLine("Name,Age,BirthDate");
	//    }

	//    foreach (TestRecord record in records)
	//    {
	//        writer.WriteLine($"{record.Name},{record.Age},{record.BirthDate:O}");
	//    }

	//    return writer.ToString();
	//}

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

	#region Consolidated Validation Tests

	[Theory]
	[InlineData(CsvReadMethodType.Sync)]
	[InlineData(CsvReadMethodType.Async)]
	[InlineData(CsvReadMethodType.AsyncEnumerable)]
	[InlineData(CsvReadMethodType.DataTable)]
	[InlineData(CsvReadMethodType.DataTableWithType)]
	public async Task ReadCsv_AllMethods_WithInvalidFilePath_ShouldThrowFileNotFoundException(CsvReadMethodType methodType)
	{
		// Arrange
		string invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		// Act & Assert
		switch (methodType)
		{
			case CsvReadMethodType.Sync:
				Should.Throw<FileNotFoundException>(() => CsvReadHelpers.ReadCsv<TestRecord>(invalidPath));
				break;
			case CsvReadMethodType.Async:
				await Should.ThrowAsync<FileNotFoundException>(async () => await CsvReadHelpers.ReadCsvAsync<TestRecord>(invalidPath, cancellationToken: TestContext.Current.CancellationToken));
				break;
			case CsvReadMethodType.AsyncEnumerable:
				await Should.ThrowAsync<FileNotFoundException>(async () =>
				{
					await foreach (TestRecord _ in CsvReadHelpers.ReadCsvAsyncEnumerable<TestRecord>(invalidPath, cancellationToken: TestContext.Current.CancellationToken))
					{
						// Should not reach here
					}
				});
				break;
			case CsvReadMethodType.DataTable:
				Should.Throw<FileNotFoundException>(() => CsvReadHelpers.ReadCsvToDataTable(invalidPath));
				break;
			case CsvReadMethodType.DataTableWithType:
				Should.Throw<FileNotFoundException>(() => CsvReadHelpers.ReadCsvToDataTable(invalidPath, typeof(TestRecord)));
				break;
		}
	}

	[Theory]
	[InlineData(CsvReadMethodType.Sync)]
	[InlineData(CsvReadMethodType.Async)]
	[InlineData(CsvReadMethodType.AsyncEnumerable)]
	[InlineData(CsvReadMethodType.DataTable)]
	[InlineData(CsvReadMethodType.DataTableWithType)]
	public async Task ReadCsv_AllMethods_WithNullStream_ShouldThrowArgumentNullException(CsvReadMethodType methodType)
	{
		// Act & Assert
		switch (methodType)
		{
			case CsvReadMethodType.Sync:
				Should.Throw<ArgumentNullException>(() => CsvReadHelpers.ReadCsv<TestRecord>((Stream)null!));
				break;
			case CsvReadMethodType.Async:
				await Should.ThrowAsync<ArgumentNullException>(async () => await CsvReadHelpers.ReadCsvAsync<TestRecord>((Stream)null!, cancellationToken: TestContext.Current.CancellationToken));
				break;
			case CsvReadMethodType.AsyncEnumerable:
				await Should.ThrowAsync<ArgumentNullException>(async () =>
				{
					await foreach (TestRecord _ in CsvReadHelpers.ReadCsvAsyncEnumerable<TestRecord>((Stream)null!, cancellationToken: TestContext.Current.CancellationToken))
					{
						// Should not reach here
					}
				});
				break;
			case CsvReadMethodType.DataTable:
				Should.Throw<ArgumentNullException>(() => CsvReadHelpers.ReadCsvToDataTable((Stream)null!));
				break;
			case CsvReadMethodType.DataTableWithType:
				Should.Throw<ArgumentNullException>(() => CsvReadHelpers.ReadCsvToDataTable((Stream)null!, typeof(TestRecord)));
				break;
		}
	}

	[Theory]
	[InlineData(CsvReadMethodType.Sync)]
	[InlineData(CsvReadMethodType.Async)]
	[InlineData(CsvReadMethodType.AsyncEnumerable)]
	[InlineData(CsvReadMethodType.DataTable)]
	[InlineData(CsvReadMethodType.DataTableWithType)]
	public async Task ReadCsv_AllMethods_WithEmptyStream_ShouldReturnEmpty(CsvReadMethodType methodType)
	{
		// Arrange & Act & Assert
		switch (methodType)
		{
			case CsvReadMethodType.Sync:
				{
					using MemoryStream emptyStream = new();

#pragma warning disable S6966
					List<TestRecord> result = CsvReadHelpers.ReadCsv<TestRecord>(emptyStream);
#pragma warning restore S6966

					result.ShouldNotBeNull();
					result.ShouldBeEmpty();
					break;
				}
			case CsvReadMethodType.Async:
				{
					await using MemoryStream emptyStream = new();
					List<TestRecord> result = await CsvReadHelpers.ReadCsvAsync<TestRecord>(emptyStream, cancellationToken: TestContext.Current.CancellationToken);
					result.ShouldNotBeNull();
					result.ShouldBeEmpty();
					break;
				}
			case CsvReadMethodType.AsyncEnumerable:
				{
					await using MemoryStream emptyStream = new();
					List<TestRecord> result = new();
					await foreach (TestRecord record in CsvReadHelpers.ReadCsvAsyncEnumerable<TestRecord>(emptyStream, cancellationToken: TestContext.Current.CancellationToken))
					{
						result.Add(record);
					}
					result.ShouldNotBeNull();
					result.ShouldBeEmpty();
					break;
				}
			case CsvReadMethodType.DataTable:
				{
					using MemoryStream emptyStream = new();
					using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(emptyStream, false);
					dataTable.ShouldNotBeNull();
					dataTable.Rows.Count.ShouldBe(0);
					break;
				}
			case CsvReadMethodType.DataTableWithType:
				{
					await using MemoryStream emptyStream = new();
					using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(emptyStream, typeof(TestRecord), false);
					dataTable.ShouldNotBeNull();
					dataTable.Rows.Count.ShouldBe(0);
					break;
				}
		}
	}

	#endregion

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
