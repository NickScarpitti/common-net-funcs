using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CommonNetFuncs.Web.Api.MsgPack;

public static class Extensions
{
	/// <summary>
	/// Adds <see cref="MsgPackRequestMiddleware"/> to the pipeline.
	/// Call this before <c>app.MapControllers()</c> / <c>app.MapGroup()</c> so the
	/// middleware runs before parameter binding consumes the request body.
	/// </summary>
	public static IApplicationBuilder UseMsgPackRequestBody(this IApplicationBuilder app, MessagePackSerializerOptions? options = null)
		=> app.UseMiddleware<MsgPackRequestMiddleware>(options ?? MessagePackSerializerOptions.Standard);

	/// <summary>
	/// Attaches <see cref="MsgPackOutputFilter"/> to an endpoint or route group so
	/// that responses are serialized directly to MsgPack when the client's
	/// <c>Accept</c> header requests it, bypassing the STJ serialization step entirely.
	/// </summary>
	/// <param name="builder">The endpoint or group builder to extend.</param>
	/// <param name="options">
	/// MsgPack serializer options.  Defaults to <see cref="MessagePackSerializerOptions.Standard"/>
	/// when <see langword="null"/>.
	/// </param>
	public static TBuilder WithMsgPackOutput<TBuilder>(this TBuilder builder, MessagePackSerializerOptions? options = null) where TBuilder : IEndpointConventionBuilder
		=> builder.AddEndpointFilter(new MsgPackOutputFilter(options ?? MessagePackSerializerOptions.Standard));
}
