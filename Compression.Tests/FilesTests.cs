using System.IO.Compression;
using static CommonNetFuncs.Compression.Files;

namespace Compression.Tests;

public sealed class FilesTests
{
    private readonly Fixture _fixture;

    public FilesTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public async Task ZipFile_Should_Create_Zip_With_Single_File()
    {
        // Arrange
        string fileName = _fixture.Create<string>();
        MemoryStream fileStream = new(_fixture.CreateMany<byte>(100).ToArray());
        MemoryStream zipFileStream = new();

        // Act
        await (fileStream, fileName).ZipFile(zipFileStream);

        // Assert
        zipFileStream.Length.ShouldBeGreaterThan(0);
        using ZipArchive archive = new(zipFileStream, ZipArchiveMode.Read);
        archive.Entries.Count.ShouldBe(1);
        archive.Entries[0].Name.ShouldBe(fileName);
    }

    [Fact]
    public async Task ZipFiles_Should_Create_Zip_With_Multiple_Files()
    {
        // Arrange
        List<(Stream?, string)> files =
        [
            (new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray()), "file1.txt"),
            (new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)(i + 1)).ToArray()), "file2.txt"),
            (new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)(i + 2)).ToArray()), "file3.txt")
        ];
        await using MemoryStream zipFileStream = new();

        // Act
        await files.ZipFiles(zipFileStream);

        // Assert
        zipFileStream.Length.ShouldBeGreaterThan(0);
        using ZipArchive archive = new(zipFileStream, ZipArchiveMode.Read);
        archive.Entries.Count.ShouldBe(files.Count);
        foreach ((Stream?, string fileName) file in files)
        {
            archive.Entries.ShouldContain(e => e.Name == file.fileName);
        }
    }

    [Fact]
    public async Task AddFilesToZip_Should_Add_Files_To_Archive()
    {
        // Arrange
        List<(Stream?, string)> files =
        [
            (new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray()), "file1.txt"),
            (new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)(i + 1)).ToArray()), "file2.txt")
        ];
        await using MemoryStream memoryStream = new();
        using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true))
        {
            // Act
            await files.AddFilesToZip(archive);
        }

        // Reopen the ZipArchive in Read mode to verify its contents
        using ZipArchive readArchive = new(memoryStream, ZipArchiveMode.Read, true);

        // Assert
        readArchive.Entries.Count.ShouldBe(files.Count);
        foreach ((Stream?, string fileName) file in files)
        {
            readArchive.Entries.ShouldContain(e => e.Name == file.fileName);
        }
    }

    [Fact]
    public async Task AddFileToZip_Should_Add_Single_File_To_Archive()
    {
        // Arrange
        string fileName = _fixture.Create<string>();
        await using MemoryStream fileStream = new(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray());
        await using MemoryStream memoryStream = new();

        // Act
        using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true))
        {
            await fileStream.AddFileToZip(archive, fileName);
        }

        // Assert
        using ZipArchive readArchive = new(memoryStream, ZipArchiveMode.Read, true);
        readArchive.Entries.Count.ShouldBe(1);
        readArchive.Entries[0].Name.ShouldBe(fileName);
    }

    [Fact]
    public async Task AddFileToZip_Should_Handle_Null_Stream_Gracefully()
    {
        // Arrange
        Stream? fileStream = null;
        string fileName = _fixture.Create<string>();
        await using MemoryStream memoryStream = new();

        // Act
        using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true))
        {
            await fileStream.AddFileToZip(archive, fileName);
        }

        // Assert
        using ZipArchive readArchive = new(memoryStream, ZipArchiveMode.Read, true);
        readArchive.Entries.Count.ShouldBe(0);
    }
}
