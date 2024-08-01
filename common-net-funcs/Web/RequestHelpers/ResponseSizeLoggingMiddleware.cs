using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static Common_Net_Funcs.Conversion.UnitConversion;

namespace Common_Net_Funcs.Web.RequestHelpers;

/// <summary>
/// Middleware to expose response body size for development
/// NOT recommended for production environments
/// </summary>
public class ResponseSizeLoggingMiddleware(RequestDelegate next, ILogger<ResponseSizeLoggingMiddleware> logger)
{
    private readonly RequestDelegate next = next;
    private readonly ILogger<ResponseSizeLoggingMiddleware> logger = logger;

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
        logger.LogWarning("{msg}", $"Response to {context.Request.Path} [{context.Request.Method}] of type [{context.Request.Headers.Accept}{(string.IsNullOrEmpty(context.Request.Headers.AcceptEncoding) ? string.Empty : $" + { context.Request.Headers.AcceptEncoding }")}] " +
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
        return builder.UseMiddleware<ResponseSizeLoggingMiddleware>();
    }
}
