# CommonNetFuncs.Web.Requests

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Web.Requests)](https://www.nuget.org/packages/CommonNetFuncs.Web.Requests)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Web.Requests)](https://www.nuget.org/packages/CommonNetFuncs.Web.Requests/)

This lightweight project contains helper methods for several common functions required by applications.

## Contents

- [CommonNetFuncs.Web.Requests](#commonnetfuncswebrequests)
  - [Contents](#contents)
  - [RestHelpers](#resthelpers)
    - [RestHelpers Usage Examples](#resthelpers-usage-examples)
      - [RestRequest](#restrequest)
      - [StreamingRestRequest](#streamingrestrequest)
      - [RestRequestObject](#restrequestobject)
  - [RestHelpersWrapper](#resthelperswrapper)
    - [RestHelpersWrapper Usage Examples](#resthelperswrapper-usage-examples)
      - [Basic Usage](#basic-usage)
      - [RestHelperOptionsDefaultConfig](#resthelperoptionsdefaultconfig)
      - [Dependency Injection Setup](#dependency-injection-setup)
  - [PatchCreator](#patchcreator)
    - [PatchCreator Usage Examples](#patchcreator-usage-examples)
      - [CreatePatch](#createpatch)
  - [DistributedCacheExtensions](#distributedcacheextensions)
    - [DistributedCacheExtensions Usage Examples](#distributedcacheextensions-usage-examples)
  - [JsonPatchFormatter](#jsonpatchformatter)
    - [JsonPatchFormatter Usage Examples](#jsonpatchformatter-usage-examples)
  - [Installation](#installation)
  - [License](#license)

---

## RestHelpers

A generic HTTP client wrapper that sends typed REST requests and deserializes responses. Built on a long-lived `SocketsHttpHandler`-backed `HttpClient` with configurable keep-alive, per-request bearer tokens, custom headers, timeouts, and optional MessagePack serialization. All requests are made via a `RequestOptions<TBody>` configuration object so callers never construct `HttpRequestMessage` by hand.

### RestHelpers Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### RestRequest

Sends a request and returns a deserialized response, or `null` on failure.

```cs
using CommonNetFuncs.Web.Requests.Rest;

RestHelpers rest = new();

MyResponse? response = await rest.RestRequest<MyResponse, MyBody>(
    new RequestOptions<MyBody>
    {
        Url = "https://api.example.com/items",
        HttpMethod = HttpMethod.Post,
        BodyObject = new MyBody { Name = "Widget" },
        BearerToken = "my-token",
        Timeout = 30, // seconds
    }
);
```

#### StreamingRestRequest

Streams a newline-delimited JSON (NDJSON) response as an `IAsyncEnumerable<T>`, useful for large or server-sent data sets.

```cs
await foreach (MyItem? item in rest.StreamingRestRequest<MyItem, object?>(
    new RequestOptions<object?> { Url = "https://api.example.com/stream", HttpMethod = HttpMethod.Get }))
{
    if (item is not null) Process(item);
}
```

#### RestRequestObject

Returns a `RestObject<TResponse>` wrapping both the deserialized response and the raw `HttpResponseMessage`, giving access to status codes and headers.

```cs
RestObject<MyResponse> result = await rest.RestRequestObject<MyResponse, object?>(
    new RequestOptions<object?> { Url = "https://api.example.com/items/1", HttpMethod = HttpMethod.Get }
);

if (result.Response?.IsSuccessStatusCode == true)
{
    MyResponse? data = result.Result;
}
```

</details>

---

## RestHelpersWrapper

A higher-level HTTP client facade built on top of `RestHelpers`. It adds automatic retry/resilience logic, bearer token management (including automatic refresh on 401/403), and support for a shared `RestHelperOptionsDefaultConfig` that applies common settings across all calls made through the same wrapper instance.

### RestHelpersWrapper Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Basic Usage

Make typed HTTP calls with built-in retry and resilience without manually constructing `HttpRequestMessage`.

```cs
using CommonNetFuncs.Web.Requests.Rest.Options;
using CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

RestHelpersWrapper wrapper = new(restClientFactory);

// GET
MyResponse? item = await wrapper.Get<MyResponse>(
    new RestHelperOptions("api/items/1", "MyApi",
        ResilienceOptions: new ResilienceOptions(MaxRetry: 3, RetryDelay: 500)));

// POST
MyResponse? created = await wrapper.PostRequest(
    new RestHelperOptions("api/items", "MyApi"),
    new MyRequest { Name = "Widget" });

// PATCH (diffs old vs new and sends only changed fields)
MyResponse? updated = await wrapper.PatchRequest(
    new RestHelperOptions("api/items/1", "MyApi"), newModel, oldModel);

// DELETE
MyResponse? deleted = await wrapper.DeleteRequest<MyResponse>(
    new RestHelperOptions("api/items/1", "MyApi"));
```

#### RestHelperOptionsDefaultConfig

`RestHelperOptionsDefaultConfig` lets you define fallback values applied to every call made through a `RestHelpersWrapper` instance. Per-call options always take precedence; defaults only fill in what is `null` on the per-call `RestHelperOptions` (except `UseBearerToken`, which always overrides when non-`null`).

| Property | Behavior |
|---|---|
| `UseBearerToken` | When non-`null`, always overrides the per-call value |
| `ResilienceOptions.GetBearerTokenFunc` | Fills in if the per-call options has no `GetBearerTokenFunc` |
| `JsonSerializerOptions` | Fills in if `null` on the per-call options |
| `MsgPackOptions` | Fills in if `null` on the per-call options |
| `CompressionOptions` | Fills in if `null` on the per-call options |

```cs
using CommonNetFuncs.Web.Requests;
using CommonNetFuncs.Web.Requests.Rest.Options;
using CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

RestHelperOptionsDefaultConfig defaultConfig = new()
{
    // Always enforce bearer token auth for every call through this wrapper.
    // When non-null, overrides the per-call UseBearerToken value.
    UseBearerToken = true,

    // Token retrieval function. Receives the API name and a flag indicating whether
    // a forced refresh is required (set automatically after a 401/403 response).
    ResilienceOptions = new ResilienceOptions(
        MaxRetry: 3,
        RetryDelay: 500,
        GetBearerTokenFunc: async (apiName, forceRefresh) =>
            await tokenProvider.GetTokenAsync(apiName, forceRefresh)
    ),

    // Default JSON options applied when not specified per call
    JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true },

    // Default compression applied when not specified per call
    CompressionOptions = new CompressionOptions(UseCompression: true, CompressionType: ECompressionType.Gzip)
};

RestHelpersWrapper wrapper = new(restClientFactory, defaultConfig);

// Auth, serializer, and compression settings are applied automatically -
// no need to repeat them on every call.
MyResponse? result = await wrapper.Get<MyResponse>(new RestHelperOptions("api/items", "MyApi"));
```

#### Dependency Injection Setup

Register `RestHelperOptionsDefaultConfig` as a singleton. The DI container automatically injects it into `RestHelpersWrapper` via its two-parameter constructor when present; without it, the single-parameter constructor is used and no defaults are applied.

```cs
using CommonNetFuncs.Web.Requests.Rest.Options;
using CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

// In Program.cs / Startup.cs

// 1. Register your token provider (if using bearer token auth)
builder.Services.AddSingleton<ITokenProvider, MyTokenProvider>();

// 2. Register the default config, resolving dependencies from the container
builder.Services.AddSingleton(sp =>
{
    ITokenProvider tokenProvider = sp.GetRequiredService<ITokenProvider>();
    return new RestHelperOptionsDefaultConfig
    {
        UseBearerToken = true,
        ResilienceOptions = new ResilienceOptions(
            MaxRetry: 3,
            RetryDelay: 500,
            GetBearerTokenFunc: (apiName, forceRefresh) =>
                tokenProvider.GetTokenAsync(apiName, forceRefresh)
        ),
        JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    };
});

// 3. Register the named HttpClient, IRestClientFactory, and RestHelpersWrapper
builder.Services.AddRestClient("MyApi", client =>
    client.BaseAddress = new Uri("https://api.example.com/"));

// 4. Inject RestHelpersWrapper wherever needed
public class MyService(RestHelpersWrapper wrapper)
{
    public async Task<MyResponse?> GetItemAsync(int id) =>
        await wrapper.Get<MyResponse>(new RestHelperOptions($"api/items/{id}", "MyApi"));

    public async Task<MyResponse?> CreateItemAsync(MyRequest request) =>
        await wrapper.PostRequest(new RestHelperOptions("api/items", "MyApi"), request);
}
```

> **Tip:** When only sharing serializer or compression defaults without authentication, omit `UseBearerToken` (or leave it `null`) and no bearer token logic is applied.

</details>

---

## PatchCreator

Creates a Newtonsoft.Json `JsonPatchDocument` by diffing two objects of the same type. Compares property values and generates `add`, `remove`, and `replace` operations for every changed field, including nested objects.

### PatchCreator Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### CreatePatch

```cs
using CommonNetFuncs.Web.Requests;
using Microsoft.AspNetCore.JsonPatch;

MyEntity original = await GetFromDb(id);
MyEntity modified = original with { Name = "New Name", Price = 9.99m };

JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);
// patch.Operations => [ { op: "replace", path: "/Name", value: "New Name" }, { op: "replace", path: "/Price", value: 9.99 } ]

// Send via REST
await rest.RestRequest<MyEntity, HttpContent>(
    new RequestOptions<HttpContent>
    {
        Url = $"https://api.example.com/items/{id}",
        HttpMethod = HttpMethod.Patch,
        PatchDocument = patch.ToStringContent(),
    }
);
```

</details>

---

## DistributedCacheExtensions

Generic `IDistributedCache` extension methods for storing and retrieving strongly-typed objects serialized with System.Text.Json.

### DistributedCacheExtensions Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

```cs
using CommonNetFuncs.Web.Requests;

// Store
await cache.SetAsync("user:42", myUser, new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
});

// Retrieve synchronously
if (cache.TryGetValue("user:42", out MyUser? user))
{
    // cache hit
}

// Retrieve asynchronously
MyUser? user = await cache.TryGetValueAsync<MyUser>("user:42");
```

</details>

---

## JsonPatchFormatter

Provides a pre-configured `NewtonsoftJsonPatchInputFormatter` for use with MVC controller-based APIs that accept `JsonPatchDocument<T>` as a request body. Insert it as the first input formatter so Newtonsoft.Json handles JSON Patch deserialization while the rest of the pipeline uses System.Text.Json.

### JsonPatchFormatter Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

```cs
using CommonNetFuncs.Web.Requests;

builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, JsonPatchFormatter.JsonPatchInputFormatter());
});
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Web.Requests
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
