using CommonNetFuncs.Core;
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

			if (countingStream.BytesWritten > logThreshold && logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Response to {RequestPath} [{RequestMethod}] of type [{RequestHeadersAccept}{AcceptEncoding}] with Size: {ResponseSize}",
					context.Request.Path, context.Request.Method, context.Request.Headers.Accept, string.IsNullOrEmpty(context.Request.Headers.AcceptEncoding) ? string.Empty : $" + {HtmlEncode(context.Request.Headers.AcceptEncoding)}", countingStream.BytesWritten.GetFileSizeFromBytesWithUnits(2));
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
