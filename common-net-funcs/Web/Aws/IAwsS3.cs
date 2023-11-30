using System.Collections.Concurrent;

namespace RAPID_Data.ServerOps.Interfaces;
public interface IAwsS3
{
    Task<bool> UploadS3File(string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null);
    Task GetS3File(string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null);
    Task<bool> DeleteS3File(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null);
    Task<bool> S3FileExists(string bucketName, string fileName, string? versionId = null);
    Task<List<string>?> GetAllS3BucketFiles(string bucketName, int maxKeysPerQuery = 1000);
    Task<string?> GetS3Url(string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null);
    Task<bool> IsBucketValid(string bucketName, ConcurrentDictionary<string, bool>? validatedBuckets = null);
}
