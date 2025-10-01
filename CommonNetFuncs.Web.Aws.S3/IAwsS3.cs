using System.Collections.Concurrent;
using NLog;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Web.Aws.S3.ApiAwsS3;

namespace CommonNetFuncs.Web.Aws.S3;

public interface IAwsS3
{
  Task<bool> UploadS3File(string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null, long thresholdForMultiPartUpload = MultipartThreshold,
    bool compressSteam = true, ECompressionType compressionType = ECompressionType.Gzip, LogLevel? logLevel = null, CancellationToken cancellationToken = default);

  Task<bool> UploadS3File(string bucketName, string fileName, string filePath, ConcurrentDictionary<string, bool>? validatedBuckets = null, long thresholdForMultiPartUpload = MultipartThreshold, LogLevel? logLevel = null, CancellationToken cancellationToken = default);

  Task<bool> UploadMultipartAsync(string bucketName, string fileName, Stream stream, LogLevel? logLevel = null, CancellationToken cancellationToken = default);

  Task GetS3File(string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null, bool decompressGzipData = true, LogLevel? logLevel = null, CancellationToken cancellationToken = default);

  Task GetS3File(string bucketName, string? fileName, string filePath, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null, CancellationToken cancellationToken = default);

  Task<bool> DeleteS3File(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null, CancellationToken cancellationToken = default);

  Task<bool> S3FileExists(string bucketName, string fileName, string? versionId = null, LogLevel? logLevel = null, CancellationToken cancellationToken = default);

  Task<List<string>?> GetAllS3BucketFiles(string bucketName, int maxKeysPerQuery = 1000, LogLevel? logLevel = null, CancellationToken cancellationToken = default);

  Task<string?> GetS3Url(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null);

  Task<bool> IsBucketValid(string bucketName, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null);
}
