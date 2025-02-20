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

public class OptionsMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method.StrComp("OPTIONS"))
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", context.Request.Headers.Origin.ToString());
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, X-XSRF-TOKEN");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Append("Access-Control-Max-Age", "3600");
            context.Response.StatusCode = 200;
            return;
        }

        await _next(context).ConfigureAwait(false);
    }
}
