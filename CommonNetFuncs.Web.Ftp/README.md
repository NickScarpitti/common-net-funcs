# CommonNetFuncs.Web.Ftp

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Web.Ftp)](https://www.nuget.org/packages/CommonNetFuncs.Web.Ftp/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Web.Ftp)](https://www.nuget.org/packages/CommonNetFuncs.Web.Ftp/)

This lightweight project contains helper methods for several common functions required by applications.

## Contents

- [CommonNetFuncs.Web.Ftp](#commonnetfuncswebftp)
  - [Contents](#contents)
  - [SshFtp](#sshftp)
    - [SshFtp Usage Examples](#sshftp-usage-examples)
      - [Connect / ConnectAsync](#connect--connectasync)
      - [GetFileList / GetFileListAsync](#getfilelist--getfilelistasync)
      - [GetDataFromCsvAsync](#getdatafromcsvAsync)
      - [UploadFile / UploadFileAsync](#uploadfile--uploadfileasync)
      - [DeleteFile / DeleteFileAsync](#deletefile--deletefileasync)
  - [SshFtpService](#sshftpservice)
    - [SshFtpService Usage Examples](#sshftpservice-usage-examples)
  - [Installation](#installation)
  - [License](#license)

---

## SshFtp

A collection of `SftpClient` extension methods (from SSH.NET) for common SFTP operations: connecting and disconnecting, checking whether a remote path exists, listing remote files by extension, reading CSV files directly from the server, and uploading or deleting files. All methods come in both synchronous and `async` variants.

### SshFtp Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Connect / ConnectAsync

```cs
using CommonNetFuncs.Web.Ftp;
using Renci.SshNet;

FileTransferConnection conn = new()
{
    HostName = "sftp.example.com",
    Port = 22,
    UserName = "user",
    Password = "secret",
    BufferSize = 65536
};

SftpClient client = null!;
client = await client.ConnectAsync(conn);
```

#### GetFileList / GetFileListAsync

```cs
// Synchronous — returns file paths filtered by extension
IEnumerable<string> csvFiles = client.GetFileList("/remote/data", extension: "csv");

// Async — async-streams file paths
await foreach (string path in client.GetFileListAsync("/remote/data", extension: "csv"))
{
    Console.WriteLine(path);
}
```

#### GetDataFromCsvAsync

Downloads a remote CSV file and deserializes it into a `List<T>` using CsvHelper.

```cs
List<MyRecord> records = await client.GetDataFromCsvAsync<MyRecord>("/remote/data/report.csv");
```

#### UploadFile / UploadFileAsync

```cs
await using FileStream fs = File.OpenRead("local/report.csv");
await client.UploadFileAsync(fs, "/remote/data/report.csv");
```

#### DeleteFile / DeleteFileAsync

```cs
bool deleted = await client.DeleteFileAsync("/remote/data/old-report.csv");
```

</details>

---

## SshFtpService

A disposable service class that wraps an `SftpClient` and a `FileTransferConnection` for use with dependency injection. Exposes the same operations as `SshFtp` through the `ISshFtpService` interface, managing connection state automatically.

### SshFtpService Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

```cs
using CommonNetFuncs.Web.Ftp;

// Registration
builder.Services.AddSingleton<ISshFtpService>(_ =>
    new SshFtpService(new FileTransferConnection
    {
        HostName = "sftp.example.com",
        Port = 22,
        UserName = "user",
        Password = "secret"
    }));

// Usage in a controller or service
public class ReportService(ISshFtpService ftp)
{
    public async Task<List<MyRecord>> FetchLatestReport()
        => await ftp.GetDataFromCsvAsync<MyRecord>("/remote/reports/latest.csv");
}
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Web.Ftp
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
