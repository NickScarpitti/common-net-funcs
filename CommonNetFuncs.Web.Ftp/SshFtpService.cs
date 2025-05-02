
using Renci.SshNet;

namespace CommonNetFuncs.Web.Ftp;

public interface ISshFtpService
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
    List<T> GetDataFromCsv<T>(string remoteFilePath, bool csvHasHeaderRow = true);
    bool DeleteFile(string remoteFilePath);
    Task<bool> DeleteFileAsync(string remoteFilePath);
    void Dispose();
}

public sealed class SshFtpService : IDisposable, ISshFtpService
{
    private readonly SftpClient client;
    private readonly FileTransferConnection connection;
    private bool disposed;

    public SshFtpService(FileTransferConnection fileTransferConnection)
    {
        connection = fileTransferConnection;
        client = Connect();
    }

    public string GetHostName()
    {
        return connection.GetHostName();
    }

    public bool IsConnected()
    {
        return client.IsConnected();
    }

    public SftpClient Connect()
    {
        if (IsConnected())
        {
            DisconnectClient();
        }
        return client.Connect(connection);
    }

    public Task<SftpClient> ConnectAsync(CancellationTokenSource? cancellationTokenSource)
    {
        return client.ConnectAsync(connection, cancellationTokenSource);
    }

    public bool DisconnectClient()
    {
        return client.DisconnectClient();
    }

    public bool DirectoryExists(string path)
    {
        return client.DirectoryOrFileExists(path);
    }

    public Task<bool> DirectoryExistsAsync(string path)
    {
        return client.DirectoryOrFileExistsAsync(path);
    }

    public IEnumerable<string> GetFileList(string path, string extension = "*")
    {
        return client.GetFileList(path, extension);
    }

    public IAsyncEnumerable<string> GetFileListAsync(string path, string extension = "*", CancellationTokenSource? cancellationTokenSource = null)
    {
        return client.GetFileListAsync(path, extension, cancellationTokenSource);
    }

    public Task<List<T>> GetDataFromCsvAsync<T>(string remoteFilePath, bool csvHasHeaderRow = true)
    {
        return client.GetDataFromCsvAsync<T>(remoteFilePath, csvHasHeaderRow);
    }

    public List<T> GetDataFromCsv<T>(string remoteFilePath, bool csvHasHeaderRow = true)
    {
        return client.GetDataFromCsv<T>(remoteFilePath, csvHasHeaderRow);
    }

    public bool DeleteFile(string remoteFilePath)
    {
        return client.DeleteSftpFile(remoteFilePath);
    }

    public Task<bool> DeleteFileAsync(string remoteFilePath)
    {
        return client.DeleteFileAsync(remoteFilePath);
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
                // Dispose managed resources
                client?.Dispose();
            }

            // Dispose unmanaged resources (if any)

            disposed = true;
        }
    }

    ~SshFtpService()
    {
        Dispose(false);
    }
}
