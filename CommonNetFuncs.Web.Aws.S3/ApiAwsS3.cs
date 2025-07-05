using System.Collections.Concurrent;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using static Amazon.S3.Util.AmazonS3Util;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Web.Aws.S3;

public sealed class ApiAwsS3(IAmazonS3 s3Client, ILogger<ApiAwsS3> logger) : IAwsS3
{
    private readonly IAmazonS3 s3Client = s3Client;
    private readonly ILogger<ApiAwsS3> logger = logger;

    private static readonly ConcurrentDictionary<string, bool> ValidatedBuckets = new();

    /// <summary>
    /// Upload a file to S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to upload file to</param>
    /// <param name="fileName">Name to save the file as in the S3 bucket</param>
    /// <param name="fileData">Stream containing the data for the file to be uploaded</param>
    /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status</param>
    /// <param name="compressSteam">Optional: If true, will compress stream sent to S3 bucket. Default = true</param>
    /// <param name="compressionType">Optional: Specifies which compression type to use when compressSteam = true. Does nothing if compressSteam = false. Valid values are GZip and Deflate</param>
    /// <returns>True if file was successfully uploaded</returns>
    public async Task<bool> UploadS3File(string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null, bool compressSteam = true, ECompressionType compressionType = ECompressionType.Gzip, CancellationToken cancellationToken = default)
    {
        if (compressSteam && compressionType is not ECompressionType.Gzip and not ECompressionType.Deflate)
        {
            throw new NotSupportedException($"Compression type {compressionType} is not valid for this method.\n\tSupported types are:\n\t\t{ECompressionType.Gzip},\n\t\t{ECompressionType.Deflate}");
        }

        bool success = false;
        try
        {
            validatedBuckets ??= ValidatedBuckets;
            if (!fileName.IsNullOrWhiteSpace() && await IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false))
            {
                if (await S3FileExists(bucketName, fileName, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    await DeleteS3File(bucketName, fileName, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                await using MemoryStream compressedStream = new();
                ECompressionType currentCompression = await fileData.DetectCompressionType();
                switch (currentCompression)
                {
                    case ECompressionType.Brotli: case ECompressionType.ZLib: // Unsupported compression types for upload
                        MemoryStream? decompressedStream = new();
                        await fileData.DecompressStream(decompressedStream, currentCompression, cancellationToken: cancellationToken).ConfigureAwait(false);
                        await decompressedStream.CopyToAsync(fileData, cancellationToken).ConfigureAwait(false);
                        await decompressedStream.DisposeAsync().ConfigureAwait(false);
                        fileData.Position = 0;
                        break;
                }

                if (compressSteam)
                {
                    await fileData.CompressStream(compressedStream, compressionType, cancellationToken).ConfigureAwait(false);
                    compressedStream.Position = 0;
                }

                PutObjectRequest request = new()
                {
                    BucketName = bucketName,
                    Key = fileName,
                    InputStream = compressSteam && compressedStream.Length < fileData.Length ? compressedStream : fileData,
                    BucketKeyEnabled = true,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                // Only add content encoding header if the compressed stream is smaller than the original file data
                if (compressSteam && compressedStream.Length < fileData.Length)
                {
                    request.Headers["Content-Encoding"] = compressionType.ToString();
                }
                else if (currentCompression != ECompressionType.None)
                {
                    request.Headers["Content-Encoding"] = currentCompression.ToString().ToLowerInvariant();
                }

                request.Headers["Content-Length"] = request.InputStream.Length.ToString();

                PutObjectResponse? response = await s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
                success = response?.HttpStatusCode == HttpStatusCode.OK;

                if (!success)
                {
                    logger.LogInformation("{msg}", response?.HttpStatusCode.ToString());
                }
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            logger.LogError(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return success;
    }

    /// <summary>
    /// Retrieve a file from the specified S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to get the file from</param>
    /// <param name="fileName">Name of the file to retrieve from the S3 bucket</param>
    /// <param name="fileData">Stream to receive the file data retrieved from the S3 bucket</param>
    /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status</param>
    public async Task GetS3File(string bucketName, string? fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null, bool decompressGzipData = true, CancellationToken cancellationToken = default)
    {
        try
        {
            validatedBuckets ??= ValidatedBuckets;
            if (!fileName.IsNullOrWhiteSpace() && await IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false))
            {
                GetObjectRequest request = new()
                {
                    BucketName = bucketName,
                    Key = fileName
                };

                GetObjectResponse? response = await s3Client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);
                if (response != null)
                {
                    MemoryStream responseStream = new();
                    try
                    {
                        await response.ResponseStream.CopyToAsync(responseStream, cancellationToken);
                        responseStream.Position = 0;

                        if (decompressGzipData && response.Headers.ContentEncoding.ContainsInvariant(["gzip", "deflate"]))
                        {
                            //await using MemoryStream decompressedStream = new();
                            await using MemoryStream intermediateStream = new();
                            await responseStream.CopyToAsync(intermediateStream, cancellationToken).ConfigureAwait(false);
                            await intermediateStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                            intermediateStream.Position = 0;
                            try
                            {
                                await intermediateStream.DecompressStream(fileData, response.Headers.ContentEncoding.ContainsInvariant("gzip") ? ECompressionType.Gzip : ECompressionType.Deflate, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    logger.LogWarning(ex, "{msg}", $"Failed to decompress {fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} from {bucketName.UrlEncodeReadable(cancellationToken: cancellationToken)} bucket. Attempting to copy raw data instead.");
                                    intermediateStream.Position = 0;
                                    await intermediateStream.CopyToAsync(fileData, cancellationToken).ConfigureAwait(false);

                                    // Re-upload the raw data to without Content-Encoding header if decompression fails
                                    await UploadS3File(bucketName, fileName, intermediateStream, validatedBuckets, compressSteam: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                                }
                                catch (Exception ex2)
                                {
                                    logger.LogError(ex2, "{msg}", $"Failed to copy raw data from {fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} in {bucketName.UrlEncodeReadable(cancellationToken: cancellationToken)} bucket after decompression failure. Abandoning request.");
                                }
                            }
                        }
                        else
                        {
                            ECompressionType currentCompression = await responseStream.DetectCompressionType();
                            if (currentCompression != ECompressionType.None)
                            {
                                await using MemoryStream intermediateStream = new();
                                await responseStream.CopyToAsync(intermediateStream, cancellationToken).ConfigureAwait(false);
                                await intermediateStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                                intermediateStream.Position = 0;
                                try
                                {
                                    await intermediateStream.DecompressStream(fileData, currentCompression, cancellationToken: cancellationToken).ConfigureAwait(false);
                                }
                                catch
                                {
                                    logger.LogWarning("{msg}", $"Failed to decompress {fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} from {bucketName.UrlEncodeReadable(cancellationToken: cancellationToken)} bucket. Attempting to copy raw data instead.");
                                    intermediateStream.Position = 0;
                                    await intermediateStream.CopyToAsync(fileData, cancellationToken).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                await responseStream.CopyToAsync(fileData, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        await fileData.FlushAsync(cancellationToken).ConfigureAwait(false);
                        fileData.Position = 0;
                    }
                    finally
                    {
                        await responseStream.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            if (awsEx.StatusCode != HttpStatusCode.NotFound)
            {
                logger.LogError(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
            }
            else
            {
                logger.LogTrace(awsEx, "{msg}", $"Unable to get file {fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} from {bucketName.UrlEncodeReadable(cancellationToken: cancellationToken)} bucket in {awsEx.GetLocationOfException()}");
                logger.LogWarning("{msg}", $"Unable to get file {fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} from {bucketName.UrlEncodeReadable(cancellationToken: cancellationToken)} bucket in {awsEx.GetLocationOfException()}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Deletes a file from the specified S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to delete the file from</param>
    /// <param name="fileName">Name of the file to delete from the S3 bucket</param>
    /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status</param>
    /// <returns>True if file was deleted successfully</returns>
    public async Task<bool> DeleteS3File(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null, CancellationToken cancellationToken = default)
    {
        bool success = false;
        try
        {
            validatedBuckets ??= ValidatedBuckets;
            if (!fileName.IsNullOrWhiteSpace() && await IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false) && await S3FileExists(bucketName, fileName, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                await s3Client.DeleteObjectAsync(bucketName, fileName, cancellationToken).ConfigureAwait(false);
                success = true;
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            if (awsEx.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("{msg}", $"{fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} not found for deletion");
            }
            else
            {
                logger.LogError(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return success;
    }

    /// <summary>
    /// Check to see if a file exists within the given S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to check for the file</param>
    /// <param name="fileName">Name of the file to look for in the S3 bucket</param>
    /// <param name="versionId">Optional: Version ID for the file being searched for</param>
    /// <returns>True if the file exists within the given S3 bucket</returns>
    public async Task<bool> S3FileExists(string bucketName, string fileName, string? versionId = null, CancellationToken cancellationToken = default)
    {
        bool success = false;
        try
        {
            if (!fileName.IsNullOrWhiteSpace())
            {
                GetObjectMetadataRequest request = new()
                {
                    BucketName = bucketName,
                    Key = fileName,
                    VersionId = !versionId.IsNullOrEmpty() ? versionId : null
                };

                GetObjectMetadataResponse? response = await s3Client.GetObjectMetadataAsync(request, cancellationToken).ConfigureAwait(false);
                success = response?.HttpStatusCode == HttpStatusCode.OK;
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            if (awsEx.StatusCode != HttpStatusCode.NotFound)
            {
                logger.LogError(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return success;
    }

    /// <summary>
    /// Get a list containing the names of every file within an S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to get file list from</param>
    /// <param name="maxKeysPerQuery">Number of records to return per request</param>
    /// <returns>List containing the names of every file within the given S3 bucket</returns>
    public async Task<List<string>?> GetAllS3BucketFiles(string bucketName, int maxKeysPerQuery = 1000, CancellationToken cancellationToken = default)
    {
        List<string> fileNames = [];
        try
        {
            if (!bucketName.IsNullOrWhiteSpace())
            {
                ListObjectsV2Response? response = new();
                ListObjectsV2Request request = new()
                {
                    BucketName = bucketName,
                    MaxKeys = maxKeysPerQuery
                };

                do
                {
                    response = await s3Client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);
                    fileNames.AddRange(response?.S3Objects.ConvertAll(x => x.Key) ?? []);
                    request.ContinuationToken = response?.NextContinuationToken;
                } while (response?.IsTruncated ?? false);
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            if (awsEx.StatusCode != HttpStatusCode.NotFound)
            {
                logger.LogError(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return fileNames;
    }

    /// <summary>
    /// Get the URL corresponding to a single file within an S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to get the file URL from</param>
    /// <param name="fileName">Name of the file to retrieve the URL for</param>
    /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status</param>
    /// <returns>String containing the URL for the specified file</returns>
    public async Task<string?> GetS3Url(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        string? url = null;
        try
        {
            validatedBuckets ??= ValidatedBuckets;
            if (await IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false))
            {
                GetPreSignedUrlRequest request = new()
                {
                    BucketName = bucketName,
                    Key = fileName,
                    Expires = DateTime.UtcNow + TimeSpan.FromMinutes(1),
                };

                url = s3Client.GetPreSignedURL(request);
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            if (awsEx.StatusCode != HttpStatusCode.NotFound)
            {
                logger.LogError(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
            }
            else
            {
                logger.LogWarning("{msg}", $"Unable to get URL for {fileName.UrlEncodeReadable()} from {bucketName.UrlEncodeReadable()}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return url;
    }

    /// <summary>
    /// Checks whether an S3 bucket exists and is reachable or not
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to validate exists and is reachable</param>
    /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status</param>
    /// <returns>True if the S# bucket exists and is reachable</returns>
    public async Task<bool> IsBucketValid(string bucketName, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        bool isValid = false;
        try
        {
            validatedBuckets ??= ValidatedBuckets;
            if (validatedBuckets != null)
            {
                if (validatedBuckets.Any(x => x.Key.StrEq(bucketName)))
                {
                    isValid = validatedBuckets.Where(x => x.Key.StrEq(bucketName)).Select(x => x.Value).First();
                }
                else
                {
                    isValid = await DoesS3BucketExistV2Async(s3Client, bucketName).ConfigureAwait(false);
                    validatedBuckets.TryAdd(bucketName, isValid);
                }
            }

            if (!isValid) //Retry in case of intermittent outage
            {
                isValid = await DoesS3BucketExistV2Async(s3Client, bucketName).ConfigureAwait(false);
                if (validatedBuckets != null)
                {
                    validatedBuckets[bucketName] = isValid;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return isValid;
    }
}
