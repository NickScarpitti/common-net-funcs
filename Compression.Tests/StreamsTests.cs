using System.IO.Compression;
using CommonNetFuncs.Compression;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Core.Random;
using static Xunit.TestContext;

namespace Compression.Tests;

public sealed class StreamsTests
{
	private readonly byte[] smallData;
	private readonly byte[] largeData;

	public StreamsTests()
	{
		smallData = GetRandomBytes(100);
		largeData = GetRandomBytes((1024 * 1024) + 177);
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public async Task CompressStreamAsync_Should_Compress_Data(ECompressionType compressionType)
	{
		// Arrange
		byte[] uncompressedData = smallData;
		await using MemoryStream uncompressedStream = new(uncompressedData);
		await using MemoryStream compressedStream = new();

		// Act
		await uncompressedStream.CompressStreamAsync(compressedStream, compressionType, cancellationToken: Current.CancellationToken);

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
				await Should.ThrowAsync<NotSupportedException>(uncompressedStream.CompressStreamAsync(compressedStream, ECompressionType.Deflate, cancellationToken: Current.CancellationToken));
			}
			else
			{
				Should.Throw<NotSupportedException>(() => uncompressedStream.CompressStream(compressedStream, ECompressionType.Deflate));
			}
		}
		else
		{
			if (useAsync)
			{
				await Should.NotThrowAsync(uncompressedStream.CompressStreamAsync(compressedStream, ECompressionType.Deflate, cancellationToken: Current.CancellationToken));
			}
			else
			{
				Should.NotThrow(() => uncompressedStream.CompressStream(compressedStream, ECompressionType.Deflate));
			}
			compressedStream.Length.ShouldBeGreaterThan(0);
		}
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public void CompressStream_Should_Compress_Data(ECompressionType compressionType)
	{
		// Arrange
		byte[] uncompressedData = smallData;
		using MemoryStream uncompressedStream = new(uncompressedData);
		using MemoryStream compressedStream = new();

		// Act
		uncompressedStream.CompressStream(compressedStream, compressionType);

		// Assert
		compressedStream.Length.ShouldBeGreaterThan(0);
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public async Task DecompressStreamAsync_Should_Decompress_Data(ECompressionType compressionType)
	{
		// Arrange
		byte[] originalData = smallData;
		await using MemoryStream uncompressedStream = new(originalData);
		await using MemoryStream compressedStream = new();
		await using MemoryStream decompressedStream = new();

		await uncompressedStream.CompressStreamAsync(compressedStream, compressionType, cancellationToken: Current.CancellationToken);
		compressedStream.Position = 0;

		// Act
		await compressedStream.DecompressStreamAsync(decompressedStream, compressionType, cancellationToken: Current.CancellationToken);

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

		await uncompressedStream.CompressStreamAsync(compressedStream, ECompressionType.Deflate, cancellationToken: Current.CancellationToken);
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
				await Should.ThrowAsync<NotSupportedException>(compressedStream.DecompressStreamAsync(decompressedStream, ECompressionType.Deflate, cancellationToken: Current.CancellationToken));
			}
			else
			{
				Should.Throw<NotSupportedException>(() => compressedStream.DecompressStream(decompressedStream, ECompressionType.Deflate));
			}
		}
		else
		{
			if (useAsync)
			{
				await Should.NotThrowAsync(compressedStream.DecompressStreamAsync(decompressedStream, ECompressionType.Deflate, cancellationToken: Current.CancellationToken));
			}
			else
			{
				Should.NotThrow(() => compressedStream.DecompressStream(decompressedStream, ECompressionType.Deflate));
			}
			decompressedStream.ToArray().ShouldBe(originalData);
		}
	}

	[Theory]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public void DecompressStream_Should_Decompress_Data(ECompressionType compressionType)
	{
		// Arrange
		byte[] originalData = smallData;
		MemoryStream uncompressedStream = new(originalData);
		MemoryStream compressedStream = new();
		MemoryStream decompressedStream = new();

		uncompressedStream.CompressStream(compressedStream, compressionType);
		compressedStream.Position = 0;

		// Act
		compressedStream.DecompressStream(decompressedStream, compressionType);

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
		byte[] compressedData = await originalData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);

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
		byte[] mediumData = GetRandomBytes(dataSize);

		// Act
		byte[] compressedData = await mediumData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);

		// Assert
		compressedData.Length.ShouldBeGreaterThan(0);

		// Verify we can decompress it back
		byte[] decompressedData = await compressedData.DecompressAsync(compressionType, cancellationToken: Current.CancellationToken);
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
		byte[] compressedData = await originalData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);

		// Act
		byte[] decompressedData = await compressedData.DecompressAsync(compressionType, cancellationToken: Current.CancellationToken);

		// Assert
		decompressedData.ShouldBe(originalData);
	}

	// Tests for the synchronous byte[] Compress overload
	[Theory]
	[InlineData(ECompressionType.Brotli, false)]
	[InlineData(ECompressionType.Gzip, false)]
	[InlineData(ECompressionType.Deflate, false)]
	[InlineData(ECompressionType.ZLib, false)]
	[InlineData(ECompressionType.Brotli, true)]
	[InlineData(ECompressionType.Gzip, true)]
	[InlineData(ECompressionType.Deflate, true)]
	[InlineData(ECompressionType.ZLib, true)]
	public void Compress_Should_Compress_Byte_Array_Synchronously(ECompressionType compressionType, bool useLargeData)
	{
		// Arrange
		byte[] originalData = useLargeData ? largeData : smallData;

		// Act
		byte[] compressedData = originalData.Compress(compressionType);

		// Assert
		compressedData.Length.ShouldBeGreaterThan(0);
	}

	[Theory]
	[InlineData(CompressionLevel.NoCompression)]
	[InlineData(CompressionLevel.Fastest)]
	[InlineData(CompressionLevel.Optimal)]
	[InlineData(CompressionLevel.SmallestSize)]
	public void Compress_Should_Respect_CompressionLevel_Synchronously(CompressionLevel compressionLevel)
	{
		// Arrange
		byte[] originalData = smallData;

		// Act
		byte[] compressedData = originalData.Compress(ECompressionType.Gzip, compressionLevel);

		// Assert
		compressedData.Length.ShouldBeGreaterThan(0);
		compressedData.Decompress(ECompressionType.Gzip, cancellationToken: Current.CancellationToken).ShouldBe(originalData);
	}

	[Theory]
	[InlineData(ECompressionType.Gzip, 2000)]
	[InlineData(ECompressionType.Deflate, 5000)]
	[InlineData(ECompressionType.ZLib, 8000)]
	[InlineData(ECompressionType.Gzip, 1024)] // Edge case for <= 1024 branch
	[InlineData(ECompressionType.Deflate, 1025)] // Just above 1024
	[InlineData(ECompressionType.ZLib, 10240)] // Edge case for <= 10240 branch
	[InlineData(ECompressionType.Gzip, 10241)] // Just above 10240
	public void Compress_Should_Handle_Medium_Sized_Data_Synchronously(ECompressionType compressionType, int dataSize)
	{
		// Arrange - Create medium-sized data to hit the <= 10240 branch in Compress method
		byte[] mediumData = GetRandomBytes(dataSize);

		// Act
		byte[] compressedData = mediumData.Compress(compressionType);

		// Assert
		compressedData.Length.ShouldBeGreaterThan(0);

		// Verify we can decompress it back
		byte[] decompressedData = compressedData.Decompress(compressionType, cancellationToken: Current.CancellationToken);
		decompressedData.ShouldBe(mediumData);
	}

	// Tests for the synchronous byte[] Decompress overload
	[Theory]
	[InlineData(ECompressionType.Brotli, false)]
	[InlineData(ECompressionType.Gzip, false)]
	[InlineData(ECompressionType.Deflate, false)]
	[InlineData(ECompressionType.ZLib, false)]
	[InlineData(ECompressionType.Brotli, true)]
	[InlineData(ECompressionType.Gzip, true)]
	[InlineData(ECompressionType.Deflate, true)]
	[InlineData(ECompressionType.ZLib, true)]
	public void Decompress_Should_Decompress_Byte_Array_Synchronously(ECompressionType compressionType, bool useLargeData)
	{
		// Arrange
		byte[] originalData = useLargeData ? largeData : smallData;
		byte[] compressedData = originalData.Compress(compressionType);

		// Act
		byte[] decompressedData = compressedData.Decompress(compressionType, cancellationToken: Current.CancellationToken);

		// Assert
		decompressedData.ShouldBe(originalData);
	}

	[Fact]
	public void Decompress_Should_Throw_When_MaxCompressionRatio_Exceeded_Synchronously()
	{
		// Arrange
		byte[] compressedData = smallData.Compress(ECompressionType.Gzip);

		// Act & Assert
		Should.Throw<CompressionLimitExceededException>(() => compressedData.Decompress(ECompressionType.Gzip, maxCompressionRatio: 0));
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

	// Tests for byte[] Is*Compressed header-signature helpers
	[Theory]
	[InlineData(null, false)]
	[InlineData(new byte[0], false)]
	[InlineData(new byte[] { 0x1F }, false)]
	[InlineData(new byte[] { 0x1F, 0x8B }, true)]
	[InlineData(new byte[] { 0x1F, 0x8B, 0x08, 0x00 }, true)]
	[InlineData(new byte[] { 0x00, 0x00 }, false)]
	public void IsGzipCompressed_ByteArray_Should_Detect_Gzip_Header(byte[]? data, bool expected)
	{
		// Act
		bool result = data.IsGzipCompressed();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(new byte[0], false)]
	[InlineData(new byte[] { 0x78 }, false)]
	[InlineData(new byte[] { 0x78, 0x01 }, true)]
	[InlineData(new byte[] { 0x78, 0x9C }, true)]
	[InlineData(new byte[] { 0x78, 0xDA }, true)]
	[InlineData(new byte[] { 0x78, 0x00 }, false)]
	[InlineData(new byte[] { 0x00, 0x00 }, false)]
	public void IsZLibCompressed_ByteArray_Should_Detect_ZLib_Header(byte[]? data, bool expected)
	{
		// Act
		bool result = data.IsZLibCompressed();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(new byte[0], false)]
	[InlineData(new byte[] { 0xCE, 0xB2, 0xCF }, false)] // Only 3 of the 4 required bytes
	[InlineData(new byte[] { 0xCE, 0xB2, 0xCF, 0x81 }, true)]
	[InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00 }, false)]
	public void IsBrotliCompressed_ByteArray_Should_Detect_Brotli_Header(byte[]? data, bool expected)
	{
		// Act
		bool result = data.IsBrotliCompressed();

		// Assert
		result.ShouldBe(expected);
	}

	// Tests for Stream Is*Compressed header-signature helpers
	[Fact]
	public void IsGzipCompressed_Stream_Should_Return_False_For_Null_Stream()
	{
		Stream? stream = null;
		stream.IsGzipCompressed().ShouldBeFalse();
	}

	[Fact]
	public void IsGzipCompressed_Stream_Should_Return_False_For_Unreadable_Stream()
	{
		// Arrange
		using MemoryStream stream = new();
		stream.Close();

		// Act & Assert
		stream.IsGzipCompressed().ShouldBeFalse();
	}

	[Fact]
	public void IsGzipCompressed_Stream_Should_Return_False_For_Empty_Stream()
	{
		// Arrange
		using MemoryStream stream = new();

		// Act & Assert
		stream.IsGzipCompressed().ShouldBeFalse();
		stream.Position.ShouldBe(0);
	}

	[Theory]
	[InlineData(ECompressionType.Gzip, true)]
	[InlineData(ECompressionType.Deflate, false)]
	public async Task IsGzipCompressed_Stream_Should_Detect_Gzip_For_Seekable_Stream(ECompressionType compressionType, bool expected)
	{
		// Arrange
		byte[] compressedData = await smallData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);
		using MemoryStream stream = new(compressedData);

		// Act
		bool result = stream.IsGzipCompressed();

		// Assert
		result.ShouldBe(expected);
		stream.Position.ShouldBe(0);
	}

	[Fact]
	public void IsGzipCompressed_Stream_Should_Preserve_NonZero_Position_For_Seekable_Stream()
	{
		// Arrange - none of the bytes visible from the current position match the Gzip signature
		using MemoryStream stream = new(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 });
		stream.Position = 2;

		// Act
		bool result = stream.IsGzipCompressed();

		// Assert
		result.ShouldBeFalse();
		stream.Position.ShouldBe(2);
	}

	[Fact]
	public async Task IsGzipCompressed_Stream_Should_Detect_Gzip_For_NonSeekable_Stream_Without_Throwing()
	{
		// Arrange
		byte[] compressedData = await smallData.CompressAsync(ECompressionType.Gzip, cancellationToken: Current.CancellationToken);
		await using NonSeekableStream stream = new(compressedData);

		// Act & Assert
		Should.NotThrow(() => stream.IsGzipCompressed().ShouldBeTrue());
	}

	[Fact]
	public void IsZLibCompressed_Stream_Should_Return_False_For_Null_Stream()
	{
		Stream? stream = null;
		stream.IsZLibCompressed().ShouldBeFalse();
	}

	[Fact]
	public void IsZLibCompressed_Stream_Should_Return_False_For_Unreadable_Stream()
	{
		// Arrange
		using MemoryStream stream = new();
		stream.Close();

		// Act & Assert
		stream.IsZLibCompressed().ShouldBeFalse();
	}

	[Theory]
	[InlineData(ECompressionType.ZLib, true)]
	[InlineData(ECompressionType.Gzip, false)]
	public async Task IsZLibCompressed_Stream_Should_Detect_ZLib_For_Seekable_Stream(ECompressionType compressionType, bool expected)
	{
		// Arrange
		byte[] compressedData = await smallData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);
		using MemoryStream stream = new(compressedData);

		// Act
		bool result = stream.IsZLibCompressed();

		// Assert
		result.ShouldBe(expected);
		stream.Position.ShouldBe(0);
	}

	[Fact]
	public void IsZLibCompressed_Stream_Should_Preserve_NonZero_Position_For_Seekable_Stream()
	{
		// Arrange - none of the bytes visible from the current position match a valid ZLib header
		using MemoryStream stream = new(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 });
		stream.Position = 2;

		// Act
		bool result = stream.IsZLibCompressed();

		// Assert
		result.ShouldBeFalse();
		stream.Position.ShouldBe(2);
	}

	[Fact]
	public async Task IsZLibCompressed_Stream_Should_Detect_ZLib_For_NonSeekable_Stream_Without_Throwing()
	{
		// Arrange
		byte[] compressedData = await smallData.CompressAsync(ECompressionType.ZLib, cancellationToken: Current.CancellationToken);
		await using NonSeekableStream stream = new(compressedData);

		// Act & Assert
		Should.NotThrow(() => stream.IsZLibCompressed().ShouldBeTrue());
	}

	[Fact]
	public void IsBrotliCompressed_Stream_Should_Return_False_For_Null_Stream()
	{
		Stream? stream = null;
		stream.IsBrotliCompressed().ShouldBeFalse();
	}

	[Fact]
	public void IsBrotliCompressed_Stream_Should_Return_False_For_Unreadable_Stream()
	{
		// Arrange
		using MemoryStream stream = new();
		stream.Close();

		// Act & Assert
		stream.IsBrotliCompressed().ShouldBeFalse();
	}

	[Fact]
	public async Task IsBrotliCompressed_Stream_Should_Return_True_For_Data_With_Brotli_Signature()
	{
		// Arrange - .NET's BrotliStream output has no fixed magic number, so the signature is crafted manually here (matches AnalyzeHeader's convention)
		byte[] data = new byte[] { 0xCE, 0xB2, 0xCF, 0x81, 0x00, 0x00 };
		using MemoryStream stream = new(data);

		// Act
		bool result = stream.IsBrotliCompressed();

		// Assert
		result.ShouldBeTrue();
		stream.Position.ShouldBe(0);

		// Sanity check: real Gzip-compressed data should not match the Brotli signature
		byte[] gzipData = await smallData.CompressAsync(ECompressionType.Gzip, cancellationToken: Current.CancellationToken);
		using MemoryStream gzipStream = new(gzipData);
		gzipStream.IsBrotliCompressed().ShouldBeFalse();
	}

	[Fact]
	public void IsBrotliCompressed_Stream_Should_Preserve_NonZero_Position_For_Seekable_Stream()
	{
		// Arrange - none of the bytes visible from the current position match the Brotli signature
		using MemoryStream stream = new(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 });
		stream.Position = 2;

		// Act
		bool result = stream.IsBrotliCompressed();

		// Assert
		result.ShouldBeFalse();
		stream.Position.ShouldBe(2);
	}

	[Fact]
	public void IsBrotliCompressed_Stream_Should_Detect_Brotli_For_NonSeekable_Stream_Without_Throwing()
	{
		// Arrange
		byte[] data = new byte[] { 0xCE, 0xB2, 0xCF, 0x81, 0x00, 0x00 };
		using NonSeekableStream stream = new(data);

		// Act & Assert
		Should.NotThrow(() => stream.IsBrotliCompressed().ShouldBeTrue());
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
		byte[] compressedData = await originalData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);
		await using MemoryStream compressedStream = new(compressedData);

		// Act
		await using Stream decompressionStream = compressedStream.Decompress(compressionType);

		// Assert
		decompressionStream.ShouldNotBeNull();
		decompressionStream.CanRead.ShouldBeTrue();

		// Verify we can read decompressed data
		await using MemoryStream resultStream = new();
		await decompressionStream.CopyToAsync(resultStream, Current.CancellationToken);
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
		byte[] compressedData = await originalData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);
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
		byte[] compressedData = await smallData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);
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
		byte[] compressedData = await originalData.CompressAsync(ECompressionType.Deflate, cancellationToken: Current.CancellationToken);

		// Act
		bool isDeflate = await compressedData.IsDeflateCompressed();

		// Assert
		isDeflate.ShouldBeTrue();
	}

	[Fact]
	public async Task IsDeflateCompressed_Should_Return_False_For_Non_Deflate_Data()
	{
		// Arrange - Use predictable ASCII text data that's clearly not deflate-compressed
		byte[] originalData = "Hello, World! This is not compressed data."u8.ToArray();

		// Act
		bool isDeflate = await originalData.IsDeflateCompressed();

		// Assert
		isDeflate.ShouldBeFalse();
	}

	[Fact]
	public async Task IsDeflateCompressed_Should_Return_False_For_Empty_Data()
	{
		// Arrange
		byte[] emptyData = [];

		// Act
		bool isDeflate = await emptyData.IsDeflateCompressed();

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
		byte[] compressedData = await originalData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);
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
		byte[] compressedData = await originalData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);

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
		await decompressionStream.CopyToAsync(decompressedStream, Current.CancellationToken);
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
		source.CopyWithLimit(destination, maxBytes, cancellationToken: Current.CancellationToken);

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
		byte[] reallyLargeData = GetRandomBytes(5 * 1024 * 1024); // 5 MB
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
		await source.CopyWithLimitAsync(destination, maxBytes, Current.CancellationToken);

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
		await Should.ThrowAsync<CompressionLimitExceededException>(source.CopyWithLimitAsync(destination, maxBytes, Current.CancellationToken));
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
		int bytesRead = await concatenatedStream.ReadAsync(buffer.AsMemory(0, 10), Current.CancellationToken);

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
		byte[] compressedData = await originalData.CompressAsync(compressionType, cancellationToken: Current.CancellationToken);
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
