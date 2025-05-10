using CommonNetFuncs.Compression;

namespace Compression.Tests;

public class StreamsTests
{
    private readonly Fixture _fixture;

    public StreamsTests()
    {
        _fixture = new Fixture();
    }

    [Theory]
    [InlineData(Streams.ECompressionType.Brotli)]
    [InlineData(Streams.ECompressionType.Gzip)]
    [InlineData(Streams.ECompressionType.Deflate)]
    [InlineData(Streams.ECompressionType.ZLib)]
    public async Task CompressStream_Should_Compress_Data(Streams.ECompressionType compressionType)
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
    [InlineData(Streams.ECompressionType.Brotli)]
    [InlineData(Streams.ECompressionType.Gzip)]
    [InlineData(Streams.ECompressionType.Deflate)]
    [InlineData(Streams.ECompressionType.ZLib)]
    public void CompressStreamSynchronous_Should_Compress_Data(Streams.ECompressionType compressionType)
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
    [InlineData(Streams.ECompressionType.Brotli)]
    [InlineData(Streams.ECompressionType.Gzip)]
    [InlineData(Streams.ECompressionType.Deflate)]
    [InlineData(Streams.ECompressionType.ZLib)]
    public async Task DecompressStream_Should_Decompress_Data(Streams.ECompressionType compressionType)
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
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DecompressStream_Should_Throw_Error(bool canWriteDecompressedStream, bool canReadCompressedStream)
    {
        // Arrange
        byte[] originalData = _fixture.CreateMany<byte>(100).ToArray();
        MemoryStream uncompressedStream = new(originalData);
        MemoryStream compressedStream = new();

        MemoryStream decompressedStream = !canWriteDecompressedStream ? new([], false): new();

        await uncompressedStream.CompressStream(compressedStream, Streams.ECompressionType.Deflate);
        compressedStream.Position = 0;

        if (!canReadCompressedStream)
        {
            compressedStream.Close();
        }

        // Act & Assert
        if (!canWriteDecompressedStream || !canReadCompressedStream)
        {
            await Should.ThrowAsync<NotSupportedException>(async () => await compressedStream.DecompressStream(decompressedStream, Streams.ECompressionType.Deflate));
        }
        else
        {
            await Should.NotThrowAsync(compressedStream.DecompressStream(decompressedStream, Streams.ECompressionType.Deflate));
            decompressedStream.ToArray().ShouldBe(originalData);
        }
    }

    [Theory]
    [InlineData(Streams.ECompressionType.Brotli)]
    [InlineData(Streams.ECompressionType.Gzip)]
    [InlineData(Streams.ECompressionType.Deflate)]
    [InlineData(Streams.ECompressionType.ZLib)]
    public void DecompressStreamSynchronous_Should_Decompress_Data(Streams.ECompressionType compressionType)
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
    [InlineData(Streams.ECompressionType.Brotli)]
    [InlineData(Streams.ECompressionType.Gzip)]
    [InlineData(Streams.ECompressionType.Deflate)]
    [InlineData(Streams.ECompressionType.ZLib)]
    public async Task Compress_Should_Compress_Byte_Array(Streams.ECompressionType compressionType)
    {
        // Arrange
        byte[] originalData = _fixture.CreateMany<byte>(100).ToArray();

        // Act
        byte[] compressedData = await originalData.Compress(compressionType);

        // Assert
        compressedData.Length.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(Streams.ECompressionType.Brotli)]
    [InlineData(Streams.ECompressionType.Gzip)]
    [InlineData(Streams.ECompressionType.Deflate)]
    [InlineData(Streams.ECompressionType.ZLib)]
    public async Task Decompress_Should_Decompress_Byte_Array(Streams.ECompressionType compressionType)
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
