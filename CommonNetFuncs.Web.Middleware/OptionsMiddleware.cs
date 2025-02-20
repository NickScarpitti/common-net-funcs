using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CommonNetFuncs.Web.Middleware;

// Custom middleware for OPTIONS requests
public static class OptionsMiddlewareExtensions
{
    public static IApplicationBuilder UseOptions(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OptionsMiddleware>();
    }
}

public class OptionsMiddleware
{
    private readonly RequestDelegate _next;

    public OptionsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method.StrComp("OPTIONS"))
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", context.Request.Headers["Origin"].ToString());
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.StatusCode = 200;
            return;
        }

        await _next(context);
    }
}
