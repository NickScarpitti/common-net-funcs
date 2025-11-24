using CommonNetFuncs.Compression;
using static CommonNetFuncs.Compression.Streams;

namespace Compression.Tests;

public sealed class StreamsTests
{
	private readonly Fixture _fixture;

	private readonly byte[] _smallData;
	private readonly byte[] _largeData;

	public StreamsTests()
	{
		_fixture = new Fixture();
		_smallData = _fixture.CreateMany<byte>(100).ToArray();
		_largeData = _fixture.CreateMany<byte>((1024 * 1024) + 177).ToArray();
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public async Task CompressStream_Should_Compress_Data(ECompressionType compressionType)
	{
		// Arrange
		byte[] uncompressedData = _fixture.CreateMany<byte>(100).ToArray();
		await using MemoryStream uncompressedStream = new(uncompressedData);
		await using MemoryStream compressedStream = new();

		// Act
		await uncompressedStream.CompressStream(compressedStream, compressionType);

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
		byte[] uncompressedData = _smallData;
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
				await Should.ThrowAsync<NotSupportedException>(uncompressedStream.CompressStream(compressedStream, ECompressionType.Deflate));
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
				await Should.NotThrowAsync(uncompressedStream.CompressStream(compressedStream, ECompressionType.Deflate));
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
		byte[] uncompressedData = _smallData;
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
		byte[] originalData = _smallData;
		await using MemoryStream uncompressedStream = new(originalData);
		await using MemoryStream compressedStream = new();
		await using MemoryStream decompressedStream = new();

		await uncompressedStream.CompressStream(compressedStream, compressionType);
		compressedStream.Position = 0;

		// Act
		await compressedStream.DecompressStream(decompressedStream, compressionType);

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
		byte[] originalData = _smallData;
		await using MemoryStream uncompressedStream = new(originalData);
		await using MemoryStream compressedStream = new();

		await using MemoryStream decompressedStream = (!canWriteDecompressedStream) ? new([], false) : new();

		await uncompressedStream.CompressStream(compressedStream, ECompressionType.Deflate);
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
				await Should.ThrowAsync<NotSupportedException>(compressedStream.DecompressStream(decompressedStream, ECompressionType.Deflate));
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
				await Should.NotThrowAsync(compressedStream.DecompressStream(decompressedStream, ECompressionType.Deflate));
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
		byte[] originalData = _smallData;
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
	public async Task Compress_Should_Compress_Byte_Array(ECompressionType compressionType, bool largeData)
	{
		// Arrange
		byte[] originalData = largeData ? _largeData : _smallData;

		// Act
		byte[] compressedData = await originalData.Compress(compressionType);

		// Assert
		compressedData.Length.ShouldBeGreaterThan(0);
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
	public async Task Decompress_Should_Decompress_Byte_Array(ECompressionType compressionType, bool largeData)
	{
		// Arrange
		byte[] originalData = largeData ? _largeData : _smallData; //_fixture.CreateMany<byte>(arraySize).ToArray();
		byte[] compressedData = await originalData.Compress(compressionType);

		// Act
		byte[] decompressedData = await compressedData.Decompress(compressionType);

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
}
