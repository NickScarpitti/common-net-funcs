using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Common_Net_Funcs.Web.RequestHelpers;

public class UseCustomHeadersMiddleware(RequestDelegate next, IDictionary<string, string>? addHeaders = null, IEnumerable<string>? removeHeaders = null)
{
    private readonly RequestDelegate next = next;
    private readonly IEnumerable<string>? removeHeaders = removeHeaders;
    private readonly IDictionary<string, string>? addHeaders = addHeaders;

    public async Task InvokeAsync(HttpContext context)
    {
        if (removeHeaders?.Any() == true)
        {
            foreach (string header in removeHeaders)
            {
                context.Response.Headers.Remove(header);
            }
        }

        if (addHeaders?.Any() == true)
        {
            foreach (KeyValuePair<string, string> header in addHeaders)
            {
                context.Response.Headers.TryAdd(header.Key, header.Value);
            }
        }

        await next(context);
    }
}

/// <summary>
/// Extension method used to add the middleware to the HTTP request pipeline
/// </summary>
public static class UseCustomHeadersMiddlewareExtension
{
    public static IApplicationBuilder UseCustomHeaders(this IApplicationBuilder builder, IDictionary<string, string>? addHeaders = null, IEnumerable<string>? removeHeaders = null)
    {
        return builder.UseMiddleware<UseCustomHeadersMiddleware>(addHeaders, removeHeaders);
    }
}

public static class DefaultSecurityHeaders
{
    public const string XrfHeader = "X-XSRF-TOKEN";

    public static readonly Dictionary<string, string> SecurityHeaders = new()
    {
        //{ "Cache-Control", "no-cache, no-store" }, //This will prevent browser from caching JS files and is already added to controller responses
        { "X-Xss-Protection", "1; mode=block" },
        { "X-Frame-Options", "DENY" },
        { "Referrer-Policy", "no-referrer" },
        { "X-Content-Type-Options", "nosniff" },
        { "X-Permitted-Cross-Domain-Policies", "none" },
        { "Content-Security-Policy", "block-all-mixed-content; upgrade-insecure-requests; script-src 'self' http://cdnjs.cloudflare.com/ http://cdn.jsdelivr.net/; object-src 'self';" }
    };

    public static readonly List<string> HeadersToRemove = ["Server", "X-Powered-By"];
}
