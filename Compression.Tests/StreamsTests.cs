using CommonNetFuncs.Compression;
using static CommonNetFuncs.Compression.Streams;

namespace Compression.Tests;

public sealed class StreamsTests
{
	private readonly Fixture fixture;

	private readonly byte[] smallData;
	private readonly byte[] largeData;

	public StreamsTests()
	{
		fixture = new Fixture();
		smallData = fixture.CreateMany<byte>(100).ToArray();
		largeData = fixture.CreateMany<byte>((1024 * 1024) + 177).ToArray();
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public async Task CompressStream_Should_Compress_Data(ECompressionType compressionType)
	{
		// Arrange
		byte[] uncompressedData = smallData;
		await using MemoryStream uncompressedStream = new(uncompressedData);
		await using MemoryStream compressedStream = new();

		// Act
		await uncompressedStream.CompressStream(compressedStream, compressionType, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		compressedStream.Length.ShouldBeGreaterThan(0);
	}

	[Theory]
	[InlineData(true, true, true)]
	[InlineData(true, false, true)]
	[InlineData(false, true, true)]
	[InlineData(true, true, false)]
	[InlineData(true, false, false)]
	[InlineData(false, true, false)]
	public async Task CompressStream_Should_Throw_Error(bool canWriteCompressedStream, bool canReadUncompressedStream, bool useAsync)
	{
		// Arrange
		byte[] uncompressedData = smallData;
		await using MemoryStream uncompressedStream = new(uncompressedData);
		await using MemoryStream compressedStream = (!canWriteCompressedStream) ? new([], false) : new();

		if (!canReadUncompressedStream)
		{
			uncompressedStream.Close();
		}

		// Act & Assert
		if (!canWriteCompressedStream || !canReadUncompressedStream)
		{
			if (useAsync)
			{
				await Should.ThrowAsync<NotSupportedException>(uncompressedStream.CompressStream(compressedStream, ECompressionType.Deflate, cancellationToken: TestContext.Current.CancellationToken));
			}
			else
			{
				Should.Throw<NotSupportedException>(() => uncompressedStream.CompressStreamSynchronous(compressedStream, ECompressionType.Deflate));
			}
		}
		else
		{
			if (useAsync)
			{
				await Should.NotThrowAsync(uncompressedStream.CompressStream(compressedStream, ECompressionType.Deflate, cancellationToken: TestContext.Current.CancellationToken));
			}
			else
			{
				Should.NotThrow(() => uncompressedStream.CompressStreamSynchronous(compressedStream, ECompressionType.Deflate));
			}
			compressedStream.Length.ShouldBeGreaterThan(0);
		}
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public void CompressStreamSynchronous_Should_Compress_Data(ECompressionType compressionType)
	{
		// Arrange
		byte[] uncompressedData = smallData;
		using MemoryStream uncompressedStream = new(uncompressedData);
		using MemoryStream compressedStream = new();

		// Act
		uncompressedStream.CompressStreamSynchronous(compressedStream, compressionType);

		// Assert
		compressedStream.Length.ShouldBeGreaterThan(0);
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public async Task DecompressStream_Should_Decompress_Data(ECompressionType compressionType)
	{
		// Arrange
		byte[] originalData = smallData;
		await using MemoryStream uncompressedStream = new(originalData);
		await using MemoryStream compressedStream = new();
		await using MemoryStream decompressedStream = new();

		await uncompressedStream.CompressStream(compressedStream, compressionType, cancellationToken: TestContext.Current.CancellationToken);
		compressedStream.Position = 0;

		// Act
		await compressedStream.DecompressStream(decompressedStream, compressionType, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		decompressedStream.ToArray().ShouldBe(originalData);
	}

	[Theory]
	[InlineData(true, true, true)]
	[InlineData(true, false, true)]
	[InlineData(false, true, true)]
	[InlineData(true, true, false)]
	[InlineData(true, false, false)]
	[InlineData(false, true, false)]
	public async Task DecompressStream_Should_Throw_Error(bool canWriteDecompressedStream, bool canReadCompressedStream, bool useAsync)
	{
		// Arrange
		byte[] originalData = smallData;
		await using MemoryStream uncompressedStream = new(originalData);
		await using MemoryStream compressedStream = new();

		await using MemoryStream decompressedStream = (!canWriteDecompressedStream) ? new([], false) : new();

		await uncompressedStream.CompressStream(compressedStream, ECompressionType.Deflate, cancellationToken: TestContext.Current.CancellationToken);
		compressedStream.Position = 0;

		if (!canReadCompressedStream)
		{
			compressedStream.Close();
		}

		// Act & Assert
		if (!canWriteDecompressedStream || !canReadCompressedStream)
		{
			if (useAsync)
			{
				await Should.ThrowAsync<NotSupportedException>(compressedStream.DecompressStream(decompressedStream, ECompressionType.Deflate, cancellationToken: TestContext.Current.CancellationToken));
			}
			else
			{
				Should.Throw<NotSupportedException>(() => compressedStream.DecompressStreamSynchronous(decompressedStream, ECompressionType.Deflate));
			}
		}
		else
		{
			if (useAsync)
			{
				await Should.NotThrowAsync(compressedStream.DecompressStream(decompressedStream, ECompressionType.Deflate, cancellationToken: TestContext.Current.CancellationToken));
			}
			else
			{
				Should.NotThrow(() => compressedStream.DecompressStreamSynchronous(decompressedStream, ECompressionType.Deflate));
			}
			decompressedStream.ToArray().ShouldBe(originalData);
		}
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public void DecompressStreamSynchronous_Should_Decompress_Data(ECompressionType compressionType)
	{
		// Arrange
		byte[] originalData = smallData;
		MemoryStream uncompressedStream = new(originalData);
		MemoryStream compressedStream = new();
		MemoryStream decompressedStream = new();

		uncompressedStream.CompressStreamSynchronous(compressedStream, compressionType);
		compressedStream.Position = 0;

		// Act
		compressedStream.DecompressStreamSynchronous(decompressedStream, compressionType);

		// Assert
		decompressedStream.ToArray().ShouldBe(originalData);
	}

	[Theory]
	[InlineData(ECompressionType.Brotli, false)]
	[InlineData(ECompressionType.Gzip, false)]
	[InlineData(ECompressionType.Deflate, false)]
	[InlineData(ECompressionType.ZLib, false)]
	[InlineData(ECompressionType.Brotli, true)]
	[InlineData(ECompressionType.Gzip, true)]
	[InlineData(ECompressionType.Deflate, true)]
	[InlineData(ECompressionType.ZLib, true)]
	public async Task Compress_Should_Compress_Byte_Array(ECompressionType compressionType, bool useLargeData)
	{
		// Arrange
		byte[] originalData = useLargeData ? largeData : smallData;

		// Act
		byte[] compressedData = await originalData.Compress(compressionType, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		compressedData.Length.ShouldBeGreaterThan(0);
	}

	[Theory]
	[InlineData(ECompressionType.Gzip, 2000)]
	[InlineData(ECompressionType.Deflate, 5000)]
	[InlineData(ECompressionType.ZLib, 8000)]
	[InlineData(ECompressionType.Gzip, 1024)] // Edge case for <= 1024 branch
	[InlineData(ECompressionType.Deflate, 1025)] // Just above 1024
	[InlineData(ECompressionType.ZLib, 10240)] // Edge case for <= 10240 branch
	[InlineData(ECompressionType.Gzip, 10241)] // Just above 10240
	public async Task Compress_Should_Handle_Medium_Sized_Data(ECompressionType compressionType, int dataSize)
	{
		// Arrange - Create medium-sized data to hit the <= 10240 branch in Compress method
		byte[] mediumData = fixture.CreateMany<byte>(dataSize).ToArray();

		// Act
		byte[] compressedData = await mediumData.Compress(compressionType, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		compressedData.Length.ShouldBeGreaterThan(0);

		// Verify we can decompress it back
		byte[] decompressedData = await compressedData.Decompress(compressionType, cancellationToken: TestContext.Current.CancellationToken);
		decompressedData.ShouldBe(mediumData);
	}

	[Theory]
	[InlineData(ECompressionType.Brotli, false)]
	[InlineData(ECompressionType.Gzip, false)]
	[InlineData(ECompressionType.Deflate, false)]
	[InlineData(ECompressionType.ZLib, false)]
	[InlineData(ECompressionType.Brotli, true)]
	[InlineData(ECompressionType.Gzip, true)]
	[InlineData(ECompressionType.Deflate, true)]
	[InlineData(ECompressionType.ZLib, true)]
	public async Task Decompress_Should_Decompress_Byte_Array(ECompressionType compressionType, bool useLargeData)
	{
		// Arrange
		byte[] originalData = useLargeData ? largeData : smallData;
		byte[] compressedData = await originalData.Compress(compressionType, cancellationToken: TestContext.Current.CancellationToken);

		// Act
		byte[] decompressedData = await compressedData.Decompress(compressionType, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		decompressedData.ShouldBe(originalData);
	}

	[Theory]
	[MemberData(nameof(GetCompressionTestData))]
	public async Task DetectCompressionTypeNonSeekable_Should_Detect_Compression_Type(byte[] header, ECompressionType expectedType)
	{
		// Arrange
		await using MemoryStream stream = new(header, false);

		// Act
		ECompressionType detectedType = await DetectCompressionTypeNonSeekable(stream);

		// Assert
		detectedType.ShouldBe(expectedType);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	public async Task DetectCompressionTypeNonSeekable_Should_Return_None_For_Short_Stream(int streamLength)
	{
		// Arrange
		byte[] data = new byte[streamLength];
		await using MemoryStream stream = new(data, false);

		// Act
		ECompressionType detectedType = await DetectCompressionTypeNonSeekable(stream);

		// Assert
		detectedType.ShouldBe(ECompressionType.None);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	public async Task DetectCompressionTypeSeekable_Should_Return_None_For_Short_Stream(int streamLength)
	{
		// Arrange
		byte[] data = new byte[streamLength];
		await using MemoryStream stream = new(data);
		long originalPosition = stream.Position;

		// Act
		ECompressionType detectedType = await DetectCompressionTypeSeekable(stream);

		// Assert
		detectedType.ShouldBe(ECompressionType.None);
		stream.Position.ShouldBe(originalPosition);
	}

	[Fact]
	public async Task DetectCompressionTypeNonSeekable_Should_Handle_Closed_Stream()
	{
		// Arrange
		await using MemoryStream stream = new();
		stream.Close();

		// Act & Assert
		await Should.ThrowAsync<ObjectDisposedException>(DetectCompressionTypeNonSeekable(stream));
	}

	public static TheoryData<byte[], ECompressionType> GetCompressionTestData()
	{
		TheoryData<byte[], ECompressionType> data = new()
			{
				// Gzip header (1F 8B)
				{ new byte[] { 0x1F, 0x8B, 0x08, 0x00 }, ECompressionType.Gzip },

				// Zlib header (78 01, 78 9C, or 78 DA)
				{ new byte[] { 0x78, 0x01, 0x00, 0x00 }, ECompressionType.ZLib },
				{ new byte[] { 0x78, 0x9C, 0x00, 0x00 }, ECompressionType.ZLib },
				{ new byte[] { 0x78, 0xDA, 0x00, 0x00 }, ECompressionType.ZLib },

				// Invalid ZLib (78 with wrong second byte)
				{ new byte[] { 0x78, 0x00, 0x00, 0x00 }, ECompressionType.None },
				{ new byte[] { 0x78, 0xFF, 0x00, 0x00 }, ECompressionType.None },

				// Brotli header (CE B2 CF 81)
				{ new byte[] { 0xCE, 0xB2, 0xCF, 0x81 }, ECompressionType.Brotli },

				// Invalid/Unknown header
				{ new byte[] { 0x00, 0x00, 0x00, 0x00 }, ECompressionType.None },

				// Edge case - exactly 2 bytes
				{ new byte[] { 0x1F, 0x8B }, ECompressionType.Gzip },

				// Edge case - 3 bytes
				{ new byte[] { 0x78, 0x01, 0x00 }, ECompressionType.ZLib }
			};

		return data;
	}

	// Tests for AnalyzeHeader directly to ensure branch coverage
	[Theory]
	[InlineData(new byte[] { 0x78, 0x00 }, 2, ECompressionType.None)]
	[InlineData(new byte[] { 0x78, 0xFF }, 2, ECompressionType.None)]
	[InlineData(new byte[] { 0x78, 0x02 }, 2, ECompressionType.None)]
	[InlineData(new byte[] { 0x78, 0x50 }, 2, ECompressionType.None)]
	public void AnalyzeHeader_Should_Return_None_For_Invalid_ZLib_Headers(byte[] header, int bytesRead, ECompressionType expected)
	{
		// Act
		ECompressionType result = AnalyzeHeader(header, bytesRead);

		// Assert
		result.ShouldBe(expected);
	}

	// Tests for Compress(Stream) method
	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public void CompressStream_Should_Return_CompressionStream(ECompressionType compressionType)
	{
		// Arrange
		byte[] originalData = smallData;
		using MemoryStream uncompressedStream = new(originalData);

		// Act
		using Stream compressionStream = uncompressedStream.Compress(compressionType);

		// Assert
		compressionStream.ShouldNotBeNull();
		compressionStream.CanWrite.ShouldBeTrue(); // Compression streams are write-only
	}

	[Fact]
	public void CompressStream_Should_Throw_NotSupportedException_For_Unreadable_Stream()
	{
		// Arrange
		using MemoryStream stream = new([], false);
		stream.Close();

		// Act & Assert
		Should.Throw<NotSupportedException>(() => stream.Compress(ECompressionType.Gzip));
	}

	[Fact]
	public void CompressStream_Should_Throw_NotImplementedException_For_None_CompressionType()
	{
		// Arrange
		using MemoryStream stream = new(smallData);

		// Act & Assert
		Should.Throw<NotImplementedException>(() => stream.Compress(ECompressionType.None));
	}

	// Tests for Decompress(Stream) method
	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public async Task DecompressStream_Should_Return_DecompressionStream(ECompressionType compressionType)
	{
		// Arrange
		byte[] originalData = smallData;
		byte[] compressedData = await originalData.Compress(compressionType, cancellationToken: TestContext.Current.CancellationToken);
		await using MemoryStream compressedStream = new(compressedData);

		// Act
		await using Stream decompressionStream = compressedStream.Decompress(compressionType);

		// Assert
		decompressionStream.ShouldNotBeNull();
		decompressionStream.CanRead.ShouldBeTrue();

		// Verify we can read decompressed data
		await using MemoryStream resultStream = new();
		await decompressionStream.CopyToAsync(resultStream, TestContext.Current.CancellationToken);
		resultStream.ToArray().ShouldBe(originalData);
	}

	[Fact]
	public void DecompressStream_Should_Throw_NotSupportedException_For_Unreadable_Stream()
	{
		// Arrange
		using MemoryStream stream = new([], false);
		stream.Close();

		// Act & Assert
		Should.Throw<NotSupportedException>(() => stream.Decompress(ECompressionType.Gzip));
	}

	[Fact]
	public void DecompressStream_Should_Throw_NotImplementedException_For_None_CompressionType()
	{
		// Arrange
		using MemoryStream stream = new(smallData);

		// Act & Assert
		Should.Throw<NotImplementedException>(() => stream.Decompress(ECompressionType.None));
	}

	// Tests for DetectCompressionType with seekable streams
	[Theory]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public async Task DetectCompressionType_Should_Detect_Type_For_Seekable_Stream(ECompressionType compressionType)
	{
		// Arrange
		byte[] originalData = smallData;
		byte[] compressedData = await originalData.Compress(compressionType, cancellationToken: TestContext.Current.CancellationToken);
		await using MemoryStream compressedStream = new(compressedData);

		// Act
		ECompressionType detectedType = await compressedStream.DetectCompressionType();

		// Assert
		detectedType.ShouldBe(compressionType);
		compressedStream.Position.ShouldBe(0); // Position should be restored
	}

	[Fact]
	public async Task DetectCompressionType_Should_Return_None_For_Null_Stream()
	{
		// Act
		ECompressionType detectedType = await Streams.DetectCompressionType(null);

		// Assert
		detectedType.ShouldBe(ECompressionType.None);
	}

	[Fact]
	public async Task DetectCompressionType_Should_Return_None_For_Unreadable_Stream()
	{
		// Arrange
		await using MemoryStream stream = new();
		stream.Close();

		// Act
		ECompressionType detectedType = await stream.DetectCompressionType();

		// Assert
		detectedType.ShouldBe(ECompressionType.None);
	}

	[Fact]
	public async Task DetectCompressionType_Should_Return_None_For_Non_Compressed_Data()
	{
		// Arrange - Use predictable ASCII text data that won't match compression signatures
		byte[] nonCompressedData = "This is plain text data that should not match any compression signature."u8.ToArray();
		await using MemoryStream stream = new(nonCompressedData);

		// Act
		ECompressionType detectedType = await stream.DetectCompressionType();

		// Assert
		detectedType.ShouldBe(ECompressionType.None);
		stream.Position.ShouldBe(0);
	}

	[Theory]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.ZLib)]
	public async Task DetectCompressionType_Should_Detect_Type_For_NonSeekable_Stream(ECompressionType compressionType)
	{
		// Arrange
		byte[] compressedData = await smallData.Compress(compressionType, cancellationToken: TestContext.Current.CancellationToken);
		await using NonSeekableStream nonSeekableStream = new(compressedData);

		// Act
		ECompressionType detectedType = await nonSeekableStream.DetectCompressionType();

		// Assert
		detectedType.ShouldBe(compressionType);
	}

	// Tests for IsDeflateCompressed
	[Fact]
	public async Task IsDeflateCompressed_Should_Return_True_For_Deflate_Compressed_Data()
	{
		// Arrange
		byte[] originalData = smallData;
		byte[] compressedData = await originalData.Compress(ECompressionType.Deflate, cancellationToken: TestContext.Current.CancellationToken);

		// Act
		bool isDeflate = await IsDeflateCompressed(compressedData);

		// Assert
		isDeflate.ShouldBeTrue();
	}

	[Fact]
	public async Task IsDeflateCompressed_Should_Return_False_For_Non_Deflate_Data()
	{
		// Arrange - Use predictable ASCII text data that's clearly not deflate-compressed
		byte[] originalData = "Hello, World! This is not compressed data."u8.ToArray();

		// Act
		bool isDeflate = await IsDeflateCompressed(originalData);

		// Assert
		isDeflate.ShouldBeFalse();
	}

	[Fact]
	public async Task IsDeflateCompressed_Should_Return_False_For_Empty_Data()
	{
		// Arrange
		byte[] emptyData = [];

		// Act
		bool isDeflate = await IsDeflateCompressed(emptyData);

		// Assert
		isDeflate.ShouldBeFalse();
	}

	// Tests for DetectCompressionTypeAndReset
	[Theory]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.ZLib)]
	public async Task DetectCompressionTypeAndReset_Should_Detect_And_Return_Seekable_Stream(ECompressionType compressionType)
	{
		// Arrange
		byte[] originalData = smallData;
		byte[] compressedData = await originalData.Compress(compressionType, cancellationToken: TestContext.Current.CancellationToken);
		await using MemoryStream compressedStream = new(compressedData);

		// Act
		(ECompressionType detectedType, Stream resultStream) = await DetectCompressionTypeAndReset(compressedStream);

		// Assert
		detectedType.ShouldBe(compressionType);
		resultStream.ShouldBeSameAs(compressedStream);
		compressedStream.Position.ShouldBe(0);
	}

	[Theory]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.ZLib)]
	public async Task DetectCompressionTypeAndReset_Should_Detect_And_Return_NonSeekable_Stream(ECompressionType compressionType)
	{
		// Arrange
		byte[] originalData = smallData;
		byte[] compressedData = await originalData.Compress(compressionType, cancellationToken: TestContext.Current.CancellationToken);

		// Create a non-seekable stream wrapper
		await using NonSeekableStream nonSeekableStream = new(compressedData);

		// Act
		(ECompressionType detectedType, Stream resultStream) = await DetectCompressionTypeAndReset(nonSeekableStream);

		// Assert
		detectedType.ShouldBe(compressionType);
		resultStream.ShouldNotBeNull();
		resultStream.CanRead.ShouldBeTrue();

		// Verify we can decompress the entire stream
		await using MemoryStream decompressedStream = new();
		await using Stream decompressionStream = resultStream.Decompress(compressionType, leaveOpen: true);
		await decompressionStream.CopyToAsync(decompressedStream, TestContext.Current.CancellationToken);
		decompressedStream.ToArray().ShouldBe(originalData);
	}

	// Tests for CopyWithLimit
	[Fact]
	public void CopyWithLimit_Should_Copy_Data_Within_Limit()
	{
		// Arrange
		byte[] data = smallData;
		using MemoryStream source = new(data);
		using MemoryStream destination = new();
		long maxBytes = data.Length;

		// Act
		source.CopyWithLimit(destination, maxBytes, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		destination.ToArray().ShouldBe(data);
	}

	[Fact]
	public void CopyWithLimit_Should_Throw_When_Limit_Exceeded()
	{
		// Arrange
		byte[] data = smallData;
		using MemoryStream source = new(data);
		using MemoryStream destination = new();
		long maxBytes = data.Length - 10;

		// Act & Assert
		Should.Throw<CompressionLimitExceededException>(() => source.CopyWithLimit(destination, maxBytes));
	}

	[Fact]
	public void CopyWithLimit_Should_Respect_Cancellation_Token()
	{
		// Arrange
		byte[] reallyLargeData = fixture.CreateMany<byte>(5 * 1024 * 1024).ToArray(); // 5 MB
		using MemoryStream source = new(reallyLargeData);
		using MemoryStream destination = new();
		using CancellationTokenSource cts = new();
		cts.Cancel();
		long maxBytes = reallyLargeData.Length;

		// Act & Assert
		Should.Throw<OperationCanceledException>(() => source.CopyWithLimit(destination, maxBytes, cts.Token));
	}

	// Tests for CopyWithLimitAsync
	[Fact]
	public async Task CopyWithLimitAsync_Should_Copy_Data_Within_Limit()
	{
		// Arrange
		byte[] data = smallData;
		await using MemoryStream source = new(data);
		await using MemoryStream destination = new();
		long maxBytes = data.Length;

		// Act
		await source.CopyWithLimitAsync(destination, maxBytes, TestContext.Current.CancellationToken);

		// Assert
		destination.ToArray().ShouldBe(data);
	}

	[Fact]
	public async Task CopyWithLimitAsync_Should_Throw_When_Limit_Exceeded()
	{
		// Arrange
		byte[] data = smallData;
		await using MemoryStream source = new(data);
		await using MemoryStream destination = new();
		long maxBytes = data.Length - 10;

		// Act & Assert
		await Should.ThrowAsync<CompressionLimitExceededException>(source.CopyWithLimitAsync(destination, maxBytes, TestContext.Current.CancellationToken));
	}

	// Tests for CompressionLimitExceededException
	[Fact]
	public void CompressionLimitExceededException_Should_Have_Parameterless_Constructor()
	{
		// Act
		CompressionLimitExceededException exception = new();

		// Assert
		exception.ShouldNotBeNull();
	}

	[Fact]
	public void CompressionLimitExceededException_Should_Have_Message_Constructor()
	{
		// Arrange
		const string message = "Test exception message";

		// Act
		CompressionLimitExceededException exception = new(message);

		// Assert
		exception.Message.ShouldBe(message);
	}

	[Fact]
	public void CompressionLimitExceededException_Should_Have_Message_And_InnerException_Constructor()
	{
		// Arrange
		const string message = "Test exception message";
		Exception innerException = new InvalidOperationException("Inner exception");

		// Act
		CompressionLimitExceededException exception = new(message, innerException);

		// Assert
		exception.Message.ShouldBe(message);
		exception.InnerException.ShouldBeSameAs(innerException);
	}

	// Tests for ConcatenatedStream
	[Fact]
	public async Task ConcatenatedStream_Should_Read_From_Both_Streams()
	{
		// Arrange
		byte[] firstData = [1, 2, 3, 4, 5];
		byte[] secondData = [6, 7, 8, 9, 10];
		await using MemoryStream firstStream = new(firstData);
		await using MemoryStream secondStream = new(secondData);
		await using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act
		byte[] buffer = new byte[10];
		int bytesRead = await concatenatedStream.ReadAsync(buffer.AsMemory(0, 10), TestContext.Current.CancellationToken);

		// Assert
		bytesRead.ShouldBe(10);
		buffer.ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
	}


	[Fact]
	public void ConcatenatedStream_Should_Read_From_Both_Streams_Synchronously()
	{
		// Arrange
		byte[] firstData = [1, 2, 3, 4, 5];
		byte[] secondData = [6, 7, 8, 9, 10];
		using MemoryStream firstStream = new(firstData);
		using MemoryStream secondStream = new(secondData);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act
		byte[] buffer = new byte[10];
		int bytesRead = concatenatedStream.Read(buffer, 0, 10);

		// Assert
		bytesRead.ShouldBe(10);
		buffer.ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
	}

	[Fact]
	public void ConcatenatedStream_Should_Read_From_Both_Streams_Using_Span()
	{
		// Arrange
		byte[] firstData = [1, 2, 3, 4, 5];
		byte[] secondData = [6, 7, 8, 9, 10];
		using MemoryStream firstStream = new(firstData);
		using MemoryStream secondStream = new(secondData);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act
		Span<byte> buffer = stackalloc byte[10];
		int bytesRead = concatenatedStream.Read(buffer);

		// Assert
		bytesRead.ShouldBe(10);
		buffer.ToArray().ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
	}

	[Fact]
	public void ConcatenatedStream_Properties_Should_Have_Expected_Values()
	{
		// Arrange
		using MemoryStream firstStream = new([1, 2, 3]);
		using MemoryStream secondStream = new([4, 5, 6]);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Assert
		concatenatedStream.CanRead.ShouldBeTrue();
		concatenatedStream.CanSeek.ShouldBeFalse();
		concatenatedStream.CanWrite.ShouldBeFalse();
	}

	[Fact]
	public void ConcatenatedStream_Flush_Should_Not_Throw()
	{
		// Arrange
		using MemoryStream firstStream = new([1, 2, 3]);
		using MemoryStream secondStream = new([4, 5, 6]);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act & Assert
		Should.NotThrow(concatenatedStream.Flush);
	}

	[Fact]
	public void ConcatenatedStream_Length_Should_Throw_NotSupportedException()
	{
		// Arrange
		using MemoryStream firstStream = new([1, 2, 3]);
		using MemoryStream secondStream = new([4, 5, 6]);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act & Assert
		Should.Throw<NotSupportedException>(() => _ = concatenatedStream.Length);
	}

	[Fact]
	public void ConcatenatedStream_Position_Get_Should_Throw_NotSupportedException()
	{
		// Arrange
		using MemoryStream firstStream = new([1, 2, 3]);
		using MemoryStream secondStream = new([4, 5, 6]);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act & Assert
		Should.Throw<NotSupportedException>(() => _ = concatenatedStream.Position);
	}

	[Fact]
	public void ConcatenatedStream_Position_Set_Should_Throw_NotSupportedException()
	{
		// Arrange
		using MemoryStream firstStream = new([1, 2, 3]);
		using MemoryStream secondStream = new([4, 5, 6]);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act & Assert
		Should.Throw<NotSupportedException>(() => concatenatedStream.Position = 0);
	}

	[Fact]
	public void ConcatenatedStream_Seek_Should_Throw_NotSupportedException()
	{
		// Arrange
		using MemoryStream firstStream = new([1, 2, 3]);
		using MemoryStream secondStream = new([4, 5, 6]);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act & Assert
		Should.Throw<NotSupportedException>(() => concatenatedStream.Seek(0, SeekOrigin.Begin));
	}

	[Fact]
	public void ConcatenatedStream_SetLength_Should_Throw_NotSupportedException()
	{
		// Arrange
		using MemoryStream firstStream = new([1, 2, 3]);
		using MemoryStream secondStream = new([4, 5, 6]);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act & Assert
		Should.Throw<NotSupportedException>(() => concatenatedStream.SetLength(10));
	}

	[Fact]
	public void ConcatenatedStream_Write_Should_Throw_NotSupportedException()
	{
		// Arrange
		using MemoryStream firstStream = new([1, 2, 3]);
		using MemoryStream secondStream = new([4, 5, 6]);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);
		byte[] buffer = [7, 8, 9];

		// Act & Assert
		Should.Throw<NotSupportedException>(() => concatenatedStream.Write(buffer, 0, buffer.Length));
	}

	// Test for compress/decompress with leaveOpen parameter
	[Theory]
	[InlineData(ECompressionType.Gzip, true)]
	[InlineData(ECompressionType.Gzip, false)]
	[InlineData(ECompressionType.Deflate, true)]
	[InlineData(ECompressionType.Deflate, false)]
	public async Task CompressStream_Should_Respect_LeaveOpen_Parameter(ECompressionType compressionType, bool leaveOpen)
	{
		// Arrange
		byte[] originalData = smallData;
		MemoryStream uncompressedStream = new(originalData);

		// Act
		Stream compressionStream = uncompressedStream.Compress(compressionType, leaveOpen: leaveOpen);
		await compressionStream.DisposeAsync();

		// Assert
		if (leaveOpen)
		{
			uncompressedStream.CanRead.ShouldBeTrue();
		}
		else
		{
			Should.Throw<ObjectDisposedException>(() => uncompressedStream.ReadByte());
		}

		await uncompressedStream.DisposeAsync();
	}

	[Theory]
	[InlineData(ECompressionType.Gzip, true)]
	[InlineData(ECompressionType.Gzip, false)]
	[InlineData(ECompressionType.Deflate, true)]
	[InlineData(ECompressionType.Deflate, false)]
	public async Task DecompressStream_Should_Respect_LeaveOpen_Parameter(ECompressionType compressionType, bool leaveOpen)
	{
		// Arrange
		byte[] originalData = smallData;
		byte[] compressedData = await originalData.Compress(compressionType, cancellationToken: TestContext.Current.CancellationToken);
		MemoryStream compressedStream = new(compressedData);

		// Act
		Stream decompressionStream = compressedStream.Decompress(compressionType, leaveOpen: leaveOpen);
		await decompressionStream.DisposeAsync();

		// Assert
		if (leaveOpen)
		{
			compressedStream.CanRead.ShouldBeTrue();
		}
		else
		{
			Should.Throw<ObjectDisposedException>(() => compressedStream.ReadByte());
		}

		await compressedStream.DisposeAsync();
	}
}

// Helper class to create a non-seekable stream
internal class NonSeekableStream(byte[] data) : Stream
{
	private readonly MemoryStream innerStream = new(data);

	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => false;
	public override long Length => throw new NotSupportedException();
	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public override void Flush()
	{
		innerStream.Flush();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return innerStream.Read(buffer, offset, count);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return innerStream.ReadAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		return innerStream.ReadAsync(buffer, cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			innerStream?.Dispose();
		}
		base.Dispose(disposing);
	}
}
