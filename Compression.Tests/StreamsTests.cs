﻿using static CommonNetFuncs.Compression.Streams;

namespace Compression.Tests;

public sealed class StreamsTests
{
    private readonly Fixture _fixture;

    public StreamsTests()
    {
        _fixture = new Fixture();
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
        byte[] uncompressedData = _fixture.CreateMany<byte>(100).ToArray();
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
        byte[] uncompressedData = _fixture.CreateMany<byte>(100).ToArray();
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
        byte[] originalData = _fixture.CreateMany<byte>(100).ToArray();
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
        byte[] originalData = _fixture.CreateMany<byte>(100).ToArray();
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
        byte[] originalData = _fixture.CreateMany<byte>(100).ToArray();
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
    [InlineData(ECompressionType.Brotli)]
    [InlineData(ECompressionType.Gzip)]
    [InlineData(ECompressionType.Deflate)]
    [InlineData(ECompressionType.ZLib)]
    public async Task Compress_Should_Compress_Byte_Array(ECompressionType compressionType)
    {
        // Arrange
        byte[] originalData = _fixture.CreateMany<byte>(100).ToArray();

        // Act
        byte[] compressedData = await originalData.Compress(compressionType);

        // Assert
        compressedData.Length.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(ECompressionType.Brotli)]
    [InlineData(ECompressionType.Gzip)]
    [InlineData(ECompressionType.Deflate)]
    [InlineData(ECompressionType.ZLib)]
    public async Task Decompress_Should_Decompress_Byte_Array(ECompressionType compressionType)
    {
        // Arrange
        byte[] originalData = _fixture.CreateMany<byte>(100).ToArray();
        byte[] compressedData = await originalData.Compress(compressionType);

        // Act
        byte[] decompressedData = await compressedData.Decompress(compressionType);

        // Assert
        decompressedData.ShouldBe(originalData);
    }
}
