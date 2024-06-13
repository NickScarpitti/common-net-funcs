using System.Collections.Concurrent;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using RAPID_Data.ServerOps.Interfaces;
using static Common_Net_Funcs.Tools.DebugHelpers;
using static Common_Net_Funcs.Tools.ObjectHelpers;
using static Common_Net_Funcs.Tools.StringHelpers;

namespace RAPID_Data.ServerOps;

public class ApiAwsS3(IAmazonS3 s3Client, ILogger<ApiAwsS3> logger) : IAwsS3
{
    private readonly IAmazonS3 s3Client = s3Client;
    private readonly ILogger<ApiAwsS3> logger = logger;

    private readonly ConcurrentDictionary<string, bool> ValidatedBuckets = new();

    /// <summary>
    /// Upload a file to S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to upload file to</param>
    /// <param name="fileName">Name to save the file as in the S3 bucket</param>
    /// <param name="fileData">Stream containing the data for the file to be uploaded</param>
    /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status</param>
    /// <returns>True if file was successfully uploaded</returns>
    public async Task<bool> UploadS3File(string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        bool success = false;
        try
        {
            validatedBuckets ??= ValidatedBuckets;
            if (!fileName.IsNullOrWhiteSpace() && await IsBucketValid(bucketName, validatedBuckets))
            {
                if (await S3FileExists(bucketName, fileName))
                {
                    await DeleteS3File(bucketName, fileName);
                }

                PutObjectRequest request = new()
                {
                    BucketName = bucketName,
                    Key = fileName,
                    InputStream = fileData,
                    BucketKeyEnabled = true,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                PutObjectResponse? response = await s3Client.PutObjectAsync(request);
                success = response?.HttpStatusCode == HttpStatusCode.OK;

                if (!success)
                {
                    logger.LogInformation(response?.HttpStatusCode.ToString());
                }
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            logger.LogError(awsEx, $"{awsEx.GetLocationOfEexception()} AWS S3 Error");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{ex.GetLocationOfEexception()} Error");
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
    public async Task GetS3File(string bucketName, string? fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        try
        {
            validatedBuckets ??= ValidatedBuckets;
            if (!fileName.IsNullOrWhiteSpace() && await IsBucketValid(bucketName, validatedBuckets))
            {
                GetObjectRequest request = new()
                {
                    BucketName = bucketName,
                    Key = fileName
                };

                GetObjectResponse? response = await s3Client.GetObjectAsync(request);
                if (response != null)
                {
                    await using Stream responseStream = response.ResponseStream;
                    //responseStream.Position = 0;
                    await responseStream.CopyToAsync(fileData);
                    await fileData.FlushAsync();
                    fileData.Position = 0;
                }
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            if (awsEx.StatusCode != HttpStatusCode.NotFound)
            {
                logger.LogError(awsEx, $"{awsEx.GetLocationOfEexception()} AWS S3 Error");
            }
            else
            {
                logger.LogTrace(awsEx, $"Unable to get file {fileName} from {bucketName} bucket in {awsEx.GetLocationOfEexception()}");
                logger.LogWarning($"Unable to get file {fileName} from {bucketName} bucket in {awsEx.GetLocationOfEexception()}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Deletes a file from the specified S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to delete the file from</param>
    /// <param name="fileName">Name of the file to delete from the S3 bucket</param>
    /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status</param>
    /// <returns>True if file was deleted successfully</returns>
    public async Task<bool> DeleteS3File(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        bool success = false;
        try
        {
            validatedBuckets ??= ValidatedBuckets;
            if (!fileName.IsNullOrWhiteSpace() && await IsBucketValid(bucketName, validatedBuckets) && await S3FileExists(bucketName, fileName))
            {
                await s3Client.DeleteObjectAsync(bucketName, fileName);
                success = true;
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            if (awsEx.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning($"{fileName} not found for deletion");
            }
            else
            {
                logger.LogError(awsEx, $"{awsEx.GetLocationOfEexception()} AWS S3 Error");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{ex.GetLocationOfEexception()} Error");
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
    public async Task<bool> S3FileExists(string bucketName, string fileName, string? versionId = null)
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

                GetObjectMetadataResponse? response = await s3Client.GetObjectMetadataAsync(request);
                success = response?.HttpStatusCode == HttpStatusCode.OK;
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            if (awsEx.StatusCode != HttpStatusCode.NotFound)
            {
                logger.LogError(awsEx, $"{awsEx.GetLocationOfEexception()} AWS S3 Error");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return success;
    }

    /// <summary>
    /// Get a list containing the names of every file within an S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 bucket to get file list from</param>
    /// <param name="maxKeysPerQuery">Number of records to return per request</param>
    /// <returns>List containing the names of every file within the given S3 bucket</returns>
    public async Task<List<string>?> GetAllS3BucketFiles(string bucketName, int maxKeysPerQuery = 1000)
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
                    response = await s3Client.ListObjectsV2Async(request);
                    fileNames.AddRange(response?.S3Objects.ConvertAll(x => x.Key)?? []);
                    request.ContinuationToken = response?.NextContinuationToken;
                } while (response?.IsTruncated ?? false);
            }
        }
        catch (AmazonS3Exception awsEx)
        {
            if (awsEx.StatusCode != HttpStatusCode.NotFound)
            {
                logger.LogError(awsEx, $"{awsEx.GetLocationOfEexception()} AWS S3 Error");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{ex.GetLocationOfEexception()} Error");
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
            if (await IsBucketValid(bucketName, validatedBuckets))
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
                logger.LogError(awsEx, $"{awsEx.GetLocationOfEexception()} AWS S3 Error");
            }
            else
            {
                logger.LogWarning($"Unable to get URL for {fileName} from {bucketName}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{ex.GetLocationOfEexception()} Error");
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
                    isValid = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName);
                    validatedBuckets.AddDictionaryItem(bucketName, isValid);
                }
            }

            if (!isValid) //Retry in case of intermittent outage
            {
                isValid = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName);
                if (validatedBuckets != null)
                {
                    validatedBuckets[bucketName] = isValid;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return isValid;
    }
}
