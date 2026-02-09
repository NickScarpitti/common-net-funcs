using CommonNetFuncs.Web.Middleware;
using FakeItEasy.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using xRetry.v3;

namespace Web.Middleware.Tests;

public sealed class UseResponseSizeLoggingMiddlewareTests
{
	private readonly ILogger<UseResponseSizeLoggingMiddleware> logger;
	private readonly RequestDelegate next;

	public UseResponseSizeLoggingMiddlewareTests()
	{
		logger = A.Fake<ILogger<UseResponseSizeLoggingMiddleware>>();
		next = A.Fake<RequestDelegate>();
	}

	[RetryFact(3)]
	public void Constructor_ValidParameters_CreatesInstance()
	{
		// Act
		UseResponseSizeLoggingMiddleware middleware = new(next, logger, 1024 * 100);

		// Assert
		middleware.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData(0)]    // Empty response
	[InlineData(100)]  // Small response
	[InlineData(1024)] // 1KB response
	public async Task InvokeAsync_LogsResponseSize_ForVariousContentSizes(int contentSize)
	{
		// Arrange
		UseResponseSizeLoggingMiddleware middleware = new(next, logger, -1);
		DefaultHttpContext context = new();
		await using MemoryStream responseBody = new();
		context.Response.Body = responseBody;

		byte[] content = new byte[contentSize];
		A.CallTo(() => next(context)).Returns(Task.Run(() => context.Response.Body.WriteAsync(content, 0, content.Length)));
		A.CallTo(() => logger.IsEnabled(LogLevel.Warning)).Returns(true);

		// Set up request headers
		context.Request.Method = "GET";
		context.Request.Path = "/test-path";
		context.Request.Headers.Accept = "application/json";
		context.Request.Headers.AcceptEncoding = "gzip";

		// Act
		await middleware.InvokeAsync(context);

		// Assert - Get the actual call and verify the formatted message
		IFakeObjectCall call = Fake.GetCalls(logger).Single(c => c.Method.Name == "Log");
		call.Arguments[0].ShouldBe(LogLevel.Warning);

		object? state = call.Arguments[2];
		string message = state?.ToString() ?? string.Empty;
		message.ShouldContain("Response to");
		message.ShouldContain("/test-path");
		message.ShouldContain("[GET]");
		message.ShouldContain("Size:");
	}

	[RetryFact(3)]
	public async Task InvokeAsync_RestoresOriginalBodyStream()
	{
		// Arrange
		UseResponseSizeLoggingMiddleware middleware = new(next, logger, 1024 * 100);
		DefaultHttpContext context = new();
		await using MemoryStream originalBody = new();
		context.Response.Body = originalBody;

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Response.Body.ShouldBe(originalBody);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_HandlesNextDelegateException()
	{
		// Arrange
		UseResponseSizeLoggingMiddleware middleware = new(next, logger, 1024 * 100);
		DefaultHttpContext context = new();
		Exception exception = new("Test exception");

		A.CallTo(() => next(context)).ThrowsAsync(exception);

		// Act & Assert
		Exception thrownException = await Should.ThrowAsync<Exception>(
				async () => await middleware.InvokeAsync(context));

		thrownException.ShouldBe(exception);
		context.Response.Body.ShouldBe(context.Response.Body);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_HandlesEmptyHeaders()
	{
		// Arrange
		A.CallTo(() => logger.IsEnabled(LogLevel.Warning)).Returns(true);
		UseResponseSizeLoggingMiddleware middleware = new(next, logger, -1);
		DefaultHttpContext context = new();
		context.Response.Body = new MemoryStream();

		// Don't set any headers to test null handling
		context.Request.Headers.Accept = string.Empty;
		context.Request.Headers.AcceptEncoding = string.Empty;

		// Act
		await middleware.InvokeAsync(context);

		// Assert - Get the actual call and verify the formatted message
		IFakeObjectCall call = Fake.GetCalls(logger).Single(c => c.Method.Name == "Log");
		call.Arguments[0].ShouldBe(LogLevel.Warning);

		object? state = call.Arguments[2];
		string message = state?.ToString() ?? string.Empty;
		message.ShouldContain("Response to");
		message.ShouldNotContain("+");
	}
}

