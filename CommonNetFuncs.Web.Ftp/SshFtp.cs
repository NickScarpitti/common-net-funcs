﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Csv.CsvReadHelpers;

namespace CommonNetFuncs.Web.Ftp;

public static class SshFtp
{
    public static string GetHostName(this FileTransferConnection fileTransferConnection)
    {
        return fileTransferConnection.HostName;
    }

    public static bool IsConnected([NotNullWhen(true)] this SftpClient? sftpClient)
    {
        return sftpClient?.IsConnected ?? false;
    }

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

    public static async Task<SftpClient> ConnectAsync(this SftpClient? sftpClient, FileTransferConnection fileTransferConnection, CancellationTokenSource? cancellationTokenSource)
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

    public static bool DisconnectClient(this SftpClient? sftpClient)
    {
        if (sftpClient.IsConnected())
        {
            sftpClient.Disconnect();
            //sftpClient.Dispose();
        }
        return sftpClient.IsConnected();
    }

    public static bool DirectoryOrFileExists([NotNullWhen(true)] this SftpClient? sftpClient, string path)
    {
        return sftpClient?.Exists(path) ?? false;
    }

    public static async Task<bool> DirectoryOrFileExistsAsync([NotNullWhen(true)] this SftpClient? sftpClient, string path)
    {
        return sftpClient != null && await sftpClient.ExistsAsync(path).ConfigureAwait(false);
    }

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

    public static async Task<List<T>> GetDataFromCsvAsync<T>(this SftpClient? sftpClient, string remoteFilePath, bool csvHasHeaderRow = true)
    {
        if (!remoteFilePath.EndsWith(".csv") || !await sftpClient.DirectoryOrFileExistsAsync(remoteFilePath).ConfigureAwait(false))
        {
            throw new Exception($"File {remoteFilePath} is not a csv file.  Please use DownloadStream instead.");
        }

        await using SftpFileStream stream = sftpClient!.OpenRead(remoteFilePath);
        return ReadCsv<T>(stream, csvHasHeaderRow);
    }

    public static List<T> GetDataFromCsv<T>(this SftpClient? sftpClient, string remoteFilePath, bool csvHasHeaderRow = true)
    {
        if (!remoteFilePath.EndsWith(".csv") || !sftpClient.DirectoryOrFileExists(remoteFilePath))
        {
            throw new Exception($"File {remoteFilePath} is not a csv file.  Please use DownloadStream instead.");
        }

        //TODO:: See if this can be replaced by ReadCsvFromStream
        using SftpFileStream stream = sftpClient.OpenRead(remoteFilePath);
        using StreamReader reader = new(stream);
        using CsvReader csvReader = new(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = csvHasHeaderRow,
        });
        return new List<T>(csvReader.GetRecords<T>());
    }

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
