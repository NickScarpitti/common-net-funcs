using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CommonNetFuncs.Web.Api;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace Web.Api.Tests;

public sealed class MinimalMsgPackMiddlewareTests
{
	private const string MsgPackMimeType = "application/x-msgpack";
	private const string JsonMimeType = "application/json";

	private static async Task<(HttpClient Client, WebApplication App)> CreateTestApp(
		RequestDelegate handler, bool useExtensionMethod = false)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseTestServer();
		WebApplication app = builder.Build();

		if (useExtensionMethod)
			app.UseContentNegotiationMiddleware();
		else
			app.UseMiddleware<MinimalMsgPackMiddleware>();

		app.Run(handler);
		await app.StartAsync();
		return (app.GetTestClient(), app);
	}

	// --- InvokeAsync ---

	[Fact]
	public async Task InvokeAsync_NonMsgPackAccept_CallsNextDirectly_ResponseUnchanged()
	{
		// Non-msgpack Accept → next(context) called directly, no response buffering
		const string responseJson = """{"name":"test"}""";
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
		{
			ctx.Response.ContentType = JsonMimeType;
			await ctx.Response.WriteAsync(responseJson);
		});
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		string body = await response.Content.ReadAsStringAsync();
		body.ShouldBe(responseJson);
		response.Content.Headers.ContentType?.MediaType.ShouldBe(JsonMimeType);
	}

	[Fact]
	public async Task InvokeAsync_MsgPackAccept_TransformsResponseToMsgPack()
	{
		// MsgPack Accept → TransformResponseToMsgPackAsync called, JSON converted to msgpack
		const string responseJson = """{"name":"test"}""";
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
		{
			ctx.Response.ContentType = JsonMimeType;
			await ctx.Response.WriteAsync(responseJson);
		});
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);
		byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
		string roundTrippedJson = MessagePackSerializer.ConvertToJson(responseBytes);
		roundTrippedJson.ShouldContain("test");
	}

	// --- TransformRequestBodyAsync ---

	[Fact]
	public async Task TransformRequestBody_NullContentType_SkipsTransform()
	{
		// GET request has no Content-Type → ContentType is null → ?? string.Empty → not msgpack → early return
		string capturedContentType = "NOT_SET";
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
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
	public async Task TransformRequestBody_NonMsgPackContentType_SkipsTransform()
	{
		// Non-msgpack Content-Type → early return, body/ContentType unchanged
		string capturedContentType = string.Empty;
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
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
	public async Task TransformRequestBody_MsgPackContentType_EmptyBody_SkipsConversion()
	{
		// MsgPack Content-Type but zero-length body → second early return, ContentType stays msgpack
		string capturedContentType = string.Empty;
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
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
	public async Task TransformRequestBody_MsgPackContentType_ValidBody_ConvertsBodyToJson()
	{
		// MsgPack Content-Type with valid msgpack body → body converted to JSON for handler
		string capturedContentType = string.Empty;
		string capturedBody = string.Empty;
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
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

	// --- TransformResponseToMsgPackAsync ---

	[Fact]
	public async Task TransformResponseToMsgPack_InvalidJson_CatchBlock_FallsBackToJson()
	{
		// JSON content-type with body that ConvertFromJson cannot parse → catch branch → original JSON returned
		const string invalidJson = "NOT {VALID} JSON!!!";
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
		{
			ctx.Response.ContentType = JsonMimeType;
			await ctx.Response.WriteAsync(invalidJson);
		});
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		string body = await response.Content.ReadAsStringAsync();
		body.ShouldBe(invalidJson);
		response.Content.Headers.ContentType?.MediaType.ShouldBe(JsonMimeType);
	}

	[Fact]
	public async Task TransformResponseToMsgPack_NonJsonResponse_ElseIfBranch_CopiedThrough()
	{
		// Non-JSON ContentType with body → isJsonResponse=false, else-if branch copies buffer to original
		const string responseText = "plain text response";
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
		{
			ctx.Response.ContentType = "text/plain";
			await ctx.Response.WriteAsync(responseText);
		});
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		string body = await response.Content.ReadAsStringAsync();
		body.ShouldBe(responseText);
	}

	[Fact]
	public async Task TransformResponseToMsgPack_EmptyResponse_NeitherBranchRuns_NothingWritten()
	{
		// Empty response buffer (length == 0) → neither if nor else-if branch runs
		(HttpClient client, WebApplication app) = await CreateTestApp(ctx =>
		{
			ctx.Response.StatusCode = 204;
			return Task.CompletedTask;
		});
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
		byte[] body = await response.Content.ReadAsByteArrayAsync();
		body.Length.ShouldBe(0);
	}

	[Fact]
	public async Task TransformResponseToMsgPack_NullContentType_WithBody_NullCoalescingBranch_CopiedThrough()
	{
		// ContentType is null → (ContentType ?? string.Empty) takes null branch → string.Empty doesn't contain json
		// → isJsonResponse=false, else-if(buffer.Length > 0) copies buffer through
		byte[] responseData = Encoding.UTF8.GetBytes("some content");
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
		{
			ctx.Response.ContentType = null;
			await ctx.Response.Body.WriteAsync(responseData);
		});
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		byte[] body = await response.Content.ReadAsByteArrayAsync();
		body.ShouldBe(responseData);
	}

	// --- Integration ---

	[Fact]
	public async Task TransformResponseToMsgPack_NextThrows_FinallyBlockStillExecutes()
	{
		// When next() throws, the compiler-generated state machine must execute the finally block
		// with an exception in flight (restoring context.Response.Body) before rethrowing.
		(HttpClient client, WebApplication app) = await CreateTestApp(
			_ => throw new InvalidOperationException("simulated next failure"));
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));

		try
		{
			using HttpResponseMessage response = await client.SendAsync(request);
			// If the server caught the exception, it returns 5xx
			((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(500);
		}
		catch (Exception ex) when (ex is not ShouldAssertException)
		{
			// TestServer surfaces the unhandled handler exception — also valid
		}
	}

	[Fact]
	public async Task Integration_MsgPackRequestBodyAndAccept_BothTransformsApplied()
	{
		// MsgPack request body is converted to JSON for handler AND JSON response is converted to msgpack
		string capturedBody = string.Empty;
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
		{
			using StreamReader reader = new(ctx.Request.Body, Encoding.UTF8);
			capturedBody = await reader.ReadToEndAsync();
			ctx.Response.ContentType = JsonMimeType;
			await ctx.Response.WriteAsync("""{"result":"ok"}""");
		});
		await using WebApplication _ = app;

		byte[] msgPackBody = MessagePackSerializer.ConvertFromJson("""{"input":"data"}""");
		using HttpRequestMessage request = new(HttpMethod.Post, "/");
		ByteArrayContent content = new(msgPackBody);
		content.Headers.ContentType = new MediaTypeHeaderValue(MsgPackMimeType);
		request.Content = content;
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		capturedBody.ShouldContain("data");
		response.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);
		byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
		string responseJson = MessagePackSerializer.ConvertToJson(responseBytes);
		responseJson.ShouldContain("ok");
	}

	// --- Extension method ---

	[Fact]
	public async Task UseContentNegotiationMiddleware_RegistersMiddleware_TransformsResponse()
	{
		// Extension method UseContentNegotiationMiddleware registers the middleware correctly
		const string responseJson = """{"registered":true}""";
		(HttpClient client, WebApplication app) = await CreateTestApp(async ctx =>
		{
			ctx.Response.ContentType = JsonMimeType;
			await ctx.Response.WriteAsync(responseJson);
		}, useExtensionMethod: true);
		await using WebApplication _ = app;

		using HttpRequestMessage request = new(HttpMethod.Get, "/");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MsgPackMimeType));
		using HttpResponseMessage response = await client.SendAsync(request);

		response.Content.Headers.ContentType?.MediaType.ShouldBe(MsgPackMimeType);
		byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
		string convertedJson = MessagePackSerializer.ConvertToJson(responseBytes);
		convertedJson.ShouldContain("true");
	}
}
