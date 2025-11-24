﻿using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Http;

namespace Web.Middleware.Tests;

public sealed class UseCustomHeadersMiddlewareTests
{
    private readonly IFixture _fixture;
    private readonly HttpContext _context;
    private readonly RequestDelegate _next;

    public UseCustomHeadersMiddlewareTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        _context = new DefaultHttpContext();
        _next = A.Fake<RequestDelegate>();
    }

    [Theory]
    [InlineData("X-Custom-Header", "CustomValue")]
    [InlineData("X-Version", "1.0.0")]
    [InlineData("X-Powered-By", "CustomFramework")]
    public async Task InvokeAsync_WithAddHeaders_AddsSpecifiedHeaders(string headerKey, string headerValue)
    {
        // Arrange
        Dictionary<string, string> addHeaders = new() { { headerKey, headerValue } };
        UseCustomHeadersMiddleware middleware = new(_next, addHeaders);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.Headers[headerKey].ToString().ShouldBe(headerValue);
        A.CallTo(() => _next(_context)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData("X-Remove-1", "X-Remove-2")]
    [InlineData("Server", "X-Powered-By")]
    public async Task InvokeAsync_WithRemoveHeaders_RemovesSpecifiedHeaders(string header1ToRemove, string header2ToRemove)
    {
        string[] headersToRemove = [header1ToRemove, header2ToRemove];
        // Arrange
        foreach (string header in headersToRemove)
        {
            _context.Response.Headers.TryAdd(header, "InitialValue");
        }

        UseCustomHeadersMiddleware middleware = new(_next, removeHeaders: headersToRemove);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        foreach (string header in headersToRemove)
        {
            _context.Response.Headers.ContainsKey(header).ShouldBeFalse();
        }
        A.CallTo(() => _next(_context)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task InvokeAsync_WithNullHeaders_OnlyCallsNext()
    {
        // Arrange
        UseCustomHeadersMiddleware middleware = new(_next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        A.CallTo(() => _next(_context)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyHeaderCollections_OnlyCallsNext()
    {
        // Arrange
        UseCustomHeadersMiddleware middleware = new(_next,
            new Dictionary<string, string>(),
            Array.Empty<string>());

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        A.CallTo(() => _next(_context)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task InvokeAsync_WithBothAddAndRemoveHeaders_ProcessesAllHeaders()
    {
        // Arrange
        Dictionary<string, string> addHeaders = new()
        {
            { "X-New-Header", "NewValue" }
        };
        string[] removeHeaders = ["X-Old-Header"];
        _context.Response.Headers.TryAdd("X-Old-Header", "OldValue");

        UseCustomHeadersMiddleware middleware = new(_next, addHeaders, removeHeaders);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.Headers.ContainsKey("X-Old-Header").ShouldBeFalse();
        _context.Response.Headers["X-New-Header"].ToString().ShouldBe("NewValue");
        A.CallTo(() => _next(_context)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Constructor_NullNext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new UseCustomHeadersMiddleware(null!));
    }
}