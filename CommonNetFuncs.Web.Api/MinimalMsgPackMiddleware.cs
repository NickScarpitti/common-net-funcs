using System.Text;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CommonNetFuncs.Web.Api;

/// <summary>
/// Bridges the gap between the MVC formatter pipeline and minimal API endpoints for
/// MessagePack-encoded request and response bodies.
///
/// Request: If Content-Type is application/x-msgpack the body is converted to JSON so
/// that the standard [FromBody] binding in minimal APIs works transparently.
///
/// Response: If the Accept header contains application/x-msgpack and the endpoint returns
/// a JSON body, the response is converted back to MessagePack before reaching the client.
///
/// Note: MemoryPack request bodies cannot be decoded generically in middleware because
/// its binary format is not self-describing (field names are not stored in the wire
/// format). MemoryPack is still supported on controller endpoints via
/// MemoryPackInputFormatter / MemoryPackOutputFormatter.
/// </summary>
public sealed class MinimalMsgPackMiddleware(RequestDelegate next)
{
	private const string MsgPackMimeType = "application/x-msgpack";
	private const string JsonMimeType = "application/json";

	public async Task InvokeAsync(HttpContext context)
	{
		await TransformRequestBodyAsync(context);

		string acceptHeader = context.Request.Headers.Accept.ToString();
		if (acceptHeader.Contains(MsgPackMimeType, StringComparison.OrdinalIgnoreCase))
		{
			await TransformResponseToMsgPackAsync(context);
		}
		else
		{
			await next(context);
		}
	}

	/// <summary>
	/// If the request body is MessagePack, deserialise it and replace the body stream
	/// with UTF-8 JSON so that minimal API [FromBody] binding can read it.
	/// </summary>
	private static async Task TransformRequestBodyAsync(HttpContext context)
	{
		string contentType = context.Request.ContentType ?? string.Empty;
		if (!contentType.Contains(MsgPackMimeType, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		using MemoryStream buffer = new();
		await context.Request.Body.CopyToAsync(buffer);
		byte[] msgPackBytes = buffer.ToArray();

		if (msgPackBytes.Length == 0)
		{
			return;
		}

		// ConvertToJson works on raw MessagePack bytes without needing type information.
		string json = MessagePackSerializer.ConvertToJson(msgPackBytes);
		byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

		context.Request.Body = new MemoryStream(jsonBytes);
		context.Request.ContentType = "application/json; charset=utf-8";
		context.Request.ContentLength = jsonBytes.Length;
	}

	/// <summary>
	/// Capture the response body, and if the endpoint returned JSON, convert it to
	/// MessagePack before forwarding to the client.
	/// </summary>
	private async Task TransformResponseToMsgPackAsync(HttpContext context)
	{
		Stream originalBody = context.Response.Body;
		using MemoryStream responseBuffer = new();
		context.Response.Body = responseBuffer;

		try
		{
			await next(context);
		}
		finally
		{
			// Restore the real body stream before any writes so headers go out correctly.
			context.Response.Body = originalBody;

			bool isJsonResponse = responseBuffer.Length > 0 && (context.Response.ContentType ?? string.Empty).Contains(JsonMimeType, StringComparison.OrdinalIgnoreCase);

			if (isJsonResponse)
			{
				try
				{
					responseBuffer.Seek(0, SeekOrigin.Begin);
					using StreamReader reader = new(responseBuffer, Encoding.UTF8, leaveOpen: true);
					string json = await reader.ReadToEndAsync();

					// ConvertFromJson works on any well-formed JSON without type information.
					byte[] msgPackBytes = MessagePackSerializer.ConvertFromJson(json);

					context.Response.ContentType = MsgPackMimeType;
					context.Response.ContentLength = msgPackBytes.Length;
					await originalBody.WriteAsync(msgPackBytes);
				}
				catch
				{
					// Conversion failed – fall back to the original JSON response.
					context.Response.ContentType = JsonMimeType;
					responseBuffer.Seek(0, SeekOrigin.Begin);
					await responseBuffer.CopyToAsync(originalBody);
				}
			}
			else if (responseBuffer.Length > 0)
			{
				responseBuffer.Seek(0, SeekOrigin.Begin);
				await responseBuffer.CopyToAsync(originalBody);
			}
		}
	}
}

public static class ContentNegotiationMiddlewareExtensions
{
	public static IApplicationBuilder UseContentNegotiationMiddleware(this IApplicationBuilder builder)
			=> builder.UseMiddleware<MinimalMsgPackMiddleware>();
}
