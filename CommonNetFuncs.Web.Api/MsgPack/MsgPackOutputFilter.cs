
using CommonNetFuncs.Web.Api.MsgPack;
using MessagePack;
using Microsoft.AspNetCore.Http;

namespace CommonNetFuncs.Web.Api.MsgPack;

/// <summary>
/// Intercepts the handler's return value <em>before</em> System.Text.Json serializes it.
/// When the client's <c>Accept</c> header includes <c>application/x-msgpack</c>, the
/// value is wrapped in a <see cref="DirectMsgPackResult"/> that writes MsgPack directly
/// to the response stream — no JSON intermediate, no response-body buffer.
///
/// <para>
/// Results that do not carry a body (204, 404 without body, redirects, …) and
/// problem-detail results (<c>application/problem+json</c>) are passed through
/// unchanged so error responses remain in the standard JSON Problem Details format.
/// </para>
/// </summary>
public sealed class MsgPackOutputFilter(MessagePackSerializerOptions options) : IEndpointFilter
{
	private const string MsgPackMime = "application/x-msgpack";

	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		object? result = await next(context);

		// Skip if the client did not request MsgPack.
		// Iterate StringValues directly instead of calling .ToString(), which allocates
		// a joined string when multiple Accept values are present.
		if (!AcceptsMsgPack(context.HttpContext.Request.Headers.Accept))
		{
			return result;
		}

		// Skip results that carry no serializable value (NoContent, NotFound without
		// body, redirects, …).
		if (result is not IValueHttpResult { Value: var value })
		{
			return result;
		}

		// Skip problem-detail results (Content-Type: application/problem+json) so that
		// validation errors and other RFC-9457 responses remain as JSON.
		if (result is IContentTypeHttpResult { ContentType: { } ct } && !ct.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
		{
			return result;
		}

		int statusCode = result is IStatusCodeHttpResult sc ? sc.StatusCode ?? 200 : 200;
		return new DirectMsgPackResult(value, statusCode, options);
	}

	private static bool AcceptsMsgPack(Microsoft.Extensions.Primitives.StringValues accept)
	{
		for (int i = 0; i < accept.Count; i++)
		{
			string? value = accept[i];
			if (value is not null && value.Contains(MsgPackMime, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
