using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Web.Ftp;
using Shouldly;

namespace Web.Ftp.Tests;

public class SshFtpTests
{
    private readonly IFixture _fixture;

    public SshFtpTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
    }

    [Fact]
    public void GetHostName_ShouldReturnCorrectHostName()
    {
        // Arrange
        FileTransferConnection connection = _fixture.Build<FileTransferConnection>().With(x => x.HostName, "test.host.com").Create();

        // Act
        string result = connection.GetHostName();

        // Assert
        result.ShouldBe("test.host.com");
    }

    //[Theory]
    //[InlineData(true)]
    //[InlineData(false)]
    //public void IsConnected_ShouldReturnCorrectState(bool isConnected)
    //{
    //    // Arrange
    //    SftpClient sftpClient = A.Fake<SftpClient>();
    //    A.CallTo(() => sftpClient.IsConnected).Returns(isConnected);

    //    // Act
    //    bool result = sftpClient.IsConnected();

    //    // Assert
    //    result.ShouldBe(isConnected);
    //}

    //[Fact]
    //public void Connect_WhenAlreadyConnected_ShouldDisconnectFirst()
    //{
    //    // Arrange
    //    FileTransferConnection connection = _fixture.Create<FileTransferConnection>();
    //    SftpClient sftpClient = A.Fake<SftpClient>();
    //    A.CallTo(() => sftpClient.IsConnected).Returns(true);

    //    // Act
    //    sftpClient.Connect(connection);

    //    // Assert
    //    A.CallTo(() => sftpClient.Disconnect()).MustHaveHappenedOnceExactly();
    //}

    //[Theory]
    //[InlineData(true)]
    //[InlineData(false)]
    //public void DirectoryOrFileExists_ShouldReturnCorrectState(bool exists)
    //{
    //    // Arrange
    //    SftpClient sftpClient = A.Fake<SftpClient>();
    //    const string path = "/test/path";
    //    A.CallTo(() => sftpClient.Exists(path)).Returns(exists);

    //    // Act
    //    bool result = sftpClient.DirectoryOrFileExists(path);

    //    // Assert
    //    result.ShouldBe(exists);
    //}

    //[Fact]
    //public async Task DirectoryOrFileExistsAsync_ShouldReturnCorrectState()
    //{
    //    // Arrange
    //    SftpClient sftpClient = A.Fake<SftpClient>();
    //    const string path = "/test/path";
    //    A.CallTo(() => sftpClient.ExistsAsync(path, default)).Returns(true);

    //    // Act
    //    bool result = await sftpClient.DirectoryOrFileExistsAsync(path);

    //    // Assert
    //    result.ShouldBeTrue();
    //}

    //[Theory]
    //[InlineData("*")]
    //[InlineData("txt")]
    //public void GetFileList_WithValidPath_ShouldReturnFiles(string extension)
    //{
    //    // Arrange
    //    SftpClient sftpClient = A.Fake<SftpClient>();
    //    const string path = "/test/path";
    //    ISftpFile[] sftpFiles = new[]
    //    {
    //        CreateSftpFile("file1.txt", true),
    //        CreateSftpFile("file2.txt", true),
    //        CreateSftpFile("file3.csv", true)
    //    };

    //    A.CallTo(() => sftpClient.IsConnected).Returns(true);
    //    A.CallTo(() => sftpClient.Exists(path)).Returns(true);
    //    A.CallTo(() => sftpClient.ListDirectory(path, null)).Returns(sftpFiles);

    //    // Act
    //    List<string> files = sftpClient.GetFileList(path, extension).ToList();

    //    // Assert
    //    if (extension == "*")
    //    {
    //        files.Count.ShouldBe(3);
    //    }
    //    else
    //    {
    //        files.Count.ShouldBe(2);
    //        files.ShouldAllBe(f => f.EndsWith(".txt"));
    //    }
    //}

    //[Fact]
    //public void DeleteSftpFile_WhenFileExists_ShouldDeleteAndReturnTrue()
    //{
    //    // Arrange
    //    SftpClient sftpClient = A.Fake<SftpClient>();
    //    const string path = "/test/file.txt";
    //    A.CallTo(() => sftpClient.Exists(path)).Returns(true);

    //    // Act
    //    bool result = sftpClient.DeleteSftpFile(path);

    //    // Assert
    //    result.ShouldBeTrue();
    //    A.CallTo(() => sftpClient.Delete(path)).MustHaveHappenedOnceExactly();
    //}

    //[Fact]
    //public async Task DeleteFileAsync_WhenFileExists_ShouldDeleteAndReturnTrue()
    //{
    //    // Arrange
    //    SftpClient sftpClient = A.Fake<SftpClient>();
    //    const string path = "/test/file.txt";
    //    A.CallTo(() => sftpClient.ExistsAsync(path, default)).Returns(true);

    //    // Act
    //    bool result = await sftpClient.DeleteFileAsync(path);

    //    // Assert
    //    result.ShouldBeTrue();
    //    A.CallTo(() => sftpClient.DeleteAsync(path, default)).MustHaveHappenedOnceExactly();
    //}

    //private static ISftpFile CreateSftpFile(string name, bool isRegularFile)
    //{
    //    ISftpFile file = A.Fake<ISftpFile>();
    //    A.CallTo(() => file.Name).Returns(name);
    //    A.CallTo(() => file.IsRegularFile).Returns(isRegularFile);
    //    return file;
    //}
}
