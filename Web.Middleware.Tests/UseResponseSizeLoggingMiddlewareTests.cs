﻿using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Web.Middleware.Tests;

public sealed class UseResponseSizeLoggingMiddlewareTests
{
    private readonly ILogger<UseResponseSizeLoggingMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IFixture _fixture;

    public UseResponseSizeLoggingMiddlewareTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        _logger = A.Fake<ILogger<UseResponseSizeLoggingMiddleware>>();
        _next = A.Fake<RequestDelegate>();
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        UseResponseSizeLoggingMiddleware middleware = new(_next, _logger, 1024 * 100);

        // Assert
        middleware.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0)]    // Empty response
    [InlineData(100)]  // Small response
    [InlineData(1024)] // 1KB response
    public async Task InvokeAsync_LogsResponseSize_ForVariousContentSizes(int contentSize)
    {
        // Arrange
        UseResponseSizeLoggingMiddleware middleware = new(_next, _logger, 1024 * 100);
        DefaultHttpContext context = new();
        await using MemoryStream responseBody = new();
        context.Response.Body = responseBody;

        byte[] content = new byte[contentSize];
        A.CallTo(() => _next(context))
            .Returns(Task.Run(() => context.Response.Body.WriteAsync(content, 0, content.Length)));

        // Set up request headers
        context.Request.Method = "GET";
        context.Request.Path = "/test-path";
        context.Request.Headers.Accept = "application/json";
        context.Request.Headers.AcceptEncoding = "gzip";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        A.CallTo(() => _logger.Log(
            LogLevel.Warning,
            A<EventId>.Ignored,
            A<It.IsAnyType>.That.Matches(o => o.ToString()!.Contains("Response to") &&
                o.ToString()!.Contains("/test-path") &&
                o.ToString()!.Contains("[GET]") &&
                o.ToString()!.Contains("Size:")),
            null,
            A<Func<It.IsAnyType, Exception?, string>>.Ignored));
    }

    [Fact]
    public async Task InvokeAsync_RestoresOriginalBodyStream()
    {
        // Arrange
        UseResponseSizeLoggingMiddleware middleware = new(_next, _logger, 1024 * 100);
        DefaultHttpContext context = new();
        await using MemoryStream originalBody = new();
        context.Response.Body = originalBody;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.ShouldBe(originalBody);
    }

    [Fact]
    public async Task InvokeAsync_HandlesNextDelegateException()
    {
        // Arrange
        UseResponseSizeLoggingMiddleware middleware = new(_next, _logger, 1024 * 100);
        DefaultHttpContext context = new();
        Exception exception = new("Test exception");

        A.CallTo(() => _next(context)).ThrowsAsync(exception);

        // Act & Assert
        Exception thrownException = await Should.ThrowAsync<Exception>(
            async () => await middleware.InvokeAsync(context));

        thrownException.ShouldBe(exception);
        context.Response.Body.ShouldBe(context.Response.Body);
    }

    [Fact]
    public async Task InvokeAsync_HandlesEmptyHeaders()
    {
        // Arrange
        UseResponseSizeLoggingMiddleware middleware = new(_next, _logger, 1024 * 100);
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        // Don't set any headers to test null handling
        context.Request.Headers.Accept = string.Empty;
        context.Request.Headers.AcceptEncoding = string.Empty;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        A.CallTo(() => _logger.Log(
            LogLevel.Warning,
            A<EventId>.Ignored,
            A<It.IsAnyType>.That.Matches(o => o.ToString()!.Contains("Response to") &&
                !o.ToString()!.Contains('+')),
            null,
            A<Func<It.IsAnyType, Exception?, string>>.Ignored));
    }
}