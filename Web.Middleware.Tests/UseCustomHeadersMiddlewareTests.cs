using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Http;
using xRetry.v3;

namespace Web.Middleware.Tests;

public sealed class UseCustomHeadersMiddlewareTests
{
	private readonly HttpContext context;
	private readonly RequestDelegate next;

	public UseCustomHeadersMiddlewareTests()
	{
		context = new DefaultHttpContext();
		next = A.Fake<RequestDelegate>();
	}

	[RetryTheory(3)]
	[InlineData("X-Custom-Header", "CustomValue")]
	[InlineData("X-Version", "1.0.0")]
	[InlineData("X-Powered-By", "CustomFramework")]
	public async Task InvokeAsync_WithAddHeaders_AddsSpecifiedHeaders(string headerKey, string headerValue)
	{
		// Arrange
		Dictionary<string, string> addHeaders = new() { { headerKey, headerValue } };
		UseCustomHeadersMiddleware middleware = new(next, addHeaders);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Response.Headers[headerKey].ToString().ShouldBe(headerValue);
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryTheory(3)]
	[InlineData("X-Remove-1", "X-Remove-2")]
	[InlineData("Server", "X-Powered-By")]
	public async Task InvokeAsync_WithRemoveHeaders_RemovesSpecifiedHeaders(string header1ToRemove, string header2ToRemove)
	{
		string[] headersToRemove = [header1ToRemove, header2ToRemove];
		// Arrange
		foreach (string header in headersToRemove)
		{
			context.Response.Headers.TryAdd(header, "InitialValue");
		}

		UseCustomHeadersMiddleware middleware = new(next, removeHeaders: headersToRemove);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		foreach (string header in headersToRemove)
		{
			context.Response.Headers.ContainsKey(header).ShouldBeFalse();
		}
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithNullHeaders_OnlyCallsNext()
	{
		// Arrange
		UseCustomHeadersMiddleware middleware = new(next);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithEmptyHeaderCollections_OnlyCallsNext()
	{
		// Arrange
		UseCustomHeadersMiddleware middleware = new(next, new Dictionary<string, string>(), Array.Empty<string>());

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithBothAddAndRemoveHeaders_ProcessesAllHeaders()
	{
		// Arrange
		Dictionary<string, string> addHeaders = new() { { "X-New-Header", "NewValue" } };
		string[] removeHeaders = ["X-Old-Header"];
		context.Response.Headers.TryAdd("X-Old-Header", "OldValue");

		UseCustomHeadersMiddleware middleware = new(next, addHeaders, removeHeaders);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Response.Headers.ContainsKey("X-Old-Header").ShouldBeFalse();
		context.Response.Headers["X-New-Header"].ToString().ShouldBe("NewValue");
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public void Constructor_NullNext_ThrowsArgumentNullException()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new UseCustomHeadersMiddleware(null!));
	}
}
