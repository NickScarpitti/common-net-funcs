# CommonNetFuncs.Web.Aws.S3

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Web.Aws.S3)](https://www.nuget.org/packages/CommonNetFuncs.Web.Aws.S3/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Web.Aws.S3)](https://www.nuget.org/packages/CommonNetFuncs.Web.Aws.S3/)

This lightweight project contains helper methods for several common functions required by applications.

## Contents

- [CommonNetFuncs.Web.Aws.S3](#commonnetfuncswebawss3)
  - [Contents](#contents)
  - [AwsS3HelpersStatic / ApiAwsS3](#awss3helpersstatic--apiawss3)
    - [AwsS3 Usage Examples](#awss3-usage-examples)
      - [UploadS3File](#uploads3file)
      - [GetS3File](#gets3file)
      - [DeleteS3File](#deletes3file)
      - [S3FileExists](#s3fileexists)
      - [GetAllS3BucketFiles](#getalls3bucketfiles)
      - [GetS3Url](#gets3url)
  - [Installation](#installation)
  - [License](#license)

---

## AwsS3HelpersStatic / ApiAwsS3

Provides helpers for uploading, downloading, deleting, and querying files in Amazon S3 buckets. `AwsS3HelpersStatic` exposes all operations as `IAmazonS3` extension methods for direct use. `ApiAwsS3` wraps the same helpers behind an `IAwsS3` interface for dependency-injection scenarios. Large uploads (> 10 MB by default) are automatically routed through multipart upload. Download streams can be optionally decompressed on the fly. Bucket existence is validated and cached per-request to avoid redundant AWS calls.

### AwsS3 Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### UploadS3File

Upload a stream to S3, optionally compressing it with gzip or deflate before transfer.

```cs
using CommonNetFuncs.Web.Aws.S3;

// DI: inject IAwsS3 (backed by ApiAwsS3)
bool success = await _s3.UploadS3File(
    bucketName: "my-bucket",
    fileName: "reports/report.json",
    fileData: jsonStream,
    compressSteam: true,
    compressionType: ECompressionType.Gzip
);

// Static: use IAmazonS3 directly
bool success = await amazonS3Client.UploadS3File("my-bucket", "file.bin", fileStream);
```

#### GetS3File

Download an S3 object into an existing stream, optionally decompressing gzip content automatically.

```cs
using MemoryStream ms = new();
await _s3.GetS3File("my-bucket", "reports/report.json", ms, decompressGzipData: true);
```

#### DeleteS3File

```cs
bool deleted = await _s3.DeleteS3File("my-bucket", "reports/old-report.json");
```

#### S3FileExists

```cs
bool exists = await _s3.S3FileExists("my-bucket", "reports/report.json");
```

#### GetAllS3BucketFiles

Returns all object keys in a bucket, paginating automatically.

```cs
List<string>? keys = await _s3.GetAllS3BucketFiles("my-bucket", maxKeysPerQuery: 1000);
```

#### GetS3Url

Returns the public URL for an object if the bucket is publicly accessible.

```cs
string? url = await _s3.GetS3Url("my-bucket", "reports/report.json");
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Web.Aws.S3
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
