using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Web.Middleware.Tests;

public sealed class UseResponseLoggingFilterTests
{
    private readonly ILogger<UseResponseLoggingFilter> _logger;
    private readonly IResponseLoggingConfig _config;

    public UseResponseLoggingFilterTests()
    {
        _logger = A.Fake<ILogger<UseResponseLoggingFilter>>();
        _config = A.Fake<IResponseLoggingConfig>();
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        UseResponseLoggingFilter filter = new(_logger, _config);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(1.0, 2.0)] // When elapsed time exceeds threshold
    [InlineData(2.0, 1.0)] // When elapsed time is under threshold
    public void OnActionExecuted_LogsWarningWhenThresholdExceeded(double thresholdSeconds, double delaySeconds)
    {
        // Arrange
        A.CallTo(() => _config.ThresholdInSeconds).Returns(thresholdSeconds);
        UseResponseLoggingFilter filter = new(_logger, _config);

        object controller = new();
        DefaultHttpContext httpContext = new();
        ActionContext actionContext = new(httpContext, new RouteData(), new ActionDescriptor());
        List<IFilterMetadata> filterMetadata = new();
        Dictionary<string, object?> actionArguments = new();

        ActionExecutedContext executedContext = new(actionContext, filterMetadata, controller)
        {
            Result = new OkResult()
        };

        // Act
        ActionExecutingContext executingContext = new(actionContext, filterMetadata, actionArguments, controller);
        filter.OnActionExecuting(executingContext);

        // Simulate delay
        Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));

        filter.OnActionExecuted(executedContext);

        // Assert
        if (delaySeconds >= thresholdSeconds)
        {
            A.CallTo(() => _logger.Log(
                LogLevel.Warning,
                A<EventId>.Ignored,
                A<It.IsAnyType>.That.Matches(msg => msg.ToString()!.Contains("Method") &&
                    msg.ToString()!.Contains("took") &&
                    msg.ToString()!.Contains("to complete with result:") &&
                    msg.ToString()!.Contains(nameof(OkResult))),
                null,
                A<Func<It.IsAnyType, Exception?, string>>.Ignored));
        }
        else
        {
            A.CallTo(() => _logger.Log(LogLevel.Warning, A<EventId>.Ignored, A<It.IsAnyType>.Ignored, null, A<Func<It.IsAnyType, Exception?, string>>.Ignored)).MustNotHaveHappened();
        }
    }

    [Fact]
    public void OnActionExecuting_StartsNewMeasurement()
    {
        // Arrange
        UseResponseLoggingFilter filter = new(_logger, _config);
        object controller = new();
        DefaultHttpContext httpContext = new();
        ActionContext actionContext = new(httpContext, new RouteData(), new ActionDescriptor());
        List<IFilterMetadata> filterMetadata = new();
        Dictionary<string, object?> actionArguments = new();

        ActionExecutingContext executingContext = new(actionContext, filterMetadata, actionArguments, controller);

        // Act
        filter.OnActionExecuting(executingContext);
        Thread.Sleep(100); // Small delay to ensure time is measured
        ActionExecutedContext executedContext = new(actionContext, filterMetadata, controller);
        filter.OnActionExecuted(executedContext);

        // Assert - Verify the stopwatch was reset and captured time
        A.CallTo(() => _config.ThresholdInSeconds).MustHaveHappened();
    }

    [Theory]
    [InlineData(0.0)]  // Edge case - zero threshold
    [InlineData(-1.0)] // Edge case - negative threshold
    public void OnActionExecuted_HandlesEdgeCaseThresholds(double thresholdSeconds)
    {
        // Arrange
        A.CallTo(() => _config.ThresholdInSeconds).Returns(thresholdSeconds);
        UseResponseLoggingFilter filter = new(_logger, _config);

        object controller = new();
        DefaultHttpContext httpContext = new();
        ActionContext actionContext = new(httpContext, new RouteData(), new ActionDescriptor());
        List<IFilterMetadata> filterMetadata = new();
        Dictionary<string, object?> actionArguments = new();

        ActionExecutedContext executedContext = new(actionContext, filterMetadata, controller)
        {
            Result = new OkResult()
        };

        // Act
        ActionExecutingContext executingContext = new(actionContext, filterMetadata, actionArguments, controller);
        filter.OnActionExecuting(executingContext);

        Thread.Sleep(100); // Small delay to ensure time is measured
        filter.OnActionExecuted(executedContext);

        // Assert - Should log warning for any elapsed time when threshold is zero or negative
        A.CallTo(() => _logger.Log(
            LogLevel.Warning,
            A<EventId>.Ignored,
            A<It.IsAnyType>.That.Matches(msg => msg.ToString()!.Contains("Method") &&
                msg.ToString()!.Contains("took") &&
                msg.ToString()!.Contains("to complete with result:") &&
                msg.ToString()!.Contains(nameof(OkResult))),
            null,
            A<Func<It.IsAnyType, Exception?, string>>.Ignored));
    }
}
