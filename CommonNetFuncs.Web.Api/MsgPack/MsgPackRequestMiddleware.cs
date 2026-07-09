using System.Text;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CommonNetFuncs.Web.Api.MsgPack;

/// <summary>
/// Converts a MsgPack-encoded request body to JSON so that the standard
/// <c>[FromBody]</c> parameter binding can deserialise it.  Must be inserted into the
/// pipeline <em>before routing</em>.
/// </summary>
public sealed class MsgPackRequestMiddleware(RequestDelegate next, MessagePackSerializerOptions options)
{
	private const string MsgPackMime = "application/x-msgpack";

	public Task InvokeAsync(HttpContext context)
	{
		string contentType = context.Request.ContentType ?? string.Empty;
		return contentType.Contains(MsgPackMime, StringComparison.OrdinalIgnoreCase)
			? TransformThenContinueAsync(context)
			: next(context);
	}

	private async Task TransformThenContinueAsync(HttpContext context)
	{
		await TransformRequestBodyAsync(context);
		await next(context);
	}

	private async Task TransformRequestBodyAsync(HttpContext context)
	{
		// Pre-size with the declared Content-Length to avoid internal resizing.
		// Plain `using` is correct: MemoryStream does not override DisposeAsync(), so
		// `await using` would create a needless async state-machine transition.
		using MemoryStream ms = new((int)(context.Request.ContentLength ?? 4096));
		await context.Request.Body.CopyToAsync(ms, context.RequestAborted);

		int written = (int)ms.Length;
		if (written == 0)
		{
			return;
		}

		// GetBuffer() exposes the underlying array without copying — one fewer
		// allocation compared to ToArray().
		string json = MessagePackSerializer.ConvertToJson(ms.GetBuffer().AsMemory(0, written), options);
		byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

		context.Request.Body = new MemoryStream(jsonBytes, writable: false);
		context.Request.ContentType = "application/json; charset=utf-8";
		context.Request.ContentLength = jsonBytes.Length;
	}
}