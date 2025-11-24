﻿using System.Buffers;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class StreamsTests
{
    private readonly Fixture _fixture = new();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4096)]
    [InlineData(10000)]
    public async Task ReadStreamAsync_ReadsAllBytes(int length)
    {
        // Arrange
        byte[] data = _fixture.CreateMany<byte>(length).ToArray();
        await using MemoryStream stream = new(data);

        // Act
        byte[] result = await stream.ReadStreamAsync();

        // Assert
        result.ShouldBe(data);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(512)]
    [InlineData(4096)]
    public async Task ReadStreamAsync_RespectsBufferSize(int bufferSize)
    {
        // Arrange
        byte[] data = _fixture.CreateMany<byte>(bufferSize * 2).ToArray();
        await using MemoryStream stream = new(data);

        // Act
        byte[] result = await stream.ReadStreamAsync(bufferSize);

        // Assert
        result.ShouldBe(data);
    }

    [Fact]
    public async Task ReadStreamAsync_ReturnsEmptyArrayForEmptyStream()
    {
        // Arrange
        await using MemoryStream stream = new();

        // Act
        byte[] result = await stream.ReadStreamAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task WriteStreamToStream_MemoryStream_CopiesData()
    {
        // Arrange
        byte[] data = _fixture.CreateMany<byte>(1024).ToArray();
        await using MemoryStream source = new(data);
        await using MemoryStream target = new();

        // Act
        await target.WriteStreamToStream(source);

        // Assert
        target.ToArray().ShouldBe(data);
        target.Position.ShouldBe(0);
    }

    [Fact]
    public async Task WriteStreamToStream_Stream_CopiesData()
    {
        // Arrange
        byte[] data = _fixture.CreateMany<byte>(2048).ToArray();
        await using MemoryStream source = new(data);
        await using MemoryStream target = new();

        // Act
        await target.WriteStreamToStream(source);

        // Assert
        target.ToArray().ShouldBe(data);
        target.Position.ShouldBe(0);
    }

    [Fact]
    public async Task WriteStreamToStream_Stream_ResetsSourcePosition()
    {
        // Arrange
        byte[] data = _fixture.CreateMany<byte>(128).ToArray();
        await using MemoryStream source = new(data);
        await using MemoryStream target = new();

        // Move position to end
        source.Position = source.Length;

        // Act
        await target.WriteStreamToStream(source);

        // Assert
        target.ToArray().ShouldBe(data);
        target.Position.ShouldBe(0);
    }

    [Fact]
    public async Task WriteStreamToStream_Copies_All_Data()
    {
        // Arrange
        await using FileStream source = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
        await using MemoryStream target = new();
        byte[] data = await source.ReadStreamAsync();
        source.Position = 0;

        // Act
        await target.WriteStreamToStream(source);

        // Assert
        target.ToArray().ShouldBe(data);
        target.Position.ShouldBe(0);
        source.Position.ShouldBe(0); // Source is reset to 0 by the method
    }

    [Fact]
    public async Task WriteStreamToStream_Respects_CancellationToken()
    {
        // Arrange
        await using ControllableFileStream source = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
        await using MemoryStream target = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await target.WriteStreamToStream(source, cts.Token));
    }

    [Fact]
    public async Task WriteStreamToStream_Throws_If_Source_Disposed()
    {
        // Arrange
        await using ControllableFileStream source = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
        await using MemoryStream target = new();
        source.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(async () => await target.WriteStreamToStream(source));
    }

    [Fact]
    public async Task WriteStreamToStream_Throws_If_Target_Disposed()
    {
        // Arrange
        await using ControllableFileStream source = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
        await using MemoryStream target = new();
        target.Dispose();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await target.WriteStreamToStream(source));
    }

    [Fact]
    public async Task WriteStreamToStream_Leaves_Target_At_Position_Zero_And_Flushed()
    {
        // Arrange
        await using FileStream source = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
        await using MemoryStream target = new();
        byte[] data = await source.ReadStreamAsync();
        source.Position = 0;

        // Act
        await target.WriteStreamToStream(source);

        // Assert
        target.Position.ShouldBe(0);
        target.ToArray().ShouldBe(data);
    }

    [Fact]
    public async Task WriteStreamToStream_Source_Position_Is_Reset()
    {
        // Arrange
        await using FileStream source = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
        await using MemoryStream target = new();
        byte[] data = await source.ReadStreamAsync();
        source.Position = 0;

        // Move source position to end
        source.Position = source.Length;

        // Act
        await target.WriteStreamToStream(source);

        // Assert
        source.Position.ShouldBe(0);
        target.ToArray().ShouldBe(data);
    }

    [Theory]
    [InlineData(false, true, true, true)]  // source: !CanRead
    [InlineData(true, false, true, true)]  // source: !CanSeek
    [InlineData(true, true, false, true)]  // target: !CanSeek
    [InlineData(true, true, true, false)]  // target: !CanWrite
    public async Task WriteStreamToStream_Stream_Throws_On_Invalid_Capabilities(bool sourceCanRead, bool sourceCanSeek, bool targetCanSeek, bool targetCanWrite)
    {
        // Arrange
        string tempSource = Path.GetTempFileName();
        string tempTarget = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempSource, new byte[] { 1, 2, 3, 4 });
        await File.WriteAllBytesAsync(tempTarget, Array.Empty<byte>());

        Stream source = new ControllableFileStream(tempSource, FileMode.Open, FileAccess.Read, FileShare.None, sourceCanSeek, sourceCanRead);

        Stream target = new ControllableFileStream(tempTarget, FileMode.Open, targetCanWrite ? FileAccess.ReadWrite : FileAccess.Read, FileShare.None, targetCanSeek);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await target.WriteStreamToStream(source));

        await source.DisposeAsync();
        await target.DisposeAsync();
        File.Delete(tempSource);
        File.Delete(tempTarget);
    }

    // Helper for non-seekable stream
    private sealed class ControllableFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, bool canSeek = true, bool canRead = true) : FileStream(path, fileMode, fileAccess, fileShare)
    {
        public override bool CanSeek => canSeek;

        public override bool CanRead => canRead;
    }

    [Theory]
    [InlineData(false, true, true, true)]  // source: !CanRead
    [InlineData(true, false, true, true)]  // source: !CanSeek
    [InlineData(true, true, false, true)]  // target: !CanSeek
    [InlineData(true, true, true, false)]  // target: !CanWrite
    public async Task WriteStreamToStream_MemoryStream_Throws_On_Invalid_Capabilities(bool sourceCanRead, bool sourceCanSeek, bool targetCanSeek, bool targetCanWrite)
    {
        // Arrange
        byte[] data = { 1, 2, 3, 4 };
        ControllableMemoryStream source = new(data, sourceCanRead, true, sourceCanSeek);
        ControllableMemoryStream target = new([], true, targetCanWrite, targetCanSeek);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await target.WriteStreamToStream(source));

        await source.DisposeAsync();
        await target.DisposeAsync();
    }

    // Helper memory stream
    private sealed class ControllableMemoryStream(byte[] buffer, bool canRead = true, bool canWrite = true, bool canSeek = true) : MemoryStream(buffer, canWrite)
    {
        public override bool CanSeek => canSeek;

        public override bool CanRead => canRead;
    }

    [Fact]
    public async Task WriteStreamToStream_Stream_Resets_Source_Position()
    {
        // Arrange
        string tempSource = Path.GetTempFileName();
        string tempTarget = Path.GetTempFileName();
        byte[] data = { 10, 20, 30, 40 };
        await File.WriteAllBytesAsync(tempSource, data);
        await File.WriteAllBytesAsync(tempTarget, Array.Empty<byte>());

        await using FileStream source = new(tempSource, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        await using FileStream target = new(tempTarget, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        source.Position = source.Length; // Move to end

        // Act
        await target.WriteStreamToStream(source);

        // Assert
        source.Position.ShouldBe(0);
        target.Position.ShouldBe(0);
        (await target.ReadStreamAsync()).ShouldBe(data);

        source.Close();
        target.Close();
        await source.DisposeAsync();
        await target.DisposeAsync();

        File.Delete(tempSource);
        File.Delete(tempTarget);
    }

    [Fact]
    public async Task WriteStreamToStream_MemoryStream_Resets_Source_Position()
    {
        // Arrange
        byte[] data = { 5, 6, 7, 8 };
        MemoryStream source = new(data, true);
        MemoryStream target = new();

        source.Position = source.Length;

        // Act
        await target.WriteStreamToStream(source);

        // Assert
        source.Position.ShouldBe(0);
        target.Position.ShouldBe(0);
        target.ToArray().ShouldBe(data);
    }

    [Fact]
    public void CountingStream_TracksBytesWritten()
    {
        // Arrange
        using MemoryStream inner = new();
        using CountingStream counting = new(inner);

        byte[] buffer = _fixture.CreateMany<byte>(100).ToArray();

        // Act
        counting.Write(buffer, 0, buffer.Length);

        // Assert
        counting.BytesWritten.ShouldBe(buffer.Length);
    }

    [Fact]
    public async Task CountingStream_TracksBytesWrittenAsync()
    {
        // Arrange
        await using MemoryStream inner = new();
        await using CountingStream counting = new(inner);

        byte[] buffer = _fixture.CreateMany<byte>(200).ToArray();

        // Act
        await counting.WriteAsync(buffer, CancellationToken.None);

        // Assert
        counting.BytesWritten.ShouldBe(buffer.Length);
    }

    [Fact]
    public async Task CountingStream_TracksBytesWritten_ValueTask()
    {
        // Arrange
        await using MemoryStream inner = new();
        await using CountingStream counting = new(inner);

        byte[] buffer = _fixture.CreateMany<byte>(300).ToArray();

        // Act
        await counting.WriteAsync(buffer, CancellationToken.None);

        // Assert
        counting.BytesWritten.ShouldBe(buffer.Length);
    }

    [Fact]
    public void CountingStream_ReadWriteSeekFlush_DelegatesToInnerStream()
    {
        // Arrange
        using MemoryStream inner = new();
        using CountingStream counting = new(inner);

        byte[] buffer = _fixture.CreateMany<byte>(50).ToArray();

        // Act
        counting.Write(buffer, 0, buffer.Length);
        counting.Flush();
        counting.Position = 0;
        byte[] readBuffer = new byte[buffer.Length];
        int read = counting.Read(readBuffer, 0, readBuffer.Length);

        // Assert
        read.ShouldBe(buffer.Length);
        readBuffer.ShouldBe(buffer);
    }

    [Fact]
    public async Task CountingStream_CopyToAsync_CopiesData()
    {
        // Arrange
        byte[] data = _fixture.CreateMany<byte>(256).ToArray();
        await using MemoryStream inner = new(data);
        await using CountingStream counting = new(inner);
        await using MemoryStream dest = new();

        // Act
        await counting.CopyToAsync(dest, 128, CancellationToken.None);

        // Assert
        dest.ToArray().ShouldBe(data);
    }

    [Fact]
    public void CountingStream_ThrowsIfDisposed()
    {
        // Arrange
        using MemoryStream inner = new();
        CountingStream counting = new(inner);
        counting.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => counting.Write(new byte[1], 0, 1));
        Should.Throw<ObjectDisposedException>(() => counting.Read(new byte[1], 0, 1));
        Should.Throw<ObjectDisposedException>(() => counting.Flush());
    }

    [Fact]
    public async Task CountingStream_DisposeAsync_SetsDisposed()
    {
        // Arrange
        await using MemoryStream inner = new();
        CountingStream counting = new(inner);

        // Act
        await counting.DisposeAsync();

        // Assert
        Should.Throw<ObjectDisposedException>(() => counting.Write(new byte[1], 0, 1));
    }

    [Theory]
    [InlineData(0, SeekOrigin.Begin)]
    [InlineData(5, SeekOrigin.Begin)]
    [InlineData(-2, SeekOrigin.Current)]
    [InlineData(0, SeekOrigin.End)]
    public void CountingStream_Seek_DelegatesToInnerStream(long offset, SeekOrigin origin)
    {
        // Arrange
        byte[] data = _fixture.CreateMany<byte>(10).ToArray();
        using MemoryStream innerStream = new(data);
        using CountingStream countingStream = new(innerStream);

        // Act
        if (offset >= 0)
        {
            long expected = innerStream.Seek(offset, origin);
            long actual = countingStream.Seek(offset, origin);

            // Assert
            actual.ShouldBe(expected);
            countingStream.Position.ShouldBe(innerStream.Position);
        }
        else
        {
            Should.Throw<IOException>(() => innerStream.Seek(offset, origin));
        }
    }

    [Fact]
    public void CountingStream_Seek_ThrowsIfDisposed()
    {
        // Arrange
        using MemoryStream inner = new();
        CountingStream counting = new(inner);
        counting.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => counting.Seek(0, SeekOrigin.Begin));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(20)]
    public void CountingStream_SetLength_DelegatesToInnerStream(long newLength)
    {
        // Arrange
        using MemoryStream inner = new();
        using CountingStream counting = new(inner);

        // Act
        counting.SetLength(newLength);

        // Assert
        inner.Length.ShouldBe(newLength);
        counting.Length.ShouldBe(newLength);
    }

    [Fact]
    public void CountingStream_SetLength_ThrowsIfDisposed()
    {
        // Arrange
        using MemoryStream inner = new();
        CountingStream counting = new(inner);
        counting.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => counting.SetLength(10));
    }
}