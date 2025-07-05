using System.Text;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class FileHelpersTests : IDisposable
{
    private readonly string _tempDir;

    public FileHelpersTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    ~FileHelpersTests()
    {
        Dispose();
    }

    [Fact]
    public void GetSafeSaveName_String_ReturnsUniqueName_WhenFileExists()
    {
        // Arrange
        string fileName = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(fileName, "data");
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
        string newDir = Path.Combine(_tempDir, "newsubdir");
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
        string newDir = Path.Combine(_tempDir, "missingdir");
        string fileName = Path.Combine(newDir, "file.txt");

        // Act
        string safeName = fileName.GetSafeSaveName(createPathIfMissing: false);

        // Assert
        safeName.ShouldBeEmpty();
    }

    [Fact]
    public void GetSafeSaveName_PathAndFileName_ReturnsUniqueName_WhenFileExists()
    {
        // Arrange
        const string fileName = "test2.txt";
        string filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, "data");

        // Act
        string safeName = FileHelpers.GetSafeSaveName(_tempDir, fileName);

        // Assert
        safeName.ShouldNotBe(fileName);
        safeName.ShouldContain("0");
    }

    [Fact]
    public void GetSafeSaveName_PathAndFileName_CreatesDirectory_WhenMissingAndFlagSet()
    {
        // Arrange
        string newDir = Path.Combine(_tempDir, "newsubdir2");
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
        string newDir = Path.Combine(_tempDir, "missingdir2");
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
        bool result = fileName.ValidateFileExtension(validExtensions);

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
        string fileName = Path.Combine(_tempDir, $"hash_{algo}.txt");
        const string content = "hash test content";
        await File.WriteAllTextAsync(fileName, content, Encoding.UTF8);

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
        string fileName = Path.Combine(_tempDir, "doesnotexist.txt");

        // Act
        string hash = await fileName.GetHashFromFile();

        // Assert
        hash.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetAllFilesRecursive_ReturnsAllFiles()
    {
        // Arrange
        string subDir = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(subDir);
        string file1 = Path.Combine(_tempDir, "a.txt");
        string file2 = Path.Combine(subDir, "b.txt");
        File.WriteAllText(file1, "1");
        File.WriteAllText(file2, "2");

        // Act
        List<string> files = FileHelpers.GetAllFilesRecursive(_tempDir);

        // Assert
        files.ShouldContain(file1);
        files.ShouldContain(file2);
    }

    [Fact]
    public void GetAllFilesRecursive_ReturnsEmpty_WhenDirectoryMissingOrNull()
    {
        // Arrange
        string missingDir = Path.Combine(_tempDir, "notfound");

        // Act
        List<string> files1 = FileHelpers.GetAllFilesRecursive(missingDir);
        List<string> files2 = FileHelpers.GetAllFilesRecursive(null);

        // Assert
        files1.ShouldBeEmpty();
        files2.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllFilesRecursive_RespectsSearchPattern()
    {
        // Arrange
        string subDir = Path.Combine(_tempDir, "sub2");
        Directory.CreateDirectory(subDir);
        string file1 = Path.Combine(_tempDir, "a.txt");
        string file2 = Path.Combine(subDir, "b.txt");
        string file3 = Path.Combine(subDir, "c.doc");
        File.WriteAllText(file1, "1");
        File.WriteAllText(file2, "2");
        File.WriteAllText(file3, "3");

        // Act
        List<string> files = FileHelpers.GetAllFilesRecursive(_tempDir, "*.txt");

        // Assert
        files.ShouldContain(file1);
        files.ShouldContain(file2);
        files.ShouldNotContain(file3);
    }

    [Fact]
    public void GetSafeSaveName_PathAndFileName_StartFromExistingNumber()
    {
        // Arrange
        const string fileName = "test (3).txt";
        string filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, "data");

        // Act
        string safeName = FileHelpers.GetSafeSaveName(_tempDir, fileName, startFromZero: false);

        // Assert
        safeName.ShouldNotBe(fileName);
        safeName.ShouldBe("test (4).txt");
    }

    [Fact]
    public void GetSafeSaveName_PathAndFileName_AlreadyHasIterator()
    {
        // Arrange
        const string fileName = "test (1).txt";
        string filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, "data");

        // Act
        string safeName = FileHelpers.GetSafeSaveName(_tempDir, fileName);

        // Assert
        safeName.ShouldNotBe(fileName);
        safeName.ShouldBe("test (0).txt");
    }

    [Fact]
    public void GetSafeSaveName_PathAndFileName_StrCompBreaksLoop()
    {
        // Arrange
        // Simulate a scenario where StrComp returns true (file name doesn't change)
        // This is tricky to simulate directly, so we can use a file name that matches the incrementing pattern but doesn't actually change after replacement.
        const string fileName = "test (0).txt";
        string filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, "data");

        // Act
        string safeName = FileHelpers.GetSafeSaveName(_tempDir, fileName);

        // Assert
        // The method should break out of the loop and return a name (should not loop infinitely)
        safeName.ShouldNotBeNullOrWhiteSpace();
        safeName.ShouldEndWith(".txt");
    }
}
