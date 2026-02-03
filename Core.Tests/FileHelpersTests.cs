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
		string newDir = Path.Combine(tempDir, "newsubdir");
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
		string newDir = Path.Combine(tempDir, "missingdir");
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
		string newDir = Path.Combine(tempDir, "newsubdir2");
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
		string newDir = Path.Combine(tempDir, "missingdir2");
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
	public void ValidateFileExtention_Works(string fileName, string[] validExtensions, bool expected)
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
		string fileName = Path.Combine(tempDir, "doesnotexist.txt");

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
}
