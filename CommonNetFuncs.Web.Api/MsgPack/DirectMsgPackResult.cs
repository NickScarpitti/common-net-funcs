using MessagePack;
using Microsoft.AspNetCore.Http;

namespace CommonNetFuncs.Web.Api.MsgPack;


/// <summary>
/// Writes a .NET value to the HTTP response as MessagePack using the value's runtime
/// concrete type, so that type-specific formatters (e.g.
/// <see cref="MsgPackConfig.DateTimesAsStrings"/>) are honoured.
///
/// Writing goes directly to <c>Response.Body</c> (Kestrel's pipe-backed stream) with
/// no intermediate buffer and no JSON round-trip.
/// </summary>
internal sealed class DirectMsgPackResult(object? value, int statusCode, MessagePackSerializerOptions options) : IResult
{
	public async Task ExecuteAsync(HttpContext httpContext)
	{
		httpContext.Response.StatusCode = statusCode;

		if (value is null)
		{
			return;
		}

		httpContext.Response.ContentType = "application/x-msgpack";

		// Serialize the concrete runtime type directly to the Kestrel response pipe.
		// The Type overload resolves formatters the same way the generic overload would,
		// but without requiring the type parameter at compile time.
		// Propagate the cancellation token so that a client disconnect aborts in-flight
		// serialization rather than letting it run to completion.
		await MessagePackSerializer.SerializeAsync(value.GetType(), httpContext.Response.Body, value, options, httpContext.RequestAborted);
	}
}