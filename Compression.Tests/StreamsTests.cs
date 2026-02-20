using CommonNetFuncs.Compression;
using static CommonNetFuncs.Compression.Streams;

namespace Compression.Tests;

public enum ExceptionConstructorType
{
	Parameterless,
	WithMessage,
	WithMessageAndInnerException
}

public enum ConcatenatedStreamReadMode
{
	Asynchronous,
	Synchronous,
	SpanBased
}

public enum ConcatenatedStreamUnsupportedOperation
{
	Length,
	PositionGet,
	PositionSet,
	Seek,
	SetLength,
	Write
}

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

	public enum CompressStreamExceptionType
	{
		UnreadableStream,
		NoneCompressionType
	}

	[Theory]
	[InlineData(CompressStreamExceptionType.UnreadableStream)]
	[InlineData(CompressStreamExceptionType.NoneCompressionType)]
	public void CompressStream_Should_Throw_Exception_ForInvalidScenarios(CompressStreamExceptionType exceptionType)
	{
		switch (exceptionType)
		{
			case CompressStreamExceptionType.UnreadableStream:
				// Arrange
				using (MemoryStream stream = new([], false))
				{
					stream.Close();

					// Act & Assert
					Should.Throw<NotSupportedException>(() => stream.Compress(ECompressionType.Gzip));
				}
				break;

			case CompressStreamExceptionType.NoneCompressionType:
				// Arrange
				using (MemoryStream stream = new(smallData))
				{
					// Act & Assert
					Should.Throw<NotImplementedException>(() => stream.Compress(ECompressionType.None));
				}
				break;
		}
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

	public enum DecompressStreamExceptionType
	{
		UnreadableStream,
		NoneCompressionType
	}

	[Theory]
	[InlineData(DecompressStreamExceptionType.UnreadableStream)]
	[InlineData(DecompressStreamExceptionType.NoneCompressionType)]
	public void DecompressStream_Should_Throw_Exception_ForInvalidScenarios(DecompressStreamExceptionType exceptionType)
	{
		switch (exceptionType)
		{
			case DecompressStreamExceptionType.UnreadableStream:
				// Arrange
				using (MemoryStream stream = new([], false))
				{
					stream.Close();

					// Act & Assert
					Should.Throw<NotSupportedException>(() => stream.Decompress(ECompressionType.Gzip));
				}
				break;

			case DecompressStreamExceptionType.NoneCompressionType:
				// Arrange
				using (MemoryStream stream = new(smallData))
				{
					// Act & Assert
					Should.Throw<NotImplementedException>(() => stream.Decompress(ECompressionType.None));
				}
				break;
		}
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

	public enum DetectCompressionTypeNoneScenario
	{
		NullStream,
		UnreadableStream,
		NonCompressedData
	}

	[Theory]
	[InlineData(DetectCompressionTypeNoneScenario.NullStream)]
	[InlineData(DetectCompressionTypeNoneScenario.UnreadableStream)]
	[InlineData(DetectCompressionTypeNoneScenario.NonCompressedData)]
	public async Task DetectCompressionType_Should_Return_None_ForVariousScenarios(DetectCompressionTypeNoneScenario scenario)
	{
		switch (scenario)
		{
			case DetectCompressionTypeNoneScenario.NullStream:
				// Act
				ECompressionType detectedType = await Streams.DetectCompressionType(null);

				// Assert
				detectedType.ShouldBe(ECompressionType.None);
				break;

			case DetectCompressionTypeNoneScenario.UnreadableStream:
				// Arrange
				await using (MemoryStream stream = new())
				{
					stream.Close();

					// Act
					ECompressionType detectedType2 = await stream.DetectCompressionType();

					// Assert
					detectedType2.ShouldBe(ECompressionType.None);
				}
				break;

			case DetectCompressionTypeNoneScenario.NonCompressedData:
				// Arrange - Use predictable ASCII text data that won't match compression signatures
				byte[] nonCompressedData = "This is plain text data that should not match any compression signature."u8.ToArray();
				await using (MemoryStream stream = new(nonCompressedData))
				{
					// Act
					ECompressionType detectedType3 = await stream.DetectCompressionType();

					// Assert
					detectedType3.ShouldBe(ECompressionType.None);
					stream.Position.ShouldBe(0);
				}
				break;
		}
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
	public enum IsDeflateCompressedScenario
	{
		DeflateCompressedData,
		NonDeflateData,
		EmptyData
	}

	[Theory]
	[InlineData(IsDeflateCompressedScenario.DeflateCompressedData)]
	[InlineData(IsDeflateCompressedScenario.NonDeflateData)]
	[InlineData(IsDeflateCompressedScenario.EmptyData)]
	public async Task IsDeflateCompressed_Should_Work_ForVariousScenarios(IsDeflateCompressedScenario scenario)
	{
		switch (scenario)
		{
			case IsDeflateCompressedScenario.DeflateCompressedData:
				// Arrange
				byte[] originalData = smallData;
				byte[] compressedData = await originalData.Compress(ECompressionType.Deflate, cancellationToken: TestContext.Current.CancellationToken);

				// Act
				bool isDeflate = await IsDeflateCompressed(compressedData);

				// Assert
				isDeflate.ShouldBeTrue();
				break;

			case IsDeflateCompressedScenario.NonDeflateData:
				// Arrange - Use predictable ASCII text data that's clearly not deflate-compressed
				byte[] nonDeflateData = "Hello, World! This is not compressed data."u8.ToArray();

				// Act
				bool isDeflate2 = await IsDeflateCompressed(nonDeflateData);

				// Assert
				isDeflate2.ShouldBeFalse();
				break;

			case IsDeflateCompressedScenario.EmptyData:
				// Arrange
				byte[] emptyData = [];

				// Act
				bool isDeflate3 = await IsDeflateCompressed(emptyData);

				// Assert
				isDeflate3.ShouldBeFalse();
				break;
		}
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
	public enum CopyWithLimitScenario
	{
		WithinLimit,
		LimitExceeded,
		CancellationToken
	}

	[Theory]
	[InlineData(CopyWithLimitScenario.WithinLimit)]
	[InlineData(CopyWithLimitScenario.LimitExceeded)]
	[InlineData(CopyWithLimitScenario.CancellationToken)]
	public void CopyWithLimit_Should_Handle_Scenarios(CopyWithLimitScenario scenario)
	{
		switch (scenario)
		{
			case CopyWithLimitScenario.WithinLimit:
				// Arrange
				byte[] data = smallData;
				using (MemoryStream source = new(data))
				using (MemoryStream destination = new())
				{
					long maxBytes = data.Length;

					// Act
					source.CopyWithLimit(destination, maxBytes, cancellationToken: TestContext.Current.CancellationToken);

					// Assert
					destination.ToArray().ShouldBe(data);
				}
				break;

			case CopyWithLimitScenario.LimitExceeded:
				// Arrange
				byte[] limitData = smallData;
				using (MemoryStream limitSource = new(limitData))
				using (MemoryStream limitDestination = new())
				{
					long maxBytes = limitData.Length - 10;

					// Act & Assert
					Should.Throw<CompressionLimitExceededException>(() => limitSource.CopyWithLimit(limitDestination, maxBytes));
				}
				break;

			case CopyWithLimitScenario.CancellationToken:
				// Arrange
				byte[] reallyLargeData = fixture.CreateMany<byte>(5 * 1024 * 1024).ToArray(); // 5 MB
				using (MemoryStream cancelSource = new(reallyLargeData))
				using (MemoryStream cancelDestination = new())
				using (CancellationTokenSource cts = new())
				{
					cts.Cancel();
					long maxBytes = reallyLargeData.Length;

					// Act & Assert
					Should.Throw<OperationCanceledException>(() => cancelSource.CopyWithLimit(cancelDestination, maxBytes, cts.Token));
				}
				break;
		}
	}

	// Tests for CopyWithLimitAsync
	public enum CopyWithLimitAsyncScenario
	{
		WithinLimit,
		LimitExceeded
	}

	[Theory]
	[InlineData(CopyWithLimitAsyncScenario.WithinLimit)]
	[InlineData(CopyWithLimitAsyncScenario.LimitExceeded)]
	public async Task CopyWithLimitAsync_Should_Handle_Scenarios(CopyWithLimitAsyncScenario scenario)
	{
		switch (scenario)
		{
			case CopyWithLimitAsyncScenario.WithinLimit:
				// Arrange
				byte[] data = smallData;
				await using (MemoryStream source = new(data))
				await using (MemoryStream destination = new())
				{
					long maxBytes = data.Length;

					// Act
					await source.CopyWithLimitAsync(destination, maxBytes, TestContext.Current.CancellationToken);

					// Assert
					destination.ToArray().ShouldBe(data);
				}
				break;

			case CopyWithLimitAsyncScenario.LimitExceeded:
				// Arrange
				byte[] limitData = smallData;
				await using (MemoryStream limitSource = new(limitData))
				await using (MemoryStream limitDestination = new())
				{
					long maxBytes = limitData.Length - 10;

					// Act & Assert
					await Should.ThrowAsync<CompressionLimitExceededException>(
						limitSource.CopyWithLimitAsync(limitDestination, maxBytes, TestContext.Current.CancellationToken));
				}
				break;
		}
	}

	// Tests for CompressionLimitExceededException
	[Theory]
	[InlineData(ExceptionConstructorType.Parameterless)]
	[InlineData(ExceptionConstructorType.WithMessage)]
	[InlineData(ExceptionConstructorType.WithMessageAndInnerException)]
	public void CompressionLimitExceededException_Constructors_ShouldWork(ExceptionConstructorType constructorType)
	{
		// Arrange, Act & Assert
		switch (constructorType)
		{
			case ExceptionConstructorType.Parameterless:
				CompressionLimitExceededException exception1 = new();
				exception1.ShouldNotBeNull();
				break;

			case ExceptionConstructorType.WithMessage:
				const string message = "Test exception message";
				CompressionLimitExceededException exception2 = new(message);
				exception2.Message.ShouldBe(message);
				break;

			case ExceptionConstructorType.WithMessageAndInnerException:
				const string message2 = "Test exception message";
				Exception innerException = new InvalidOperationException("Inner exception");
				CompressionLimitExceededException exception3 = new(message2, innerException);
				exception3.Message.ShouldBe(message2);
				exception3.InnerException.ShouldBeSameAs(innerException);
				break;
		}
	}

	// Tests for ConcatenatedStream
	[Theory]
	[InlineData(ConcatenatedStreamReadMode.Asynchronous)]
	[InlineData(ConcatenatedStreamReadMode.Synchronous)]
	[InlineData(ConcatenatedStreamReadMode.SpanBased)]
	public async Task ConcatenatedStream_Should_Read_From_Both_Streams(ConcatenatedStreamReadMode readMode)
	{
		// Arrange
		byte[] firstData = [1, 2, 3, 4, 5];
		byte[] secondData = [6, 7, 8, 9, 10];
		int bytesRead;

		// Act & Assert
		switch (readMode)
		{
			case ConcatenatedStreamReadMode.Asynchronous:
				await using (MemoryStream firstStream = new(firstData))
				await using (MemoryStream secondStream = new(secondData))
				await using (ConcatenatedStream concatenatedStream = new(firstStream, secondStream))
				{
					byte[] buffer = new byte[10];
					bytesRead = await concatenatedStream.ReadAsync(buffer.AsMemory(0, 10), TestContext.Current.CancellationToken);
					bytesRead.ShouldBe(10);
					buffer.ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
				}
				break;

			case ConcatenatedStreamReadMode.Synchronous:
				using (MemoryStream firstStream = new(firstData))
				using (MemoryStream secondStream = new(secondData))
				using (ConcatenatedStream concatenatedStream = new(firstStream, secondStream))
				{
					byte[] buffer = new byte[10];
					bytesRead = concatenatedStream.Read(buffer, 0, 10);
					bytesRead.ShouldBe(10);
					buffer.ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
				}
				break;

			case ConcatenatedStreamReadMode.SpanBased:
				using (MemoryStream firstStream = new(firstData))
				using (MemoryStream secondStream = new(secondData))
				using (ConcatenatedStream concatenatedStream = new(firstStream, secondStream))
				{
					Span<byte> buffer = stackalloc byte[10];
					bytesRead = concatenatedStream.Read(buffer);
					bytesRead.ShouldBe(10);
					buffer.ToArray().ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
				}
				break;
		}
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

	[Theory]
	[InlineData(ConcatenatedStreamUnsupportedOperation.Length)]
	[InlineData(ConcatenatedStreamUnsupportedOperation.PositionGet)]
	[InlineData(ConcatenatedStreamUnsupportedOperation.PositionSet)]
	[InlineData(ConcatenatedStreamUnsupportedOperation.Seek)]
	[InlineData(ConcatenatedStreamUnsupportedOperation.SetLength)]
	[InlineData(ConcatenatedStreamUnsupportedOperation.Write)]
	public void ConcatenatedStream_UnsupportedOperations_ShouldThrowNotSupportedException(ConcatenatedStreamUnsupportedOperation operation)
	{
		// Arrange
		using MemoryStream firstStream = new([1, 2, 3]);
		using MemoryStream secondStream = new([4, 5, 6]);
		using ConcatenatedStream concatenatedStream = new(firstStream, secondStream);

		// Act & Assert
		switch (operation)
		{
			case ConcatenatedStreamUnsupportedOperation.Length:
				Should.Throw<NotSupportedException>(() => _ = concatenatedStream.Length);
				break;
			case ConcatenatedStreamUnsupportedOperation.PositionGet:
				Should.Throw<NotSupportedException>(() => _ = concatenatedStream.Position);
				break;
			case ConcatenatedStreamUnsupportedOperation.PositionSet:
				Should.Throw<NotSupportedException>(() => concatenatedStream.Position = 0);
				break;
			case ConcatenatedStreamUnsupportedOperation.Seek:
				Should.Throw<NotSupportedException>(() => concatenatedStream.Seek(0, SeekOrigin.Begin));
				break;
			case ConcatenatedStreamUnsupportedOperation.SetLength:
				Should.Throw<NotSupportedException>(() => concatenatedStream.SetLength(10));
				break;
			case ConcatenatedStreamUnsupportedOperation.Write:
				byte[] buffer = [7, 8, 9];
				Should.Throw<NotSupportedException>(() => concatenatedStream.Write(buffer, 0, buffer.Length));
				break;
		}
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
