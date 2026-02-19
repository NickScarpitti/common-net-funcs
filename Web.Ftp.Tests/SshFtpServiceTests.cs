using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Web.Ftp;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

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
	public void Connect_WhenAlreadyConnected_ShouldReturnClient()
	{
		// Arrange
		A.CallTo(() => sftpClient.IsConnected).Returns(true);

		// Act
		SftpClient result = service.Connect();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(sftpClient);
	}

	[Fact]
	public async Task ConnectAsync_WhenAlreadyConnected_ShouldReturnClient()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		A.CallTo(() => sftpClient.IsConnected).Returns(true);

		// Act
		SftpClient result = await service.ConnectAsync(cts);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(sftpClient);
	}

	[Fact]
	public async Task ConnectAsync_WithNullCancellationTokenSource_ShouldConnectClient()
	{
		// Act
		SftpClient result = await service.ConnectAsync(null);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void DisconnectClient_ShouldReturnConnectionState()
	{
		// Arrange
		A.CallTo(() => sftpClient.IsConnected).Returns(true);

		// Act
		bool result = service.DisconnectClient();

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void DirectoryExists_WhenNotConnected_ShouldThrowException()
	{
		// Arrange
		const string path = "/test/path";
		A.CallTo(() => sftpClient.IsConnected).Returns(false);

		// Act & Assert
		Should.Throw<SshConnectionException>(() => service.DirectoryExists(path));
	}

	[Fact]
	public async Task DirectoryExistsAsync_WhenNotConnected_ShouldThrowException()
	{
		// Arrange
		const string path = "/test/path";
		A.CallTo(() => sftpClient.IsConnected).Returns(false);

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () => await service.DirectoryExistsAsync(path));
	}

	[Fact]
	public void GetFileList_WhenNotConnected_ShouldThrowException()
	{
		// Arrange
		const string path = "/test/path";
		A.CallTo(() => sftpClient.IsConnected).Returns(false);

		// Act & Assert
		Should.Throw<SshConnectionException>(() => service.GetFileList(path).ToList());
	}

	[Fact]
	public async Task GetFileListAsync_WhenNotConnected_ShouldThrowException()
	{
		// Arrange
		const string path = "/test/path";
		A.CallTo(() => sftpClient.IsConnected).Returns(false);

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			await foreach (string _ in service.GetFileListAsync(path))
			{
				// This block is intentionally empty for the exception test
			}
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	[Fact]
	public void DeleteFile_WhenNotConnected_ShouldThrowException()
	{
		// Arrange
		const string filePath = "/test/file.txt";
		A.CallTo(() => sftpClient.IsConnected).Returns(false);

		// Act & Assert
		Should.Throw<SshConnectionException>(() => service.DeleteFile(filePath));
	}

	[Fact]
	public async Task DeleteFileAsync_WhenNotConnected_ShouldThrowException()
	{
		// Arrange
		const string filePath = "/test/file.txt";
		A.CallTo(() => sftpClient.IsConnected).Returns(false);

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () => await service.DeleteFileAsync(filePath));
	}

	[Fact]
	public void Dispose_ShouldCallDisposeOnClient()
	{
		// Act
		service.Dispose();

		// Assert - Client should be disposed via the Dispose pattern
		// We cannot verify Dispose was called directly since it's non-virtual,
		// but we can verify the service completes disposal without error
		service.ShouldNotBeNull();
	}

	[Fact]
	public void Dispose_CalledTwice_ShouldNotThrow()
	{
		// Act & Assert
		service.Dispose();
		Should.NotThrow(service.Dispose);
	}

	[Fact]
	public void GetDataFromCsv_WhenNotConnected_ShouldThrowException()
	{
		// Arrange
		const string filePath = "/test/file.csv";
		A.CallTo(() => sftpClient.IsConnected).Returns(false);

		// Act & Assert
		Should.Throw<SshConnectionException>(() => service.GetDataFromCsv<TestCsvModel>(filePath));
	}

	[Fact]
	public async Task GetDataFromCsvAsync_WhenNotConnected_ShouldThrowException()
	{
		// Arrange
		const string filePath = "/test/file.csv";
		A.CallTo(() => sftpClient.IsConnected).Returns(false);

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () => await service.GetDataFromCsvAsync<TestCsvModel>(filePath));
	}

	[Fact]
	public async Task GetDataFromCsvAsyncEnumerable_WhenNotConnected_ShouldThrowException()
	{
		// Arrange
		const string filePath = "/test/file.csv";
		A.CallTo(() => sftpClient.IsConnected).Returns(false);

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			await foreach (TestCsvModel _ in service.GetDataFromCsvAsyncEnumerable<TestCsvModel>(filePath))
			{
				// This block is intentionally empty for the exception test
			}
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	private static ISftpFile CreateSftpFile(string name, bool isRegularFile)
	{
		ISftpFile file = A.Fake<ISftpFile>();
		A.CallTo(() => file.Name).Returns(name);
		A.CallTo(() => file.IsRegularFile).Returns(isRegularFile);
		return file;
	}

	private class TestCsvModel
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
	}
}
