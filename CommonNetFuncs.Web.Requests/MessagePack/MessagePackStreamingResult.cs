using System.Buffers;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CommonNetFuncs.Web.Requests.MessagePack;

/// <summary>
/// Streams IAsyncEnumerable data as MessagePack chunks to support client-side streaming reconstruction.
/// Each item is serialized individually and written to the stream, allowing MessagePackStreamReader on the client to read them one at a time.
/// </summary>
/// <typeparam name="T">Type of items to stream</typeparam>
/// <param name="data">The async enumerable data to stream</param>
/// <param name="options">Optional MessagePack serializer options</param>
/// <param name="successStatusCode">Status code to return when data is present (default: 200 OK)</param>
/// <param name="emptyStatusCode">Status code to return when no data is present (default: 204 No Content)</param>
/// <remarks>
/// Creates a new MessagePackStreamingResult.
/// </remarks>
public class MessagePackStreamingResult<T>(IAsyncEnumerable<T> data, MessagePackSerializerOptions? options = null, int? successStatusCode = null, int? emptyStatusCode = null) : IActionResult
{
	private readonly IAsyncEnumerable<T> _data = data ?? throw new ArgumentNullException(nameof(data));
	private readonly MessagePackSerializerOptions? _options = options;
	private readonly int? _successStatusCode = successStatusCode;
	private readonly int? _emptyStatusCode = emptyStatusCode;

	public async Task ExecuteResultAsync(ActionContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		HttpResponse response = context.HttpContext.Response;
		response.ContentType = "application/x-msgpack";
		response.StatusCode = _successStatusCode ?? StatusCodes.Status200OK;

		bool hasData = false;

		try
		{
			await foreach (T item in _data.ConfigureAwait(false))
			{
				hasData = true;

				// Serialize each item to a buffer
				ArrayBufferWriter<byte> bufferWriter = new();
				MessagePackSerializer.Serialize(bufferWriter, item, _options ?? MessagePackSerializerOptions.Standard);

				// Write the serialized bytes to the response stream
				await response.Body.WriteAsync(bufferWriter.WrittenMemory, context.HttpContext.RequestAborted).ConfigureAwait(false);
				await response.Body.FlushAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
			}

			if (!hasData)
			{
				response.StatusCode = _emptyStatusCode ?? StatusCodes.Status204NoContent;
			}
		}
		catch (Exception)
		{
			if (!response.HasStarted)
			{
				response.StatusCode = StatusCodes.Status500InternalServerError;
			}
			throw;
		}
	}
}
