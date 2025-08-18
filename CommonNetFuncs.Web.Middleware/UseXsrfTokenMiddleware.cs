using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CommonNetFuncs.Web.Middleware;

public sealed class UseXsrfTokenMiddleware(RequestDelegate next, IAntiforgery antiforgery, bool httpOnly)
{
    private readonly RequestDelegate next = next;
    private readonly IAntiforgery antiforgery = antiforgery;
    private readonly bool httpOnly = httpOnly;

    public async Task InvokeAsync(HttpContext context)
    {
        AntiforgeryTokenSet tokenSet = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("XSRF-TOKEN", tokenSet.RequestToken!, new CookieOptions { HttpOnly = httpOnly });
        await next(context).ConfigureAwait(false);
    }
}

/// <summary>
/// Extension method used to add the middleware to the HTTP request pipeline
/// </summary>
public static class UseXsrfTokenMiddlewareExtension
{
    public static IApplicationBuilder UseXsrfToken(this IApplicationBuilder builder, bool httpOnly = false)
    {
        return builder.UseMiddleware<UseXsrfTokenMiddleware>(httpOnly);
    }
}
