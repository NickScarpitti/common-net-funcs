using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Web.Aws.S3;
using FakeItEasy;

using Microsoft.Extensions.Logging;
using static CommonNetFuncs.Compression.Streams;

namespace Web.Aws.S3.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly

public sealed class ApiAwsS3Tests
{
    private readonly IFixture _fixture;
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<ApiAwsS3> _logger;
    private readonly ApiAwsS3 _sut;

    public ApiAwsS3Tests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

        _s3Client = A.Fake<IAmazonS3>();
        _logger = A.Fake<ILogger<ApiAwsS3>>();
        _sut = new ApiAwsS3(_s3Client, _logger);
    }

    [Theory]
    [InlineData(true, ECompressionType.Gzip)]
    [InlineData(true, ECompressionType.Deflate)]
    [InlineData(false, ECompressionType.Gzip)]
    public async Task UploadS3File_WhenValidInputs_UploadsSuccessfully(bool compressStream, ECompressionType compressionType)
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        byte[] fileContent = _fixture.CreateMany<byte>(1000).ToArray();
        await using MemoryStream fileData = new(fileContent);

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });

        PutObjectResponse response = new() { HttpStatusCode = HttpStatusCode.OK };
        A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).Returns(response);

        // Act
        bool result = await _sut.UploadS3File(bucketName, fileName, fileData, null, compressStream, compressionType);

        // Assert
        result.ShouldBeTrue();
        A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(ECompressionType.Brotli)]
    [InlineData(ECompressionType.ZLib)]
    public async Task UploadS3File_WhenInvalidCompressionType_ThrowsNotSupportedException(ECompressionType compressionType)
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        await using MemoryStream fileData = new();

        // Act & Assert
        await Should.ThrowAsync<NotSupportedException>(async () => await _sut.UploadS3File(bucketName, fileName, fileData, compressionType: compressionType));
    }

    [Theory]
    [InlineData(true, "gzip")]
    [InlineData(true, "deflate")]
    [InlineData(false, "gzip")]
    [InlineData(true, "")]
    public async Task GetS3File_WhenFileExists_RetrievesSuccessfully(bool decompressGzipData, string contentEncoding)
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        byte[] fileContent = Encoding.UTF8.GetBytes("Test content");
        await using MemoryStream fileData = new();

        GetObjectResponse response = new()
        {
            ResponseStream = new MemoryStream(fileContent),
            Headers = { ["Content-Encoding"] = contentEncoding }
        };

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });
        A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._)).Returns(response);

        // Act
        await _sut.GetS3File(bucketName, fileName, fileData, decompressGzipData: decompressGzipData);

        // Assert
        fileData.Length.ShouldBeGreaterThan(0);
        A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteS3File_WhenFileExists_DeletesSuccessfully(bool fileExists)
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });

        if (fileExists)
        {
            A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._)).Returns(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });
        }

        // Act
        bool result = await _sut.DeleteS3File(bucketName, fileName);

        // Assert
        result.ShouldBe(fileExists);
        if (fileExists)
        {
            A.CallTo(() => _s3Client.DeleteObjectAsync(bucketName, fileName, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public async Task S3FileExists_ReturnsExpectedResult(HttpStatusCode statusCode, bool expectedResult)
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();

        GetObjectMetadataResponse response = new() { HttpStatusCode = statusCode };
        A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._)).Returns(response);

        // Act
        bool result = await _sut.S3FileExists(bucketName, fileName);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Fact]
    public async Task GetAllS3BucketFiles_WhenBucketExists_ReturnsAllFiles()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        List<S3Object> s3Objects = _fixture.CreateMany<S3Object>(3).ToList();

        ListObjectsV2Response response = new()
            {
                S3Objects = s3Objects,
                IsTruncated = false
            };

        A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._)).Returns(response);

        // Act
        List<string>? result = await _sut.GetAllS3BucketFiles(bucketName);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(s3Objects.Count);
        result.ShouldBe(s3Objects.Select(x => x.Key));
    }

    [Fact]
    public async Task GetS3Url_WhenBucketValid_ReturnsPreSignedUrl()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        const string expectedUrl = "https://test-bucket.s3.amazonaws.com/test-file";

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });
        A.CallTo(() => _s3Client.GetPreSignedURL(A<GetPreSignedUrlRequest>._)).Returns(expectedUrl);

        // Act
        string? result = await _sut.GetS3Url(bucketName, fileName);

        // Assert
        result.ShouldBe(expectedUrl);
    }

    //[Theory]
    //[InlineData(HttpStatusCode.OK, true)]
    //[InlineData(HttpStatusCode.NotFound, false)]
    //public async Task IsBucketValid_ReturnsExpectedResult(HttpStatusCode statusCode, bool expectedResult)
    //{
    //    // Arrange
    //    string bucketName = _fixture.Create<string>();

    //    //A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = statusCode });
    //    A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = statusCode });

    //    // Act
    //    bool result = await _sut.IsBucketValid(bucketName);

    //    // Assert
    //    result.ShouldBe(expectedResult);
    //}

    [Fact]
    public async Task UploadS3File_FilePath_Success()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        string filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, "test content");

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });
        A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
            .Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        bool result = await _sut.UploadS3File(bucketName, fileName, filePath);

        // Assert
        result.ShouldBeTrue();
        A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        File.Delete(filePath);
    }

    [Fact]
    public async Task UploadS3File_FilePath_ThrowsOnInvalidFileName()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string filePath = Path.GetTempFileName();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () => await _sut.UploadS3File(bucketName, "   ", filePath));

        File.Delete(filePath);
    }

    [Fact]
    public async Task UploadS3File_FilePath_ThrowsOnFileNotFound()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");

        // Act & Assert
        await Should.ThrowAsync<FileNotFoundException>(async () => await _sut.UploadS3File(bucketName, fileName, filePath));
    }

    [Fact]
    public async Task UploadS3File_FilePath_HandlesAmazonS3Exception()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        string filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, "test content");

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .Throws(new AmazonS3Exception("AWS error"));

        // Act
        bool result = await _sut.UploadS3File(bucketName, fileName, filePath);

        // Assert
        result.ShouldBeFalse();

        File.Delete(filePath);
    }

    [Fact]
    public async Task GetS3File_FilePath_Success()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        string filePath = Path.GetTempFileName();
        byte[] fileContent = Encoding.UTF8.GetBytes("Test content");

        GetObjectResponse response = new()
        {
            ResponseStream = new MemoryStream(fileContent),
            Headers = { ["Content-Encoding"] = string.Empty
            }
        };

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });
        A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
            .Returns(response);

        // Act
        await _sut.GetS3File(bucketName, fileName, filePath);

        // Assert
        File.Exists(filePath).ShouldBeTrue();
        File.ReadAllText(filePath).ShouldBe("Test content");

        File.Delete(filePath);
    }

    [Fact]
    public async Task GetS3File_FilePath_HandlesAmazonS3Exception()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        string filePath = Path.GetTempFileName();

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .Throws(new AmazonS3Exception("AWS error"));

        // Act
        await _sut.GetS3File(bucketName, fileName, filePath);

        // Assert
        // No exception thrown, file should be empty
        File.Exists(filePath).ShouldBeTrue();

        File.Delete(filePath);
    }

    [Fact]
    public async Task GetS3File_FilePath_HandlesGeneralException()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();
        string filePath = Path.GetTempFileName();

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .Throws(new Exception("General error"));

        // Act
        await _sut.GetS3File(bucketName, fileName, filePath);

        // Assert
        File.Exists(filePath).ShouldBeTrue();

        File.Delete(filePath);
    }

    [Fact]
    public async Task GetS3Url_WhenBucketInvalid_ReturnsNull()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.NotFound });

        // Act
        string? result = await _sut.GetS3Url(bucketName, fileName);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetS3Url_HandlesAmazonS3Exception()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .Throws(new AmazonS3Exception("AWS error"));

        // Act
        string? result = await _sut.GetS3Url(bucketName, fileName);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetS3Url_HandlesGeneralException()
    {
        // Arrange
        string bucketName = _fixture.Create<string>();
        string fileName = _fixture.Create<string>();

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .Throws(new Exception("General error"));

        // Act
        string? result = await _sut.GetS3Url(bucketName, fileName);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task IsBucketValid_CacheHit_ReturnsCachedValue()
    {
        // Arrange
        string bucketName = Guid.NewGuid().ToString();
        ConcurrentDictionary<string, bool> validatedBuckets = new();
        validatedBuckets[bucketName] = true;

        // Act
        bool result = await _sut.IsBucketValid(bucketName, validatedBuckets);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsBucketValid_CacheMiss_ValidatesAndCaches()
    {
        // Arrange
        string bucketName = Guid.NewGuid().ToString();
        ConcurrentDictionary<string, bool> validatedBuckets = new();

        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        bool result = await _sut.IsBucketValid(bucketName, validatedBuckets);

        // Assert
        result.ShouldBeTrue();
        validatedBuckets[bucketName].ShouldBeTrue();
    }

    [Fact]
    public async Task IsBucketValid_RetryOnInvalid()
    {
        // Arrange
        string bucketName = Guid.NewGuid().ToString();
        ConcurrentDictionary<string, bool> validatedBuckets = new();

        // First call returns false, second returns true
        int callCount = 0;
        A.CallTo(() => _s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
            .ReturnsLazily(() =>
            {
                callCount++;
                return new GetBucketLocationResponse { HttpStatusCode = callCount == 1 ? HttpStatusCode.NotFound : HttpStatusCode.OK };
            });

        // Act
        bool result = await _sut.IsBucketValid(bucketName, validatedBuckets);

        // Assert
        result.ShouldBeTrue();
        validatedBuckets[bucketName].ShouldBeTrue();
    }

    [Fact]
    public async Task UploadMultipartAsync_SuccessfulUpload_ReturnsTrue()
    {
        // Arrange
        const string bucketName = "bucket";
        const string fileName = "file";
        const string uploadId = "upload-id";
        byte[] data = new byte[25 * 1024 * 1024]; // 25MB, triggers multipart
        await using MemoryStream stream = new(data);

        InitiateMultipartUploadResponse initiateResponse = new() { UploadId = uploadId };
        CompleteMultipartUploadResponse completeResponse = new() { HttpStatusCode = HttpStatusCode.OK };

        A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
            .Returns(initiateResponse);

        // Simulate 3 parts
        A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
            .ReturnsLazily((UploadPartRequest req, CancellationToken _) => new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = $"etag-{req.PartNumber}" });

        A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
            .Returns(completeResponse);

        // Act
        bool result = await _sut.UploadMultipartAsync(bucketName, fileName, stream, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._)).MustHaveHappened(3, Times.Exactly);
        A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UploadMultipartAsync_PartUploadFails_ThrowsAndAborts()
    {
        // Arrange
        const string bucketName = "bucket";
        const string fileName = "file";
        const string uploadId = "upload-id";
        byte[] data = new byte[15 * 1024 * 1024]; // 15MB, triggers multipart
        await using MemoryStream stream = new(data);

        InitiateMultipartUploadResponse initiateResponse = new() { UploadId = uploadId };

        A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
            .Returns(initiateResponse);

        // Simulate one part fails (returns null)
        A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
            .ReturnsNextFromSequence(
                new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "etag-1" },
                new UploadPartResponse { HttpStatusCode = HttpStatusCode.BadRequest });

        A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
            .Returns(new AbortMultipartUploadResponse());

        // Act
        bool result = await _sut.UploadMultipartAsync(bucketName, fileName, stream, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UploadMultipartAsync_ExceptionDuringComplete_AbortsAndReturnsFalse()
    {
        // Arrange
        const string bucketName = "bucket";
        const string fileName = "file";
        const string uploadId = "upload-id";
        byte[] data = new byte[15 * 1024 * 1024];
        await using MemoryStream stream = new(data);

        InitiateMultipartUploadResponse initiateResponse = new() { UploadId = uploadId };

        A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
            .Returns(initiateResponse);

        A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
            .Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "etag-1" });

        A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
            .Throws(new Exception("fail"));

        A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
            .Returns(new AbortMultipartUploadResponse());

        // Act
        bool result = await _sut.UploadMultipartAsync(bucketName, fileName, stream, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UploadPartAsync_Success_ReturnsPartETag()
    {
        // Arrange
        const string bucketName = "bucket";
        const string fileName = "file";
        const string uploadId = "upload-id";
        const int partNumber = 1;
        const long chunkSize = 5 * 1024 * 1024;
        const long totalSize = chunkSize;
        SemaphoreSlim semaphore = new(1, 1);
        byte[] buffer = new byte[chunkSize];
        await using MemoryStream stream = new(buffer);

        UploadPartResponse uploadPartResponse = new() { HttpStatusCode = HttpStatusCode.OK, ETag = "etag-1" };
        A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
            .Returns(uploadPartResponse);

        // Act
        PartETag? result = await _sut.UploadPartAsync(bucketName, fileName, uploadId, stream, partNumber, chunkSize, totalSize, semaphore, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.PartNumber.ShouldBe(partNumber);
        result.ETag.ShouldBe("etag-1");
    }

    [Fact]
    public async Task UploadPartAsync_PartUploadFails_ReturnsNull()
    {
        // Arrange
        const string bucketName = "bucket";
        const string fileName = "file";
        const string uploadId = "upload-id";
        const int partNumber = 1;
        const long chunkSize = 5 * 1024 * 1024;
        const long totalSize = chunkSize;
        SemaphoreSlim semaphore = new(1, 1);
        byte[] buffer = new byte[chunkSize];
        await using MemoryStream stream = new(buffer);

        UploadPartResponse uploadPartResponse = new() { HttpStatusCode = HttpStatusCode.BadRequest };
        A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
            .Returns(uploadPartResponse);

        // Act
        PartETag? result = await _sut.UploadPartAsync(bucketName, fileName, uploadId, stream, partNumber, chunkSize, totalSize, semaphore, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task UploadPartAsync_ReadsLessThanExpected_ThrowsAndReturnsNull()
    {
        // Arrange
        const string bucketName = "bucket";
        const string fileName = "file";
        const string uploadId = "upload-id";
        const int partNumber = 1;
        const long chunkSize = 5 * 1024 * 1024;
        const long totalSize = chunkSize;
        SemaphoreSlim semaphore = new(1, 1);
        // Stream with less data than chunkSize
        await using MemoryStream stream = new(new byte[1024]);

        // Act
        PartETag? result = await _sut.UploadPartAsync(bucketName, fileName, uploadId, stream, partNumber, chunkSize, totalSize, semaphore, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task UploadPartAsync_ExceptionDuringUpload_ReturnsNull()
    {
        // Arrange
        const string bucketName = "bucket";
        const string fileName = "file";
        const string uploadId = "upload-id";
        const int partNumber = 1;
        const long chunkSize = 5 * 1024 * 1024;
        const long totalSize = chunkSize;
        SemaphoreSlim semaphore = new(1, 1);
        byte[] buffer = new byte[chunkSize];
        await using MemoryStream stream = new(buffer);

        A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
            .Throws(new Exception("fail"));

        // Act
        PartETag? result = await _sut.UploadPartAsync(bucketName, fileName, uploadId, stream, partNumber, chunkSize, totalSize, semaphore, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}

#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
