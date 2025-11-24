﻿using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Web.Middleware.Tests;

public sealed class UseXsrfTokenMiddlewareTests
{
    private readonly IFixture _fixture;
    private readonly IApplicationBuilder _builder;

    public UseXsrfTokenMiddlewareTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        _builder = A.Fake<IApplicationBuilder>();
        // Setup UseMiddleware to return the builder for fluent chaining
        A.CallTo(() => _builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(_builder);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvokeAsync_ShouldSetXsrfCookie_WithCorrectHttpOnlyFlag(bool httpOnly)
    {
        // Arrange
        RequestDelegate next = A.Fake<RequestDelegate>();
        IAntiforgery antiforgery = A.Fake<IAntiforgery>();
        HttpContext context = new DefaultHttpContext();

        string expectedToken = _fixture.Create<string>();
        AntiforgeryTokenSet tokenSet = new(expectedToken, expectedToken, "form", "header");

        A.CallTo(() => antiforgery.GetAndStoreTokens(context))
            .Returns(tokenSet);

        UseXsrfTokenMiddleware middleware = new(next, antiforgery, httpOnly);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ShouldContainKey("Set-Cookie");
        string? setCookieHeader = context.Response.Headers.SetCookie.ToString();
        setCookieHeader.ShouldContain("XSRF-TOKEN");
        setCookieHeader.ShouldContain(expectedToken);

        if (httpOnly)
        {
            setCookieHeader.ShouldContain("HttpOnly");
        }
        else
        {
            setCookieHeader.ShouldNotContain("HttpOnly");
        }

        A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseXsrfToken_Extension_ShouldAddMiddleware(bool httpOnly)
    {
        // Act
        IApplicationBuilder result = _builder.UseMiddleware<UseXsrfTokenMiddleware>(httpOnly);

        // Assert
        result.ShouldBe(_builder);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextDelegate()
    {
        // Arrange
        RequestDelegate next = A.Fake<RequestDelegate>();
        IAntiforgery antiforgery = A.Fake<IAntiforgery>();
        HttpContext context = new DefaultHttpContext();

        AntiforgeryTokenSet tokenSet = new(_fixture.Create<string>(),
            _fixture.Create<string>(), "form", "header");

        A.CallTo(() => antiforgery.GetAndStoreTokens(context))
            .Returns(tokenSet);

        UseXsrfTokenMiddleware middleware = new(next, antiforgery, false);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task InvokeAsync_ShouldUseAntiforgeryToGenerateTokens()
    {
        // Arrange
        RequestDelegate next = A.Fake<RequestDelegate>();
        IAntiforgery antiforgery = A.Fake<IAntiforgery>();
        HttpContext context = new DefaultHttpContext();

        AntiforgeryTokenSet tokenSet = new(_fixture.Create<string>(),
            _fixture.Create<string>(), "form", "header");

        A.CallTo(() => antiforgery.GetAndStoreTokens(context))
            .Returns(tokenSet);

        UseXsrfTokenMiddleware middleware = new(next, antiforgery, false);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        A.CallTo(() => antiforgery.GetAndStoreTokens(context))
            .MustHaveHappenedOnceExactly();
    }
}