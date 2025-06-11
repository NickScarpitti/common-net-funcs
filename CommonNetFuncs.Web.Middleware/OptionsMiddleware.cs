using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Web.Middleware;

// Custom middleware for OPTIONS requests
public static class OptionsMiddlewareExtensions
{
    public static IApplicationBuilder UseOptions(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OptionsMiddleware>();
    }
}

public sealed class OptionsMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;

    //public async Task InvokeAsync(HttpContext context)
    //{
    //    if (context.Request.Method.StrComp("OPTIONS"))
    //    {
    //        // Get origin, defaulting to "*" if null
    //        string origin = context.Request.Headers.Origin.ToString();
    //        if (string.IsNullOrWhiteSpace(origin))
    //        {
    //            origin = "*";
    //        }

    //        context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
    //        context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, X-XSRF-TOKEN");
    //        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
    //        context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
    //        context.Response.Headers.Append("Access-Control-Max-Age", "3600");
    //        context.Response.StatusCode = 200;
    //        return;
    //    }

    //    await _next(context).ConfigureAwait(false);
    //}

    public async Task InvokeAsync(HttpContext context)
    {
        // Handle preflight OPTIONS request
        if (context.Request.Method.StrComp("OPTIONS"))
        {
            string origin = context.Request.Headers.Origin.ToString();
            if (string.IsNullOrWhiteSpace(origin))
            {
                origin = "*";
            }

            context.Response.Headers.AccessControlAllowOrigin = origin;
            context.Response.Headers.AccessControlAllowHeaders = "Content-Type, Authorization, X-Requested-With, X-XSRF-TOKEN";
            context.Response.Headers.AccessControlAllowMethods = "GET, POST, PUT, DELETE, OPTIONS";
            context.Response.Headers.AccessControlAllowCredentials = "true";
            context.Response.Headers.AccessControlMaxAge = "3600";
            context.Response.StatusCode = 200;
            return;
        }

        // Add CORS headers for non-OPTIONS requests
        string requestOrigin = context.Request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(requestOrigin))
        {
            context.Response.Headers.AccessControlAllowOrigin = requestOrigin;
            context.Response.Headers.AccessControlAllowCredentials = "true";
        }

        await next(context).ConfigureAwait(false);
    }
}
