﻿namespace CommonNetFuncs.Core;

public static class Streams
{
    /// <summary>
    /// Read a stream into a byte array asynchronously
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="bufferSize">Buffer size to use when reading from the stream</param>
    /// <returns>Byte array containing contents of stream</returns>
    public static async Task<byte[]> ReadStreamAsync(this Stream stream, int bufferSize = 4096)
    {
        int read;
        await using MemoryStream ms = new();
        byte[] buffer = new byte[bufferSize];
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            ms.Write(buffer, 0, read);
        }
        return ms.ToArray();
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
        await tempStream.WriteAsync(sourceStream.ToArray());
        await tempStream.FlushAsync();
        tempStream.Position = 0;
        await tempStream.CopyToAsync(targetStream);
        await tempStream.DisposeAsync();
        await targetStream.FlushAsync();
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
        await tempStream.WriteAsync(await sourceStream.ReadStreamAsync());
        await tempStream.FlushAsync();
        tempStream.Position = 0;
        await tempStream.CopyToAsync(targetStream);
        await tempStream.DisposeAsync();
        await targetStream.FlushAsync();
        targetStream.Position = 0;
    }
}
