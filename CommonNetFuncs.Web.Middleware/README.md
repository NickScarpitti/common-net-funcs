# CommonNetFuncs.Web.Middleware

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Web.Middleware)](https://www.nuget.org/packages/CommonNetFuncs.Web.Middleware/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Web.Middleware)](https://www.nuget.org/packages/CommonNetFuncs.Web.Middleware/)

This project contains a collection of ASP.NET Core middleware components and endpoint filters covering response logging, caching, CORS, XSRF protection, custom headers, and JSON Patch support.

## Contents

- [CommonNetFuncs.Web.Middleware](#commonnetfuncswebmiddleware)
  - [Contents](#contents)
  - [JSON Patch Middleware](#json-patch-middleware)
    - [JSON Patch Middleware Usage Examples](#json-patch-middleware-usage-examples)
      - [Program.cs — registering the middleware and STJ converter](#programcs--registering-the-middleware-and-stj-converter)
      - [Sending a JSON Patch request](#sending-a-json-patch-request)
  - [MemoryCacheMiddleware](#memorycachemiddleware)
    - [MemoryCacheMiddleware Usage Examples](#memorycachemiddleware-usage-examples)
      - [UseMemoryCache](#usememorycache)
      - [Cache query parameters](#cache-query-parameters)
  - [OptionsMiddleware](#optionsmiddleware)
    - [OptionsMiddleware Usage Examples](#optionsmiddleware-usage-examples)
      - [UseOptions](#useoptions)
  - [UseCustomHeadersMiddleware](#usecustomheadersmiddleware)
    - [UseCustomHeadersMiddleware Usage Examples](#usecustomheadersmiddleware-usage-examples)
      - [UseCustomHeaders](#usecustomheaders)
  - [UseResponseLoggingEndpointFilter](#useresponseloggingendpointfilter)
    - [UseResponseLoggingEndpointFilter Usage Examples](#useresponseloggingendpointfilter-usage-examples)
      - [Program.cs setup](#programcs-setup)
  - [UseResponseLoggingFilter](#useresponseloggingfilter)
    - [UseResponseLoggingFilter Usage Examples](#useresponseloggingfilter-usage-examples)
      - [Program.cs setup](#programcs-setup-1)
  - [UseResponseSizeLoggingMiddleware](#useresponsesizeloggingmiddleware)
    - [UseResponseSizeLoggingMiddleware Usage Examples](#useresponsesizeloggingmiddleware-usage-examples)
      - [UseResponseSizeLogging](#useresponsesizelogging)
  - [UseXsrfTokenMiddleware](#usexsrftokenmiddleware)
    - [UseXsrfTokenMiddleware Usage Examples](#usexsrftokenmiddleware-usage-examples)
      - [UseXsrfToken](#usexsrftoken)
  - [Installation](#installation)
  - [License](#license)

---

## JSON Patch Middleware

Bridges the gap between JSON Patch requests (`Content-Type: application/json-patch+json`) and minimal API endpoints that accept `JsonPatchDocument<T>` parameters.

The minimal API parameter-binding pipeline uses System.Text.Json, which does not natively understand the `JsonPatchDocument<T>` wire format (a JSON array of operations) defined by Newtonsoft.Json. `JsonPatchDocumentConverterFactory` and `JsonPatchDocumentConverter<T>` bridge the two serializers: STJ captures the raw JSON and hands it to Newtonsoft.Json, which correctly constructs the document and strongly-types each operation's `value` field. Without this, complex object values in patch operations are left as `JsonElement` and fail when the patch is applied.

`JsonPatchRequestMiddleware` additionally normalizes the `Content-Type` header from `application/json-patch+json` to `application/json` as an explicit safety net, since some hosts or proxies may strip or alter structured-syntax suffixes.

### JSON Patch Middleware Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Program.cs — registering the middleware and STJ converter

Call `UseJsonPatchRequestBody()` early in the pipeline, before routing, so the Content-Type rewrite occurs before parameter binding reads the request body. Register `JsonPatchDocumentConverterFactory` in the JSON serializer options so that `JsonPatchDocument<T>` parameters are deserialized correctly.

```cs
using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.JsonPatch;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonPatchDocumentConverterFactory());
});

// Also add to MVC JSON options if using controllers
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonPatchDocumentConverterFactory());
});

WebApplication app = builder.Build();

// Must be before UseRouting / MapXxx calls
app.UseJsonPatchRequestBody();

app.UseRouting();

app.MapPatch("/api/items/{id}", (int id, JsonPatchDocument<Item> patch) =>
{
    Item item = GetItemById(id); // retrieve your entity
    patch.ApplyTo(item);
    return Results.Ok(item);
});

app.Run();
```

#### Sending a JSON Patch request

```http
PATCH /api/items/1 HTTP/1.1
Content-Type: application/json-patch+json

[
  { "op": "replace", "path": "/name", "value": "Updated Name" },
  { "op": "replace", "path": "/price", "value": 9.99 }
]
```

</details>

---

## MemoryCacheMiddleware

An in-process response cache backed by `IMemoryCache`. When a request includes the `useCache=true` query parameter the response is stored after the first successful (HTTP 200) call and served from memory on subsequent requests, bypassing the rest of the pipeline entirely. Cached entries can be associated with string tags and invalidated individually or by tag via the `evict=true` query parameter. Optional gzip/deflate compression reduces the in-memory footprint of large payloads. An optional `CacheMetrics` object can be injected to track hits, misses, evictions, and total cache size.

### MemoryCacheMiddleware Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### UseMemoryCache

```cs
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache(o => o.SizeLimit = 500 * 1024 * 1024); // 500 MB hard limit enforced by IMemoryCache
builder.Services.AddSingleton<CacheMetrics>();   // optional metrics tracking
builder.Services.AddSingleton<CacheTracker>();

CacheOptions cacheOptions = new()
{
    DefaultCacheDuration = TimeSpan.FromMinutes(5),
    MaxCacheSizeInBytes = 400 * 1024 * 1024, // soft limit — evicts oldest entries when exceeded
    UseCompression = true,
    CompressionType = ECompressionType.Gzip,
    HeadersToCache = ["Content-Type", "Content-Encoding"],
};

WebApplication app = builder.Build();

app.UseMemoryCache(cacheOptions); // register before routing

app.MapGet("/data", () => Results.Ok(GetExpensiveData()));

app.Run();
```

#### Cache query parameters

| Parameter          | Effect                                                       |
| ------------------ | ------------------------------------------------------------ |
| `useCache=true`    | Serve from cache or populate cache on first call             |
| `evict=true`       | Remove the matching cache entry before executing the request |
| `evictTag=<tag>`   | Remove all entries associated with the given tag             |
| `cacheSeconds=<n>` | Override default duration (seconds)                          |
| `cacheMinutes=<n>` | Override default duration (minutes)                          |
| `cacheHours=<n>`   | Override default duration (hours)                            |

</details>

---

## OptionsMiddleware

Handles HTTP `OPTIONS` preflight requests for CORS and attaches `Access-Control-*` response headers to all other requests that carry an `Origin` header. Configurable allowed origins, headers, methods, credentials, and max-age.

### OptionsMiddleware Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### UseOptions

```cs
using CommonNetFuncs.Web.Middleware;

WebApplication app = builder.Build();

app.UseOptions(
    defaultAllowedOrigin: "https://example.com",
    allowedHeaders: ["Content-Type", "Authorization", "X-XSRF-TOKEN"],
    allowedMethods: ["GET", "POST", "PUT", "PATCH", "DELETE"],
    allowCredentials: true,
    maxAge: 3600
);
```

</details>

---

## UseCustomHeadersMiddleware

Adds or removes HTTP response headers on every response. Useful for injecting security headers or stripping server-identifying headers.

### UseCustomHeadersMiddleware Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### UseCustomHeaders

```cs
using CommonNetFuncs.Web.Middleware;

WebApplication app = builder.Build();

app.UseCustomHeaders(
    addHeaders: new Dictionary<string, string>
    {
        { "X-Frame-Options", "DENY" },
        { "X-Content-Type-Options", "nosniff" },
    },
    removeHeaders: ["Server", "X-Powered-By"]
);
```

</details>

---

## UseResponseLoggingEndpointFilter

An `IEndpointFilter` for minimal API endpoints that emits a `Warning` log when an endpoint's total execution time exceeds a configurable threshold. Apply it per-endpoint or to a route group.

### UseResponseLoggingEndpointFilter Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Program.cs setup

Register `IResponseLoggingConfig` and apply the filter to a route group or individual endpoints.

```cs
using CommonNetFuncs.Web.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IResponseLoggingConfig>(new ResponseLoggingConfig { ThresholdInSeconds = 2.0 });
builder.Services.AddSingleton<UseResponseLoggingEndpointFilter>();

WebApplication app = builder.Build();

// Apply to all endpoints in a group
RouteGroupBuilder api = app.MapGroup("/api")
    .AddEndpointFilter<UseResponseLoggingEndpointFilter>();

api.MapGet("/slow", async () =>
{
    await Task.Delay(3000);
    return Results.Ok("done");
});

app.Run();
// Warning logged: "Endpoint GET /api/slow took 00:00:03.0123456 to complete with status code: 200"
```

</details>

---

## UseResponseLoggingFilter

An MVC `IActionFilter` that emits a `Warning` log when a controller action's execution time exceeds a configurable threshold. Register it as a global MVC filter.

### UseResponseLoggingFilter Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Program.cs setup

```cs
using CommonNetFuncs.Web.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IResponseLoggingConfig>(new ResponseLoggingConfig { ThresholdInSeconds = 2.0 });

builder.Services.AddControllers(options =>
{
    options.Filters.Add<UseResponseLoggingFilter>();
});
```

</details>

---

## UseResponseSizeLoggingMiddleware

Wraps the response body stream in a counting stream to measure the number of bytes written. Emits a `Warning` log when the response exceeds a configurable byte threshold. Intended for development and staging environments to detect unexpectedly large responses.

### UseResponseSizeLoggingMiddleware Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### UseResponseSizeLogging

```cs
using CommonNetFuncs.Web.Middleware;

WebApplication app = builder.Build();

// Log a warning whenever a response exceeds 1 MB
app.UseResponseSizeLogging(logThreshold: 1 * 1024 * 1024);
```

</details>

---

## UseXsrfTokenMiddleware

Generates an anti-forgery token on every request and writes it to an `XSRF-TOKEN` cookie, enabling client-side frameworks (Angular, etc.) to read the token and send it back in a request header for CSRF protection.

### UseXsrfTokenMiddleware Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### UseXsrfToken

```cs
using CommonNetFuncs.Web.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();

WebApplication app = builder.Build();

// httpOnly: false — the cookie must be readable by JavaScript (required for SPA frameworks)
app.UseXsrfToken(httpOnly: false);
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Web.Middleware
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
