using CommonNetFuncs.Web.Ftp;
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
	#region GetHostName Tests

	[Fact]
	public void GetHostName_ShouldReturnHostName()
	{
		// Arrange
		FileTransferConnection connection = new()
		{
			HostName = "test.server.com",
			UserName = "testuser",
			Password = "testpass",
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
		string path = "/test/path";

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.DirectoryOrFileExists(path))
			.Message.ShouldBe("SFTP client is not connected.");
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
		Should.Throw<SshConnectionException>(() => client.DirectoryOrFileExists(path))
			.Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region DirectoryOrFileExistsAsync Tests

	[Fact]
	public async Task DirectoryOrFileExistsAsync_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		string path = "/test/path";

		// Act & Assert
		var exception = await Should.ThrowAsync<SshConnectionException>(async () =>
			await client.DirectoryOrFileExistsAsync(path));
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
		var exception = await Should.ThrowAsync<SshConnectionException>(async () =>
			await client.DirectoryOrFileExistsAsync(path));
		exception.Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region GetFileList Tests

	[Fact]
	public void GetFileList_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		string path = "/test/path";

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.GetFileList(path))
			.Message.ShouldBe("SFTP client is not connected.");
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
		Should.Throw<SshConnectionException>(() => client.GetFileList(path, extension))
			.Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region GetFileListAsync Tests

	[Fact]
	public async Task GetFileListAsync_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		string path = "/test/path";

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
			await foreach (var _ in client.GetFileListAsync(path))
			{
			}
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
			await foreach (var _ in client.GetFileListAsync(path, extension))
			{
			}
		});
	}

	#endregion

	#region GetDataFromCsvAsync Tests

	[Fact]
	public async Task GetDataFromCsvAsync_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		string path = "/test/file.csv";

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
			await client.GetDataFromCsvAsync<TestCsvModel>(path));
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
		await Should.ThrowAsync<SshConnectionException>(async () =>
			await client.GetDataFromCsvAsync<TestCsvModel>(path));
	}

	#endregion

	#region GetDataFromCsvAsyncEnumerable Tests

	[Fact]
	public async Task GetDataFromCsvAsyncEnumerable_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		string path = "/test/file.csv";

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
			await foreach (var _ in client.GetDataFromCsvAsyncEnumerable<TestCsvModel>(path))
			{
			}
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
			await foreach (var _ in client.GetDataFromCsvAsyncEnumerable<TestCsvModel>(path))
			{
			}
		});
	}

	#endregion

	#region GetDataFromCsvCopyAsyncEnumerable Tests

	[Fact]
	public async Task GetDataFromCsvCopyAsyncEnumerable_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		string path = "/test/file.csv";

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
			await foreach (var _ in client.GetDataFromCsvCopyAsyncEnumerable<TestCsvModel>(path))
			{
			}
		});
	}

	[Theory]
	[InlineData("/backup/data.csv")]
	[InlineData("/tmp/export.csv")]
	public async Task GetDataFromCsvCopyAsyncEnumerable_WhenClientIsNull_ShouldThrowForAnyCsvPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		await Should.ThrowAsync<SshConnectionException>(async () =>
		{
			await foreach (var _ in client.GetDataFromCsvCopyAsyncEnumerable<TestCsvModel>(path))
			{
			}
		});
	}

	#endregion

	#region GetDataFromCsv Tests

	[Fact]
	public void GetDataFromCsv_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		string path = "/test/file.csv";

		// Act & Assert
		Should.Throw<SshConnectionException>(() =>
			client.GetDataFromCsv<TestCsvModel>(path));
	}

	[Theory]
	[InlineData("/uploads/data.csv")]
	[InlineData("/share/report.csv")]
	public void GetDataFromCsv_WhenClientIsNull_ShouldThrowForAnyCsvPath(string path)
	{
		// Arrange
		SftpClient? client = null;

		// Act & Assert
		Should.Throw<SshConnectionException>(() =>
			client.GetDataFromCsv<TestCsvModel>(path));
	}

	#endregion

	#region DeleteSftpFile Tests

	[Fact]
	public void DeleteSftpFile_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		string path = "/test/file.txt";

		// Act & Assert
		Should.Throw<SshConnectionException>(() => client.DeleteSftpFile(path))
			.Message.ShouldBe("SFTP client is not connected.");
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
		Should.Throw<SshConnectionException>(() => client.DeleteSftpFile(path))
			.Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region DeleteFileAsync Tests

	[Fact]
	public async Task DeleteFileAsync_WhenClientIsNull_ShouldThrowSshConnectionException()
	{
		// Arrange
		SftpClient? client = null;
		string path = "/test/file.txt";

		// Act & Assert
		var exception = await Should.ThrowAsync<SshConnectionException>(async () =>
			await client.DeleteFileAsync(path));
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
		var exception = await Should.ThrowAsync<SshConnectionException>(async () =>
			await client.DeleteFileAsync(path));
		exception.Message.ShouldBe("SFTP client is not connected.");
	}

	#endregion

	#region Test Models

	private class TestCsvModel
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
	}

	#endregion
}
