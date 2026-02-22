using System.Net;
using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using xRetry.v3;

namespace Web.Middleware.Tests;

public sealed class OptionsMiddlewareTests
{
	private readonly HttpContext context;
	private readonly RequestDelegate next;

	public OptionsMiddlewareTests()
	{
		context = new DefaultHttpContext();
		next = A.Fake<RequestDelegate>();
	}

	[RetryTheory(3)]
	[InlineData("OPTIONS", null, "*")]
	[InlineData("OPTIONS", "", "*")]
	[InlineData("OPTIONS", "https://example.com", "https://example.com")]
	public async Task InvokeAsync_OptionsRequest_SetsCorrectHeaders(string method, string? originHeader, string expectedOrigin)
	{
		// Arrange

		context.Request.Method = method;
		if (originHeader != null)
		{
			context.Request.Headers.Origin = originHeader;
		}

		OptionsMiddleware middleware = new(next, "*", ["Content-Type, Authorization, X-Requested-With, X-XSRF-TOKEN"], ["GET, POST, PUT, DELETE, OPTIONS"], true, 3600, HttpStatusCode.OK);

		// Act

		await middleware.InvokeAsync(context);

		// Assert

		context.Response.Headers.AccessControlAllowOrigin.ToString().ShouldBe(expectedOrigin);
		context.Response.Headers.AccessControlAllowHeaders.ToString().ShouldBe("Content-Type, Authorization, X-Requested-With, X-XSRF-TOKEN");
		context.Response.Headers.AccessControlAllowMethods.ToString().ShouldBe("GET, POST, PUT, DELETE, OPTIONS");
		context.Response.Headers.AccessControlAllowCredentials.ToString().ShouldBe("true");
		context.Response.Headers.AccessControlMaxAge.ToString().ShouldBe("3600");
		context.Response.StatusCode.ShouldBe(200);

		A.CallTo(() => next(context)).MustNotHaveHappened();
	}

	[RetryTheory(3)]
	[InlineData("GET", null)]
	[InlineData("POST", "")]
	[InlineData("PUT", "https://example.com")]
	public async Task InvokeAsync_NonOptionsRequest_ProcessesNormallyWithHeaders(string method, string? originHeader)
	{
		// Arrange

		context.Request.Method = method;
		if (originHeader != null)
		{
			context.Request.Headers.Origin = originHeader;
		}

		OptionsMiddleware middleware = new(next, "*", [], [], true, 3600, HttpStatusCode.OK);

		// Act

		await middleware.InvokeAsync(context);

		// Assert

		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();

		if (!string.IsNullOrWhiteSpace(originHeader))
		{
			context.Response.Headers.AccessControlAllowOrigin.ToString().ShouldBe(originHeader);
			context.Response.Headers.AccessControlAllowCredentials.ToString().ShouldBe("true");
		}
		else
		{
			context.Response.Headers.AccessControlAllowOrigin.ToString().ShouldBeEmpty();
			context.Response.Headers.AccessControlAllowCredentials.ToString().ShouldBeEmpty();
		}
	}

	[RetryFact(3)]
	public async Task InvokeAsync_NonOptionsRequest_CallsNextMiddleware()
	{
		// Arrange

		context.Request.Method = "GET";
		OptionsMiddleware middleware = new(next, "*", [], [], true, 3600, HttpStatusCode.OK);

		// Act

		await middleware.InvokeAsync(context);

		// Assert

		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public void Constructor_NullNext_ThrowsArgumentNullException()
	{
		// Act & Assert

		Should.Throw<ArgumentNullException>(() => new OptionsMiddleware(null!, "*", [], [], true, 3600, HttpStatusCode.OK));
	}

	[RetryFact(3)]
	public void UseOptions_WithAllDefaults_ReturnsBuilder()
	{
		// Arrange
		IApplicationBuilder builder = A.Fake<IApplicationBuilder>();
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);

		// Act
		IApplicationBuilder result = builder.UseOptions();

		// Assert
		result.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void UseOptions_WithCustomParameters_ReturnsBuilder()
	{
		// Arrange
		IApplicationBuilder builder = A.Fake<IApplicationBuilder>();
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);

		// Act
		IApplicationBuilder result = builder.UseOptions(
			defaultAllowedOrigin: "https://example.com",
			allowedHeaders: ["Content-Type"],
			allowedMethods: ["GET", "POST"],
			allowCredentials: false,
			maxAge: 7200,
			defaultStatusCode: HttpStatusCode.NoContent
		);

		// Assert
		result.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void UseOptions_WithNullHeaders_ReturnsBuilder()
	{
		// Arrange
		IApplicationBuilder builder = A.Fake<IApplicationBuilder>();
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);

		// Act
		IApplicationBuilder result = builder.UseOptions(allowedHeaders: null, allowedMethods: null);

		// Assert
		result.ShouldNotBeNull();
	}
}
