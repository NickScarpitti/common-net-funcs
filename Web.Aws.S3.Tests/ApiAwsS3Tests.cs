using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Compression;
using CommonNetFuncs.Core;
using CommonNetFuncs.Web.Aws.S3;
using FakeItEasy;
using static CommonNetFuncs.Compression.Streams;

namespace Web.Aws.S3.Tests;

public sealed class ApiAwsS3Tests
{
	private readonly IFixture fixture;
	private readonly IAmazonS3 s3Client;
	private readonly ApiAwsS3 sut;

	public ApiAwsS3Tests()
	{
		fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

		s3Client = A.Fake<IAmazonS3>();
		sut = new ApiAwsS3(s3Client);
	}

	[Theory]
	[InlineData(true, ECompressionType.Gzip)]
	[InlineData(true, ECompressionType.Deflate)]
	[InlineData(false, ECompressionType.Gzip)]
	public async Task UploadS3File_WhenValidInputs_UploadsSuccessfully(bool compressStream, ECompressionType compressionType)
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		byte[] fileContent = fixture.CreateMany<byte>(1000).ToArray();
		await using MemoryStream fileData = new(fileContent);

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });

		PutObjectResponse response = new() { HttpStatusCode = HttpStatusCode.OK };
		A.CallTo(() => s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).Returns(response);

		// Act
		bool result = await sut.UploadS3File(bucketName, fileName, fileData, null, compressSteam: compressStream, compressionType: compressionType, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		A.CallTo(() => s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.ZLib)]
	public async Task UploadS3File_WhenInvalidCompressionType_ThrowsNotSupportedException(ECompressionType compressionType)
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		await using MemoryStream fileData = new();

		// Act & Assert
		await Should.ThrowAsync<NotSupportedException>(async () => await sut.UploadS3File(bucketName, fileName, fileData, compressionType: compressionType));
	}

	[Theory]
	[InlineData(true, "gzip")]
	[InlineData(true, "deflate")]
	[InlineData(false, "gzip")]
	[InlineData(true, "")]
	public async Task GetS3File_WhenFileExists_RetrievesSuccessfully(bool decompressGzipData, string contentEncoding)
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		byte[] fileContent = Encoding.UTF8.GetBytes("Test content");
		await using MemoryStream fileData = new();

		byte[] compressedContent = fileContent;
		if (!contentEncoding.IsNullOrWhiteSpace())
		{
			compressedContent = await fileContent.Compress(contentEncoding == "gzip" ? ECompressionType.Gzip : ECompressionType.Deflate, cancellationToken: TestContext.Current.CancellationToken);
		}
		GetObjectResponse response = new()
		{
			ResponseStream = new MemoryStream(compressedContent),
			Headers = { ["Content-Encoding"] = contentEncoding }
		};

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });
		A.CallTo(() => s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._)).Returns(response);

		// Act
		await sut.GetS3File(bucketName, fileName, fileData, decompressGzipData: decompressGzipData, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		fileData.Length.ShouldBeGreaterThan(0);
		A.CallTo(() => s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task DeleteS3File_WhenFileExists_DeletesSuccessfully(bool fileExists)
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });

		if (fileExists)
		{
			A.CallTo(() => s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._)).Returns(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });
		}

		// Act
		bool result = await sut.DeleteS3File(bucketName, fileName, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(fileExists);
		if (fileExists)
		{
			A.CallTo(() => s3Client.DeleteObjectAsync(bucketName, fileName, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
		}
	}

	[Theory]
	[InlineData(HttpStatusCode.OK, true)]
	[InlineData(HttpStatusCode.NotFound, false)]
	public async Task S3FileExists_ReturnsExpectedResult(HttpStatusCode statusCode, bool expectedResult)
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();

		GetObjectMetadataResponse response = new() { HttpStatusCode = statusCode };
		A.CallTo(() => s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._)).Returns(response);

		// Act
		bool result = await sut.S3FileExists(bucketName, fileName, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(expectedResult);
	}

	[Fact]
	public async Task GetAllS3BucketFiles_WhenBucketExists_ReturnsAllFiles()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		List<S3Object> s3Objects = fixture.CreateMany<S3Object>(3).ToList();

		ListObjectsV2Response response = new()
		{
			S3Objects = s3Objects,
			IsTruncated = false
		};

		A.CallTo(() => s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._)).Returns(response);

		// Act
		List<string>? result = await sut.GetAllS3BucketFiles(bucketName, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(s3Objects.Count);
		result.ShouldBe(s3Objects.Select(x => x.Key));
	}

	[Fact]
	public async Task GetS3Url_WhenBucketValid_ReturnsPreSignedUrl()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		const string expectedUrl = "https://test-bucket.s3.amazonaws.com/test-file";

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });
		A.CallTo(() => s3Client.GetPreSignedURLAsync(A<GetPreSignedUrlRequest>._)).Returns(expectedUrl);

		// Act
		string? result = await sut.GetS3Url(bucketName, fileName);

		// Assert
		result.ShouldBe(expectedUrl);
	}

	//[Theory]
	//[InlineData(HttpStatusCode.OK, true)]
	//[InlineData(HttpStatusCode.NotFound, false)]
	//public async Task IsBucketValid_ReturnsExpectedResult(HttpStatusCode statusCode, bool expectedResult)
	//{
	//    // Arrange
	//    string bucketName = fixture.Create<string>();

	//    //A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = statusCode });
	//    A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = statusCode });

	//    // Act
	//    bool result = await sut.IsBucketValid(bucketName);

	//    // Assert
	//    result.ShouldBe(expectedResult);
	//}

	[Fact]
	public async Task UploadS3File_FilePath_Success()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		string filePath = Path.GetTempFileName();
		await File.WriteAllTextAsync(filePath, "test content", TestContext.Current.CancellationToken);

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });
		A.CallTo(() => s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await sut.UploadS3File(bucketName, fileName, filePath, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		A.CallTo(() => s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();

		File.Delete(filePath);
	}

	[Fact]
	public async Task UploadS3File_FilePath_ThrowsOnInvalidFileName()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string filePath = Path.GetTempFileName();

		// Act & Assert
		await Should.ThrowAsync<ArgumentException>(async () => await sut.UploadS3File(bucketName, "   ", filePath));

		File.Delete(filePath);
	}

	[Fact]
	public async Task UploadS3File_FilePath_ThrowsOnFileNotFound()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");

		// Act & Assert
		await Should.ThrowAsync<FileNotFoundException>(async () => await sut.UploadS3File(bucketName, fileName, filePath));
	}

	[Fact]
	public async Task UploadS3File_FilePath_HandlesAmazonS3Exception()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		string filePath = Path.GetTempFileName();
		await File.WriteAllTextAsync(filePath, "test content", TestContext.Current.CancellationToken);

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
						.Throws(new AmazonS3Exception("AWS error"));

		// Act
		bool result = await sut.UploadS3File(bucketName, fileName, filePath, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();

		File.Delete(filePath);
	}

	[Fact]
	public async Task GetS3File_FilePath_Success()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		string filePath = Path.GetTempFileName();
		byte[] fileContent = Encoding.UTF8.GetBytes("Test content");

		GetObjectResponse response = new()
		{
			ResponseStream = new MemoryStream(fileContent),
			Headers = { ["Content-Encoding"] = string.Empty }
		};

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._)).Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });
		A.CallTo(() => s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._)).Returns(response);

		// Act
		await sut.GetS3File(bucketName, fileName, filePath, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		File.Exists(filePath).ShouldBeTrue();
		(await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken)).ShouldBe("Test content");

		File.Delete(filePath);
	}

	[Fact]
	public async Task GetS3File_FilePath_HandlesAmazonS3Exception()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		string filePath = Path.GetTempFileName();

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
						.Throws(new AmazonS3Exception("AWS error"));

		// Act
		await sut.GetS3File(bucketName, fileName, filePath, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		// No exception thrown, file should be empty
		File.Exists(filePath).ShouldBeTrue();

		File.Delete(filePath);
	}

	[Fact]
	public async Task GetS3File_FilePath_HandlesGeneralException()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();
		string filePath = Path.GetTempFileName();

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
						.Throws(new Exception("General error"));

		// Act
		await sut.GetS3File(bucketName, fileName, filePath, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		File.Exists(filePath).ShouldBeTrue();

		File.Delete(filePath);
	}

	[Fact]
	public async Task GetS3Url_WhenBucketInvalid_ReturnsNull()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
						.Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.NotFound });

		// Act
		string? result = await sut.GetS3Url(bucketName, fileName);

		// Assert
		result.ShouldBe(string.Empty);
	}

	[Fact]
	public async Task GetS3Url_HandlesAmazonS3Exception()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
						.Throws(new AmazonS3Exception("AWS error"));

		// Act
		string? result = await sut.GetS3Url(bucketName, fileName);

		// Assert
		result.ShouldBe(string.Empty);
	}

	[Fact]
	public async Task GetS3Url_HandlesGeneralException()
	{
		// Arrange
		string bucketName = fixture.Create<string>();
		string fileName = fixture.Create<string>();

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
						.Throws(new Exception("General error"));

		// Act
		string? result = await sut.GetS3Url(bucketName, fileName);

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
		bool result = await sut.IsBucketValid(bucketName, validatedBuckets);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task IsBucketValid_CacheMiss_ValidatesAndCaches()
	{
		// Arrange
		string bucketName = Guid.NewGuid().ToString();
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
						.Returns(new GetBucketLocationResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await sut.IsBucketValid(bucketName, validatedBuckets);

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
		A.CallTo(() => s3Client.GetBucketLocationAsync(bucketName, A<CancellationToken>._))
						.ReturnsLazily(() =>
						{
							callCount++;
							return new GetBucketLocationResponse { HttpStatusCode = callCount == 1 ? HttpStatusCode.NotFound : HttpStatusCode.OK };
						});

		// Act
		bool result = await sut.IsBucketValid(bucketName, validatedBuckets);

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

		A.CallTo(() => s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(initiateResponse);

		// Simulate 3 parts
		A.CallTo(() => s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.ReturnsLazily((UploadPartRequest req, CancellationToken _) => new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = $"etag-{req.PartNumber}" });

		A.CallTo(() => s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(completeResponse);

		// Act
		bool result = await sut.UploadMultipartAsync(bucketName, fileName, stream, CancellationToken.None);

		// Assert
		result.ShouldBeTrue();
		A.CallTo(() => s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
		A.CallTo(() => s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._)).MustHaveHappened(3, Times.Exactly);
		A.CallTo(() => s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
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

		A.CallTo(() => s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(initiateResponse);

		// Simulate one part fails (returns null)
		A.CallTo(() => s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.ReturnsNextFromSequence
			(
				new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "etag-1" },
				new UploadPartResponse { HttpStatusCode = HttpStatusCode.BadRequest }
			);

		A.CallTo(() => s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new AbortMultipartUploadResponse());

		// Act
		bool result = await sut.UploadMultipartAsync(bucketName, fileName, stream, CancellationToken.None);

		// Assert
		result.ShouldBeFalse();
		A.CallTo(() => s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
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

		A.CallTo(() => s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(initiateResponse);

		A.CallTo(() => s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "etag-1" });

		A.CallTo(() => s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
			.Throws(new Exception("fail"));

		A.CallTo(() => s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new AbortMultipartUploadResponse());

		// Act
		bool result = await sut.UploadMultipartAsync(bucketName, fileName, stream, CancellationToken.None);

		// Assert
		result.ShouldBeFalse();
		A.CallTo(() => s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
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
		A.CallTo(() => s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(uploadPartResponse);

		// Act
		PartETag? result = await sut.UploadPartAsync(bucketName, fileName, uploadId, stream, partNumber, chunkSize, totalSize, semaphore, CancellationToken.None);

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
		A.CallTo(() => s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(uploadPartResponse);

		// Act
		PartETag? result = await sut.UploadPartAsync(bucketName, fileName, uploadId, stream, partNumber, chunkSize, totalSize, semaphore, CancellationToken.None);

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
		PartETag? result = await sut.UploadPartAsync(bucketName, fileName, uploadId, stream, partNumber, chunkSize, totalSize, semaphore, CancellationToken.None);

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

		A.CallTo(() => s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Throws(new Exception("fail"));

		// Act
		PartETag? result = await sut.UploadPartAsync(bucketName, fileName, uploadId, stream, partNumber, chunkSize, totalSize, semaphore, CancellationToken.None);

		// Assert
		result.ShouldBeNull();
	}

	[Theory]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.None)]
	public async Task DetectCompressionTypeAndReset_SeekableStream_ReturnsCorrectTypeAndResets(ECompressionType compressionType)
	{
		// Arrange
		byte[] original = Encoding.UTF8.GetBytes("Hello Compression!");
		MemoryStream stream;
		if (compressionType == ECompressionType.Gzip)
		{
			stream = new MemoryStream();
			await using (GZipStream gzip = new(stream, CompressionLevel.Optimal, leaveOpen: true))
			{
				await gzip.WriteAsync(original, TestContext.Current.CancellationToken);
			}

			stream.Position = 0;
		}
		else if (compressionType == ECompressionType.Deflate)
		{
			stream = new MemoryStream();
			await using (DeflateStream deflate = new(stream, CompressionLevel.Optimal, leaveOpen: true))
			{
				await deflate.WriteAsync(original, TestContext.Current.CancellationToken);
			}

			stream.Position = 0;
		}
		else
		{
			stream = new MemoryStream(original);
		}

		long initialPosition = stream.Position;

		// Act
		(ECompressionType detected, Stream resetStream) = await DetectCompressionTypeAndReset(stream);

		// Assert
		detected.ShouldBe(compressionType);
		resetStream.ShouldBe(stream);
		stream.Position.ShouldBe(initialPosition);
	}

	[Theory]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.None)]
	public async Task DetectCompressionTypeAndReset_UnseekableStream_ReturnsCorrectTypeAndConcatenatedStream(ECompressionType compressionType)
	{
		// Arrange
		byte[] original = Encoding.UTF8.GetBytes("Hello Compression!");
		MemoryStream baseStream;
		if (compressionType == ECompressionType.Gzip)
		{
			baseStream = new MemoryStream();
			await using (GZipStream gzip = new(baseStream, CompressionLevel.Optimal, leaveOpen: true))
			{
				await gzip.WriteAsync(original, TestContext.Current.CancellationToken);
			}
			baseStream.Position = 0;
		}
		else if (compressionType == ECompressionType.Deflate)
		{
			baseStream = new MemoryStream();
			await using (DeflateStream deflate = new(baseStream, CompressionLevel.Optimal, leaveOpen: true))
			{
				await deflate.WriteAsync(original, TestContext.Current.CancellationToken);
			}
			baseStream.Position = 0;
		}
		else
		{
			baseStream = new MemoryStream(original);
		}

		// Wrap in a stream that is not seekable
		UnseekableStream unseekable = new(baseStream);

		// Act
		(ECompressionType detected, Stream resetStream) = await DetectCompressionTypeAndReset(unseekable);

		// Assert
		detected.ShouldBe(compressionType);
		resetStream.ShouldBeOfType<ConcatenatedStream>();

		// Decompress if needed, then read the original string
		Stream toRead;
		if (compressionType == ECompressionType.Gzip)
		{
			toRead = new GZipStream(resetStream, CompressionMode.Decompress, leaveOpen: true);
		}
		else if (compressionType == ECompressionType.Deflate)
		{
			toRead = new DeflateStream(resetStream, CompressionMode.Decompress, leaveOpen: true);
		}
		else
		{
			toRead = resetStream;
		}

		using StreamReader reader = new(toRead);
		string text = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
		text.ShouldBe("Hello Compression!");
	}

	[Fact]
	public async Task DetectCompressionTypeAndReset_UnseekableStream_Empty_ReturnsNone()
	{
		// Arrange
		UnseekableStream emptyStream = new(new MemoryStream());

		// Act
		(ECompressionType detected, Stream resetStream) = await DetectCompressionTypeAndReset(emptyStream);

		// Assert
		detected.ShouldBe(ECompressionType.None);
		resetStream.ShouldBeOfType<ConcatenatedStream>();
	}

	// Helper for unseekable stream
	private sealed class UnseekableStream(Stream inner) : Stream
	{
		private readonly Stream inner = inner;

		public override bool CanRead => inner.CanRead;

		public override bool CanSeek => false;

		public override bool CanWrite => inner.CanWrite;

		public override long Length => inner.Length;

		public override long Position { get => inner.Position; set => inner.Position = value; }

		public override void Flush()
		{
			inner.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return inner.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			inner.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			inner.Write(buffer, offset, count);
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			return await inner.ReadAsync(buffer, cancellationToken);
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			await inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			return inner.WriteAsync(buffer, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				inner.Dispose();
			}

			base.Dispose(disposing);
		}

		public override async ValueTask DisposeAsync()
		{
			await inner.DisposeAsync();
			await base.DisposeAsync();
		}
	}
}
