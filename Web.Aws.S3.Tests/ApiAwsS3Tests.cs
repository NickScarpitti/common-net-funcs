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
}
