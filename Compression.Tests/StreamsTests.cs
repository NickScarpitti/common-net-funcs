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
        MemoryStream uncompressedStream = new(uncompressedData);
        MemoryStream compressedStream = new();

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
        MemoryStream uncompressedStream = new(uncompressedData);
        MemoryStream compressedStream = new();

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
        MemoryStream uncompressedStream = new(originalData);
        MemoryStream compressedStream = new();
        MemoryStream decompressedStream = new();

        await uncompressedStream.CompressStream(compressedStream, compressionType);
        compressedStream.Position = 0;

        // Act
        await compressedStream.DecompressStream(decompressedStream, compressionType);

        // Assert
        decompressedStream.ToArray().ShouldBe(originalData);
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
