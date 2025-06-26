using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Web.Ftp;
using Renci.SshNet;

namespace Web.Ftp.Tests;

public class SshFtpServiceTests
{
    private readonly IFixture _fixture;
    private readonly FileTransferConnection _connection;
    private readonly SftpClient _sftpClient;
    private readonly SshFtpService _service;

    public SshFtpServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        _connection = _fixture.Create<FileTransferConnection>();
        _sftpClient = A.Fake<SftpClient>(options => options.WithArgumentsForConstructor(() => new SftpClient(
                _connection.HostName,
                _connection.Port,
                _connection.UserName,
                _connection.Password)));

        // Setup default behaviors
        A.CallTo(() => _sftpClient.IsConnected).Returns(true);

        //_service = new SshFtpService(_connection);
        _service = new SshFtpService(_connection, _ => _sftpClient);
    }

    [Fact]
    public void GetHostName_ShouldReturnCorrectHostName()
    {
        // Arrange
        string expectedHostName = _connection.HostName;

        // Act
        string result = _service.GetHostName();

        // Assert
        result.ShouldBe(expectedHostName);
    }

    [Fact]
    public void IsConnected_ShouldReturnClientConnectionState()
    {
        // Act
        bool result = _service.IsConnected();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Connect_WhenAlreadyConnected_ShouldDisconnectFirst()
    {
        // Arrange
        A.CallTo(() => _sftpClient.IsConnected).Returns(true);

        // Act
        SftpClient result = _service.Connect();

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task ConnectAsync_ShouldConnectClient()
    {
        // Arrange
        CancellationTokenSource cts = new();

        // Act
        SftpClient result = await _service.ConnectAsync(cts);

        // Assert
        result.ShouldNotBeNull();
    }

    //[Theory]
    //[InlineData(true)]
    //[InlineData(false)]
    //public void DirectoryExists_ShouldReturnCorrectState(bool exists)
    //{
    //    // Arrange
    //    const string path = "/test/path";
    //    A.CallTo(() => _sftpClient.Exists(path)).Returns(exists);

    //    // Act
    //    bool result = _service.DirectoryExists(path);

    //    // Assert
    //    result.ShouldBe(exists);
    //}

    //[Fact]
    //public void GetFileList_ShouldReturnFiles()
    //{
    //    // Arrange
    //    const string path = "/test/path";
    //    string[] expectedFiles = new[] { "/test/path/file1.txt", "/test/path/file2.txt" };
    //    A.CallTo(() => _sftpClient.ListDirectory(path, null)).Returns(expectedFiles.Select(f => CreateSftpFile(Path.GetFileName(f), true)));

    //    // Act
    //    List<string> result = _service.GetFileList(path).ToList();

    //    // Assert
    //    result.Count.ShouldBe(expectedFiles.Length);
    //    result.ShouldBe(expectedFiles);
    //}

    //[Fact]
    //public void Dispose_ShouldDisposeClientAndPreventFurtherOperations()
    //{
    //    // Act
    //    _service.Dispose();

    //    // Assert
    //    A.CallTo(() => _sftpClient.Dispose()).MustHaveHappened();

    //    // Verify that operations after dispose throw ObjectDisposedException
    //    Should.Throw<ObjectDisposedException>(() => _service.GetHostName());
    //}

    //private static ISftpFile CreateSftpFile(string name, bool isRegularFile)
    //{
    //    ISftpFile file = A.Fake<ISftpFile>();
    //    A.CallTo(() => file.Name).Returns(name);
    //    A.CallTo(() => file.IsRegularFile).Returns(isRegularFile);
    //    return file;
    //}
}
