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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();
		const long maxSize = 1024;
		const string errorValue = "Error occurred";

		string? ErrorHandler(Exception ex)
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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();
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
		using MemoryStream outputStream = new();

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
		using MemoryStream outputStream = new();
		const string errorValue = "Error occurred during read";

		string? ErrorHandler(Exception ex)
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
		using MemoryStream outputStream = new();

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
		using MemoryStream outputStream = new();

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
		using MemoryStream outputStream = new();

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
		using MemoryStream outputStream = new();

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
		Task writeTask = Task.Run(async () =>
		{
			await pipe.Writer.CompleteAsync(new InvalidOperationException("Simulated pipe error"));
		});

		return pipe.Reader;
	}
#pragma warning restore S1481 // Remove the unused local variable

	#endregion
}
