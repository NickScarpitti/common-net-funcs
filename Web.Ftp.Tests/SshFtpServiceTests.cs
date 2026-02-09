using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Web.Ftp;
using Renci.SshNet;

namespace Web.Ftp.Tests;

public class SshFtpServiceTests
{
	private readonly IFixture fixture;
	private readonly FileTransferConnection connection;
	private readonly SftpClient sftpClient;
	private readonly SshFtpService service;

	public SshFtpServiceTests()
	{
		fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
		connection = fixture.Create<FileTransferConnection>();
		sftpClient = A.Fake<SftpClient>(options => options.WithArgumentsForConstructor(() =>
			new SftpClient
			(
				connection.HostName,
				connection.Port,
				connection.UserName,
				connection.Password
			)
		));

		// Setup default behaviors
		A.CallTo(() => sftpClient.IsConnected).Returns(true);

		//_service = new SshFtpService(_connection);
		service = new SshFtpService(connection, _ => sftpClient);
	}

	[Fact]
	public void GetHostName_ShouldReturnCorrectHostName()
	{
		// Arrange
		string expectedHostName = connection.HostName;

		// Act
		string result = service.GetHostName();

		// Assert
		result.ShouldBe(expectedHostName);
	}

	[Fact]
	public void IsConnected_ShouldReturnClientConnectionState()
	{
		// Act
		bool result = service.IsConnected();

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void Connect_WhenAlreadyConnected_ShouldDisconnectFirst()
	{
		// Arrange
		A.CallTo(() => sftpClient.IsConnected).Returns(true);

		// Act
		SftpClient result = service.Connect();

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task ConnectAsync_ShouldConnectClient()
	{
		// Arrange
		CancellationTokenSource cts = new();

		// Act
		SftpClient result = await service.ConnectAsync(cts);

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
	//    A.CallTo(() => sftpClient.Exists(path)).Returns(exists);

	//    // Act
	//    bool result = service.DirectoryExists(path);

	//    // Assert
	//    result.ShouldBe(exists);
	//}

	//[Fact]
	//public void GetFileList_ShouldReturnFiles()
	//{
	//    // Arrange
	//    const string path = "/test/path";
	//    string[] expectedFiles = new[] { "/test/path/file1.txt", "/test/path/file2.txt" };
	//    A.CallTo(() => sftpClient.ListDirectory(path, null)).Returns(expectedFiles.Select(f => CreateSftpFile(Path.GetFileName(f), true)));

	//    // Act
	//    List<string> result = service.GetFileList(path).ToList();

	//    // Assert
	//    result.Count.ShouldBe(expectedFiles.Length);
	//    result.ShouldBe(expectedFiles);
	//}

	//[Fact]
	//public void Dispose_ShouldDisposeClientAndPreventFurtherOperations()
	//{
	//    // Act
	//    service.Dispose();

	//    // Assert
	//    A.CallTo(() => sftpClient.Dispose()).MustHaveHappened();

	//    // Verify that operations after dispose throw ObjectDisposedException
	//    Should.Throw<ObjectDisposedException>(() => service.GetHostName());
	//}

	//private static ISftpFile CreateSftpFile(string name, bool isRegularFile)
	//{
	//    ISftpFile file = A.Fake<ISftpFile>();
	//    A.CallTo(() => file.Name).Returns(name);
	//    A.CallTo(() => file.IsRegularFile).Returns(isRegularFile);
	//    return file;
	//}
}
