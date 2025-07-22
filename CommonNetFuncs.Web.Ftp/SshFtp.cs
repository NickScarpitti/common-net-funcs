using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Csv.CsvReadHelpers;

namespace CommonNetFuncs.Web.Ftp;

public static class SshFtp
{
    /// <summary>
    /// Gets the host name from the specified file transfer connection.
    /// </summary>
    /// <param name="fileTransferConnection">The file transfer connection.</param>
    /// <returns>The host name.</returns>
    public static string GetHostName(this FileTransferConnection fileTransferConnection)
    {
        return fileTransferConnection.HostName;
    }

    /// <summary>
    /// Determines whether the SFTP client is connected.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <returns>True if connected, otherwise false.</returns>
    public static bool IsConnected([NotNullWhen(true)] this SftpClient? sftpClient)
    {
        return sftpClient?.IsConnected ?? false;
    }

    /// <summary>
    /// Connects the SFTP client using the specified file transfer connection.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="fileTransferConnection">The file transfer connection.</param>
    /// <returns>The connected SFTP client.</returns>
    public static SftpClient Connect(this SftpClient? sftpClient, FileTransferConnection fileTransferConnection)
    {
        if (sftpClient.IsConnected())
        {
            sftpClient.Disconnect();
        }
        sftpClient = new(fileTransferConnection.HostName, fileTransferConnection.Port, fileTransferConnection.UserName, fileTransferConnection.Password);
        sftpClient.Connect();

        return sftpClient;
    }

    /// <summary>
    /// Asynchronously connects the SFTP client using the specified file transfer connection.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="fileTransferConnection">The file transfer connection.</param>
    /// <param name="cancellationTokenSource">Optional: The cancellation token source.</param>
    /// <returns>The connected SFTP client.</returns>
    public static async Task<SftpClient> ConnectAsync(this SftpClient? sftpClient, FileTransferConnection fileTransferConnection, CancellationTokenSource? cancellationTokenSource = null)
    {
        cancellationTokenSource ??= new();
        if (sftpClient.IsConnected())
        {
            sftpClient.Disconnect();
        }
        sftpClient = new(fileTransferConnection.HostName, fileTransferConnection.Port, fileTransferConnection.UserName, fileTransferConnection.Password);
        await sftpClient.ConnectAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        return sftpClient;
    }

    /// <summary>
    /// Disconnects the SFTP client if it is connected.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <returns>True if still connected after disconnect attempt, otherwise false.</returns>
    public static bool DisconnectClient(this SftpClient? sftpClient)
    {
        if (sftpClient.IsConnected())
        {
            sftpClient.Disconnect();
            //sftpClient.Dispose();
        }
        return sftpClient.IsConnected();
    }

    /// <summary>
    /// Determines whether a directory or file exists at the specified path on the SFTP server.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the directory or file exists, otherwise false.</returns>
    public static bool DirectoryOrFileExists([NotNullWhen(true)] this SftpClient? sftpClient, string path)
    {
        return sftpClient?.Exists(path) ?? false;
    }

    /// <summary>
    /// Asynchronously determines whether a directory or file exists at the specified path on the SFTP server.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the directory or file exists, otherwise false.</returns>
    public static async Task<bool> DirectoryOrFileExistsAsync([NotNullWhen(true)] this SftpClient? sftpClient, string path)
    {
        return sftpClient != null && await sftpClient.ExistsAsync(path).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an enumerable of file paths in the specified directory on the SFTP server, optionally filtered by extension.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="path">The directory path.</param>
    /// <param name="extension">Optional: The file extension to filter by. Default is "*".</param>
    /// <returns>An enumerable of file paths.</returns>
    public static IEnumerable<string> GetFileList(this SftpClient? sftpClient, string path, string extension = "*")
    {
        IEnumerable<string> files = [];
        if (sftpClient.IsConnected())
        {
            if (!sftpClient.Exists(path))
            {
                throw new Exception($"Path <{path}> cannot be found on host.");
            }

            files = sftpClient.ListDirectory(path).Where(x => x.IsRegularFile && (extension.StrComp("*") || x.Name.EndsWith($".{extension}"))).Select(x => $"{path}/{x.Name}").DefaultIfEmpty(string.Empty);
        }
        return files;
    }

    /// <summary>
    /// Asynchronously enumerates the list of file paths in the specified directory on the SFTP server, optionally filtered by extension.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="path">The directory path.</param>
    /// <param name="extension">Optional: The file extension to filter by. Default is "*".</param>
    /// <param name="cancellationTokenSource">Optional: The cancellation token source.</param>
    /// <returns>An async enumerable of file paths.</returns>
    public static async IAsyncEnumerable<string> GetFileListAsync(this SftpClient? sftpClient, string path, string extension = "*", CancellationTokenSource? cancellationTokenSource = null)
    {
        if (sftpClient.IsConnected())
        {
            if (!sftpClient.Exists(path))
            {
                throw new Exception($"Path <{path}> cannot be found on host.");
            }

            cancellationTokenSource ??= new();
            await foreach (ISftpFile sftpFile in sftpClient.ListDirectoryAsync(path, cancellationTokenSource.Token))
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                if (sftpFile.IsRegularFile && (extension.StrComp("*") || sftpFile.Name.EndsWith($".{extension}")))
                {
                    yield return $"{path}/{sftpFile.Name}";
                }
            }
        }
    }

    /// <summary>
    /// Asynchronously reads data from a CSV file on the SFTP server and returns a list of records of type T.
    /// </summary>
    /// <typeparam name="T">Type to read from rows.</typeparam>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="remoteFilePath">The remote CSV file path.</param>
    /// <param name="csvHasHeaderRow">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>List of T read from the CSV file.</returns>
    public static async Task<List<T>> GetDataFromCsvAsync<T>(this SftpClient? sftpClient, string remoteFilePath, bool csvHasHeaderRow = true, CultureInfo? cultureInfo = null)
    {
        if (!remoteFilePath.EndsWith(".csv") || !await sftpClient.DirectoryOrFileExistsAsync(remoteFilePath).ConfigureAwait(false))
        {
            throw new Exception($"File {remoteFilePath} is not a csv file.  Please use DownloadStream instead.");
        }

        await using SftpFileStream stream = sftpClient!.OpenRead(remoteFilePath);
        return await ReadCsvAsync<T>(stream, csvHasHeaderRow, cultureInfo);
    }

    /// <summary>
    /// Asynchronously reads and enumerates data from a CSV file on the SFTP server and returns an async enumerable of records of type T.
    /// </summary>
    /// <typeparam name="T">Type to read from rows.</typeparam>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="remoteFilePath">The remote CSV file path.</param>
    /// <param name="csvHasHeaderRow">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>Async enumerable of T read from the CSV file.</returns>
    public static async IAsyncEnumerable<T> GetDataFromCsvAsyncEnumerable<T>(this SftpClient? sftpClient, string remoteFilePath, bool csvHasHeaderRow = true, CultureInfo? cultureInfo = null)
    {
        if (!remoteFilePath.EndsWith(".csv") || !await sftpClient.DirectoryOrFileExistsAsync(remoteFilePath).ConfigureAwait(false))
        {
            throw new Exception($"File {remoteFilePath} is not a csv file.  Please use DownloadStream instead.");
        }

        await using SftpFileStream stream = sftpClient!.OpenRead(remoteFilePath);
        await foreach (T item in ReadCsvAsyncEnumerable<T>(stream, csvHasHeaderRow, cultureInfo))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Reads data from a CSV file on the SFTP server and returns a list of records of type T.
    /// </summary>
    /// <typeparam name="T">Type to read from rows.</typeparam>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="remoteFilePath">The remote CSV file path.</param>
    /// <param name="csvHasHeaderRow">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>List of T read from the CSV file.</returns>
    public static List<T> GetDataFromCsv<T>(this SftpClient? sftpClient, string remoteFilePath, bool csvHasHeaderRow = true, CultureInfo? cultureInfo = null)
    {
        if (!remoteFilePath.EndsWith(".csv") || !sftpClient.DirectoryOrFileExists(remoteFilePath))
        {
            throw new Exception($"File {remoteFilePath} is not a csv file.  Please use DownloadStream instead.");
        }

        using SftpFileStream stream = sftpClient.OpenRead(remoteFilePath);
        return ReadCsv<T>(stream, csvHasHeaderRow, cultureInfo);
    }

    /// <summary>
    /// Deletes a file from the SFTP server.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="remoteFilePath">The remote file path to delete.</param>
    /// <returns>True if the file was deleted or does not exist; otherwise, false.</returns>
    public static bool DeleteSftpFile(this SftpClient? sftpClient, string remoteFilePath)
    {
        if (sftpClient == null)
        {
            return false;
        }

        if (sftpClient.Exists(remoteFilePath))
        {
            sftpClient.Delete(remoteFilePath);
        }
        return !sftpClient.Exists(remoteFilePath);
    }

    /// <summary>
    /// Asynchronously deletes a file from the SFTP server.
    /// </summary>
    /// <param name="sftpClient">The SFTP client.</param>
    /// <param name="remoteFilePath">The remote file path to delete.</param>
    /// <returns>True if the file was deleted or does not exist; otherwise, false.</returns>
    public static async Task<bool> DeleteFileAsync(this SftpClient? sftpClient, string remoteFilePath)
    {
        if (sftpClient == null)
        {
            return false;
        }

        if (await sftpClient.ExistsAsync(remoteFilePath).ConfigureAwait(false))
        {
            await sftpClient.DeleteAsync(remoteFilePath).ConfigureAwait(false);
        }
        return !await sftpClient.ExistsAsync(remoteFilePath).ConfigureAwait(false);
    }
}
