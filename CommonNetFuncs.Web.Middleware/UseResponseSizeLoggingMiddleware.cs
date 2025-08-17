<<<<<<< HEAD
﻿using CommonNetFuncs.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static System.Web.HttpUtility;
using static CommonNetFuncs.Core.UnitConversion;

namespace CommonNetFuncs.Web.Middleware;

/// <summary>
/// Middleware to expose response body size for development
/// NOT recommended for production environments
/// </summary>
public sealed class UseResponseSizeLoggingMiddleware(RequestDelegate next, ILogger<UseResponseSizeLoggingMiddleware> logger)
{
    private readonly RequestDelegate next = next;
    private readonly ILogger<UseResponseSizeLoggingMiddleware> logger = logger;
    //private readonly RecyclableMemoryStreamManager streamManager = new();

    //public async Task InvokeAsync(HttpContext context)
    //{
    //    Stopwatch stopwatch = new();
    //    stopwatch.Start();

    //    //Hold the original response stream
    //    Stream originalResponseBodyStream = context.Response.Body;

    //    //Create a new memory stream to hold the response
    //    await using MemoryStream responseBodyStream = new();
    //    context.Response.Body = responseBodyStream;

    //    //Call the next middleware in the pipeline
    //    await next(context);

    //    //Log the response size
    //    logger.LogWarning("{msg}", $"Response to {HtmlEncode(context.Request.Path)} [{HtmlEncode(context.Request.Method)}] of type [{HtmlEncode(context.Request.Headers.Accept)}{(string.IsNullOrEmpty(context.Request.Headers.AcceptEncoding) ? string.Empty : $" + {HtmlEncode(context.Request.Headers.AcceptEncoding)}")}] " +
    //        $"with Size: {responseBodyStream.Length.GetFileSizeFromBytesWithUnits(2)}");

    //    //Copy the contents of the new memory stream to the original response stream
    //    responseBodyStream.Position = 0;
    //    await responseBodyStream.CopyToAsync(originalResponseBodyStream);
    //    context.Response.Body = originalResponseBodyStream;

    //    TimeSpan elapsedTime = stopwatch.Elapsed;
    //    logger.LogError($"Elapsed time = {elapsedTime}");
    //}

    ////Working, best so far
    //public async Task InvokeAsync(HttpContext context)
    //{
    //    Stream originalBodyStream = context.Response.Body;

    //    await using RecyclableMemoryStream recyclableStream = streamManager.GetStream();
    //    context.Response.Body = recyclableStream;

    //    await next(context);

    //    Stopwatch stopwatch = new();
    //    stopwatch.Start();

    //    // Log size before resetting position
    //    logger.LogWarning("{msg}", $"Response to {HtmlEncode(context.Request.Path)} [{HtmlEncode(context.Request.Method)}] of type [{HtmlEncode(context.Request.Headers.Accept)}{(string.IsNullOrEmpty(context.Request.Headers.AcceptEncoding) ? string.Empty : $" + {HtmlEncode(context.Request.Headers.AcceptEncoding)}")}] " +
    //        $"with Size: {recyclableStream.Length.GetFileSizeFromBytesWithUnits(2)}");

    //    recyclableStream.Position = 0;
    //    await recyclableStream.CopyToAsync(originalBodyStream);
    //    context.Response.Body = originalBodyStream;

    //    stopwatch.Stop();
    //    TimeSpan elapsedTime = stopwatch.Elapsed;
    //    logger.LogError($"Elapsed time = {elapsedTime}");
    //}

    public async Task InvokeAsync(HttpContext context)
    {
        Stream originalBodyStream = context.Response.Body;
        CountingStream countingStream = new(originalBodyStream);
        context.Response.Body = countingStream;

        try
        {
            await next(context).ConfigureAwait(false);

            logger.LogWarning("{msg}", $"Response to {HtmlEncode(context.Request.Path)} [{HtmlEncode(context.Request.Method)}] of type [{HtmlEncode(context.Request.Headers.Accept)}{(string.IsNullOrEmpty(context.Request.Headers.AcceptEncoding) ? string.Empty : $" + {HtmlEncode(context.Request.Headers.AcceptEncoding)}")}] " +
                $"with Size: {countingStream.BytesWritten.GetFileSizeFromBytesWithUnits(2)}");
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}

/// <summary>
/// Extension method used to add the middleware to the HTTP request pipeline
/// </summary>
public static class ResponseSizeLoggingMiddlewareExtension
{
    public static IApplicationBuilder UseResponseSizeLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UseResponseSizeLoggingMiddleware>();
    }
}
=======
﻿using CommonNetFuncs.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static CommonNetFuncs.Core.UnitConversion;
using static System.Web.HttpUtility;

namespace CommonNetFuncs.Web.Middleware;

/// <summary>
/// Middleware to expose response body size for development
/// NOT recommended for production environments
/// </summary>
public sealed class UseResponseSizeLoggingMiddleware(RequestDelegate next, ILogger<UseResponseSizeLoggingMiddleware> logger, long logThreshold)
{
    private readonly RequestDelegate next = next;
    private readonly ILogger<UseResponseSizeLoggingMiddleware> logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        Stream originalBodyStream = context.Response.Body;
        CountingStream countingStream = new(originalBodyStream);
        context.Response.Body = countingStream;

        try
        {
            await next(context).ConfigureAwait(false);

            if (countingStream.BytesWritten > logThreshold)
            {
                logger.LogWarning("{msg}", $"Response to {HtmlEncode(context.Request.Path)} [{HtmlEncode(context.Request.Method)}] of type [{HtmlEncode(context.Request.Headers.Accept)}{(string.IsNullOrEmpty(context.Request.Headers.AcceptEncoding) ? string.Empty : $" + {HtmlEncode(context.Request.Headers.AcceptEncoding)}")}] " +
                    $"with Size: {countingStream.BytesWritten.GetFileSizeFromBytesWithUnits(2)}");
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
            await countingStream.DisposeAsync().ConfigureAwait(false);
            await originalBodyStream.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Extension method used to add the middleware to the HTTP request pipeline
/// </summary>
public static class ResponseSizeLoggingMiddlewareExtension
{
    public static IApplicationBuilder UseResponseSizeLogging(this IApplicationBuilder builder, long logThreshold = -1)
    {
        return builder.UseMiddleware<UseResponseSizeLoggingMiddleware>(logThreshold);
    }
}
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
