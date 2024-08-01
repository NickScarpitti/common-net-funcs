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
