using System.Net;
using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Http;
using xRetry;

namespace Web.Middleware.Tests;

public sealed class OptionsMiddlewareTests
{
	private readonly HttpContext _context;
	private readonly RequestDelegate _next;

	public OptionsMiddlewareTests()
	{
		_context = new DefaultHttpContext();
		_next = A.Fake<RequestDelegate>();
	}

	[RetryTheory(3)]
	[InlineData("OPTIONS", null, "*")]
	[InlineData("OPTIONS", "", "*")]
	[InlineData("OPTIONS", "https://example.com", "https://example.com")]
	public async Task InvokeAsync_OptionsRequest_SetsCorrectHeaders(string method, string? originHeader, string expectedOrigin)
	{
		// Arrange
		_context.Request.Method = method;
		if (originHeader != null)
		{
			_context.Request.Headers.Origin = originHeader;
		}

		OptionsMiddleware middleware = new(_next, "*", ["Content-Type, Authorization, X-Requested-With, X-XSRF-TOKEN"], ["GET, POST, PUT, DELETE, OPTIONS"], true, 3600, HttpStatusCode.OK);

		// Act
		await middleware.InvokeAsync(_context);

		// Assert
		_context.Response.Headers.AccessControlAllowOrigin.ToString().ShouldBe(expectedOrigin);
		_context.Response.Headers.AccessControlAllowHeaders.ToString().ShouldBe("Content-Type, Authorization, X-Requested-With, X-XSRF-TOKEN");
		_context.Response.Headers.AccessControlAllowMethods.ToString().ShouldBe("GET, POST, PUT, DELETE, OPTIONS");
		_context.Response.Headers.AccessControlAllowCredentials.ToString().ShouldBe("true");
		_context.Response.Headers.AccessControlMaxAge.ToString().ShouldBe("3600");
		_context.Response.StatusCode.ShouldBe(200);

		A.CallTo(() => _next(_context)).MustNotHaveHappened();
	}

	[RetryTheory(3)]
	[InlineData("GET", null)]
	[InlineData("POST", "")]
	[InlineData("PUT", "https://example.com")]
	public async Task InvokeAsync_NonOptionsRequest_ProcessesNormallyWithHeaders(string method, string? originHeader)
	{
		// Arrange
		_context.Request.Method = method;
		if (originHeader != null)
		{
			_context.Request.Headers.Origin = originHeader;
		}

		OptionsMiddleware middleware = new(_next, "*", [], [], true, 3600, HttpStatusCode.OK);

		// Act
		await middleware.InvokeAsync(_context);

		// Assert
		A.CallTo(() => _next(_context)).MustHaveHappenedOnceExactly();

		if (!string.IsNullOrWhiteSpace(originHeader))
		{
			_context.Response.Headers.AccessControlAllowOrigin.ToString().ShouldBe(originHeader);
			_context.Response.Headers.AccessControlAllowCredentials.ToString().ShouldBe("true");
		}
		else
		{
			_context.Response.Headers.AccessControlAllowOrigin.ToString().ShouldBeEmpty();
			_context.Response.Headers.AccessControlAllowCredentials.ToString().ShouldBeEmpty();
		}
	}

	[RetryFact(3)]
	public async Task InvokeAsync_NonOptionsRequest_CallsNextMiddleware()
	{
		// Arrange
		_context.Request.Method = "GET";
		OptionsMiddleware middleware = new(_next, "*", [], [], true, 3600, HttpStatusCode.OK);

		// Act
		await middleware.InvokeAsync(_context);

		// Assert
		A.CallTo(() => _next(_context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public void Constructor_NullNext_ThrowsArgumentNullException()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new OptionsMiddleware(null!, "*", [], [], true, 3600, HttpStatusCode.OK));
	}
}
