using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using RAPID_Data.ServerOps.Interfaces;
using Amazon.S3.Util;
using System.Collections.Concurrent;
using static Common_Net_Funcs.Tools.DataValidation;
using static Common_Net_Funcs.Tools.DebugHelpers;

namespace RAPID_Data.ServerOps;
public class ApiAwsS3(IAmazonS3 s3Client, ILogger<ApiAwsS3> logger) : IAwsS3
{
    private readonly IAmazonS3 s3Client = s3Client;
    private readonly ILogger<ApiAwsS3> logger = logger;

    public async Task<bool> UploadS3File(string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        bool success = false;
        try
        {
            if (!string.IsNullOrWhiteSpace(fileName) && await IsBucketValid(bucketName, validatedBuckets))
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

    public async Task GetS3File(string bucketName, string? fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(fileName) && await IsBucketValid(bucketName, validatedBuckets))
            {
                GetObjectRequest request = new()
                {
                    BucketName = bucketName,
                    Key = fileName
                };

                GetObjectResponse? response = await s3Client.GetObjectAsync(request);
                if (response != null)
                {
                    using Stream responseStream = response.ResponseStream;
                    //responseStream.Seek(0, SeekOrigin.Begin);
                    await responseStream.CopyToAsync(fileData);
                    await fileData.FlushAsync();
                    fileData.Seek(0, SeekOrigin.Begin);
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
                logger.LogWarning(awsEx, $"Unable to get file {fileName} from {bucketName} bucket Error");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }

    public async Task DeleteS3File(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(fileName) && await IsBucketValid(bucketName, validatedBuckets) && await S3FileExists(bucketName, fileName))
            {
                await s3Client.DeleteObjectAsync(bucketName, fileName);
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
    }

    public async Task<bool> S3FileExists(string bucketName, string fileName, string? versionId = null)
    {
        bool success = false;
        try
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                GetObjectMetadataRequest request = new()
                {
                    BucketName = bucketName,
                    Key = fileName,
                    VersionId = !string.IsNullOrEmpty(versionId) ? versionId : null
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

    public async Task<List<string>?> GetAllS3BucketFiles(string bucketName, int maxKeysPerQuery = 1000)
    {
        List<string> fileNames = new();
        try
        {
            if (!string.IsNullOrWhiteSpace(bucketName))
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
                    fileNames.AddRange(response?.S3Objects.ConvertAll(x => x.Key)?? new());
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

    public async Task<string?> GetS3Url(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        string? url = null;
        try
        {
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

    public async Task<bool> IsBucketValid(string bucketName, ConcurrentDictionary<string, bool>? validatedBuckets = null)
    {
        bool isValid = false;
        try
        {
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
