using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CommonNetFuncs.Web.Api.MsgPack;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Web.Api.Tests;

[MessagePackObject]
public sealed class TestPayload
{
	[Key(0)]
	public string Name { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// MsgPackRequestMiddleware
// ---------------------------------------------------------------------------

public sealed class MsgPackRequestMiddlewareTests
{
	private const string MsgPackMimeType = "application/x-msgpack";
	private const string JsonMimeType = "application/json";

	private static async Task<(HttpClient Client, WebApplication App)> CreateMiddlewareApp(RequestDelegate handler)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseTestServer();
		WebApplication app = builder.Build();
		app.UseMsgPackRequestBody();
		app.Run(handler);
		await app.StartAsync();
		return (app.GetTestClient(), app);
	}

	[Fact]
	public async Task InvokeAsync_NullContentType_CallsNextDirectly()
	{
		// GET request has no Content-Type → not msgpack → next called directly
		string capturedContentType = "NOT_SET";
		(HttpClient client, WebApplication app) = await CreateMiddlewareApp(async ctx =>
		{
			capturedContentType = ctx.Request.ContentType ?? string.Empty;
			ctx.Response.StatusCode = 200;
			await ctx.Response.CompleteAsync();
		});
		await using WebApplication _ = app;

		using HttpResponseMessage response = await client.GetAsync("/");

		capturedContentType.ShouldNotContain(MsgPackMimeType);
	}

	[Fact]
	public async Task InvokeAsync_NonMsgPackContentType_CallsNextDirectly()
	{
		// Non-msgpack Content-Type → next called directly, body/ContentType unchanged
		string capturedContentType = string.Empty;
		(HttpClient client, WebApplication app) = await CreateMiddlewareApp(async ctx =>
		{
			capturedContentType = ctx.Request.ContentType ?? string.Empty;
			ctx.Response.StatusCode = 200;
			await ctx.Response.CompleteAsync();
		});
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Post, "/");
		request.Content = new StringContent("""{"data":"test"}""", Encoding.UTF8, JsonMimeType);
		await client.SendAsync(request);

		capturedContentType.ShouldContain(JsonMimeType);
		capturedContentType.ShouldNotContain(MsgPackMimeType);
	}

	[Fact]
	public async Task InvokeAsync_MsgPackContentType_EmptyBody_ContentTypeUnchanged()
	{
		// MsgPack Content-Type but zero-length body → second early return, ContentType stays msgpack
		string capturedContentType = string.Empty;
		(HttpClient client, WebApplication app) = await CreateMiddlewareApp(async ctx =>
		{
			capturedContentType = ctx.Request.ContentType ?? string.Empty;
			ctx.Response.StatusCode = 200;
			await ctx.Response.CompleteAsync();
		});
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Post, "/");
		ByteArrayContent emptyContent = new([]);
		emptyContent.Headers.ContentType = new MediaTypeHeaderValue(MsgPackMimeType);
		request.Content = emptyContent;
		await client.SendAsync(request);

		capturedContentType.ShouldContain(MsgPackMimeType);
	}

	[Fact]
	public async Task InvokeAsync_MsgPackContentType_ValidBody_ConvertsBodyToJson()
	{
		// MsgPack Content-Type with valid body → body converted to JSON, ContentType set to JSON
		string capturedContentType = string.Empty;
		string capturedBody = string.Empty;
		(HttpClient client, WebApplication app) = await CreateMiddlewareApp(async ctx =>
		{
			capturedContentType = ctx.Request.ContentType ?? string.Empty;
			using StreamReader reader = new(ctx.Request.Body, Encoding.UTF8);
			capturedBody = await reader.ReadToEndAsync();
			ctx.Response.StatusCode = 200;
			await ctx.Response.CompleteAsync();
		});
		await using WebApplication _ = app;

		byte[] msgPackBytes = MessagePackSerializer.ConvertFromJson("""{"name":"msgpack"}""");
		using HttpRequestMessage request = new(HttpMethod.Post, "/");
		ByteArrayContent content = new(msgPackBytes);
		content.Headers.ContentType = new MediaTypeHeaderValue(MsgPackMimeType);
		request.Content = content;
		await client.SendAsync(request);

		capturedContentType.ShouldContain(JsonMimeType);
		capturedBody.ShouldContain("msgpack");
	}

	[Fact]
	public async Task UseMsgPackRequestBody_ExtensionMethod_RegistersMiddleware()
	{
		// Extension method correctly registers MsgPackRequestMiddleware in the pipeline
		string capturedContentType = string.Empty;
		string capturedBody = string.Empty;
		(HttpClient client, WebApplication app) = await CreateMiddlewareApp(async ctx =>
		{
			capturedContentType = ctx.Request.ContentType ?? string.Empty;
			using StreamReader reader = new(ctx.Request.Body, Encoding.UTF8);
			capturedBody = await reader.ReadToEndAsync();
			ctx.Response.StatusCode = 200;
			await ctx.Response.CompleteAsync();
		});
		await using WebApplication _ = app;

		byte[] msgPackBytes = MessagePackSerializer.ConvertFromJson("""{"name":"registered"}""");
		using HttpRequestMessage request = new(HttpMethod.Post, "/");
		ByteArrayContent content = new(msgPackBytes);
		content.Headers.ContentType = new MediaTypeHeaderValue(MsgPackMimeType);
		request.Content = content;
		await client.SendAsync(request);

		capturedContentType.ShouldContain(JsonMimeType);
		capturedBody.ShouldContain("registered");
	}
}

// ---------------------------------------------------------------------------
// MsgPackOutputFilter
// ---------------------------------------------------------------------------

public sealed class MsgPackOutputFilterTests
{
	private const string MsgPackMimeType = "application/x-msgpack";
	private const string JsonMimeType = "application/json";

	private static async Task<WebApplication> BuildRouteApp(Action<WebApplication> configure)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseTestServer();
		builder.Services.AddRouting();
		WebApplication app = builder.Build();
		app.UseRouting();
		configure(app);
		await app.StartAsync();
		return app;
	}

	[Fact]
	public async Task InvokeAsync_NoMsgPackAccept_ReturnsResultUnchanged()
	{
		// Client does not send Accept: application/x-msgpack → filter returns result unchanged (JSON)
		WebApplication app = await BuildRouteApp(a =>
			a.MapGet("/", () => Results.Ok(new TestPayload { Name = "test" })).WithMsgPackOutput());
		await using WebApplication _ = app;

		using HttpClient client = app.GetTestClient();
		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.IsSuccessStatusCode.ShouldBeTrue();
		response.Content.Headers.ContentType?.MediaType.ShouldBe(JsonMimeType);
	}

	[Fact]
	public async Task InvokeAsync_MsgPackAccept_NonValueResult_PassesThroughUnchanged()
	{
		// Result is not IValueHttpResult (NoContent) → filter skips conversion, 204 returned
		WebApplication app = await BuildRouteApp(a =>
			a.MapGet("/", () => Results.NoContent()).WithMsgPackOutput());
		await using WebApplication _ = app;

		using HttpClient client = app.GetTestClient();
		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task InvokeAsync_MsgPackAccept_ProblemDetailResult_PassesThroughUnchanged()
	{
		// Result has ContentType: application/problem+json → filter skips conversion
		WebApplication app = await BuildRouteApp(a =>
			a.MapGet("/", () => Results.Problem("error")).WithMsgPackOutput());
		await using WebApplication _ = app;

		using HttpClient client = app.GetTestClient();
		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.Content.Headers.ContentType?.MediaType.ShouldNotBe(MsgPackMimeType);
	}

	[Fact]
	public async Task InvokeAsync_MsgPackAccept_OkValueResult_ConvertedToMsgPack()
	{
		// Results.Ok with value + MsgPack Accept → filter wraps in DirectMsgPackResult
		WebApplication app = await BuildRouteApp(a =>
			a.MapGet("/", () => Results.Ok(new TestPayload { Name = "hello" })).WithMsgPackOutput());
		await using WebApplication _ = app;

		using HttpClient client = app.GetTestClient();
		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);
		byte[] bytes = await response.Content.ReadAsByteArrayAsync();
		TestPayload? deserialized = MessagePackSerializer.Deserialize<TestPayload>(bytes);
		deserialized?.Name.ShouldBe("hello");
	}

	[Fact]
	public async Task InvokeAsync_MsgPackAccept_CustomOptions_UsedForSerialization()
	{
		// Custom options passed to WithMsgPackOutput are forwarded to the serializer
		MessagePackSerializerOptions customOptions = MessagePackSerializer.DefaultOptions;
		WebApplication app = await BuildRouteApp(a =>
			a.MapGet("/", () => Results.Ok(new TestPayload { Name = "custom" })).WithMsgPackOutput(customOptions));
		await using WebApplication _ = app;

		using HttpClient client = app.GetTestClient();
		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);
		byte[] bytes = await response.Content.ReadAsByteArrayAsync();
		TestPayload? deserialized = MessagePackSerializer.Deserialize<TestPayload>(bytes, customOptions);
		deserialized?.Name.ShouldBe("custom");
	}

	[Fact]
	public async Task WithMsgPackOutput_NullOptions_FallsBackToDefaultOptions()
	{
		// Passing null options → uses MessagePackSerializer.DefaultOptions
		WebApplication app = await BuildRouteApp(a =>
			a.MapGet("/", () => Results.Ok(new TestPayload { Name = "default" })).WithMsgPackOutput(null));
		await using WebApplication _ = app;

		using HttpClient client = app.GetTestClient();
		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);
		byte[] bytes = await response.Content.ReadAsByteArrayAsync();
		TestPayload? result = MessagePackSerializer.Deserialize<TestPayload>(bytes);
		result?.Name.ShouldBe("default");
	}

	[Fact]
	public async Task WithMsgPackOutput_OnRouteGroup_AppliesFilterToAllEndpoints()
	{
		// WithMsgPackOutput on a route group applies the filter to all endpoints in the group
		WebApplication app = await BuildRouteApp(a =>
		{
			RouteGroupBuilder group = a.MapGroup("/items").WithMsgPackOutput();
			group.MapGet("/one", IResult () => Results.Ok(new TestPayload { Name = "one" }));
			group.MapGet("/two", IResult () => Results.Ok(new TestPayload { Name = "two" }));
		});
		await using WebApplication _ = app;

		using HttpClient client = app.GetTestClient();

		using HttpRequestMessage r1 = new(HttpMethod.Get, "/items/one");
		r1.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage resp1 = await client.SendAsync(r1);
		resp1.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);

		using HttpRequestMessage r2 = new(HttpMethod.Get, "/items/two");
		r2.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage resp2 = await client.SendAsync(r2);
		resp2.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);
	}
}

// ---------------------------------------------------------------------------
// DirectMsgPackResult (tested indirectly via MsgPackOutputFilter + WithMsgPackOutput)
// ---------------------------------------------------------------------------

public sealed class DirectMsgPackResultTests
{
	private const string MsgPackMimeType = "application/x-msgpack";

	private static async Task<(HttpClient Client, WebApplication App)> CreateApp(Action<WebApplication> configure)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseTestServer();
		builder.Services.AddRouting();
		WebApplication app = builder.Build();
		app.UseRouting();
		configure(app);
		await app.StartAsync();
		return (app.GetTestClient(), app);
	}

	[Fact]
	public async Task ExecuteAsync_NullValue_WritesStatusCodeOnly_NoBody()
	{
		// Ok<TestPayload?>(null) → DirectMsgPackResult(null) → no body, no content-type set
		(HttpClient client, WebApplication app) = await CreateApp(a =>
			a.MapGet("/", () => Results.Ok<TestPayload?>(null)).WithMsgPackOutput());
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		byte[] body = await response.Content.ReadAsByteArrayAsync();
		body.Length.ShouldBe(0);
	}

	[Fact]
	public async Task ExecuteAsync_NonNullValue_WritesMsgPackBodyWithCorrectContentType()
	{
		// DirectMsgPackResult with value → ContentType=application/x-msgpack, body serialized
		(HttpClient client, WebApplication app) = await CreateApp(a =>
			a.MapGet("/", () => Results.Ok(new TestPayload { Name = "direct" })).WithMsgPackOutput());
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);
		byte[] bytes = await response.Content.ReadAsByteArrayAsync();
		bytes.Length.ShouldBeGreaterThan(0);
		TestPayload? deserialized = MessagePackSerializer.Deserialize<TestPayload>(bytes);
		deserialized?.Name.ShouldBe("direct");
	}

	[Fact]
	public async Task ExecuteAsync_StatusCodeFromWrappedResult_ReflectedInResponse()
	{
		// Status code of the original IResult (Created=201) flows through DirectMsgPackResult
		(HttpClient client, WebApplication app) = await CreateApp(a =>
			a.MapGet("/", () => Results.Created("/entities/1", new TestPayload { Name = "created" })).WithMsgPackOutput());
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.StatusCode.ShouldBe(HttpStatusCode.Created);
		response.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);
	}
}
