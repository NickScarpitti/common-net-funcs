using System.Buffers;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class StreamsTests
{
	private readonly Fixture fixture = new();

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(4096)]
	[InlineData(10000)]
	public async Task ReadStreamAsync_ReadsAllBytes(int length)
	{
		// Arrange
		byte[] data = fixture.CreateMany<byte>(length).ToArray();
		await using MemoryStream stream = new(data);

		// Act
		byte[] result = await stream.ReadStreamAsync(cancellationToken: TestContext.Current.CancellationToken);

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
		byte[] data = fixture.CreateMany<byte>(bufferSize * 2).ToArray();
		await using MemoryStream stream = new(data);

		// Act
		byte[] result = await stream.ReadStreamAsync(bufferSize, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(data);
	}

	[Fact]
	public async Task ReadStreamAsync_ReturnsEmptyArrayForEmptyStream()
	{
		// Arrange
		await using MemoryStream stream = new();

		// Act
		byte[] result = await stream.ReadStreamAsync(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public async Task ReadStreamAsync_HandlesNonSeekableStream()
	{
		// Arrange
		byte[] data = fixture.CreateMany<byte>(1024).ToArray();
		await using NonSeekableMemoryStream stream = new(data);

		// Act
		byte[] result = await stream.ReadStreamAsync(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(data);
	}

	// Helper for non-seekable stream
	private sealed class NonSeekableMemoryStream(byte[] buffer) : MemoryStream(buffer)
	{
		public override bool CanSeek => false;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin loc)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}
	}

	[Fact]
	public async Task WriteStreamToStream_MemoryStream_CopiesData()
	{
		// Arrange
		byte[] data = fixture.CreateMany<byte>(1024).ToArray();
		await using MemoryStream source = new(data);
		await using MemoryStream target = new();

		// Act
		await target.WriteStreamToStream(source, TestContext.Current.CancellationToken);

		// Assert
		target.ToArray().ShouldBe(data);
		target.Position.ShouldBe(0);
	}

	[Fact]
	public async Task WriteStreamToStream_Stream_CopiesData()
	{
		// Arrange
		byte[] data = fixture.CreateMany<byte>(2048).ToArray();
		await using MemoryStream source = new(data);
		await using MemoryStream target = new();

		// Act
		await target.WriteStreamToStream(source, TestContext.Current.CancellationToken);

		// Assert
		target.ToArray().ShouldBe(data);
		target.Position.ShouldBe(0);
	}

	[Fact]
	public async Task WriteStreamToStream_Stream_ResetsSourcePosition()
	{
		// Arrange
		byte[] data = fixture.CreateMany<byte>(128).ToArray();
		await using MemoryStream source = new(data);
		await using MemoryStream target = new();

		// Move position to end
		source.Position = source.Length;

		// Act
		await target.WriteStreamToStream(source, TestContext.Current.CancellationToken);

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
		byte[] data = await source.ReadStreamAsync(cancellationToken: TestContext.Current.CancellationToken);
		source.Position = 0;

		// Act
		await target.WriteStreamToStream(source, TestContext.Current.CancellationToken);

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
		await cts.CancelAsync();

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(async () => await target.WriteStreamToStream(source, cts.Token));
	}

	[Fact]
	public async Task WriteStreamToStream_Throws_If_Source_Disposed()
	{
		// Arrange
		await using ControllableFileStream source = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
		await using MemoryStream target = new();
		await source.DisposeAsync();

		// Act & Assert
		await Should.ThrowAsync<ObjectDisposedException>(async () => await target.WriteStreamToStream(source));
	}

	[Fact]
	public async Task WriteStreamToStream_Throws_If_Target_Disposed()
	{
		// Arrange
		await using ControllableFileStream source = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
		await using MemoryStream target = new();
		await target.DisposeAsync();

		// Act & Assert
		await Should.ThrowAsync<InvalidOperationException>(async () => await target.WriteStreamToStream(source));
	}

	[Fact]
	public async Task WriteStreamToStream_Leaves_Target_At_Position_Zero_And_Flushed()
	{
		// Arrange
		await using FileStream source = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
		await using MemoryStream target = new();
		byte[] data = await source.ReadStreamAsync(cancellationToken: TestContext.Current.CancellationToken);
		source.Position = 0;

		// Act
		await target.WriteStreamToStream(source, TestContext.Current.CancellationToken);

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
		byte[] data = await source.ReadStreamAsync(cancellationToken: TestContext.Current.CancellationToken);
		source.Position = 0;

		// Move source position to end
		source.Position = source.Length;

		// Act
		await target.WriteStreamToStream(source, TestContext.Current.CancellationToken);

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
		await File.WriteAllBytesAsync(tempSource, new byte[] { 1, 2, 3, 4 }, TestContext.Current.CancellationToken);
		await File.WriteAllBytesAsync(tempTarget, Array.Empty<byte>(), TestContext.Current.CancellationToken);

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
		await File.WriteAllBytesAsync(tempSource, data, TestContext.Current.CancellationToken);
		await File.WriteAllBytesAsync(tempTarget, Array.Empty<byte>(), TestContext.Current.CancellationToken);

		await using FileStream source = new(tempSource, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
		await using FileStream target = new(tempTarget, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

		source.Position = source.Length; // Move to end

		// Act
		await target.WriteStreamToStream(source, TestContext.Current.CancellationToken);

		// Assert
		source.Position.ShouldBe(0);
		target.Position.ShouldBe(0);
		(await target.ReadStreamAsync(cancellationToken: TestContext.Current.CancellationToken)).ShouldBe(data);

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
		await target.WriteStreamToStream(source, TestContext.Current.CancellationToken);

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

		byte[] buffer = fixture.CreateMany<byte>(100).ToArray();

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

		byte[] buffer = fixture.CreateMany<byte>(200).ToArray();

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

		byte[] buffer = fixture.CreateMany<byte>(300).ToArray();

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

		byte[] buffer = fixture.CreateMany<byte>(50).ToArray();

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
		byte[] data = fixture.CreateMany<byte>(256).ToArray();
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
		Should.Throw<ObjectDisposedException>(counting.Flush);
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
		byte[] data = fixture.CreateMany<byte>(10).ToArray();
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

	[Fact]
	public void CountingStream_Constructor_ThrowsOnNullStream()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new CountingStream(null!));
	}

	[Fact]
	public async Task CountingStream_CopyToAsync_ThrowsOnNullDestination()
	{
		// Arrange
		await using MemoryStream inner = new();
		await using CountingStream counting = new(inner);

		// Act & Assert
		await Should.ThrowAsync<ArgumentNullException>(async () => await counting.CopyToAsync(null!, 128, CancellationToken.None));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public async Task CountingStream_CopyToAsync_ThrowsOnInvalidBufferSize(int bufferSize)
	{
		// Arrange
		await using MemoryStream inner = new();
		await using CountingStream counting = new(inner);
		await using MemoryStream dest = new();

		// Act & Assert
		await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await counting.CopyToAsync(dest, bufferSize, CancellationToken.None));
	}

	[Fact]
	public void CountingStream_Finalizer_DisposesCorrectly()
	{
		// Arrange
		using MemoryStream inner = new();
		WeakReference weakRef;

		// Act
		void CreateCountingStream()
		{
			CountingStream counting = new(inner);
			weakRef = new WeakReference(counting);
			// Let counting go out of scope without disposing
		}

		CreateCountingStream();
#pragma warning disable S1215 // "GC.Collect" should not be called
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
#pragma warning restore S1215 // "GC.Collect" should not be called

		// Assert - finalizer should have run
		weakRef.IsAlive.ShouldBeFalse();
	}

	[Fact]
	public async Task CountingStream_ReadAsync_Memory_ReturnsCorrectData()
	{
		// Arrange
		byte[] data = fixture.CreateMany<byte>(100).ToArray();
		await using MemoryStream inner = new(data);
		await using CountingStream counting = new(inner);
		Memory<byte> buffer = new byte[data.Length];

		// Act
		int bytesRead = await counting.ReadAsync(buffer, CancellationToken.None);

		// Assert
		bytesRead.ShouldBe(data.Length);
		buffer.ToArray().ShouldBe(data);
	}

	[Fact]
	public async Task CountingStream_ReadAsync_ThrowsIfDisposed()
	{
		// Arrange
		await using MemoryStream inner = new();
		CountingStream counting = new(inner);
		await counting.DisposeAsync();

		// Act & Assert
#pragma warning disable CA2022 // Avoid inexact read with 'Stream.Read'
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
		await Should.ThrowAsync<ObjectDisposedException>(async () => await counting.ReadAsync(new byte[1], 0, 1));
		await Should.ThrowAsync<ObjectDisposedException>(async () => await counting.ReadAsync(new Memory<byte>(new byte[1]), CancellationToken.None));
#pragma warning restore CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
#pragma warning restore CA2022 // Avoid inexact read with 'Stream.Read'
	}

	[Fact]
	public async Task CountingStream_WriteAsync_Memory_IncrementsCounter()
	{
		// Arrange
		byte[] data = fixture.CreateMany<byte>(150).ToArray();
		await using MemoryStream inner = new();
		await using CountingStream counting = new(inner);
		ReadOnlyMemory<byte> buffer = data;

		// Act
		await counting.WriteAsync(buffer, CancellationToken.None);

		// Assert
		counting.BytesWritten.ShouldBe(data.Length);
		inner.ToArray().ShouldBe(data);
	}

	[Fact]
	public async Task CountingStream_WriteAsync_ThrowsIfDisposed()
	{
		// Arrange
		await using MemoryStream inner = new();
		CountingStream counting = new(inner);
		await counting.DisposeAsync();

		// Act & Assert
		await Should.ThrowAsync<ObjectDisposedException>(async () => await counting.WriteAsync((new byte[1]).AsMemory(0, 1)));
	}

	[Fact]
	public async Task CountingStream_FlushAsync_ThrowsIfDisposed()
	{
		// Arrange
		await using MemoryStream inner = new();
		CountingStream counting = new(inner);
		await counting.DisposeAsync();

		// Act & Assert
		await Should.ThrowAsync<ObjectDisposedException>(async () => await counting.FlushAsync(CancellationToken.None));
	}

	[Fact]
	public void CountingStream_Position_SetterWorks()
	{
		// Arrange
		byte[] data = fixture.CreateMany<byte>(100).ToArray();
		using MemoryStream inner = new(data);
		using CountingStream counting = new(inner);

		// Act
		counting.Position = 50;

		// Assert
		counting.Position.ShouldBe(50);
		inner.Position.ShouldBe(50);
	}

	[Fact]
	public void CountingStream_CanRead_ReturnsInnerStreamCanRead()
	{
		// Arrange
		using MemoryStream inner = new();
		using CountingStream counting = new(inner);

		// Assert
		counting.CanRead.ShouldBe(inner.CanRead);
	}

	[Fact]
	public void CountingStream_CanSeek_ReturnsInnerStreamCanSeek()
	{
		// Arrange
		using MemoryStream inner = new();
		using CountingStream counting = new(inner);

		// Assert
		counting.CanSeek.ShouldBe(inner.CanSeek);
	}

	[Fact]
	public void CountingStream_CanWrite_ReturnsInnerStreamCanWrite()
	{
		// Arrange
		using MemoryStream inner = new();
		using CountingStream counting = new(inner);

		// Assert
		counting.CanWrite.ShouldBe(inner.CanWrite);
	}

	[Fact]
	public void CountingStream_Length_ReturnsInnerStreamLength()
	{
		// Arrange
		byte[] data = fixture.CreateMany<byte>(200).ToArray();
		using MemoryStream inner = new(data);
		using CountingStream counting = new(inner);

		// Assert
		counting.Length.ShouldBe(inner.Length);
	}

	[Fact]
	public void CountingStream_Dispose_CalledMultipleTimes_DoesNotThrow()
	{
		// Arrange
		using MemoryStream inner = new();
		CountingStream counting = new(inner);

		// Act & Assert
		Should.NotThrow(() =>
		{
			counting.Dispose();
			counting.Dispose(); // Should not throw on second dispose
		});
	}

	[Fact]
	public async Task CountingStream_DisposeAsync_CalledMultipleTimes_DoesNotThrow()
	{
		// Arrange
		await using MemoryStream inner = new();
		CountingStream counting = new(inner);

		// Act & Assert
		await Should.NotThrowAsync(async () =>
		{
			await counting.DisposeAsync();
			await counting.DisposeAsync(); // Should not throw on second dispose
		});
	}
}
