using System.Collections.Concurrent;
using Amazon.S3;
using Amazon.S3.Model;
using NLog;
using static CommonNetFuncs.Compression.Streams;

namespace CommonNetFuncs.Web.Aws.S3;

public sealed class ApiAwsS3(IAmazonS3 s3Client) : IAwsS3
{
  private readonly IAmazonS3 s3Client = s3Client;

  public bool EnableInfoLogging { get; set; } = true;

  internal const long MultipartThreshold = 10 * 1024 * 1024; // 10MB

  /// <summary>
  /// Upload a file to S3 bucket.
  /// </summary>
  /// <param name="bucketName">Name of the S3 bucket to upload file to.</param>
  /// <param name="fileName">Name to save the file as in the S3 bucket.</param>
  /// <param name="fileData">Stream containing the data for the file to be uploaded.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <param name="compressSteam">Optional: If <see langword="true"/>, will compress stream sent to S3 bucket. Default is <see langword="true"/></param>
  /// <param name="compressionType">Optional: Specifies which compression type to use when compressSteam = <see langword="true"/>. Does nothing if compressSteam = <see langword="false"/>.  Valid values are GZip and Deflate</param>
  /// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
  /// <returns><see langword="true"/> if file was successfully uploaded.</returns>
  public Task<bool> UploadS3File(string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null,
        long thresholdForMultiPartUpload = MultipartThreshold, bool compressSteam = true, ECompressionType compressionType = ECompressionType.Gzip, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    return s3Client.UploadS3File(bucketName, fileName, fileData, validatedBuckets, thresholdForMultiPartUpload, compressSteam, compressionType, logLevel, cancellationToken);
  }

  /// <summary>
  /// Upload a file to S3 bucket.
  /// </summary>
  /// <param name="bucketName">Name of the S3 bucket to upload file to.</param>
  /// <param name="fileName">Name to save the file as in the S3 bucket.</param>
  /// <param name="filePath">Path of the file to be uploaded.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
  /// <returns><see langword="true"/> if file was successfully uploaded.</returns>
  public Task<bool> UploadS3File(string bucketName, string fileName, string filePath, ConcurrentDictionary<string, bool>? validatedBuckets = null,
        long thresholdForMultiPartUpload = MultipartThreshold, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    return s3Client.UploadS3File(bucketName, fileName, filePath, validatedBuckets, thresholdForMultiPartUpload, logLevel, cancellationToken);
  }

  /// <summary>
  /// Uploads a file to S3 using multipart upload (typically for large files only).
  /// </summary>
  /// <param name="bucketName">The name of the S3 bucket.</param>
  /// <param name="fileName">The name of the file to upload.</param>
  /// <param name="stream">The stream containing the file data.</param>
  /// <param name="cancellationToken">The cancellation token for this operation.</param>
  /// <returns><see langword="true"/> if the upload was successful.</returns>
  /// <exception cref="InvalidOperationException">Thrown when the upload fails.</exception>
  public Task<bool> UploadMultipartAsync(string bucketName, string fileName, Stream stream, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    return s3Client.UploadMultipartAsync(bucketName, fileName, stream, logLevel, cancellationToken);
  }

  internal Task<PartETag?> UploadPartAsync(string bucketName, string fileName, string uploadId, Stream sourceStream, int partNumber, long chunkSize, long totalSize, SemaphoreSlim semaphore, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    return s3Client.UploadPartAsync(bucketName, fileName, uploadId, sourceStream, partNumber, chunkSize, totalSize, semaphore, logLevel, cancellationToken);
  }

  /// <summary>
  /// Retrieve a file from the specified S3 bucket.
  /// </summary>
  /// <param name="bucketName">Name of the S3 bucket to get the file from.</param>
  /// <param name="fileName">Name of the file to retrieve from the S3 bucket.</param>
  /// <param name="fileData">Stream to receive the file data retrieved from the S3 bucket.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  public Task GetS3File(string bucketName, string? fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null, bool decompressGzipData = true, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    return s3Client.GetS3File(bucketName, fileName, fileData, validatedBuckets, decompressGzipData, logLevel, cancellationToken);
  }

  /// <summary>
  /// Retrieve a file from the specified S3 bucket.
  /// </summary>
  /// <param name="bucketName">Name of the S3 bucket to get the file from.</param>
  /// <param name="fileName">Name of the file to retrieve from the S3 bucket.</param>
  /// <param name="filePath">File path to put the downloaded file from the S3 bucket.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  public Task GetS3File(string bucketName, string? fileName, string filePath, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    return s3Client.GetS3File(bucketName, fileName, filePath, validatedBuckets, logLevel, cancellationToken);
  }

  /// <summary>
  /// Deletes a file from the specified S3 bucket.
  /// </summary>
  /// <param name="bucketName">Name of the S3 bucket to delete the file from.</param>
  /// <param name="fileName">Name of the file to delete from the S3 bucket.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <returns><see langword="true"/> if file was deleted successfully.</returns>
  public Task<bool> DeleteS3File(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    return s3Client.DeleteS3File(bucketName, fileName, validatedBuckets, logLevel, cancellationToken);
  }

  /// <summary>
  /// Check to see if a file exists within the given S3 bucket.
  /// </summary>
  /// <param name="bucketName">Name of the S3 bucket to check for the file.</param>
  /// <param name="fileName">Name of the file to look for in the S3 bucket.</param>
  /// <param name="versionId">Optional: Version ID for the file being searched for.</param>
  /// <returns><see langword="true"/> if the file exists within the given S3 bucket.</returns>
  public Task<bool> S3FileExists(string bucketName, string fileName, string? versionId = null, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    return s3Client.S3FileExists(bucketName, fileName, versionId, logLevel, cancellationToken);
  }

  /// <summary>
  /// Get a list containing the names of every file within an S3 bucket.
  /// </summary>
  /// <param name="bucketName">Name of the S3 bucket to get file list from.</param>
  /// <param name="maxKeysPerQuery">Number of records to return per request.</param>
  /// <returns><see cref="List{T}"/> containing the names of every file within the given S3 bucket.</returns>
  public Task<List<string>?> GetAllS3BucketFiles(string bucketName, int maxKeysPerQuery = 1000, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    return s3Client.GetAllS3BucketFiles(bucketName, maxKeysPerQuery, logLevel, cancellationToken);
  }

  /// <summary>
  /// Get the URL corresponding to a single file within an S3 bucket.
  /// </summary>
  /// <param name="bucketName">Name of the S3 bucket to get the file URL from.</param>
  /// <param name="fileName">Name of the file to retrieve the URL for.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <returns>String containing the URL for the specified file.</returns>
  public Task<string?> GetS3Url(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null)
  {
    return s3Client.GetS3Url(bucketName, fileName, validatedBuckets, logLevel);
  }

  /// <summary>
  /// Checks whether an S3 bucket exists and is reachable or not.
  /// </summary>
  /// <param name="bucketName">Name of the S3 bucket to validate exists and is reachable.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <returns><see langword="true"/> if the S3 bucket exists and is reachable.</returns>
  public Task<bool> IsBucketValid(string bucketName, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null)
  {
    return s3Client.IsBucketValid(bucketName, validatedBuckets, logLevel);
  }
}
