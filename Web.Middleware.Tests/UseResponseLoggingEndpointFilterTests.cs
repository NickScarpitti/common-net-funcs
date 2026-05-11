using CommonNetFuncs.Web.Middleware;
using FakeItEasy;
using FakeItEasy.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using xRetry.v3;

namespace Web.Middleware.Tests;

public sealed class UseResponseLoggingEndpointFilterTests
{
	private readonly ILogger<UseResponseLoggingEndpointFilter> logger;
	private readonly IResponseLoggingConfig config;

	public UseResponseLoggingEndpointFilterTests()
	{
		logger = A.Fake<ILogger<UseResponseLoggingEndpointFilter>>();
		config = A.Fake<IResponseLoggingConfig>();
	}

	private static DefaultHttpContext CreateHttpContextWithEndpoint(string? displayName = "TestEndpoint GET /test")
	{
		DefaultHttpContext httpContext = new();
		if (displayName is not null)
		{
			httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), displayName));
		}

		return httpContext;
	}

	[RetryFact(3)]
	public void Constructor_ValidParameters_CreatesInstance()
	{
		// Act
		UseResponseLoggingEndpointFilter filter = new(logger, config);

		// Assert
		filter.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData(1.0, 2.0)] // elapsed time exceeds threshold - should log
	[InlineData(2.0, 1.0)] // elapsed time under threshold - should not log
	public async Task InvokeAsync_LogsWarningWhenThresholdExceeded(double thresholdSeconds, double delaySeconds)
	{
		// Arrange
		A.CallTo(() => config.ThresholdInSeconds).Returns(thresholdSeconds);
		A.CallTo(() => logger.IsEnabled(LogLevel.Warning)).Returns(true);
		UseResponseLoggingEndpointFilter filter = new(logger, config);

		DefaultHttpContext httpContext = CreateHttpContextWithEndpoint();
		EndpointFilterInvocationContext context = EndpointFilterInvocationContext.Create(httpContext);
		EndpointFilterDelegate next = _ =>
		{
			Task.Delay(TimeSpan.FromSeconds(delaySeconds)).Wait();
			return ValueTask.FromResult<object?>(Results.Ok());
		};

		// Act
		await filter.InvokeAsync(context, next);

		// Assert
		if (delaySeconds >= thresholdSeconds)
		{
			IFakeObjectCall call = Fake.GetCalls(logger).Single(c => c.Method.Name == "Log");
			call.Arguments[0].ShouldBe(LogLevel.Warning);
			string message = call.Arguments[2]?.ToString() ?? string.Empty;
			message.ShouldContain("Endpoint");
			message.ShouldContain("took");
			message.ShouldContain("to complete with status code:");
		}
		else
		{
			Fake.GetCalls(logger).Where(c => c.Method.Name == "Log").ShouldBeEmpty();
		}
	}

	[RetryTheory(3)]
	[InlineData(0.0)]  // Edge case - zero threshold
	[InlineData(-1.0)] // Edge case - negative threshold
	public async Task InvokeAsync_HandlesEdgeCaseThresholds(double thresholdSeconds)
	{
		// Arrange
		A.CallTo(() => config.ThresholdInSeconds).Returns(thresholdSeconds);
		A.CallTo(() => logger.IsEnabled(LogLevel.Warning)).Returns(true);
		UseResponseLoggingEndpointFilter filter = new(logger, config);

		DefaultHttpContext httpContext = CreateHttpContextWithEndpoint("TestEndpoint GET /edge");
		httpContext.Response.StatusCode = 200;
		EndpointFilterInvocationContext context = EndpointFilterInvocationContext.Create(httpContext);
		EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

		// Act
		await filter.InvokeAsync(context, next);

		// Assert - Any elapsed time exceeds zero or negative threshold, so warning should be logged
		IFakeObjectCall call = Fake.GetCalls(logger).Single(c => c.Method.Name == "Log");
		call.Arguments[0].ShouldBe(LogLevel.Warning);

		object? state = call.Arguments[2];
		string message = state?.ToString() ?? string.Empty;
		message.ShouldContain("Endpoint");
		message.ShouldContain("took");
		message.ShouldContain("to complete with status code:");
	}

	[RetryFact(3)]
	public async Task InvokeAsync_LogsEndpointDisplayName_WhenEndpointIsSet()
	{
		// Arrange
		const string displayName = "GET /api/items";
		A.CallTo(() => config.ThresholdInSeconds).Returns(-1.0);
		A.CallTo(() => logger.IsEnabled(LogLevel.Warning)).Returns(true);
		UseResponseLoggingEndpointFilter filter = new(logger, config);

		DefaultHttpContext httpContext = CreateHttpContextWithEndpoint(displayName);
		EndpointFilterInvocationContext context = EndpointFilterInvocationContext.Create(httpContext);
		EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(null);

		// Act
		await filter.InvokeAsync(context, next);

		// Assert
		IFakeObjectCall call = Fake.GetCalls(logger).Single(c => c.Method.Name == "Log");
		string message = call.Arguments[2]?.ToString() ?? string.Empty;
		message.ShouldContain(displayName);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_LogsNullDisplayName_WhenEndpointIsNotSet()
	{
		// Arrange
		A.CallTo(() => config.ThresholdInSeconds).Returns(-1.0);
		A.CallTo(() => logger.IsEnabled(LogLevel.Warning)).Returns(true);
		UseResponseLoggingEndpointFilter filter = new(logger, config);

		DefaultHttpContext httpContext = new(); // No endpoint set
		EndpointFilterInvocationContext context = EndpointFilterInvocationContext.Create(httpContext);
		EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(null);

		// Act
		await filter.InvokeAsync(context, next);

		// Assert - should still log (with null display name)
		IFakeObjectCall call = Fake.GetCalls(logger).Single(c => c.Method.Name == "Log");
		call.Arguments[0].ShouldBe(LogLevel.Warning);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_DoesNotLog_WhenWarningIsDisabled()
	{
		// Arrange
		A.CallTo(() => config.ThresholdInSeconds).Returns(-1.0); // Always exceeds threshold
		A.CallTo(() => logger.IsEnabled(LogLevel.Warning)).Returns(false);
		UseResponseLoggingEndpointFilter filter = new(logger, config);

		DefaultHttpContext httpContext = CreateHttpContextWithEndpoint();
		EndpointFilterInvocationContext context = EndpointFilterInvocationContext.Create(httpContext);
		EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

		// Act
		await filter.InvokeAsync(context, next);

		// Assert
		A.CallTo(() => logger.Log(LogLevel.Warning, A<EventId>.Ignored, A<It.IsAnyType>.Ignored, A<Exception?>.Ignored, A<Func<It.IsAnyType, Exception?, string>>.Ignored)).MustNotHaveHappened();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_ReturnsNextResult()
	{
		// Arrange
		A.CallTo(() => config.ThresholdInSeconds).Returns(999.0); // High threshold, won't log
		UseResponseLoggingEndpointFilter filter = new(logger, config);

		DefaultHttpContext httpContext = CreateHttpContextWithEndpoint();
		EndpointFilterInvocationContext context = EndpointFilterInvocationContext.Create(httpContext);
		IResult expected = Results.Ok("payload");
		EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(expected);

		// Act
		object? result = await filter.InvokeAsync(context, next);

		// Assert
		result.ShouldBe(expected);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_PropagatesExceptionFromNext()
	{
		// Arrange
		A.CallTo(() => config.ThresholdInSeconds).Returns(999.0);
		UseResponseLoggingEndpointFilter filter = new(logger, config);

		DefaultHttpContext httpContext = CreateHttpContextWithEndpoint();
		EndpointFilterInvocationContext context = EndpointFilterInvocationContext.Create(httpContext);
		InvalidOperationException expected = new("next delegate failed");
		EndpointFilterDelegate next = _ => throw expected;

		// Act & Assert
		InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() => filter.InvokeAsync(context, next).AsTask());
		thrown.ShouldBe(expected);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_LogsResponseStatusCode()
	{
		// Arrange
		A.CallTo(() => config.ThresholdInSeconds).Returns(-1.0);
		A.CallTo(() => logger.IsEnabled(LogLevel.Warning)).Returns(true);
		UseResponseLoggingEndpointFilter filter = new(logger, config);

		DefaultHttpContext httpContext = CreateHttpContextWithEndpoint();
		httpContext.Response.StatusCode = 404;
		EndpointFilterInvocationContext context = EndpointFilterInvocationContext.Create(httpContext);
		EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.NotFound());

		// Act
		await filter.InvokeAsync(context, next);

		// Assert
		IFakeObjectCall call = Fake.GetCalls(logger).Single(c => c.Method.Name == "Log");
		string message = call.Arguments[2]?.ToString() ?? string.Empty;
		message.ShouldContain("404");
	}
}
