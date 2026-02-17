using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using CommonNetFuncs.Web.Aws.S3;
using FakeItEasy;
using NLog;
using NLog.Config;
using NLog.Targets;
using static CommonNetFuncs.Compression.Streams;

namespace Web.Aws.S3.Tests;

public class AwsS3HelpersStaticTests
{
	private readonly IAmazonS3 _s3Client;
	private const string TestBucketName = "test-bucket";
	private const string TestFileName = "test-file.txt";

	static AwsS3HelpersStaticTests()
	{
		// Configure NLog programmatically to enable Trace and Debug logging for coverage
		var config = new LoggingConfiguration();

		// Create a memory target to capture log messages
		var memoryTarget = new MemoryTarget("memory");

		// Add rule that enables Trace level for AWS S3 helpers
		config.AddRule(LogLevel.Trace, LogLevel.Fatal, memoryTarget, "CommonNetFuncs.Web.Aws.S3.*");

		// Apply configuration
		LogManager.Configuration = config;
	}

	public AwsS3HelpersStaticTests()
	{
		_s3Client = A.Fake<IAmazonS3>();
	}

	private ConcurrentDictionary<string, bool> CreateValidatedBucketsCache()
	{
		ConcurrentDictionary<string, bool> cache = new();
		cache[TestBucketName] = true;
		return cache;
	}

	#region UploadS3File (Stream) Tests

	[Fact]
	public async Task UploadS3File_Stream_Should_Upload_Small_File_Successfully()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]); // 1KB file
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeTrue();
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task UploadS3File_Stream_Should_Upload_Large_File_Using_Multipart()
	{
		// Arrange
		byte[] largeData = new byte[15 * 1024 * 1024]; // 15MB
		await using MemoryStream fileData = new(largeData);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });
		A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new CompleteMultipartUploadResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task UploadS3File_Stream_Should_Throw_For_Invalid_Compression_Type()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		// Act & Assert
		await Should.ThrowAsync<NotSupportedException>(() =>
			_s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets,
				compressSteam: true, compressionType: ECompressionType.Brotli));
	}

	[Fact]
	public async Task UploadS3File_Stream_Should_Return_False_For_Empty_FileName()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, "", fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadS3File_Stream_Should_Upload_Without_Compression()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData,
			validatedBuckets, compressSteam: false);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task UploadS3File_Stream_Should_Handle_AmazonS3Exception()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("AWS Error"));

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadS3File_Stream_Should_Delete_Existing_File()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Returns(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });
		// The method calls DeleteObjectAsync with bucketName and key as separate parameters
		A.CallTo(() => _s3Client.DeleteObjectAsync(A<string>._, A<string>._, A<CancellationToken>._))
			.Returns(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeTrue();
		A.CallTo(() => _s3Client.DeleteObjectAsync(A<string>._, A<string>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task UploadS3File_Stream_Should_Return_False_When_Response_Not_OK()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.BadRequest });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadS3File_Stream_Should_Use_Deflate_Compression()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData,
			validatedBuckets, compressSteam: true, compressionType: ECompressionType.Deflate);

		// Assert
		result.ShouldBeTrue();
	}

	#endregion

	#region UploadS3File (FilePath) Tests

	[Fact]
	public async Task UploadS3File_FilePath_Should_Throw_For_Empty_FileName()
	{
		// Act & Assert
		await Should.ThrowAsync<ArgumentException>(() =>
			_s3Client.UploadS3File(TestBucketName, "", "somepath.txt"));
	}

	[Fact]
	public async Task UploadS3File_FilePath_Should_Throw_For_NonExistent_File()
	{
		// Act & Assert
		await Should.ThrowAsync<FileNotFoundException>(() =>
			_s3Client.UploadS3File(TestBucketName, TestFileName, "c:\\nonexistent\\file.txt"));
	}

	[Fact]
	public async Task UploadS3File_FilePath_Should_Upload_Small_File_Successfully()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[1024]);
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
			A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
				.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

			// Act
			bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert
			result.ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task UploadS3File_FilePath_Should_Upload_Large_File_Using_Multipart()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[15 * 1024 * 1024]);
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
			A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
				.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
			A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
				.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });
			A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
				.Returns(new CompleteMultipartUploadResponse { HttpStatusCode = HttpStatusCode.OK });

			// Act
			bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert
			result.ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task UploadS3File_FilePath_Should_Handle_General_Exception()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[1024]);
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new InvalidOperationException("Error"));

			// Act
			bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	#endregion

	#region UploadMultipartAsync Tests

	[Fact]
	public async Task UploadMultipartAsync_Should_Upload_Successfully()
	{
		// Arrange
		byte[] data = new byte[15 * 1024 * 1024]; // 15MB
		await using MemoryStream stream = new(data);

		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });
		A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new CompleteMultipartUploadResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.UploadMultipartAsync(TestBucketName, TestFileName, stream);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task UploadMultipartAsync_Should_Return_False_On_Exception()
	{
		// Arrange
		byte[] data = new byte[15 * 1024 * 1024]; // 15MB
		await using MemoryStream stream = new(data);

		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Error"));

		// Act
		bool result = await _s3Client.UploadMultipartAsync(TestBucketName, TestFileName, stream);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadMultipartAsync_Should_Abort_On_Failure()
	{
		// Arrange
		byte[] data = new byte[15 * 1024 * 1024]; // 15MB
		await using MemoryStream stream = new(data);

		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Upload failed"));
		A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new AbortMultipartUploadResponse());

		// Act
		bool result = await _s3Client.UploadMultipartAsync(TestBucketName, TestFileName, stream);

		// Assert
		result.ShouldBeFalse();
		A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
			.MustHaveHappened();
	}

	[Fact]
	public async Task UploadMultipartAsync_Should_Return_False_When_Complete_Response_Not_OK()
	{
		// Arrange
		byte[] data = new byte[15 * 1024 * 1024]; // 15MB
		await using MemoryStream stream = new(data);

		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });
		A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new CompleteMultipartUploadResponse { HttpStatusCode = HttpStatusCode.BadRequest });

		// Act
		bool result = await _s3Client.UploadMultipartAsync(TestBucketName, TestFileName, stream);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region UploadPartAsync Tests

	[Fact]
	public async Task UploadPartAsync_Should_Upload_Part_Successfully()
	{
		// Arrange
		byte[] data = new byte[10 * 1024 * 1024]; // 10MB
		await using MemoryStream stream = new(data);
		using SemaphoreSlim semaphore = new(1);

		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });

		// Act
		PartETag? result = await _s3Client.UploadPartAsync(TestBucketName, TestFileName, "upload-id",
			stream, 1, 10 * 1024 * 1024, 10 * 1024 * 1024, semaphore);

		// Assert
		result.ShouldNotBeNull();
		result.ETag.ShouldBe("test-etag");
		result.PartNumber.ShouldBe(1);
	}

	[Fact]
	public async Task UploadPartAsync_Should_Return_Null_On_Non_OK_Response()
	{
		// Arrange
		byte[] data = new byte[10 * 1024 * 1024]; // 10MB
		await using MemoryStream stream = new(data);
		using SemaphoreSlim semaphore = new(1);

		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.BadRequest });

		// Act
		PartETag? result = await _s3Client.UploadPartAsync(TestBucketName, TestFileName, "upload-id",
			stream, 1, 10 * 1024 * 1024, 10 * 1024 * 1024, semaphore);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task UploadPartAsync_Should_Return_Null_On_Exception()
	{
		// Arrange
		byte[] data = new byte[10 * 1024 * 1024]; // 10MB
		await using MemoryStream stream = new(data);
		using SemaphoreSlim semaphore = new(1);

		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Error"));

		// Act
		PartETag? result = await _s3Client.UploadPartAsync(TestBucketName, TestFileName, "upload-id",
			stream, 1, 10 * 1024 * 1024, 10 * 1024 * 1024, semaphore);

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region GetS3File (Stream) Tests

	[Fact]
	public async Task GetS3File_Stream_Should_Download_File_Successfully()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		byte[] fileContent = new byte[1024];
		await using MemoryStream responseStream = new(fileContent);

		GetObjectResponse response = new()
		{
			ResponseStream = responseStream,
			HttpStatusCode = HttpStatusCode.OK
		};

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Returns(response);

		// Act
		await _s3Client.GetS3File(TestBucketName, TestFileName, outputStream, validatedBuckets, false);

		// Assert
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetS3File_Stream_Should_Return_For_Empty_FileName()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		// Act
		await _s3Client.GetS3File(TestBucketName, "", outputStream, validatedBuckets);

		// Assert
		outputStream.Length.ShouldBe(0);
	}

	[Fact]
	public async Task GetS3File_Stream_Should_Return_For_Invalid_Bucket()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = new();
		validatedBuckets[TestBucketName] = false;

		// Act
		await _s3Client.GetS3File(TestBucketName, TestFileName, outputStream, validatedBuckets);

		// Assert
		outputStream.Length.ShouldBe(0);
	}

	[Fact]
	public async Task GetS3File_Stream_Should_Handle_NotFound_Exception()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		await _s3Client.GetS3File(TestBucketName, TestFileName, outputStream, validatedBuckets);

		// Assert
		outputStream.Length.ShouldBe(0);
	}

	[Fact]
	public async Task GetS3File_Stream_Should_Handle_Non_NotFound_AmazonS3Exception()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Server error") { StatusCode = HttpStatusCode.InternalServerError });

		// Act
		await _s3Client.GetS3File(TestBucketName, TestFileName, outputStream, validatedBuckets);

		// Assert
		outputStream.Length.ShouldBe(0);
	}

	[Fact]
	public async Task GetS3File_Stream_Should_Handle_General_Exception()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new InvalidOperationException("Error"));

		// Act
		await _s3Client.GetS3File(TestBucketName, TestFileName, outputStream, validatedBuckets);

		// Assert
		outputStream.Length.ShouldBe(0);
	}

	#endregion

	#region GetS3File (FilePath) Tests

	[Fact]
	public async Task GetS3File_FilePath_Should_Download_File_Successfully()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			byte[] fileContent = new byte[1024];
			await using MemoryStream responseStream = new(fileContent);

			GetObjectResponse response = new()
			{
				ResponseStream = responseStream,
				HttpStatusCode = HttpStatusCode.OK
			};

			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.Returns(response);

			// Act
			await _s3Client.GetS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert
			FileInfo fileInfo = new(tempFile);
			fileInfo.Length.ShouldBeGreaterThan(0);
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task GetS3File_FilePath_Should_Handle_Invalid_Bucket()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			ConcurrentDictionary<string, bool> validatedBuckets = new();
			validatedBuckets[TestBucketName] = false;

			// Act
			await _s3Client.GetS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert - Should not throw
			FileInfo fileInfo = new(tempFile);
			fileInfo.Length.ShouldBe(0);
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task GetS3File_FilePath_Should_Handle_NotFound_Exception()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

			// Act
			await _s3Client.GetS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert - Should handle gracefully
			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.MustHaveHappened();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task GetS3File_FilePath_Should_Handle_Non_NotFound_Exception()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Server error") { StatusCode = HttpStatusCode.InternalServerError });

			// Act
			await _s3Client.GetS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert - Should handle gracefully
			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.MustHaveHappened();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task GetS3File_FilePath_Should_Handle_General_Exception()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new InvalidOperationException("Error"));

			// Act
			await _s3Client.GetS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert - Should handle gracefully
			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.MustHaveHappened();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	#endregion

	#region DeleteS3File Tests

	[Fact]
	public async Task DeleteS3File_Should_Delete_File_Successfully()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Returns(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });
		A.CallTo(() => _s3Client.DeleteObjectAsync(A<DeleteObjectRequest>._, A<CancellationToken>._))
			.Returns(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

		// Act
		bool result = await _s3Client.DeleteS3File(TestBucketName, TestFileName, validatedBuckets);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task DeleteS3File_Should_Return_False_For_Empty_FileName()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		// Act
		bool result = await _s3Client.DeleteS3File(TestBucketName, "", validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteS3File_Should_Return_False_For_Invalid_Bucket()
	{
		// Arrange
		string uniqueBucket = "test-bucket-" + Guid.NewGuid().ToString("N");
		ConcurrentDictionary<string, bool> validatedBuckets = new();
		validatedBuckets[uniqueBucket] = false;

		// Act
		bool result = await _s3Client.DeleteS3File(uniqueBucket, TestFileName, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteS3File_Should_Return_False_When_File_Does_Not_Exist()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		bool result = await _s3Client.DeleteS3File(TestBucketName, TestFileName, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteS3File_Should_Return_True_On_NotFound_Exception_During_Delete()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Returns(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });
		A.CallTo(() => _s3Client.DeleteObjectAsync(A<DeleteObjectRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		bool result = await _s3Client.DeleteS3File(TestBucketName, TestFileName, validatedBuckets);

		// Assert
		result.ShouldBeTrue();
	}

	#endregion

	#region S3FileExists Tests

	[Fact]
	public async Task S3FileExists_Should_Return_True_When_File_Exists()
	{
		// Arrange
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Returns(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.S3FileExists(TestBucketName, TestFileName);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task S3FileExists_Should_Return_False_When_File_Does_Not_Exist()
	{
		// Arrange
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		bool result = await _s3Client.S3FileExists(TestBucketName, TestFileName);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task S3FileExists_Should_Return_False_For_Empty_FileName()
	{
		// Act
		bool result = await _s3Client.S3FileExists(TestBucketName, "");

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task S3FileExists_Should_Handle_VersionId()
	{
		// Arrange
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>.That.Matches(r => r.VersionId == "version123"), A<CancellationToken>._))
			.Returns(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.S3FileExists(TestBucketName, TestFileName, "version123");

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task S3FileExists_Should_Return_False_On_Non_NotFound_AmazonS3Exception()
	{
		// Arrange
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Server error") { StatusCode = HttpStatusCode.InternalServerError });

		// Act
		bool result = await _s3Client.S3FileExists(TestBucketName, TestFileName);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task S3FileExists_Should_Return_False_On_General_Exception()
	{
		// Arrange
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new InvalidOperationException("Error"));

		// Act
		bool result = await _s3Client.S3FileExists(TestBucketName, TestFileName);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region GetAllS3BucketFiles Tests

	[Fact]
	public async Task GetAllS3BucketFiles_Should_Return_All_Files()
	{
		// Arrange
		ListObjectsV2Response response = new()
		{
			S3Objects = new List<S3Object>
			{
				new() { Key = "file1.txt" },
				new() { Key = "file2.txt" },
				new() { Key = "file3.txt" }
			},
			IsTruncated = false
		};

		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._))
			.Returns(response);

		// Act
		List<string>? result = await _s3Client.GetAllS3BucketFiles(TestBucketName);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(3);
		result.ShouldContain("file1.txt");
		result.ShouldContain("file2.txt");
		result.ShouldContain("file3.txt");
	}

	[Fact]
	public async Task GetAllS3BucketFiles_Should_Handle_Pagination()
	{
		// Arrange
		ListObjectsV2Response response1 = new()
		{
			S3Objects = new List<S3Object>
			{
				new() { Key = "file1.txt" },
				new() { Key = "file2.txt" }
			},
			IsTruncated = true,
			NextContinuationToken = "token123"
		};

		ListObjectsV2Response response2 = new()
		{
			S3Objects = new List<S3Object>
			{
				new() { Key = "file3.txt" }
			},
			IsTruncated = false
		};

		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.ContinuationToken == null), A<CancellationToken>._))
			.Returns(response1);
		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.ContinuationToken == "token123"), A<CancellationToken>._))
			.Returns(response2);

		// Act
		List<string>? result = await _s3Client.GetAllS3BucketFiles(TestBucketName);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(3);
	}

	[Fact]
	public async Task GetAllS3BucketFiles_Should_Return_Empty_For_Empty_BucketName()
	{
		// Act
		List<string>? result = await _s3Client.GetAllS3BucketFiles("");

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(0);
	}

	[Fact]
	public async Task GetAllS3BucketFiles_Should_Return_Empty_On_NotFound_Exception()
	{
		// Arrange
		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		List<string>? result = await _s3Client.GetAllS3BucketFiles(TestBucketName);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(0);
	}

	[Fact]
	public async Task GetAllS3BucketFiles_Should_Return_Empty_On_Non_NotFound_AmazonS3Exception()
	{
		// Arrange
		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Server error") { StatusCode = HttpStatusCode.InternalServerError });

		// Act
		List<string>? result = await _s3Client.GetAllS3BucketFiles(TestBucketName);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(0);
	}

	[Fact]
	public async Task GetAllS3BucketFiles_Should_Return_Empty_On_General_Exception()
	{
		// Arrange
		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._))
			.ThrowsAsync(new InvalidOperationException("Error"));

		// Act
		List<string>? result = await _s3Client.GetAllS3BucketFiles(TestBucketName);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(0);
	}

	[Fact]
	public async Task GetAllS3BucketFiles_Should_Use_MaxKeysPerQuery()
	{
		// Arrange
		ListObjectsV2Response response = new()
		{
			S3Objects = new List<S3Object>
			{
				new() { Key = "file1.txt" }
			},
			IsTruncated = false
		};

		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.MaxKeys == 500), A<CancellationToken>._))
			.Returns(response);

		// Act
		List<string>? result = await _s3Client.GetAllS3BucketFiles(TestBucketName, 500);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.MaxKeys == 500), A<CancellationToken>._))
			.MustHaveHappened();
	}

	#endregion

	#region IsBucketValid Tests

	[Fact]
	public async Task IsBucketValid_Should_Return_True_For_Valid_Bucket()
	{
		// Arrange
		string uniqueBucket = "test-bucket-" + Guid.NewGuid().ToString("N");
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.MaxKeys == 1 && r.BucketName == uniqueBucket), A<CancellationToken>._))
			.Returns(new ListObjectsV2Response { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.IsBucketValid(uniqueBucket, validatedBuckets);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task IsBucketValid_Should_Use_Cache()
	{
		// Arrange
		string uniqueBucket = "test-bucket-" + Guid.NewGuid().ToString("N");
		ConcurrentDictionary<string, bool> validatedBuckets = new();
		validatedBuckets[uniqueBucket] = true;

		// Act
		bool result = await _s3Client.IsBucketValid(uniqueBucket, validatedBuckets);

		// Assert
		result.ShouldBeTrue();
		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task IsBucketValid_Should_Cache_Result_After_Validation()
	{
		// Arrange
		string uniqueBucket = "test-bucket-" + Guid.NewGuid().ToString("N");
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.BucketName == uniqueBucket), A<CancellationToken>._))
			.Returns(new ListObjectsV2Response { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result1 = await _s3Client.IsBucketValid(uniqueBucket, validatedBuckets);
		bool result2 = await _s3Client.IsBucketValid(uniqueBucket, validatedBuckets);

		// Assert
		result1.ShouldBeTrue();
		result2.ShouldBeTrue();
		validatedBuckets.ContainsKey(uniqueBucket).ShouldBeTrue();
	}

	#endregion

	#region UploadS3File Compression Tests

	[Fact]
	public async Task UploadS3File_Should_Upload_With_Gzip_Compression()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[5 * 1024]); // 5KB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets, AwsS3HelpersStatic.MultipartThreshold, true, ECompressionType.Gzip);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task UploadS3File_Should_Throw_For_Brotli_Compression()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[5 * 1024]); // 5KB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		// Act & Assert - Brotli not supported
		await Should.ThrowAsync<NotSupportedException>(() =>
			_s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets, AwsS3HelpersStatic.MultipartThreshold, true, ECompressionType.Brotli));
	}

	[Fact]
	public async Task UploadS3File_Should_Upload_With_Deflate_Compression()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[5 * 1024]); // 5KB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets, AwsS3HelpersStatic.MultipartThreshold, true, ECompressionType.Deflate);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task UploadS3File_Should_Throw_For_ZLib_Compression()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[5 * 1024]); // 5KB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		// Act & Assert - ZLib not supported
		await Should.ThrowAsync<NotSupportedException>(() =>
			_s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets, AwsS3HelpersStatic.MultipartThreshold, true, ECompressionType.ZLib));
	}

	[Fact]
	public async Task UploadS3File_Should_Upload_Without_Compression()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[5 * 1024]); // 5KB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act - compressSteam = false
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets, AwsS3HelpersStatic.MultipartThreshold, false);

		// Assert
		result.ShouldBeTrue();
	}

	#endregion

	#region MultipartThreshold Tests

	[Fact]
	public void MultipartThreshold_Should_Be_10MB()
	{
		// Assert
		AwsS3HelpersStatic.MultipartThreshold.ShouldBe(10 * 1024 * 1024);
	}

	#endregion

	#region UploadS3File Edge Cases and Additional Coverage Tests

	[Fact]
	public async Task UploadS3File_Should_Return_False_For_Empty_FileName_Stream()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, "", fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadS3File_Should_Return_False_For_Whitespace_FileName_Stream()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, "   ", fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadS3File_Should_Use_Custom_Threshold()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[2 * 1024]); // 2KB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });
		A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new CompleteMultipartUploadResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act - Use 1KB threshold, so 2KB file will use multipart
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets, 1024);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task UploadS3File_FilePath_Should_Use_Multipart_For_Large_File_With_Custom_Threshold()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[2 * 1024]); // 2KB
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
			A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
				.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
			A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
				.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });
			A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
				.Returns(new CompleteMultipartUploadResponse { HttpStatusCode = HttpStatusCode.OK });

			// Act - Use 1KB threshold
			bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, tempFile, validatedBuckets, 1024);

			// Assert
			result.ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task UploadMultipartAsync_Should_Handle_Upload_Part_Failure()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[15 * 1024 * 1024]); // 15MB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Upload failed") { StatusCode = HttpStatusCode.InternalServerError });
		A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new AbortMultipartUploadResponse { HttpStatusCode = HttpStatusCode.NoContent });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadMultipartAsync_Should_Abort_On_Complete_Failure()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[15 * 1024 * 1024]); // 15MB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });
		A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Complete failed"));
		A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new AbortMultipartUploadResponse { HttpStatusCode = HttpStatusCode.NoContent });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task GetAllS3BucketFiles_Should_Handle_Empty_Response()
	{
		// Arrange
		ListObjectsV2Response response = new()
		{
			S3Objects = new List<S3Object>(),
			IsTruncated = false
		};

		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._))
			.Returns(response);

		// Act
		List<string>? result = await _s3Client.GetAllS3BucketFiles(TestBucketName);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(0);
	}

	[Fact]
	public async Task S3FileExists_Should_Return_False_For_Whitespace_FileName()
	{
		// Act
		bool result = await _s3Client.S3FileExists(TestBucketName, "   ");

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadS3File_Should_Return_False_On_PutObject_Non_OK_Status()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.BadRequest });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadS3File_FilePath_Should_Return_False_On_PutObject_Non_OK_Status()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[1024]);
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
			A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
				.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.BadRequest });

			// Act
			bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task UploadMultipartAsync_Should_Return_False_On_Non_OK_Complete_Status()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[15 * 1024 * 1024]); // 15MB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });
		A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new CompleteMultipartUploadResponse { HttpStatusCode = HttpStatusCode.BadRequest });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task GetS3File_Should_Return_Silently_For_Whitespace_FileName()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		// Act
		await _s3Client.GetS3File(TestBucketName, "   ", outputStream, validatedBuckets);

		// Assert
		outputStream.Length.ShouldBe(0);
	}

	[Fact]
	public async Task GetS3File_FilePath_Should_Return_Silently_For_Empty_FileName()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			ConcurrentDictionary<string, bool> validatedBuckets = new();

			// Act
			await _s3Client.GetS3File(TestBucketName, "", tempFile, validatedBuckets);

			// Assert - Should not throw, file should remain empty
			FileInfo fileInfo = new(tempFile);
			fileInfo.Length.ShouldBe(0);
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task DeleteS3File_Should_Return_False_For_Whitespace_FileName()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		// Act
		bool result = await _s3Client.DeleteS3File(TestBucketName, "   ", validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task S3FileExists_Should_Return_False_For_Null_FileName()
	{
		// Act
		bool result = await _s3Client.S3FileExists(TestBucketName, null!);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region GetS3File Decompression Tests

	[Fact]
	public async Task GetS3File_Should_Decompress_Gzip_Data()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		byte[] uncompressed = new byte[1024];
		await using MemoryStream compressed = new();
		await using (var gzipStream = new System.IO.Compression.GZipStream(compressed, System.IO.Compression.CompressionLevel.Optimal, true))
		{
			await gzipStream.WriteAsync(uncompressed);
		}
		compressed.Position = 0;

		GetObjectResponse response = new()
		{
			ResponseStream = compressed,
			HttpStatusCode = HttpStatusCode.OK
		};
		response.Headers["Content-Encoding"] = "gzip";

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Returns(response);

		// Act
		await _s3Client.GetS3File(TestBucketName, TestFileName, outputStream, validatedBuckets, decompressGzipData: true);

		// Assert
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetS3File_Should_Decompress_Deflate_Data()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		byte[] uncompressed = new byte[1024];
		await using MemoryStream compressed = new();
		await using (var deflateStream = new System.IO.Compression.DeflateStream(compressed, System.IO.Compression.CompressionLevel.Optimal, true))
		{
			await deflateStream.WriteAsync(uncompressed);
		}
		compressed.Position = 0;

		GetObjectResponse response = new()
		{
			ResponseStream = compressed,
			HttpStatusCode = HttpStatusCode.OK
		};
		response.Headers["Content-Encoding"] = "deflate";

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Returns(response);

		// Act
		await _s3Client.GetS3File(TestBucketName, TestFileName, outputStream, validatedBuckets, decompressGzipData: true);

		// Assert
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetS3File_Should_Not_Decompress_When_Flag_Is_False()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		byte[] fileContent = new byte[1024];
		await using MemoryStream responseStream = new(fileContent);

		GetObjectResponse response = new()
		{
			ResponseStream = responseStream,
			HttpStatusCode = HttpStatusCode.OK
		};
		response.Headers["Content-Encoding"] = "gzip";

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Returns(response);

		// Act
		await _s3Client.GetS3File(TestBucketName, TestFileName, outputStream, validatedBuckets, decompressGzipData: false);

		// Assert
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	#endregion

	#region UploadS3File Additional Multipart Tests

	[Fact]
	public async Task UploadS3File_FilePath_Multipart_Should_Succeed()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[20 * 1024 * 1024]); // 20MB
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
			A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
				.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
			A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
				.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.OK, ETag = "test-etag" });
			A.CallTo(() => _s3Client.CompleteMultipartUploadAsync(A<CompleteMultipartUploadRequest>._, A<CancellationToken>._))
				.Returns(new CompleteMultipartUploadResponse { HttpStatusCode = HttpStatusCode.OK });

			// Act
			bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert
			result.ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task UploadS3File_FilePath_Multipart_Should_Abort_On_Exception()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[20 * 1024 * 1024]); // 20MB
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
			A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
				.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
			A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Part upload failed"));
			A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
				.Returns(new AbortMultipartUploadResponse { HttpStatusCode = HttpStatusCode.NoContent });

			// Act
			bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	#endregion

	#region Exception Path Coverage Tests

	[Fact]
	public async Task UploadS3File_Should_Handle_AmazonS3Exception()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("AWS Error") { StatusCode = HttpStatusCode.InternalServerError });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadS3File_FilePath_Should_Handle_AmazonS3Exception()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[1024]);
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("AWS Error") { StatusCode = HttpStatusCode.ServiceUnavailable });

			// Act
			bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task GetS3File_FilePath_Should_Handle_NotFound_AmazonS3Exception()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

			// Act
			await _s3Client.GetS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert - Should handle gracefully with special NotFound logging
			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.MustHaveHappened();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task GetS3File_FilePath_Should_Handle_Non_NotFound_AmazonS3Exception()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.ThrowsAsync(new AmazonS3Exception("Server error") { StatusCode = HttpStatusCode.InternalServerError });

			// Act
			await _s3Client.GetS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert - Should handle with error logging
			A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
				.MustHaveHappened();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public async Task GetAllS3BucketFiles_Should_Handle_AmazonS3Exception_NotFound()
	{
		// Arrange
		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		List<string>? result = await _s3Client.GetAllS3BucketFiles(TestBucketName);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(0);
	}

	[Fact]
	public async Task S3FileExists_Should_Handle_Non_NotFound_AmazonS3Exception()
	{
		// Arrange
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Server error") { StatusCode = HttpStatusCode.InternalServerError });

		// Act
		bool result = await _s3Client.S3FileExists(TestBucketName, TestFileName);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteS3File_Should_Handle_Non_NotFound_AmazonS3Exception_During_Check()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Server error") { StatusCode = HttpStatusCode.InternalServerError });

		// Act
		bool result = await _s3Client.DeleteS3File(TestBucketName, TestFileName, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteS3File_Should_Handle_General_Exception_During_Check()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new InvalidOperationException("Error"));

		// Act
		bool result = await _s3Client.DeleteS3File(TestBucketName, TestFileName, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadS3File_Stream_Should_Handle_General_Exception()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new InvalidOperationException("Error"));

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UploadPartAsync_Should_Return_Null_On_Non_OK_Status()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[15 * 1024 * 1024]); // 15MB
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.InitiateMultipartUploadAsync(A<InitiateMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new InitiateMultipartUploadResponse { UploadId = "test-upload-id" });
		A.CallTo(() => _s3Client.UploadPartAsync(A<UploadPartRequest>._, A<CancellationToken>._))
			.Returns(new UploadPartResponse { HttpStatusCode = HttpStatusCode.BadRequest }); // Non-OK status
		A.CallTo(() => _s3Client.AbortMultipartUploadAsync(A<AbortMultipartUploadRequest>._, A<CancellationToken>._))
			.Returns(new AbortMultipartUploadResponse { HttpStatusCode = HttpStatusCode.NoContent });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region GetS3Url Tests

	[Fact]
	public async Task GetS3Url_Should_Return_Url_For_Valid_File()
	{
		// Arrange
		string uniqueBucket = "test-bucket-" + Guid.NewGuid().ToString("N");
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.BucketName == uniqueBucket), A<CancellationToken>._))
			.Returns(new ListObjectsV2Response { HttpStatusCode = HttpStatusCode.OK });
		A.CallTo(() => _s3Client.GetPreSignedURLAsync(A<GetPreSignedUrlRequest>._))
			.Returns("https://s3.amazonaws.com/test-bucket/test-file.txt?signature=xyz");

		// Act
		string? result = await _s3Client.GetS3Url(uniqueBucket, TestFileName, validatedBuckets);

		// Assert
		result.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetS3Url_Should_Return_Null_For_Invalid_Bucket()
	{
		// Arrange
		string uniqueBucket = "test-bucket-" + Guid.NewGuid().ToString("N");
		ConcurrentDictionary<string, bool> validatedBuckets = new();

		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.BucketName == uniqueBucket), A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		string? result = await _s3Client.GetS3Url(uniqueBucket, TestFileName, validatedBuckets);

		// Assert - When bucket validation fails, method returns null
		result.ShouldBeNullOrEmpty();
	}

	[Fact]
	public async Task GetS3Url_Should_Handle_AmazonS3Exception_NotFound()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetPreSignedURLAsync(A<GetPreSignedUrlRequest>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

		// Act
		string? result = await _s3Client.GetS3Url(TestBucketName, TestFileName, validatedBuckets);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetS3Url_Should_Handle_AmazonS3Exception_Non_NotFound()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetPreSignedURLAsync(A<GetPreSignedUrlRequest>._))
			.ThrowsAsync(new AmazonS3Exception("Server error") { StatusCode = HttpStatusCode.InternalServerError });

		// Act
		string? result = await _s3Client.GetS3Url(TestBucketName, TestFileName, validatedBuckets);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetS3Url_Should_Handle_General_Exception()
	{
		// Arrange
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetPreSignedURLAsync(A<GetPreSignedUrlRequest>._))
			.ThrowsAsync(new InvalidOperationException("Error"));

		// Act
		string? result = await _s3Client.GetS3Url(TestBucketName, TestFileName, validatedBuckets);

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region CheckForExistingFile Path Tests

	[Fact]
	public async Task UploadS3File_Should_Delete_Existing_File_Before_Upload()
	{
		// Arrange
		await using MemoryStream fileData = new(new byte[1024]);
		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		// File exists
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Returns(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });
		// Delete succeeds
		A.CallTo(() => _s3Client.DeleteObjectAsync(A<string>._, A<string>._, A<CancellationToken>._))
			.Returns(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });
		// Upload succeeds
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, fileData, validatedBuckets);

		// Assert
		result.ShouldBeTrue();
		A.CallTo(() => _s3Client.DeleteObjectAsync(A<string>._, A<string>._, A<CancellationToken>._))
			.MustHaveHappened();
	}

	[Fact]
	public async Task UploadS3File_FilePath_Should_Delete_Existing_File_Before_Upload()
	{
		// Arrange
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[1024]);
			ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

			// File exists
			A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
				.Returns(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });
			// Delete succeeds
			A.CallTo(() => _s3Client.DeleteObjectAsync(A<string>._, A<string>._, A<CancellationToken>._))
				.Returns(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });
			// Upload succeeds
			A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
				.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

			// Act
			bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, tempFile, validatedBuckets);

			// Assert
			result.ShouldBeTrue();
			A.CallTo(() => _s3Client.DeleteObjectAsync(A<string>._, A<string>._, A<CancellationToken>._))
				.MustHaveHappened();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	#endregion

	#region Additional Decompression Scenarios

	[Fact]
	public async Task GetS3File_Should_Handle_Brotli_Compressed_Stream()
	{
		// Arrange
		await using MemoryStream outputStream = new();
		byte[] testData = "Test data"u8.ToArray();

		// Create Brotli compressed data
		using MemoryStream compressedStream = new();
		using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Compress, true))
		{
			await brotliStream.WriteAsync(testData);
		}
		compressedStream.Position = 0;

		GetObjectResponse response = new()
		{
			HttpStatusCode = HttpStatusCode.OK,
			ResponseStream = compressedStream,
			ContentLength = compressedStream.Length
		};

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Returns(response);

		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		// Act
		await _s3Client.GetS3File(TestBucketName, TestFileName, outputStream, validatedBuckets, decompressGzipData: true);

		// Assert - File should be decompressed
		outputStream.Position = 0;
		byte[] result = outputStream.ToArray();
		result.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task UploadS3File_Should_Handle_Already_Compressed_Brotli_Stream()
	{
		// Arrange
		byte[] testData = "Test data for Brotli compression"u8.ToArray();

		// Create Brotli compressed stream
		await using MemoryStream compressedStream = new();
		await using (BrotliStream brotliStream = new(compressedStream, CompressionMode.Compress, true))
		{
			await brotliStream.WriteAsync(testData);
		}
		compressedStream.Position = 0;

		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act - Upload with compression, but stream is already Brotli compressed
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, compressedStream, validatedBuckets, compressSteam: true, compressionType: ECompressionType.Gzip);

		// Assert - Should decompress Brotli first, then recompress with Gzip
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task UploadS3File_Should_Handle_Already_Compressed_ZLib_Stream()
	{
		// Arrange
		byte[] testData = "Test data for ZLib compression"u8.ToArray();

		// Create ZLib compressed stream
		await using MemoryStream compressedStream = new();
		await using (ZLibStream zlibStream = new(compressedStream, CompressionMode.Compress, true))
		{
			await zlibStream.WriteAsync(testData);
		}
		compressedStream.Position = 0;

		ConcurrentDictionary<string, bool> validatedBuckets = CreateValidatedBucketsCache();

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

		// Act - Upload with compression, but stream is already ZLib compressed
		bool result = await _s3Client.UploadS3File(TestBucketName, TestFileName, compressedStream, validatedBuckets, compressSteam: true, compressionType: ECompressionType.Gzip);

		// Assert - Should decompress ZLib first, then recompress with Gzip
		result.ShouldBeTrue();
	}

	#endregion
}
