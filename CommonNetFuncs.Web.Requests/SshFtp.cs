using CsvHelper.Configuration;
using CsvHelper;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System.Globalization;

namespace CommonNetFuncs.Web.Requests;
public class SshFtp<T>
{
    private SftpClient Client;
    private FileTransferConnection Connection;

    public SshFtp(FileTransferConnection fileTransferConnection)
    {
        Connection = fileTransferConnection;
        Client = _Connect();
    }

    public string GetHostName()
    {
        return Connection.HostName;
    }

    public bool IsConnected()
    {
        return Client?.IsConnected ?? false;
    }

    private SftpClient _Connect()
    {
        if (IsConnected())
        {
            this.Client!.Disconnect();
            this.Client.Dispose();
        }
        SftpClient client = new SftpClient(Connection.HostName, Connection.Port, Connection.UserName, Connection.Password);
        client.Connect();

        return client;
    }
    public bool Connect(FileTransferConnection fileTransferConnection)
    {
        Connection = fileTransferConnection;
        Client = _Connect();
        return IsConnected();
    }

    public bool Connect()
    {
        Client = _Connect();
        return IsConnected();
    }

    public bool Disconnect()
    {
        if (IsConnected())
        {
            Client!.Disconnect();
            Client.Dispose();
        }
        return IsConnected();
    }

    public bool DirectoryExists(string path)
    {
        return Client.Exists(path);
    }

    public List<string> GetFileList(string path, string extension = "*")
    {
        List<string> files = [];
        if (IsConnected())
        {
            if (!Client.Exists(path))
            {
                throw new Exception($"Path <{path}> cannot be found on host.");
            }
            List<ISftpFile> sftpFiles = Client!.ListDirectory(path).ToList();
            files = sftpFiles.Where(x => x.IsRegularFile && (extension == "*" || x.Name.EndsWith($".{extension}"))).Select(x => $"{path}/{x.Name}").DefaultIfEmpty(string.Empty).ToList();
        }
        return files;
    }

    public List<T> GetDataFromCsv(string remoteFilePath, bool csvHasHeaderRow = true)
    {
        if (!remoteFilePath.EndsWith(".csv") || !Client.Exists(remoteFilePath))
        {
            throw new Exception($"File {remoteFilePath} is not a csv file.  Please use DownloadStream instead.");
        }
        //string[] data = Client.ReadAllLines(remoteFilePath);

        using (SftpFileStream stream = Client.OpenRead(remoteFilePath))
        using (StreamReader reader = new StreamReader(stream))
        using (CsvReader csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = csvHasHeaderRow,
        }))
        {
            return new List<T>(csvReader.GetRecords<T>());
        }

    }

    public bool DeleteFile(string remoteFilePath)
    {
        if (Client.Exists(remoteFilePath))
        {
            Client.Delete(remoteFilePath);            
        }
        return !Client.Exists(remoteFilePath);
    }
}
