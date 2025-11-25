using System.Globalization;
using Renci.SshNet;

namespace CommonNetFuncs.Web.Ftp;

public interface ISshFtpService : IDisposable
{
	string GetHostName();

	bool IsConnected();

	SftpClient Connect();

	Task<SftpClient> ConnectAsync(CancellationTokenSource? cancellationTokenSource);

	bool DisconnectClient();

	bool DirectoryExists(string path);

	Task<bool> DirectoryExistsAsync(string path);

	IEnumerable<string> GetFileList(string path, string extension = "*");

	IAsyncEnumerable<string> GetFileListAsync(string path, string extension = "*", CancellationTokenSource? cancellationTokenSource = null);

	List<T> GetDataFromCsv<T>(string remoteFilePath, bool csvHasHeaderRow = true, CultureInfo? cultureInfo = null, int bufferSize = 4096);

	Task<List<T>> GetDataFromCsvAsync<T>(string remoteFilePath, bool csvHasHeaderRow = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, CancellationToken cancellationToken = default);

	IAsyncEnumerable<T> GetDataFromCsvAsyncEnumerable<T>(string remoteFilePath, bool csvHasHeaderRow = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, CancellationToken cancellationToken = default);

	bool DeleteFile(string remoteFilePath);

	Task<bool> DeleteFileAsync(string remoteFilePath);
}

public sealed class SshFtpService : ISshFtpService
{
	public SshFtpService(FileTransferConnection fileTransferConnection, Func<FileTransferConnection, SftpClient>? clientFactory = null)
	{
		connection = fileTransferConnection;
		Client = clientFactory?.Invoke(connection) ?? Connect();
	}

	public SftpClient Client { get; private set; }

	//private readonly SftpClient Client;
	private readonly FileTransferConnection connection;
	private bool disposed;

	public string GetHostName()
	{
		return connection.GetHostName();
	}

	public bool IsConnected()
	{
		return Client.IsConnected();
	}

	public SftpClient Connect()
	{
		if (!Client.IsConnected())
		{
			Client = Client.Connect(connection);
		}
		return Client;
	}

	public async Task<SftpClient> ConnectAsync(CancellationTokenSource? cancellationTokenSource)
	{
		if (!Client.IsConnected())
		{
			await Client.ConnectAsync(cancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
		}
		return Client;
	}

	public bool DisconnectClient()
	{
		return Client.DisconnectClient();
	}

	public bool DirectoryExists(string path)
	{
		return Client.DirectoryOrFileExists(path);
	}

	public Task<bool> DirectoryExistsAsync(string path)
	{
		return Client.DirectoryOrFileExistsAsync(path);
	}

	public IEnumerable<string> GetFileList(string path, string extension = "*")
	{
		return Client.GetFileList(path, extension);
	}

	public IAsyncEnumerable<string> GetFileListAsync(string path, string extension = "*", CancellationTokenSource? cancellationTokenSource = null)
	{
		return Client.GetFileListAsync(path, extension, cancellationTokenSource);
	}

	public Task<List<T>> GetDataFromCsvAsync<T>(string remoteFilePath, bool csvHasHeaderRow = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, CancellationToken cancellationToken = default)
	{
		return Client.GetDataFromCsvAsync<T>(remoteFilePath, csvHasHeaderRow, cultureInfo, bufferSize, cancellationToken);
	}

	public IAsyncEnumerable<T> GetDataFromCsvAsyncEnumerable<T>(string remoteFilePath, bool csvHasHeaderRow = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, CancellationToken cancellationToken = default)
	{
		return Client.GetDataFromCsvAsyncEnumerable<T>(remoteFilePath, csvHasHeaderRow, cultureInfo, bufferSize, cancellationToken);
	}

	public List<T> GetDataFromCsv<T>(string remoteFilePath, bool csvHasHeaderRow = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
	{
		return Client.GetDataFromCsv<T>(remoteFilePath, csvHasHeaderRow, cultureInfo, bufferSize);
	}

	public bool DeleteFile(string remoteFilePath)
	{
		return Client.DeleteSftpFile(remoteFilePath);
	}

	public Task<bool> DeleteFileAsync(string remoteFilePath)
	{
		return Client.DeleteFileAsync(remoteFilePath);
	}

	// IDisposable implementation
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				Client?.Dispose();
			}
			disposed = true;
		}
	}

	~SshFtpService()
	{
		Dispose(false);
	}
}
