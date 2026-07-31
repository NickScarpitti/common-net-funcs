using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Web.Ftp;
using FakeItEasy;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Web.Ftp.Tests;

/// <summary>
/// Tests for SshFtp extension methods.
/// Note: Many methods cannot be fully tested with mocks because SftpClient methods are non-virtual.
/// These tests focus on null checks and basic validation logic that can be tested without actual SFTP connections.
/// </summary>
public class SshFtpTests
{
	// SftpClient.IsConnected is the only virtual member on SftpClient, so it's the only thing FakeItEasy can override.
	// Faking it to true (without a real network connection) lets these tests exercise the "client believes it's connected"
	// branch of each extension method; the underlying real SftpClient methods then throw their own SshConnectionException
	// (message "Client not connected.") because the internal SFTP session was never actually established.
	private static SftpClient CreateConnectedFakeClient()
	{
		IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
		FileTransferConnection connection = fixture.Create<FileTransferConnection>();
		SftpClient sftpClient = A.Fake<SftpClient>(options => options.WithArgumentsForConstructor(() =>
			new SftpClient(connection.HostName, connection.Port, connection.UserName, connection.Password)));
		A.CallTo(() => sftpClient.IsConnected).Returns(true);
		return sftpClient;
	}

	#region GetHostName Tests

	[Fact]
	public void GetHostName_ShouldReturnHostName()
	{
		// Arrange
		FileTransferConnection connection = new()
		{
			HostName = "test.server.com",
			UserName = "TestUser",
			Password = "TestPass",
			Port = 22
		};

		// Act
		string result = connection.GetHostName();

		// Assert
		result.ShouldBe("test.server.com");
	}

	[Fact]
	public void GetHostName_WithDifferentHostNames_ShouldReturnCorrectValue()
	{
		// Arrange
		FileTransferConnection connection1 = new() { HostName = "server1.com", UserName = "user", Password = "pass", Port = 22 };
		FileTransferConnection connection2 = new() { HostName = "ftp.example.org", UserName = "user", Password = "pass", Port = 22 };

		// Act & Assert
		connection1.GetHostName().ShouldBe("server1.com");
		connection2.GetHostName().ShouldBe("ftp.example.org");
	}

	#endregion

	#region IsConnected Tests

	[Fact]
	public void IsConnected_WhenClientIsNull_ShouldReturnFalse()
	{
		// Arrange
		SftpClient? client = null;

		// Act
		bool result = client.IsConnected();

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region DisconnectClient Tests

	[Fact]
	public void DisconnectClient_WhenClientIsNull_ShouldReturnFalse()
	{
		// Arrange
		SftpClient? client = null;

		// Act
		bool result = client.DisconnectClient();

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region DirectoryOrFileExists Tests

	[Fact]
	public void DirectoryOrFileExists_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/path";

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.DirectoryOrFileExists(path)).Message.ShouldBe("SFTP client is not connected.");
	}

	[Theory]
	[InlineData("/test/path")]
	[InlineData("/another/directory")]
	[InlineData("/file.txt")]
	public void DirectoryOrFileExists_WhenClientIsNull_ShouldThrowForAnyPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.DirectoryOrFileExists(path)).Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region DirectoryOrFileExistsAsync Tests

	[Fact]
	public async Task DirectoryOrFileExistsAsync_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/path";

		// Act & Assert
		SshConnectionException exception = await Should.ThrowAsync<SshConnectionException>(async () => await client.DirectoryOrFileExistsAsync(path));
		exception.Message.ShouldBe("SFTP client is not connected.");
	}

	[Theory]
	[InlineData("/test/path")]
	[InlineData("/data/file.xml")]
	[InlineData("/home/user/docs")]
	public async Task DirectoryOrFileExistsAsync_WhenClientIsNull_ShouldThrowForAnyPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		SshConnectionException exception = await Should.ThrowAsync<SshConnectionException>(async () => await client.DirectoryOrFileExistsAsync(path));
		exception.Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region GetFileList Tests

	[Fact]
	public void GetFileList_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/path";

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.GetFileList(path)).Message.ShouldBe("SFTP client is not connected.");
	}

	[Theory]
	[InlineData("/test/path", "*")]
	[InlineData("/data", "txt")]
	[InlineData("/files", "csv")]
	public void GetFileList_WhenClientIsNull_ShouldThrowForAnyPathAndExtension(string path, string extension)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.GetFileList(path, extension)).Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region GetFileListAsync Tests

	[Fact]
	public async Task GetFileListAsync_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/path";

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			await foreach (string _ in client.GetFileListAsync(path)) { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	[Theory]
	[InlineData("/test", "*")]
	[InlineData("/data", "log")]
	[InlineData("/files", "json")]
	public async Task GetFileListAsync_WhenClientIsNull_ShouldThrowForAnyPathAndExtension(string path, string extension)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			await foreach (string _ in client.GetFileListAsync(path, extension)) { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	#endregion

	#region GetDataFromCsvAsync Tests

	[Fact]
	public async Task GetDataFromCsvAsync_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/file.csv";

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () => await client.GetDataFromCsvAsync<TestCsvModel>(path));
	}

	[Theory]
	[InlineData("/data/test.csv")]
	[InlineData("/files/data.csv")]
	[InlineData("/export.csv")]
	public async Task GetDataFromCsvAsync_WhenClientIsNull_ShouldThrowForAnyCsvPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () => await client.GetDataFromCsvAsync<TestCsvModel>(path));
	}

	#endregion

	#region GetDataFromCsvAsyncEnumerable Tests

	[Fact]
	public async Task GetDataFromCsvAsyncEnumerable_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/file.csv";

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			await foreach (TestCsvModel _ in client.GetDataFromCsvAsyncEnumerable<TestCsvModel>(path)) { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	[Theory]
	[InlineData("/data/records.csv")]
	[InlineData("/output.csv")]
	public async Task GetDataFromCsvAsyncEnumerable_WhenClientIsNull_ShouldThrowForAnyCsvPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			await foreach (TestCsvModel _ in client.GetDataFromCsvAsyncEnumerable<TestCsvModel>(path)) { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	#endregion

	#region GetDataFromCsvCopyAsyncEnumerable Tests

	[Fact]
	public async Task GetDataFromCsvEnumerable_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/file.csv";

		// Act & Assert
		Should.Throw<SshConnectionException>(() =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			foreach (TestCsvModel _ in client.GetDataFromCsvEnumerable<TestCsvModel>(path)) { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	[Theory]
	[InlineData("/backup/data.csv")]
	[InlineData("/tmp/export.csv")]
	public void GetDataFromCsvEnumerable_WhenClientIsNull_ShouldThrowForAnyCsvPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		Should.Throw<SshConnectionException>(() =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			foreach (TestCsvModel _ in client.GetDataFromCsvEnumerable<TestCsvModel>(path)) { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	#endregion

	#region GetDataFromCsv Tests

	[Fact]
	public void GetDataFromCsv_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/file.csv";

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.GetDataFromCsv<TestCsvModel>(path));
	}

	[Theory]
	[InlineData("/uploads/data.csv")]
	[InlineData("/share/report.csv")]
	public void GetDataFromCsv_WhenClientIsNull_ShouldThrowForAnyCsvPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.GetDataFromCsv<TestCsvModel>(path));
	}

	#endregion

	#region DeleteSftpFile Tests

	[Fact]
	public void DeleteSftpFile_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/file.txt";

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.DeleteSftpFile(path)).Message.ShouldBe("SFTP client is not connected.");
	}

	[Theory]
	[InlineData("/tmp/file.log")]
	[InlineData("/data/old.dat")]
	[InlineData("/delete-me.txt")]
	public void DeleteSftpFile_WhenClientIsNull_ShouldThrowForAnyPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.DeleteSftpFile(path)).Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region DeleteFileAsync Tests

	[Fact]
	public async Task DeleteFileAsync_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		const string path = "/test/file.txt";

		// Act & Assert
		SshConnectionException exception = await Should.ThrowAsync<SshConnectionException>(async () => await client.DeleteFileAsync(path));
		exception.Message.ShouldBe("SFTP client is not connected.");
	}

	[Theory]
	[InlineData("/archive/old.zip")]
	[InlineData("/temp/cache.tmp")]
	[InlineData("/remove.dat")]
	public async Task DeleteFileAsync_WhenClientIsNull_ShouldThrowForAnyPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		SshConnectionException exception = await Should.ThrowAsync<SshConnectionException>(async () => await client.DeleteFileAsync(path));
		exception.Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region Connected (faked) client tests

	// These exercise the "IsConnected() == true" branch of each guard clause, which is unreachable when the client is null.
	// Since the fake client was never actually connected to a real server, the underlying real SftpClient calls then throw
	// their own SshConnectionException("Client not connected.") once they reach the SFTP session null-check.

	[Fact]
	public void DisconnectClient_WhenConnected_ShouldAttemptDisconnectAndReturnConnectionState()
	{
		SftpClient client = CreateConnectedFakeClient();

		// The fake reports IsConnected == true, so DisconnectClient calls the real (non-virtual) Disconnect(); since there's
		// no actual session, SSH.NET no-ops rather than throwing, and the final IsConnected() check (still faked true) returns true.
		bool result = client.DisconnectClient();
		result.ShouldBeTrue();
	}

	[Fact]
	public void DirectoryOrFileExists_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		Should.Throw<SshConnectionException>(() => client.DirectoryOrFileExists("/test/path")).Message.ShouldBe("Client not connected.");
	}

	[Fact]
	public async Task DirectoryOrFileExistsAsync_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		SshConnectionException exception = await Should.ThrowAsync<SshConnectionException>(async () => await client.DirectoryOrFileExistsAsync("/test/path"));
		exception.Message.ShouldBe("Client not connected.");
	}

	[Fact]
	public void GetFileList_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		Should.Throw<SshConnectionException>(() => client.GetFileList("/test/path")).Message.ShouldBe("Client not connected.");
	}

	[Fact]
	public async Task GetFileListAsync_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			await foreach (string _ in client.GetFileListAsync("/test/path")) { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	[Fact]
	public async Task GetDataFromCsvAsync_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		await Should.ThrowAsync<SshConnectionException>(async () => await client.GetDataFromCsvAsync<TestCsvModel>("/test/file.csv"));
	}

	[Fact]
	public async Task GetDataFromCsvAsyncEnumerable_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			await foreach (TestCsvModel _ in client.GetDataFromCsvAsyncEnumerable<TestCsvModel>("/test/file.csv")) { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	[Fact]
	public void GetDataFromCsvEnumerable_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		Should.Throw<SshConnectionException>(() =>
		{
#pragma warning disable S108 // Nested blocks of code should not be left empty
			foreach (TestCsvModel _ in client.GetDataFromCsvEnumerable<TestCsvModel>("/test/file.csv")) { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
		});
	}

	[Fact]
	public void GetDataFromCsv_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		Should.Throw<SshConnectionException>(() => client.GetDataFromCsv<TestCsvModel>("/test/file.csv"));
	}

	[Fact]
	public void DeleteSftpFile_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		Should.Throw<SshConnectionException>(() => client.DeleteSftpFile("/test/file.txt")).Message.ShouldBe("Client not connected.");
	}

	[Fact]
	public async Task DeleteFileAsync_WhenConnected_ShouldThrowUnderlyingConnectionException()
	{
		SftpClient client = CreateConnectedFakeClient();
		SshConnectionException exception = await Should.ThrowAsync<SshConnectionException>(async () => await client.DeleteFileAsync("/test/file.txt"));
		exception.Message.ShouldBe("Client not connected.");
	}

	#endregion

	#region Test Models

#pragma warning disable S1144 // Unused private types or members should be removed
	private class TestCsvModel
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
#pragma warning restore S1144 // Unused private types or members should be removed
	}

	#endregion
}
