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
public class UseResponseSizeLoggingMiddleware(RequestDelegate next, ILogger<UseResponseSizeLoggingMiddleware> logger)
{
    private readonly RequestDelegate next = next;
    private readonly ILogger<UseResponseSizeLoggingMiddleware> logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        //Hold the original response stream
        Stream originalResponseBodyStream = context.Response.Body;

        //Create a new memory stream to hold the response
        await using MemoryStream responseBodyStream = new();
        context.Response.Body = responseBodyStream;

        //Call the next middleware in the pipeline
        await next(context);

        //Log the response size
        logger.LogWarning("{msg}", $"Response to {HtmlEncode(context.Request.Path)} [{HtmlEncode(context.Request.Method)}] of type [{HtmlEncode(context.Request.Headers.Accept)}{(string.IsNullOrEmpty(context.Request.Headers.AcceptEncoding) ? string.Empty : $" + {HtmlEncode(context.Request.Headers.AcceptEncoding)}")}] " +
            $"with Size: {responseBodyStream.Length.GetFileSizeFromBytesWithUnits(2)}");

        //Copy the contents of the new memory stream to the original response stream
        responseBodyStream.Position = 0;
        await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        context.Response.Body = originalResponseBodyStream;
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
