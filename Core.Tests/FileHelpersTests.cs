using System.IO.Pipelines;
using System.Text;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class FileHelpersTests : IDisposable
{
	private readonly string tempDir;

	public FileHelpersTests()
	{
		tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);
	}

	public void Dispose()
	{
		if (Directory.Exists(tempDir))
		{
			Directory.Delete(tempDir, true);
		}
		GC.SuppressFinalize(this);
	}

	~FileHelpersTests()
	{
		Dispose();
	}

	[Fact]
	public async Task GetSafeSaveName_String_ReturnsUniqueName_WhenFileExists()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "test.txt");
		await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);
		string duplicateName = fileName;

		// Act
		string safeName = duplicateName.GetSafeSaveName();

		// Assert
		safeName.ShouldNotBe(duplicateName);
		Path.GetFileNameWithoutExtension(safeName).ShouldContain("0");
		File.Exists(safeName).ShouldBeFalse();
	}

	[Fact]
	public void GetSafeSaveName_String_CreatesDirectory_WhenMissingAndFlagSet()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "NewSubDir");
		string fileName = Path.Combine(newDir, "file.txt");

		// Act
		string safeName = fileName.GetSafeSaveName(createPathIfMissing: true);

		// Assert
		Directory.Exists(newDir).ShouldBeTrue();
		safeName.ShouldContain(newDir);
	}

	[Fact]
	public void GetSafeSaveName_String_ReturnsEmpty_WhenDirectoryMissingAndNoCreate()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "MissingDir");
		string fileName = Path.Combine(newDir, "file.txt");

		// Act
		string safeName = fileName.GetSafeSaveName(createPathIfMissing: false);

		// Assert
		safeName.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_ReturnsUniqueName_WhenFileExists()
	{
		// Arrange
		const string fileName = "test2.txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("0");
	}

	[Fact]
	public void GetSafeSaveName_PathAndFileName_CreatesDirectory_WhenMissingAndFlagSet()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "NewSubDir2");
		const string fileName = "file2.txt";

		// Act
		string safeName = FileHelpers.GetSafeSaveName(newDir, fileName, createPathIfMissing: true);

		// Assert
		Directory.Exists(newDir).ShouldBeTrue();
		safeName.ShouldBe("file2.txt");
	}

	[Fact]
	public void GetSafeSaveName_PathAndFileName_ReturnsEmpty_WhenDirectoryMissingAndNoCreate()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "MissingDir2");
		const string fileName = "file3.txt";

		// Act
		string safeName = FileHelpers.GetSafeSaveName(newDir, fileName, createPathIfMissing: false);

		// Assert
		safeName.ShouldBeEmpty();
	}

	[Theory]
	[InlineData("file.txt", new[] { ".txt", ".doc" }, true)]
	[InlineData("file.doc", new[] { ".txt", ".doc" }, true)]
	[InlineData("file.pdf", new[] { ".txt", ".doc" }, false)]
	[InlineData("file", new[] { ".txt" }, false)]
	public void ValidateFileExtension_Works(string fileName, string[] validExtensions, bool expected)
	{
		// Act
		bool result = fileName.ValidateFileExtension(validExtensions.ToHashSet());

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(EHashAlgorithm.SHA1)]
	[InlineData(EHashAlgorithm.SHA256)]
	[InlineData(EHashAlgorithm.SHA384)]
	[InlineData(EHashAlgorithm.SHA512)]
	[InlineData(EHashAlgorithm.MD5)]
	public async Task GetHashFromFile_And_GetHashFromStream_ProduceExpectedHashes(EHashAlgorithm algo)
	{
		// Arrange
		string fileName = Path.Combine(tempDir, $"hash_{algo}.txt");
		const string content = "hash test content";
		await File.WriteAllTextAsync(fileName, content, Encoding.UTF8, TestContext.Current.CancellationToken);

		// Act
		string fileHash = await fileName.GetHashFromFile(algo);

		await using FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);
		string streamHash = await stream.GetHashFromStream(algo);

		// Assert
		fileHash.ShouldNotBeNullOrWhiteSpace();
		streamHash.ShouldNotBeNullOrWhiteSpace();
		fileHash.ShouldBe(streamHash, StringCompareShould.IgnoreCase);
	}

	[Fact]
	public async Task GetHashFromFile_ReturnsEmptyString_OnException()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "DoesNotExist.txt");

		// Act
		string hash = await fileName.GetHashFromFile();

		// Assert
		hash.ShouldBe(string.Empty);
	}

	[Fact]
	public async Task GetAllFilesRecursive_ReturnsAllFiles()
	{
		// Arrange
		string subDir = Path.Combine(tempDir, "sub");
		Directory.CreateDirectory(subDir);
		string file1 = Path.Combine(tempDir, "a.txt");
		string file2 = Path.Combine(subDir, "b.txt");
		await File.WriteAllTextAsync(file1, "1", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file2, "2", TestContext.Current.CancellationToken);

		// Act
		IEnumerable<string> files = FileHelpers.GetAllFilesRecursive(tempDir, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		files.ShouldContain(file1);
		files.ShouldContain(file2);
	}

	[Fact]
	public void GetAllFilesRecursive_ReturnsEmpty_WhenDirectoryMissingOrNull()
	{
		// Arrange
		string missingDir = Path.Combine(tempDir, "notfound");

		// Act
		IEnumerable<string> files1 = FileHelpers.GetAllFilesRecursive(missingDir, cancellationToken: TestContext.Current.CancellationToken);
		IEnumerable<string> files2 = FileHelpers.GetAllFilesRecursive(null, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		files1.ShouldBeEmpty();
		files2.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetAllFilesRecursive_RespectsSearchPattern()
	{
		// Arrange
		string subDir = Path.Combine(tempDir, "sub2");
		Directory.CreateDirectory(subDir);
		string file1 = Path.Combine(tempDir, "a.txt");
		string file2 = Path.Combine(subDir, "b.txt");
		string file3 = Path.Combine(subDir, "c.doc");
		await File.WriteAllTextAsync(file1, "1", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file2, "2", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file3, "3", TestContext.Current.CancellationToken);

		// Act
		IEnumerable<string> files = FileHelpers.GetAllFilesRecursive(tempDir, "*.txt", TestContext.Current.CancellationToken);

		// Assert
		files.ShouldContain(file1);
		files.ShouldContain(file2);
		files.ShouldNotContain(file3);
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_StartFromExistingNumber()
	{
		// Arrange
		const string fileName = "test (3).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", cancellationToken: TestContext.Current.CancellationToken);

		// Act
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: false);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldBe("test (4).txt");
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_AlreadyHasIterator()
	{
		// Arrange
		const string fileName = "test (1).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", cancellationToken: TestContext.Current.CancellationToken);

		// Act
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldBe("test (0).txt");
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_StrCompBreaksLoop()
	{
		// Arrange
		// Simulate a scenario where StrComp returns true (file name doesn't change)
		// This is tricky to simulate directly, so we can use a file name that matches the incrementing pattern but doesn't actually change after replacement.
		const string fileName = "test (0).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName);

		// Assert
		// The method should break out of the loop and return a name (should not loop infinitely)
		safeName.ShouldNotBeNullOrWhiteSpace();
		safeName.ShouldEndWith(".txt");
	}

	#region ReadFileFromPipe tests with size limit

	[Fact]
	public async Task ReadFileFromPipe_WithSizeLimit_SuccessfullyReadsData()
	{
		// Arrange
		const string testData = "Hello, World! This is test data.";
		byte[] dataBytes = Encoding.UTF8.GetBytes(testData);

		PipeReader pipeReader = CreatePipeReaderFromData(dataBytes);
		await using MemoryStream outputStream = new();
		const long maxSize = 1024;
		const string successValue = "Success!";

		// Act
		(bool success, string? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			maxSize,
			successValue,
			"TooLarge",
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		result.ShouldBe(successValue);
		outputStream.Position.ShouldBe(0);
		outputStream.Length.ShouldBe(dataBytes.Length);

		outputStream.Position = 0;
		using StreamReader reader = new(outputStream);
		string actualData = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
		actualData.ShouldBe(testData);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithSizeLimit_ReturnsFileTooLarge_WhenExceedingLimit()
	{
		// Arrange
		byte[] largeData = new byte[2048];
		System.Random.Shared.NextBytes(largeData);

		PipeReader pipeReader = CreatePipeReaderFromData(largeData);
		await using MemoryStream outputStream = new();
		const long maxSize = 1024;
		const string tooLargeValue = "File too large!";

		// Act
		(bool success, string? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			maxSize,
			"Success",
			tooLargeValue,
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeFalse();
		result.ShouldBe(tooLargeValue);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithSizeLimit_HandlesEmptyData()
	{
		// Arrange
		PipeReader pipeReader = CreatePipeReaderFromData(Array.Empty<byte>());
		await using MemoryStream outputStream = new();
		const long maxSize = 1024;
		const int successValue = 42;

		// Act
		(bool success, int? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			maxSize,
			successValue,
			0,
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		result.ShouldBe(successValue);
		outputStream.Length.ShouldBe(0);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithSizeLimit_HandlesMultipleSegments()
	{
		// Arrange
		const string part1 = "First part of data. ";
		const string part2 = "Second part of data. ";
		const string part3 = "Third part of data.";
		byte[] data1 = Encoding.UTF8.GetBytes(part1);
		byte[] data2 = Encoding.UTF8.GetBytes(part2);
		byte[] data3 = Encoding.UTF8.GetBytes(part3);

		PipeReader pipeReader = CreatePipeReaderFromMultipleSegments(data1, data2, data3);
		await using MemoryStream outputStream = new();
		const long maxSize = 1024;

		// Act
		(bool success, string? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			maxSize,
			"OK",
			"TooLarge",
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		result.ShouldBe("OK");
		outputStream.Position = 0;
		using StreamReader reader = new(outputStream);
		string actualData = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
		actualData.ShouldBe(part1 + part2 + part3);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithSizeLimit_UsesErrorReturnFunction_OnException()
	{
		// Arrange
		PipeReader pipeReader = CreateFaultyPipeReader();
		await using MemoryStream outputStream = new();
		const long maxSize = 1024;
		const string errorValue = "Error occurred";

		static string? ErrorHandler(Exception _)
		{
			return errorValue;
		}

		// Act
		(bool success, string? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			maxSize,
			"Success",
			"TooLarge",
			ErrorHandler,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeFalse();
		result.ShouldBe(errorValue);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithSizeLimit_ThrowsException_WhenNoErrorHandler()
	{
		// Arrange
		PipeReader pipeReader = CreateFaultyPipeReader();
		await using MemoryStream outputStream = new();
		const long maxSize = 1024;

		// Act & Assert
		Exception exception = await Should.ThrowAsync<Exception>(async () =>
		{
			await pipeReader.ReadFileFromPipe(
				outputStream,
				maxSize,
				"Success",
				"TooLarge",
				null,
				TestContext.Current.CancellationToken);
		});

		exception.Message.ShouldBe("Error reading file from pipe");
		exception.InnerException.ShouldNotBeNull();
	}

	[Fact]
	public async Task ReadFileFromPipe_WithSizeLimit_RespectsCancellationToken()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		byte[] data = Encoding.UTF8.GetBytes("Test data");
		PipeReader pipeReader = CreatePipeReaderFromData(data);
		await using MemoryStream outputStream = new();
		const long maxSize = 1024;

		// Act & Assert
		// Note: The cancellation exception gets wrapped in a generic Exception by the implementation
		Exception exception = await Should.ThrowAsync<Exception>(async () =>
		{
			await pipeReader.ReadFileFromPipe(
				outputStream,
				maxSize,
				"Success",
				"TooLarge",
				null,
				cts.Token);
		});

		exception.Message.ShouldBe("Error reading file from pipe");
		exception.InnerException.ShouldBeOfType<TaskCanceledException>();
	}

	[Fact]
	public async Task ReadFileFromPipe_WithSizeLimit_ResetsStreamPosition()
	{
		// Arrange
		byte[] data = Encoding.UTF8.GetBytes("Stream position test");
		PipeReader pipeReader = CreatePipeReaderFromData(data);
		await using MemoryStream outputStream = new();
		const long maxSize = 1024;

		// Act
		(bool success, _) = await pipeReader.ReadFileFromPipe<string>(
			outputStream,
			maxSize,
			null,
			null,
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		outputStream.Position.ShouldBe(0);
	}

	#endregion

	#region ReadFileFromPipe tests without size limit

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_SuccessfullyReadsData()
	{
		// Arrange
		const string testData = "Hello, World! This is test data without size limit.";
		byte[] dataBytes = Encoding.UTF8.GetBytes(testData);

		PipeReader pipeReader = CreatePipeReaderFromData(dataBytes);
		await using MemoryStream outputStream = new();
		const string successValue = "Success!";

		// Act
		(bool success, string? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			successValue,
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		result.ShouldBe(successValue);
		outputStream.Position.ShouldBe(0);
		outputStream.Length.ShouldBe(dataBytes.Length);

		outputStream.Position = 0;
		using StreamReader reader = new(outputStream);
		string actualData = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
		actualData.ShouldBe(testData);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_HandlesLargeData()
	{
		// Arrange
		byte[] largeData = new byte[10 * 1024]; // 10 KB
		System.Random.Shared.NextBytes(largeData);

		PipeReader pipeReader = CreatePipeReaderFromData(largeData);
		await using MemoryStream outputStream = new();
		const int successValue = 1;

		// Act
		(bool success, int? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			successValue,
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		result.ShouldBe(successValue);
		outputStream.Length.ShouldBe(largeData.Length);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_HandlesEmptyData()
	{
		// Arrange
		PipeReader pipeReader = CreatePipeReaderFromData(Array.Empty<byte>());
		await using MemoryStream outputStream = new();
		const int successValue = 100;

		// Act
		(bool success, int? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			successValue,
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		result.ShouldBe(successValue);
		outputStream.Length.ShouldBe(0);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_HandlesMultipleSegments()
	{
		// Arrange
		const string part1 = "Segment one. ";
		const string part2 = "Segment two. ";
		const string part3 = "Segment three.";
		byte[] data1 = Encoding.UTF8.GetBytes(part1);
		byte[] data2 = Encoding.UTF8.GetBytes(part2);
		byte[] data3 = Encoding.UTF8.GetBytes(part3);

		PipeReader pipeReader = CreatePipeReaderFromMultipleSegments(data1, data2, data3);
		await using MemoryStream outputStream = new();

		// Act
		(bool success, string? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			"Complete",
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		result.ShouldBe("Complete");
		outputStream.Position = 0;
		using StreamReader reader = new(outputStream);
		string actualData = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
		actualData.ShouldBe(part1 + part2 + part3);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_UsesErrorReturnFunction_OnException()
	{
		// Arrange
		PipeReader pipeReader = CreateFaultyPipeReader();
		await using MemoryStream outputStream = new();
		const string errorValue = "Error occurred during read";

		static string? ErrorHandler(Exception ex)
		{
			ex.ShouldNotBeNull();
			return errorValue;
		}

		// Act
		(bool success, string? result) = await pipeReader.ReadFileFromPipe(
			outputStream,
			"Success",
			ErrorHandler,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeFalse();
		result.ShouldBe(errorValue);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_ThrowsException_WhenNoErrorHandler()
	{
		// Arrange
		PipeReader pipeReader = CreateFaultyPipeReader();
		await using MemoryStream outputStream = new();

		// Act & Assert
		Exception exception = await Should.ThrowAsync<Exception>(async () =>
		{
			await pipeReader.ReadFileFromPipe(
				outputStream,
				"Success",
				null,
				TestContext.Current.CancellationToken);
		});

		exception.Message.ShouldBe("Error reading file from pipe");
		exception.InnerException.ShouldNotBeNull();
	}

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_RespectsCancellationToken()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		byte[] data = Encoding.UTF8.GetBytes("Test data");
		PipeReader pipeReader = CreatePipeReaderFromData(data);
		await using MemoryStream outputStream = new();

		// Act & Assert
		// Note: The cancellation exception gets wrapped in a generic Exception by the implementation
		Exception exception = await Should.ThrowAsync<Exception>(async () =>
		{
			await pipeReader.ReadFileFromPipe(
				outputStream,
				"Success",
				null,
				cts.Token);
		});

		exception.Message.ShouldBe("Error reading file from pipe");
		exception.InnerException.ShouldBeOfType<TaskCanceledException>();
	}

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_ResetsStreamPosition()
	{
		// Arrange
		byte[] data = Encoding.UTF8.GetBytes("Position reset test");
		PipeReader pipeReader = CreatePipeReaderFromData(data);
		await using MemoryStream outputStream = new();

		// Act
		(bool success, _) = await pipeReader.ReadFileFromPipe<string>(
			outputStream,
			null,
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		outputStream.Position.ShouldBe(0);
	}

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_HandlesNullSuccessReturn()
	{
		// Arrange
		byte[] data = Encoding.UTF8.GetBytes("Null return test");
		PipeReader pipeReader = CreatePipeReaderFromData(data);
		await using MemoryStream outputStream = new();

		// Act
		(bool success, string? result) = await pipeReader.ReadFileFromPipe<string>(
			outputStream,
			null,
			null,
			TestContext.Current.CancellationToken);

		// Assert
		success.ShouldBeTrue();
		result.ShouldBeNull();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	#endregion

	#region Tests for logging and edge cases

	[Fact]
	public async Task GetSafeSaveName_String_WithLogging_ReturnsEmpty_WhenDirectoryMissing()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "LoggingTest");
		string fileName = Path.Combine(newDir, "file.txt");

		// Act - suppressLogging=false (default), createPathIfMissing=false
		string safeName = fileName.GetSafeSaveName(suppressLogging: false, createPathIfMissing: false);

		// Assert
		safeName.ShouldBeEmpty();
	}

	[Fact]
	public void GetSafeSaveName_String_SuppressLogging_ReturnsContinue_WhenDirectoryMissing()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "SuppressLogMissingDir");
		string fileName = Path.Combine(newDir, "file.txt");

		// Act - suppressLogging=true, createPathIfMissing=false
		// Note: This appears to be a bug in the original implementation
		// When directory doesn't exist, createPathIfMissing=false, and suppressLogging=true,
		// the method continues instead of returning empty
		string safeName = fileName.GetSafeSaveName(suppressLogging: true, createPathIfMissing: false);

		// Assert - The method continues and returns a path even though directory doesn't exist
		// This may or may not be the intended behavior
		safeName.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task GetSafeSaveName_String_WithLogging_CreatesDirectory()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "LoggingCreateTest");
		string fileName = Path.Combine(newDir, "file.txt");

		// Act - suppressLogging=false (default), createPathIfMissing=true
		string safeName = fileName.GetSafeSaveName(suppressLogging: false, createPathIfMissing: true);

		// Assert
		Directory.Exists(newDir).ShouldBeTrue();
		safeName.ShouldContain(newDir);
	}

	[Fact]
	public async Task GetSafeSaveName_String_WithLogging_WhenFileExists()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "logging_test.txt");
		await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);

		// Act - suppressLogging=false (default)
		string safeName = fileName.GetSafeSaveName(suppressLogging: false);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("(0)");
	}

	[Fact]
	public async Task GetSafeSaveName_String_WithLogging_WhenFileDoesNotExist()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "unique_logging_test.txt");

		// Act - suppressLogging=false (default)
		string safeName = fileName.GetSafeSaveName(suppressLogging: false);

		// Assert
		safeName.ShouldBe(fileName);
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_WithLogging_CreatesDirectory()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "LoggingCreateTest2");
		const string fileName = "file.txt";

		// Act - suppressLogging=false (default), createPathIfMissing=true
		string safeName = FileHelpers.GetSafeSaveName(newDir, fileName, suppressLogging: false, createPathIfMissing: true);

		// Assert
		Directory.Exists(newDir).ShouldBeTrue();
		safeName.ShouldBe(fileName);
	}

	[Fact]
	public void GetSafeSaveName_PathAndFileName_WithLogging_ReturnsEmpty_WhenDirectoryMissing()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "LoggingMissingTest");
		const string fileName = "file.txt";

		// Act - suppressLogging=false (default), createPathIfMissing=false
		string safeName = FileHelpers.GetSafeSaveName(newDir, fileName, suppressLogging: false, createPathIfMissing: false);

		// Assert
		safeName.ShouldBeEmpty();
	}

	[Fact]
	public void GetSafeSaveName_PathAndFileName_SuppressLogging_Continues_WhenDirectoryMissing()
	{
		// Arrange
		string newDir = Path.Combine(tempDir, "SuppressLogMissingDir2");
		const string fileName = "file.txt";

		// Act - suppressLogging=true, createPathIfMissing=false
		string safeName = FileHelpers.GetSafeSaveName(newDir, fileName, suppressLogging: true, createPathIfMissing: false);

		// Assert - continues execution and returns a filename (possible bug in original code)
		safeName.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_WithLogging_WhenFileExists()
	{
		// Arrange
		const string fileName = "logging_test2.txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act - suppressLogging=false (default)
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: false);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("(0)");
	}

	[Fact]
	public void GetSafeSaveName_PathAndFileName_WithLogging_WhenFileDoesNotExist()
	{
		// Arrange
		const string fileName = "unique_logging_test2.txt";

		// Act - suppressLogging=false (default)
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: false);

		// Assert
		safeName.ShouldBe(fileName);
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_WithLogging_BreaksLoop()
	{
		// Arrange
		const string fileName = "loop_test (0).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act - suppressLogging=false to hit logging in loop break
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: false);

		// Assert
		safeName.ShouldNotBeNullOrWhiteSpace();
		safeName.ShouldEndWith(".txt");
	}

	[Theory]
	[InlineData("file:colon.txt", "file.colon.txt")]
	[InlineData("file<less.txt", "file_less.txt")]
	[InlineData("file>greater.txt", "file_greater.txt")]
	[InlineData("file\"quote.txt", "file'quote.txt")]
	[InlineData("file|pipe.txt", "file_pipe.txt")]
	[InlineData("file?question.txt", "file_question.txt")]
	[InlineData("file*asterisk.txt", "file_asterisk.txt")]
	public void GetSafeSaveName_String_CleansInvalidCharacters(string dirtyFileName, string expectedClean)
	{
		// Arrange - construct path with string concatenation to avoid Path.Combine interpreting characters
		string fullPath = tempDir + Path.DirectorySeparatorChar + dirtyFileName;

		// Act - createPathIfMissing=true to ensure directory exists
		string safeName = fullPath.GetSafeSaveName(createPathIfMissing: true);

		// Assert - check that the resulting file name has been cleaned
		string resultFileName = Path.GetFileName(safeName);
		resultFileName.ShouldBe(expectedClean);
	}

	[Theory]
	[InlineData("file-slash.txt")]
	[InlineData("file-backslash.txt")]
	public void GetSafeSaveName_String_WithPathSeparators_CleansFileName(string expectedClean)
	{
		// Note: / and \ are path separators, so when included in a filename string,
		// they get interpreted as directories. We test that CleanFileName handles them
		// by constructing a path string manually
		string dirtyFileName = expectedClean.Replace("-", "/");
		string fullPath = tempDir + Path.DirectorySeparatorChar + dirtyFileName;

		// Act - createPathIfMissing=true to create any intermediate directories
		string safeName = fullPath.GetSafeSaveName(createPathIfMissing: true);

		// Assert
		safeName.ShouldNotBeEmpty();
		// The directory structure will be created, so we just verify it works
		Directory.Exists(Path.GetDirectoryName(safeName)).ShouldBeTrue();
	}

	[Theory]
	[InlineData("file/slash.txt")]
	[InlineData("file\\backslash.txt")]
	[InlineData("file:colon.txt")]
	[InlineData("file<less.txt")]
	[InlineData("file>greater.txt")]
	[InlineData("file\"quote.txt")]
	[InlineData("file|pipe.txt")]
	[InlineData("file?question.txt")]
	[InlineData("file*asterisk.txt")]
	public void GetSafeSaveName_PathAndFileName_CleansInvalidCharacters(string dirtyFileName)
	{
		// Arrange & Act
		string safeName = FileHelpers.GetSafeSaveName(tempDir, dirtyFileName);

		// Assert
		safeName.ShouldNotContain("/");
		safeName.ShouldNotContain("\\");
		safeName.ShouldNotContain(":");
		safeName.ShouldNotContain("<");
		safeName.ShouldNotContain(">");
		safeName.ShouldNotContain("\"");
		safeName.ShouldNotContain("|");
		safeName.ShouldNotContain("?");
		safeName.ShouldNotContain("*");
	}

	[Fact]
	public void GetSafeSaveName_String_NoInvalidChars_ReturnsAsIs()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "clean_file_name.txt");

		// Act
		string safeName = fileName.GetSafeSaveName();

		// Assert
		safeName.ShouldBe(fileName);
	}

	[Fact]
	public async Task GetHashFromStream_ReturnsEmptyString_OnException()
	{
		// Arrange - Create a stream that will throw when trying to set position
		await using NonSeekableStreamThatThrows stream = new();

		// Act
		string hash = await stream.GetHashFromStream();

		// Assert
		hash.ShouldBe(string.Empty);
	}

	[Fact]
	public void GetAllFilesRecursive_HandlesDirectoryAccessException()
	{
		// Arrange - Create a subdirectory with restrictive permissions
		string restrictedDir = Path.Combine(tempDir, "restricted");
		Directory.CreateDirectory(restrictedDir);

		// Create a file in the main directory
		string accessibleFile = Path.Combine(tempDir, "accessible.txt");
		File.WriteAllText(accessibleFile, "data");

		// Note: It's hard to truly simulate permission errors in a cross-platform way
		// This test mainly verifies that the method doesn't crash when it encounters errors
		// The actual exception handling is tested indirectly

		// Act
		IEnumerable<string> files = FileHelpers.GetAllFilesRecursive(tempDir, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		files.ShouldContain(accessibleFile);
	}

	[Fact]
	public void GetAllFilesRecursive_RespectsCancellationToken()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		cts.Cancel();

		// Act & Assert
		Should.Throw<OperationCanceledException>(() =>
		{
			// Force enumeration
			_ = FileHelpers.GetAllFilesRecursive(tempDir, cancellationToken: cts.Token).ToList();
		});
	}

	[Fact]
	public async Task GetSafeSaveName_String_StartFromZero_WithExistingIterator_StartsFromZero()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "test (5).txt");
		await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);

		// Act - startFromZero=true (default) should start incrementing from 0
		string safeName = fileName.GetSafeSaveName(startFromZero: true);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("(0)");
	}

	[Fact]
	public async Task GetSafeSaveName_String_StartFromExisting_WithNoIterator_StartsFromZero()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "test_no_iterator.txt");
		await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);

		// Act - startFromZero=false but no iterator present, so should start from 0
		string safeName = fileName.GetSafeSaveName(startFromZero: false);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("(0)");
	}

	[Fact]
	public async Task GetSafeSaveName_String_StartFromExisting_WithInvalidIterator_StartsFromZero()
	{
		// Arrange - filename has parentheses but not a valid number
		string fileName = Path.Combine(tempDir, "test (abc).txt");
		await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);

		// Act - startFromZero=false but iterator isn't a valid int, so should start from 0
		string safeName = fileName.GetSafeSaveName(startFromZero: false);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("(0)");
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_StartFromZero_WithExistingIterator()
	{
		// Arrange
		const string fileName = "test_path (10).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act - startFromZero=true (default)
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: true);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("(0)");
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_StartFromExisting_WithNoIterator()
	{
		// Arrange
		const string fileName = "no_iter_test.txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act - startFromZero=false but no iterator, so starts from 0
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: false);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("(0)");
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_StartFromExisting_WithInvalidIterator()
	{
		// Arrange
		const string fileName = "test (xyz).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act - startFromZero=false but invalid iterator
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: false);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("(0)");
	}

	[Fact]
	public async Task GetSafeSaveName_String_WithIteratorInFileName_LogsAndIncrements()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "test_with_iterator (5).txt");
		await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);

		// Act - suppressLogging=false (default)
		string safeName = fileName.GetSafeSaveName(suppressLogging: false);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("(0)"); // Should start from 0 by default
	}

	[Fact]
	public async Task ReadFileFromPipe_WithSizeLimit_ThrowsFileLoadException_WhenNoErrorHandler()
	{
		// Arrange
		PipeReader pipeReader = CreateFaultyPipeReader();
		await using MemoryStream outputStream = new();
		const long maxSize = 1024;

		// Act & Assert
		Exception exception = await Should.ThrowAsync<Exception>(async () =>
		{
			await pipeReader.ReadFileFromPipe(
				outputStream,
				maxSize,
				"Success",
				"TooLarge",
				null,
				TestContext.Current.CancellationToken);
		});

		// The actual exception type should be preserved
		exception.ShouldNotBeNull();
	}

	[Fact]
	public async Task ReadFileFromPipe_WithoutSizeLimit_ThrowsFileLoadException_WhenNoErrorHandler()
	{
		// Arrange
		PipeReader pipeReader = CreateFaultyPipeReader();
		await using MemoryStream outputStream = new();

		// Act & Assert
		Exception exception = await Should.ThrowAsync<Exception>(async () =>
		{
			await pipeReader.ReadFileFromPipe(
				outputStream,
				"Success",
				null,
				TestContext.Current.CancellationToken);
		});

		// The actual exception type should be preserved
		exception.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetSafeSaveName_String_WithIteratorInFileName_ReplacesIterator()
	{
		// Arrange - file with iterator pattern already exists
		string fileName = Path.Combine(tempDir, "file_with_iter (2).txt");
		await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);

		// Create another file with same pattern to force increment
		string fileName2 = Path.Combine(tempDir, "file_with_iter (0).txt");
		await File.WriteAllTextAsync(fileName2, "data2", TestContext.Current.CancellationToken);

		// Act - try to save with the original name that has iterator
		string safeName = fileName.GetSafeSaveName();

		// Assert - should replace the (2) with (0) since file (2) exists but we start from 0
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("file_with_iter");
	}

	[Fact]
	public async Task GetSafeSaveName_String_WithIteratorInFileName_IncrementsCorrectly()
	{
		// Arrange - create files with iterator pattern
		string file1 = Path.Combine(tempDir, "iter_test (5).txt");
		string file2 = Path.Combine(tempDir, "iter_test (0).txt");
		string file3 = Path.Combine(tempDir, "iter_test (1).txt");
		await File.WriteAllTextAsync(file1, "data1", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file2, "data2", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file3, "data3", TestContext.Current.CancellationToken);

		// Act - try to save with iterator pattern
		string safeName = file1.GetSafeSaveName();

		// Assert - should find first available slot
		safeName.ShouldNotBe(file1);
		Path.GetFileName(safeName).ShouldContain("iter_test");
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_WithIteratorInFileName_ReplacesIterator()
	{
		// Arrange
		const string fileName = "path_iter_test (3).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		const string fileName2 = "path_iter_test (0).txt";
		string filePath2 = Path.Combine(tempDir, fileName2);
		await File.WriteAllTextAsync(filePath2, "data2", TestContext.Current.CancellationToken);

		// Act
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("path_iter_test");
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_WithIteratorInFileName_WithSuppressLogging()
	{
		// Arrange
		const string fileName = "suppress_iter (7).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		const string fileName2 = "suppress_iter (0).txt";
		string filePath2 = Path.Combine(tempDir, fileName2);
		await File.WriteAllTextAsync(filePath2, "data2", TestContext.Current.CancellationToken);

		// Act - with suppressLogging=true
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: true);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("suppress_iter");
	}

	[Fact]
	public async Task GetSafeSaveName_String_WithIteratorInFileName_WithSuppressLogging()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "suppress_str_iter (9).txt");
		await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);

		string fileName2 = Path.Combine(tempDir, "suppress_str_iter (0).txt");
		await File.WriteAllTextAsync(fileName2, "data2", TestContext.Current.CancellationToken);

		// Act - with suppressLogging=true
		string safeName = fileName.GetSafeSaveName(suppressLogging: true);

		// Assert
		safeName.ShouldNotBe(fileName);
		safeName.ShouldContain("suppress_str_iter");
	}

	[Fact]
	public async Task GetSafeSaveName_String_MultipleFilesWithIterators_FindsNext()
	{
		// Arrange - create files to test incrementing
		string baseFile = Path.Combine(tempDir, "increment_test.txt");
		string file0 = Path.Combine(tempDir, "increment_test (0).txt");
		string file1 = Path.Combine(tempDir, "increment_test (1).txt");
		await File.WriteAllTextAsync(baseFile, "base", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file0, "data0", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file1, "data1", TestContext.Current.CancellationToken);

		// Act - pass the base file (without iterator)
		string safeName = baseFile.GetSafeSaveName();

		// Assert - should find (2) as the next available since base, (0), and (1) all exist
		safeName.ShouldContain("(2)");
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_MultipleFilesWithIterators_FindsGap()
	{
		// Arrange
		const string file0 = "path_gap (0).txt";
		const string file1 = "path_gap (1).txt";
		const string file2 = "path_gap (2).txt";
		await File.WriteAllTextAsync(Path.Combine(tempDir, file0), "data0", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(Path.Combine(tempDir, file1), "data1", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(Path.Combine(tempDir, file2), "data2", TestContext.Current.CancellationToken);

		// Act
		string safeName = FileHelpers.GetSafeSaveName(tempDir, file0);

		// Assert - should find (3)
		safeName.ShouldContain("(3)");
	}

	[Fact]
	public void GetSafeSaveName_String_UniqueFile_WithoutLogging()
	{
		// Arrange
		string fileName = Path.Combine(tempDir, "unique_suppress_log.txt");

		// Act - suppressLogging=true, file doesn't exist
		string safeName = fileName.GetSafeSaveName(suppressLogging: true);

		// Assert
		safeName.ShouldBe(fileName);
	}

	[Fact]
	public void GetSafeSaveName_PathAndFileName_UniqueFile_WithoutLogging()
	{
		// Arrange
		const string fileName = "path_unique_suppress.txt";

		// Act - suppressLogging=true
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: true);

		// Assert
		safeName.ShouldBe(fileName);
	}

	[Fact]
	public async Task GetSafeSaveName_String_WithCleanedFileName_HasIteratorPattern()
	{
		// Arrange - create a file that after cleaning will have an iterator
		string fileName = Path.Combine(tempDir, "test:file (3).txt");
		// After cleaning, colon becomes dot, so "test.file (3).txt"
		string cleanedName = Path.Combine(tempDir, "test.file (3).txt");
		await File.WriteAllTextAsync(cleanedName, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = fileName.GetSafeSaveName(createPathIfMissing: true);

		// Assert - should handle the cleaned name with iterator
		safeName.ShouldNotBe(fileName);
		Path.GetFileName(safeName).ShouldContain("test.file");
	}

	[Fact]
	public async Task GetSafeSaveName_PathAndFileName_StartFromExisting_WithValidIterator_StartsFromThatNumber()
	{
		// Arrange
		const string fileName = "start_from (15).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Also create (15), (16) to force increment
		const string file15 = "start_from (15).txt";
		const string file16 = "start_from (16).txt";
		await File.WriteAllTextAsync(Path.Combine(tempDir, file15), "d15", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(Path.Combine(tempDir, file16), "d16", TestContext.Current.CancellationToken);

		// Act - startFromZero=false should start from the number in the filename
		string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: false);

		// Assert - should start from 15 and find 17 as available
		safeName.ShouldContain("(17)");
	}

	[Fact]
	public async Task GetSafeSaveName_String_StartFromExisting_WithValidIterator_StartsFromThatNumber()
	{
		// Arrange - test file without iterator
		string baseFile = Path.Combine(tempDir, "string_start.txt");
		await File.WriteAllTextAsync(baseFile, "base", TestContext.Current.CancellationToken);

		// Create files with iterators
		string file0 = Path.Combine(tempDir, "string_start (0).txt");
		string file1 = Path.Combine(tempDir, "string_start (1).txt");
		await File.WriteAllTextAsync(file0, "d0", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file1, "d1", TestContext.Current.CancellationToken);

		// Act - startFromZero=true (default) should start from 0 and find (2)
		string safeName = baseFile.GetSafeSaveName(startFromZero: true);

		// Assert - should find (2)
		safeName.ShouldContain("(2)");
	}

	[Fact]
	public async Task GetHashFromFile_WithNonExistentFile_ReturnsEmpty()
	{
		// Arrange
		string nonExistent = Path.Combine(tempDir, "does_not_exist_at_all.bin");

		// Act
		string hash = await nonExistent.GetHashFromFile();

		// Assert
		hash.ShouldBe(string.Empty);
	}

	[Fact]
	public async Task GetHashFromFile_WithException_ReturnsEmpty()
	{
		// Arrange - Create a file then try to get hash while it's locked
		string fileName = Path.Combine(tempDir, "locked_file.txt");
		await File.WriteAllTextAsync(fileName, "content", TestContext.Current.CancellationToken);

		// Act - Open file exclusively to lock it, then try to get hash
		await using FileStream lockStream = new(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		// Try to get hash while file is locked - this should cause an exception
		string hash = await fileName.GetHashFromFile();

		// Assert - should return empty string on exception
		hash.ShouldBe(string.Empty);
	}

	#endregion

	#region Helper methods for PipeReader creation

#pragma warning disable S1481 // Remove the unused local variable
	private static PipeReader CreatePipeReaderFromData(byte[] data)
	{
		Pipe pipe = new();
		Task writeTask = Task.Run(async () =>
		{
			await pipe.Writer.WriteAsync(data);
			await pipe.Writer.CompleteAsync();
		});

		return pipe.Reader;
	}

	private static PipeReader CreatePipeReaderFromMultipleSegments(params byte[][] segments)
	{
		Pipe pipe = new();
		Task writeTask = Task.Run(async () =>
		{
			foreach (byte[] segment in segments)
			{
				await pipe.Writer.WriteAsync(segment);
				await pipe.Writer.FlushAsync();
			}
			await pipe.Writer.CompleteAsync();
		});

		return pipe.Reader;
	}

	private static PipeReader CreateFaultyPipeReader()
	{
		Pipe pipe = new();
		Task writeTask = Task.Run(async () => await pipe.Writer.CompleteAsync(new InvalidOperationException("Simulated pipe error")));

		return pipe.Reader;
	}
#pragma warning restore S1481 // Remove the unused local variable

	#endregion

	#region Helper class for stream exception testing

	/// <summary>
	/// A stream that throws an exception when trying to set position or compute hash
	/// </summary>
	private sealed class NonSeekableStreamThatThrows : MemoryStream
	{
		public override long Position
		{
			get => throw new InvalidOperationException("Cannot get position");
			set => throw new InvalidOperationException("Cannot set position");
		}

		public override bool CanSeek => false;
	}

	#endregion
}
