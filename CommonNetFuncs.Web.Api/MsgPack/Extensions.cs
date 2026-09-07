using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace CommonNetFuncs.Web.Api.MsgPack;

public static class Extensions
{
	/// <summary>
	/// Registers the DateTime, DateTimeOffset, and TimeSpan JSON converters with ASP.NET Core's
	/// JSON serialization options. Call this in <c>Program.cs</c> during service configuration
	/// when using <see cref="MsgPackSerializerConfig.DateTimesAsStrings"/> to ensure consistent
	/// serialization behavior between MsgPack and JSON.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMsgPackDateTimeJsonConverters(this IServiceCollection services)
	{
		services.Configure<JsonOptions>(options =>
		{
			options.SerializerOptions.Converters.Add(DateTimeUtcJsonConverter.Instance);
			options.SerializerOptions.Converters.Add(TimeSpanJsonConverter.Instance);
			options.SerializerOptions.Converters.Add(DateTimeOffsetJsonConverter.Instance);
		});
		return services;
	}

	/// <summary>
	/// Adds <see cref="MsgPackRequestMiddleware"/> to the pipeline.
	/// Call this before <c>app.MapControllers()</c> / <c>app.MapGroup()</c> so the
	/// middleware runs before parameter binding consumes the request body.
	/// </summary>
	/// <param name="app">The application builder.</param>
	/// <param name="options">
	/// MsgPack serializer options used when converting the request body from MsgPack to JSON.
	/// Defaults to <see cref="MessagePackSerializer.DefaultOptions"/> when <see langword="null"/>.
	/// </param>
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
