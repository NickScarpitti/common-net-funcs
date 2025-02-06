namespace CommonNetFuncs.Web.Common;

public static class SecurityHeadersStore
{
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
