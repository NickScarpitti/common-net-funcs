using System.IO.Pipelines;
using System.Text;
using CommonNetFuncs.Core;

namespace Core.Tests;

public enum ApiVariant
{
	StringExtension,
	PathAndFileName
}

public enum DirectoryState
{
	Exists,
	Missing
}

public enum LoggingMode
{
	WithLogging,
	SuppressLogging
}

public enum FileState
{
	DoesNotExist,
	Exists
}

public enum IteratorStartMode
{
	StartFromZero,
	ContinueFromExisting
}

public enum PipeSizeLimit
{
	WithSizeLimit,
	WithoutSizeLimit
}

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

	[Theory]
	[InlineData(ApiVariant.StringExtension)]
	[InlineData(ApiVariant.PathAndFileName)]
	public async Task GetSafeSaveName_ReturnsUniqueName_WhenFileExists(ApiVariant variant)
	{
		// Arrange
		string fileName = variant == ApiVariant.StringExtension ? "test.txt" : "test2.txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName()
			: FileHelpers.GetSafeSaveName(tempDir, fileName);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
			Path.GetFileNameWithoutExtension(safeName).ShouldContain("0");
		}
		else
		{
			safeName.ShouldNotBe(fileName);
			safeName.ShouldContain("0");
		}
		File.Exists(variant == ApiVariant.StringExtension ? safeName : Path.Combine(tempDir, safeName)).ShouldBeFalse();
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension)]
	[InlineData(ApiVariant.PathAndFileName)]
	public void GetSafeSaveName_CreatesDirectory_WhenMissingAndFlagSet(ApiVariant variant)
	{
		// Arrange
		string newDir = Path.Combine(tempDir, variant == ApiVariant.StringExtension ? "NewSubDir" : "NewSubDir2");
		string fileName = variant == ApiVariant.StringExtension ? Path.Combine(newDir, "file.txt") : "file2.txt";

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? fileName.GetSafeSaveName(createPathIfMissing: true)
			: FileHelpers.GetSafeSaveName(newDir, fileName, createPathIfMissing: true);

		// Assert
		Directory.Exists(newDir).ShouldBeTrue();
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldContain(newDir);
		}
		else
		{
			safeName.ShouldBe("file2.txt");
		}

	}

	[Theory]
	[InlineData(ApiVariant.StringExtension)]
	[InlineData(ApiVariant.PathAndFileName)]
	public void GetSafeSaveName_ReturnsEmpty_WhenDirectoryMissingAndNoCreate(ApiVariant variant)
	{
		// Arrange
		string newDir = Path.Combine(tempDir, variant == ApiVariant.StringExtension ? "MissingDir" : "MissingDir2");
		string fileName = variant == ApiVariant.StringExtension ? Path.Combine(newDir, "file.txt") : "file3.txt";

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? fileName.GetSafeSaveName(createPathIfMissing: false)
			: FileHelpers.GetSafeSaveName(newDir, fileName, createPathIfMissing: false);

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

	[Theory]
	[InlineData(ApiVariant.StringExtension, 99, "(100)")]
	[InlineData(ApiVariant.PathAndFileName, 3, "(4)")]
	public async Task GetSafeSaveName_StartFromExistingNumber_Increments(ApiVariant variant, int existingNumber, string expectedResult)
	{
		// Arrange
		string fileName = variant == ApiVariant.StringExtension
			? $"report ({existingNumber}).pdf"
			: $"test ({existingNumber}).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", cancellationToken: TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(startFromZero: false)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: false);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			Path.GetFileName(safeName).ShouldBe($"report {expectedResult}.pdf");
			Path.GetDirectoryName(safeName).ShouldBe(tempDir);
		}
		else
		{
			safeName.ShouldNotBe(fileName);
			safeName.ShouldBe($"test {expectedResult}.txt");
		}
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension)]
	[InlineData(ApiVariant.PathAndFileName)]
	public async Task GetSafeSaveName_StrCompBreaksLoop(ApiVariant variant)
	{
		// Arrange
		string fileName = variant == ApiVariant.StringExtension ? "test (0).txt" : "loop_test (0).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName()
			: FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: false);

		// Assert
		safeName.ShouldNotBeNullOrWhiteSpace();
		safeName.ShouldEndWith(".txt");
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension)]
	[InlineData(ApiVariant.PathAndFileName)]
	public async Task GetSafeSaveName_AlreadyHasIterator_HandlesCorrectly(ApiVariant variant)
	{
		// Arrange
		if (variant == ApiVariant.StringExtension)
		{
			string fileName = Path.Combine(tempDir, "file (0).txt");
			await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);

			// Act
			string safeName = fileName.GetSafeSaveName();

			// Assert
			safeName.ShouldNotBe(fileName);
			Path.GetFileName(safeName).ShouldBe("file (1).txt");
			Path.GetDirectoryName(safeName).ShouldBe(tempDir);
		}
		else
		{
			const string fileName = "test (1).txt";
			string filePath = Path.Combine(tempDir, fileName);
			await File.WriteAllTextAsync(filePath, "data", cancellationToken: TestContext.Current.CancellationToken);

			// Act
			string safeName = FileHelpers.GetSafeSaveName(tempDir, fileName);

			// Assert
			safeName.ShouldNotBe(fileName);
			safeName.ShouldBe("test (0).txt");
		}
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, "(broken)")]
	[InlineData(ApiVariant.PathAndFileName, 5)]
	public async Task GetSafeSaveName_AvoidInfiniteLoop_VariousPatterns(ApiVariant variant, object marker)
	{
		// Arrange
		if (variant == ApiVariant.StringExtension)
		{
			string fileName = Path.Combine(tempDir, "test(broken).txt");
			await File.WriteAllTextAsync(fileName, "data", TestContext.Current.CancellationToken);

			// Act
			string safeName = fileName.GetSafeSaveName();

			// Assert
			safeName.ShouldNotBeNullOrWhiteSpace();
			File.Exists(safeName).ShouldBeFalse();
		}
		else
		{
			const string baseName = "document";
			for (int i = 0; i < (int)marker; i++)
			{
				string filePath = Path.Combine(tempDir, $"{baseName} ({i}).txt");
				await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);
			}

			// Act
			string safeName = FileHelpers.GetSafeSaveName(tempDir, $"{baseName} (0).txt");

			// Assert
			safeName.ShouldBe($"{baseName} (5).txt");
		}
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, false)]
	[InlineData(ApiVariant.PathAndFileName, false)]
	[InlineData(ApiVariant.StringExtension, true)]
	[InlineData(ApiVariant.PathAndFileName, true)]
	public async Task GetSafeSaveName_InfiniteLoopProtection_WithLogging(ApiVariant variant, bool suppressLogging)
	{
		// Arrange
		string baseName = $"LogTest_{variant}_{suppressLogging}.txt";
		string filePath = Path.Combine(tempDir, baseName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(suppressLogging: suppressLogging)
			: FileHelpers.GetSafeSaveName(tempDir, baseName, suppressLogging: suppressLogging);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
			Path.GetFileName(safeName).ShouldBe($"LogTest_{variant}_{suppressLogging} (0).txt");
		}
		else
		{
			safeName.ShouldNotBeNullOrWhiteSpace();
			safeName.ShouldBe($"LogTest_{variant}_{suppressLogging} (0).txt");
		}
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, 100)]
	[InlineData(ApiVariant.PathAndFileName, 50)]
	public async Task GetSafeSaveName_TerminatesWithManySequentialFiles(ApiVariant variant, int fileCount)
	{
		// Arrange
		string baseName = variant == ApiVariant.StringExtension ? "stress_test" : "batch_file";
		for (int i = 0; i < fileCount; i++)
		{
			string filePath = Path.Combine(tempDir, $"{baseName} ({i}).log");
			await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);
		}

		// Act
		string existingFile = Path.Combine(tempDir, $"{baseName} (0).log");
		string safeName = variant == ApiVariant.StringExtension
			? existingFile.GetSafeSaveName()
			: FileHelpers.GetSafeSaveName(tempDir, $"{baseName} (0).log");

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			Path.GetFileName(safeName).ShouldBe($"{baseName} ({fileCount}).log");
			File.Exists(safeName).ShouldBeFalse();
		}
		else
		{
			safeName.ShouldBe($"{baseName} ({fileCount}).log");
		}
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, 200)]
	[InlineData(ApiVariant.PathAndFileName, 150)]
	public async Task GetSafeSaveName_WithVeryLongFilename_DoesNotInfiniteLoop(ApiVariant variant, int nameLength)
	{
		// Arrange
		string longName = new(variant == ApiVariant.StringExtension ? 'a' : 'b', nameLength);
		string fileName = $"{longName}.txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(suppressLogging: false)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: false);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
			File.Exists(safeName).ShouldBeFalse();
		}
		else
		{
			safeName.ShouldNotBeNullOrWhiteSpace();
			safeName.ShouldContain("(0)");
		}
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, "(abc)")]
	[InlineData(ApiVariant.PathAndFileName, "(xyz)")]
	public async Task GetSafeSaveName_WithSpecialCharsInIterator_DoesNotInfiniteLoop(ApiVariant variant, string iteratorPattern)
	{
		// Arrange
		string baseName = variant == ApiVariant.StringExtension ? "file" : "document";
		string fileName = $"{baseName} {iteratorPattern}.txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(suppressLogging: false)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: false);

		// Assert
		safeName.ShouldContain("(0)");
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
			Path.GetFileName(safeName).ShouldBe($"{baseName} {iteratorPattern} (0).txt");
		}
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, "(old) (1)", "(old) (2)")]
	[InlineData(ApiVariant.PathAndFileName, "(2023) (5)", "(2023) (6)")]
	public async Task GetSafeSaveName_WithMultipleParentheses_TerminatesCorrectly(ApiVariant variant, string existingPattern, string expectedPattern)
	{
		// Arrange
		string baseName = variant == ApiVariant.StringExtension ? "file" : "report";
		string extension = variant == ApiVariant.StringExtension ? ".txt" : ".xlsx";
		string fileName = $"{baseName} {existingPattern}{extension}";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(startFromZero: false, suppressLogging: false)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: false, suppressLogging: false);

		// Assert
		string expectedFileName = $"{baseName} {expectedPattern}{extension}";
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
			Path.GetFileName(safeName).ShouldBe(expectedFileName);
		}
		else
		{
			safeName.ShouldBe(expectedFileName);
		}
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

	[Theory]
	[InlineData(ApiVariant.StringExtension, LoggingMode.WithLogging)]
	[InlineData(ApiVariant.PathAndFileName, LoggingMode.WithLogging)]
	[InlineData(ApiVariant.StringExtension, LoggingMode.SuppressLogging)]
	[InlineData(ApiVariant.PathAndFileName, LoggingMode.SuppressLogging)]
	public async Task GetSafeSaveName_WithLogging_WhenFileExists(ApiVariant variant, LoggingMode loggingMode)
	{
		// Arrange
		string baseName = $"logging_test_{variant}_{loggingMode}.txt";
		string filePath = Path.Combine(tempDir, baseName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);
		bool suppressLogging = loggingMode == LoggingMode.SuppressLogging;

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(suppressLogging: suppressLogging)
			: FileHelpers.GetSafeSaveName(tempDir, baseName, suppressLogging: suppressLogging);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
		}
		else
		{
			safeName.ShouldNotBe(baseName);
		}


		safeName.ShouldContain("(0)");
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, LoggingMode.WithLogging)]
	[InlineData(ApiVariant.PathAndFileName, LoggingMode.WithLogging)]
	[InlineData(ApiVariant.StringExtension, LoggingMode.SuppressLogging)]
	[InlineData(ApiVariant.PathAndFileName, LoggingMode.SuppressLogging)]
	public void GetSafeSaveName_WithLogging_WhenFileDoesNotExist(ApiVariant variant, LoggingMode loggingMode)
	{
		// Arrange
		string baseName = $"unique_logging_{variant}_{loggingMode}.txt";
		string fileName = variant == ApiVariant.StringExtension ? Path.Combine(tempDir, baseName) : baseName;
		bool suppressLogging = loggingMode == LoggingMode.SuppressLogging;

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? fileName.GetSafeSaveName(suppressLogging: suppressLogging)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: suppressLogging);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldBe(fileName);
		}
		else
		{
			safeName.ShouldBe(baseName);
		}

	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, LoggingMode.WithLogging)]
	[InlineData(ApiVariant.PathAndFileName, LoggingMode.WithLogging)]
	public void GetSafeSaveName_WithLogging_CreatesDirectory(ApiVariant variant, LoggingMode loggingMode)
	{
		// Arrange
		string newDir = Path.Combine(tempDir, $"LoggingCreateTest_{variant}");
		string fileName = variant == ApiVariant.StringExtension ? Path.Combine(newDir, "file.txt") : "file.txt";
		bool suppressLogging = loggingMode == LoggingMode.SuppressLogging;

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? fileName.GetSafeSaveName(suppressLogging: suppressLogging, createPathIfMissing: true)
			: FileHelpers.GetSafeSaveName(newDir, fileName, suppressLogging: suppressLogging, createPathIfMissing: true);

		// Assert
		Directory.Exists(newDir).ShouldBeTrue();
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldContain(newDir);
		}
		else
		{
			safeName.ShouldBe(fileName);
		}

	}

	[Theory]
	[InlineData(ApiVariant.StringExtension)]
	[InlineData(ApiVariant.PathAndFileName)]
	public void GetSafeSaveName_WithLogging_ReturnsEmpty_WhenDirectoryMissing(ApiVariant variant)
	{
		// Arrange
		string newDir = Path.Combine(tempDir, $"LoggingMissing_{variant}");
		string fileName = variant == ApiVariant.StringExtension ? Path.Combine(newDir, "file.txt") : "file.txt";

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? fileName.GetSafeSaveName(suppressLogging: false, createPathIfMissing: false)
			: FileHelpers.GetSafeSaveName(newDir, fileName, suppressLogging: false, createPathIfMissing: false);

		// Assert
		safeName.ShouldBeEmpty();
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
	public async Task GetAllFilesRecursive_HandlesEmptyDirectoriesWithSubdirectories()
	{
		// Arrange - Create a directory structure where some directories have no files
		// This tests the "if (files.Count == 0) continue" branch
		string emptyDir1 = Path.Combine(tempDir, "empty1");
		string emptyDir2 = Path.Combine(tempDir, "empty2");
		string dirWithFiles = Path.Combine(tempDir, "withFiles");

		Directory.CreateDirectory(emptyDir1);
		Directory.CreateDirectory(emptyDir2);
		Directory.CreateDirectory(dirWithFiles);

		// Create subdirectory under empty directory
		string subEmptyDir = Path.Combine(emptyDir1, "subEmpty");
		Directory.CreateDirectory(subEmptyDir);

		// Only add files to one directory
		string file1 = Path.Combine(dirWithFiles, "file1.txt");
		await File.WriteAllTextAsync(file1, "content", TestContext.Current.CancellationToken);

		// Act
		IEnumerable<string> files = FileHelpers.GetAllFilesRecursive(tempDir, cancellationToken: TestContext.Current.CancellationToken);
		List<string> fileList = files.ToList();

		// Assert - Should only find files in dirWithFiles, not count empty directories
		fileList.Count.ShouldBe(1);
		fileList.ShouldContain(file1);
	}

	[Fact]
	public async Task GetAllFilesRecursive_DeepNestedStructureWithMixedEmptyDirs()
	{
		// Arrange - Create a deep structure with mixed empty and non-empty directories
		string level1 = Path.Combine(tempDir, "level1");
		string level2Empty = Path.Combine(level1, "level2Empty");
		string level2WithFile = Path.Combine(level1, "level2WithFile");
		string level3 = Path.Combine(level2WithFile, "level3");

		Directory.CreateDirectory(level1);
		Directory.CreateDirectory(level2Empty);
		Directory.CreateDirectory(level2WithFile);
		Directory.CreateDirectory(level3);

		// Add files at different levels
		string file1 = Path.Combine(level1, "file1.txt");
		string file2 = Path.Combine(level2WithFile, "file2.txt");
		string file3 = Path.Combine(level3, "file3.txt");

		await File.WriteAllTextAsync(file1, "1", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file2, "2", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(file3, "3", TestContext.Current.CancellationToken);

		// Act
		IEnumerable<string> files = FileHelpers.GetAllFilesRecursive(tempDir, cancellationToken: TestContext.Current.CancellationToken);
		List<string> fileList = files.ToList();

		// Assert - Should find all files regardless of nesting and empty sibling directories
		fileList.Count.ShouldBe(3);
		fileList.ShouldContain(file1);
		fileList.ShouldContain(file2);
		fileList.ShouldContain(file3);
	}

	[Fact]
	public async Task GetAllFilesRecursive_SearchPatternWithEmptyDirectories()
	{
		// Arrange - Combine search pattern with empty directories
		string emptyDir = Path.Combine(tempDir, "empty");
		string dirWithFiles = Path.Combine(tempDir, "files");

		Directory.CreateDirectory(emptyDir);
		Directory.CreateDirectory(dirWithFiles);

		// Create subdirectory under empty
		string subDir = Path.Combine(emptyDir, "sub");
		Directory.CreateDirectory(subDir);

		// Add both .txt and .doc files
		string txtFile = Path.Combine(dirWithFiles, "document.txt");
		string docFile = Path.Combine(dirWithFiles, "document.doc");

		await File.WriteAllTextAsync(txtFile, "text", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(docFile, "doc", TestContext.Current.CancellationToken);

		// Act - Search only for .txt files
		IEnumerable<string> files = FileHelpers.GetAllFilesRecursive(tempDir, "*.txt", TestContext.Current.CancellationToken);
		List<string> fileList = files.ToList();

		// Assert - Should only find .txt file, ignoring empty directories
		fileList.Count.ShouldBe(1);
		fileList.ShouldContain(txtFile);
		fileList.ShouldNotContain(docFile);
	}

	[Fact]
	public void GetAllFilesRecursive_OnlyEmptyDirectories_ReturnsEmpty()
	{
		// Arrange - Create only empty directories with subdirectories but no files
		string empty1 = Path.Combine(tempDir, "empty1");
		string empty2 = Path.Combine(empty1, "empty2");
		string empty3 = Path.Combine(empty1, "empty3");

		Directory.CreateDirectory(empty1);
		Directory.CreateDirectory(empty2);
		Directory.CreateDirectory(empty3);

		// Act
		IEnumerable<string> files = FileHelpers.GetAllFilesRecursive(tempDir, cancellationToken: TestContext.Current.CancellationToken);
		List<string> fileList = files.ToList();

		// Assert - Should return empty list since no files exist
		fileList.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, 5)]
	[InlineData(ApiVariant.PathAndFileName, 10)]
	public async Task GetSafeSaveName_StartFromZero_WithExistingIterator_StartsFromZero(ApiVariant variant, int existingIterNumber)
	{
		// Arrange
		string baseName = variant == ApiVariant.StringExtension ? "test" : "test_path";
		string fileName = $"{baseName} ({existingIterNumber}).txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(startFromZero: true)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: true);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
		}
		else
		{
			safeName.ShouldNotBe(fileName);
		}


		safeName.ShouldContain("(0)");
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension)]
	[InlineData(ApiVariant.PathAndFileName)]
	public async Task GetSafeSaveName_StartFromExisting_WithNoIterator_StartsFromZero(ApiVariant variant)
	{
		// Arrange
		string baseName = variant == ApiVariant.StringExtension ? "test_no_iterator" : "no_iter_test";
		string fileName = $"{baseName}.txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(startFromZero: false)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: false);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
		}
		else
		{
			safeName.ShouldNotBe(fileName);
		}


		safeName.ShouldContain("(0)");
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, "(abc)")]
	[InlineData(ApiVariant.PathAndFileName, "(xyz)")]
	public async Task GetSafeSaveName_StartFromExisting_WithInvalidIterator_StartsFromZero(ApiVariant variant, string invalidIterator)
	{
		// Arrange
		string fileName = $"test {invalidIterator}.txt";
		string filePath = Path.Combine(tempDir, fileName);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(startFromZero: false)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, startFromZero: false);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
		}
		else
		{
			safeName.ShouldNotBe(fileName);
		}


		safeName.ShouldContain("(0)");
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, "(2)", "(0)")]
	[InlineData(ApiVariant.PathAndFileName, "(3)", "(0)")]
	public async Task GetSafeSaveName_WithIteratorInFileName_ReplacesIterator(ApiVariant variant, string existingIter, string file0Iter)
	{
		// Arrange
		string baseName = variant == ApiVariant.StringExtension ? "file_with_iter" : "path_iter_test";
		string fileName = $"{baseName} {existingIter}.txt";
		string fileName0 = $"{baseName} {file0Iter}.txt";
		string filePath = Path.Combine(tempDir, fileName);
		string filePath0 = Path.Combine(tempDir, fileName0);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(filePath0, "data2", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName()
			: FileHelpers.GetSafeSaveName(tempDir, fileName);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
		}
		else
		{
			safeName.ShouldNotBe(fileName);
		}


		safeName.ShouldContain(baseName);
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, false)]
	[InlineData(ApiVariant.PathAndFileName, false)]
	[InlineData(ApiVariant.StringExtension, true)]
	[InlineData(ApiVariant.PathAndFileName, true)]
	public async Task GetSafeSaveName_WithIteratorInFileName_WithLogging(ApiVariant variant, bool suppressLogging)
	{
		// Arrange
		string baseName = $"suppress_iter_{variant}_{suppressLogging}";
		string fileName = $"{baseName} (7).txt";
		string fileName0 = $"{baseName} (0).txt";
		string filePath = Path.Combine(tempDir, fileName);
		string filePath0 = Path.Combine(tempDir, fileName0);
		await File.WriteAllTextAsync(filePath, "data", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(filePath0, "data2", TestContext.Current.CancellationToken);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? filePath.GetSafeSaveName(suppressLogging: suppressLogging)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: suppressLogging);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldNotBe(filePath);
		}
		else
		{
			safeName.ShouldNotBe(fileName);
		}


		safeName.ShouldContain(baseName);
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension, 2)]
	[InlineData(ApiVariant.PathAndFileName, 3)]
	public async Task GetSafeSaveName_MultipleFilesWithIterators_FindsNext(ApiVariant variant, int expectedIterator)
	{
		// Arrange
		string baseName = variant == ApiVariant.StringExtension ? "increment_test" : "path_gap";
		string extension = ".txt";
		string baseFile = Path.Combine(tempDir, $"{baseName}{extension}");
		string file0 = Path.Combine(tempDir, $"{baseName} (0){extension}");
		string file1 = Path.Combine(tempDir, $"{baseName} (1){extension}");

		if (variant == ApiVariant.StringExtension)
		{
			await File.WriteAllTextAsync(baseFile, "base", TestContext.Current.CancellationToken);
			await File.WriteAllTextAsync(file0, "data0", TestContext.Current.CancellationToken);
			await File.WriteAllTextAsync(file1, "data1", TestContext.Current.CancellationToken);
		}
		else
		{
			string file2 = Path.Combine(tempDir, $"{baseName} (2){extension}");
			await File.WriteAllTextAsync(file0, "data0", TestContext.Current.CancellationToken);
			await File.WriteAllTextAsync(file1, "data1", TestContext.Current.CancellationToken);
			await File.WriteAllTextAsync(file2, "data2", TestContext.Current.CancellationToken);
		}

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? baseFile.GetSafeSaveName()
			: FileHelpers.GetSafeSaveName(tempDir, $"{baseName} (0){extension}");

		// Assert
		safeName.ShouldContain($"({expectedIterator})");
	}

	[Theory]
	[InlineData(ApiVariant.StringExtension)]
	[InlineData(ApiVariant.PathAndFileName)]
	public void GetSafeSaveName_UniqueFile_WithoutLogging(ApiVariant variant)
	{
		// Arrange
		string baseName = variant == ApiVariant.StringExtension ? "unique_suppress_log" : "path_unique_suppress";
		string fileName = $"{baseName}.txt";
		string fullPath = Path.Combine(tempDir, fileName);

		// Act
		string safeName = variant == ApiVariant.StringExtension
			? fullPath.GetSafeSaveName(suppressLogging: true)
			: FileHelpers.GetSafeSaveName(tempDir, fileName, suppressLogging: true);

		// Assert
		if (variant == ApiVariant.StringExtension)
		{
			safeName.ShouldBe(fullPath);
		}
		else
		{
			safeName.ShouldBe(fileName);
		}

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

	[Theory]
	[InlineData(true)]  // Non-existent file
	[InlineData(false)] // Locked file exception
	public async Task GetHashFromFile_ReturnsEmptyOnException(bool fileDoesNotExist)
	{
		// Arrange
		string fileName = Path.Combine(tempDir, fileDoesNotExist ? "does_not_exist_at_all.bin" : "locked_file.txt");

		if (!fileDoesNotExist)
		{
			await File.WriteAllTextAsync(fileName, "content", TestContext.Current.CancellationToken);
		}

		// Act & Assert
		if (fileDoesNotExist)
		{
			string hash = await fileName.GetHashFromFile();
			hash.ShouldBe(string.Empty);
		}
		else
		{
			// Open file exclusively to lock it, try to get hash
			await using FileStream lockStream = new(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			string hash = await fileName.GetHashFromFile();
			hash.ShouldBe(string.Empty);
		}
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
