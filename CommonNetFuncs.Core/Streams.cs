using System.Buffers;

namespace CommonNetFuncs.Core;

public static class Streams
{
    /// <summary>
    /// Read a stream into a byte array asynchronously
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="bufferSize">Buffer size to use when reading from the stream</param>
    /// <returns>Byte array containing contents of stream</returns>
    //public static async Task<byte[]> ReadStreamAsync(this Stream stream, int bufferSize = 4096)
    //{
    //    int read;
    //    await using MemoryStream ms = new();
    //    byte[] buffer = new byte[bufferSize];
    //    while ((read = await stream.ReadAsync(buffer)) > 0)
    //    {
    //        await ms.WriteAsync(buffer.AsMemory(0, read));
    //    }
    //    return ms.ToArray();
    //}

    /// <summary>
/// Read a stream into a byte array asynchronously
/// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="bufferSize">Buffer size to use when reading from the stream</param>
    /// <returns>Byte array containing contents of stream</returns>
    public static async ValueTask<byte[]> ReadStreamAsync(this Stream stream, int bufferSize = 4096)
    {
        // If stream length is known, use it to pre-allocate
        MemoryStream ms = stream.CanSeek ? new MemoryStream(capacity: (int)stream.Length) : new MemoryStream();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory()).ConfigureAwait(false)) > 0)
            {
                await ms.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
            }
            return ms.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            await ms.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Copy local MemoryStream to passed in Stream
    /// </summary>
    /// <param name="targetStream">Stream to copy from</param>
    /// <param name="sourceStream">MemoryStream to copy to</param>
    public static async Task WriteStreamToStream(this Stream targetStream, MemoryStream sourceStream)
    {
        await using MemoryStream tempStream = new();

        sourceStream.Position = 0;

        //wb.SaveAs(tempStream, options);
        await tempStream.WriteAsync(sourceStream.ToArray()).ConfigureAwait(false);
        await tempStream.FlushAsync().ConfigureAwait(false);
        tempStream.Position = 0;
        await tempStream.CopyToAsync(targetStream).ConfigureAwait(false);
        await tempStream.DisposeAsync().ConfigureAwait(false);
        await targetStream.FlushAsync().ConfigureAwait(false);
        targetStream.Position = 0;
    }

    /// <summary>
    /// Copy local Stream to passed in Stream
    /// </summary>
    /// <param name="targetStream">Stream to copy from</param>
    /// <param name="sourceStream">Stream to copy to</param>
    public static async Task WriteStreamToStream(this Stream targetStream, Stream sourceStream)
    {
        await using MemoryStream tempStream = new();

        sourceStream.Position = 0;

        //wb.SaveAs(tempStream, options);
        await tempStream.WriteAsync(await sourceStream.ReadStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
        await tempStream.FlushAsync().ConfigureAwait(false);
        tempStream.Position = 0;
        await tempStream.CopyToAsync(targetStream).ConfigureAwait(false);
        await tempStream.DisposeAsync().ConfigureAwait(false);
        await targetStream.FlushAsync().ConfigureAwait(false);
        targetStream.Position = 0;
    }
}

public sealed class CountingStream(Stream innerStream) : Stream
{
    private readonly Stream _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
    private long _bytesWritten;
    private bool _disposed;

    // Consider adding a buffer size constant if you want to optimize buffer operations
    //private const int DefaultBufferSize = 81920; // Same as StreamReader's default

    public long BytesWritten => Interlocked.Read(ref _bytesWritten);

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length => _innerStream.Length;

    public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

    // Implement CopyToAsync for better performance when copying streams
    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        while ((bytesRead = await ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await _innerStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _innerStream.ReadAsync(buffer, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await _innerStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        Interlocked.Add(ref _bytesWritten, count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _innerStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        Interlocked.Add(ref _bytesWritten, buffer.Length);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Don't dispose the inner stream as it's managed by ASP.NET Core
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            // Don't dispose the inner stream as it's managed by ASP.NET Core
            GC.SuppressFinalize(this);
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void ThrowIfDisposed() { ObjectDisposedException.ThrowIf(_disposed, this); }
}
