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
