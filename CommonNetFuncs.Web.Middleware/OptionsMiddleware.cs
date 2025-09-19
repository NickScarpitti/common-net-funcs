using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Web.Middleware;

// Custom middleware for OPTIONS requests
public static class OptionsMiddlewareExtensions
{
  public static IApplicationBuilder UseOptions(this IApplicationBuilder builder, string defaultAllowedOrigin = "*", string[]? allowedHeaders = null, string[]? allowedMethods = null,
        bool allowCredentials = true, int? maxAge = 3600, HttpStatusCode defaultStatusCode = HttpStatusCode.OK)
  {
    return builder.UseMiddleware<OptionsMiddleware>(defaultAllowedOrigin, allowedHeaders ?? [], allowedMethods ?? [], allowCredentials, maxAge, defaultStatusCode);
  }
}

public sealed class OptionsMiddleware(RequestDelegate next, string defaultAllowedOrigin, string[] allowedHeaders, string[] allowedMethods,
        bool allowCredentials, int maxAge, HttpStatusCode defaultStatusCode)
{
  private readonly RequestDelegate next = next ?? throw new ArgumentNullException(nameof(next), "RequestDelegate \"next\" cannot be null!");

  private readonly string defaultAllowedOrigin = defaultAllowedOrigin;
  private readonly string allowedHeaders = string.Join(", ", allowedHeaders);
  private readonly string allowedMethods = string.Join(", ", allowedMethods).ToUpper();
  private readonly string allowCredentials = allowCredentials.ToString().ToLower();
  private readonly string maxAge = maxAge.ToString();
  private readonly int defaultStatusCode = (int)defaultStatusCode;

  public async Task InvokeAsync(HttpContext context)
  {
    // Handle preflight OPTIONS request
    if (context.Request.Method.StrComp("OPTIONS"))
    {
      string origin = context.Request.Headers.Origin.ToString();
      if (string.IsNullOrWhiteSpace(origin))
      {
        origin = defaultAllowedOrigin;
      }

      context.Response.Headers.AccessControlAllowOrigin = origin;
      context.Response.Headers.AccessControlAllowHeaders = allowedHeaders;
      context.Response.Headers.AccessControlAllowMethods = allowedMethods;
      context.Response.Headers.AccessControlAllowCredentials = allowCredentials;
      context.Response.Headers.AccessControlMaxAge = maxAge;
      context.Response.StatusCode = defaultStatusCode;
      return;
    }

    // Add CORS headers for non-OPTIONS requests
    string requestOrigin = context.Request.Headers.Origin.ToString();
    if (!string.IsNullOrWhiteSpace(requestOrigin))
    {
      context.Response.Headers.AccessControlAllowOrigin = requestOrigin;
      context.Response.Headers.AccessControlAllowCredentials = allowCredentials;
    }

    await next(context).ConfigureAwait(false);
  }
}
