# CommonNetFuncs.Web.Common

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Web.Common)](https://www.nuget.org/packages/CommonNetFuncs.Web.Common/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Web.Common)](https://www.nuget.org/packages/CommonNetFuncs.Web.Common/)

This lightweight project contains helper methods for several common functions required by applications.

## Contents

- [CommonNetFuncs.Web.Common](#commonnetfuncswebcommon)
  - [Contents](#contents)
  - [ContentTypes](#contenttypes)
    - [ContentTypes Usage Examples](#contenttypes-usage-examples)
      - [GetContentType / GetContentTypeByExtension](#getcontenttype--getcontenttypebyextension)
  - [PascalCaseJsonNamingPolicy](#pascalcasejsonnamingpolicy)
    - [PascalCaseJsonNamingPolicy Usage Examples](#pascalcasejsonnamingpolicy-usage-examples)
  - [SecurityHeadersStore](#securityheadersstore)
    - [SecurityHeadersStore Usage Examples](#securityheadersstore-usage-examples)
  - [UriHelpers](#urihelpers)
    - [UriHelpers Usage Examples](#urihelpers-usage-examples)
      - [ListToQueryParameters](#listtoqueryparameters)
  - [Installation](#installation)
  - [License](#license)

---

## ContentTypes

A static class exposing MIME type constants for common file formats (JSON, images, Office documents, media, web types) and a `GetContentType` extension method that resolves the MIME type from a file name or extension.

### ContentTypes Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetContentType / GetContentTypeByExtension

```cs
using CommonNetFuncs.Web.Common;

string mime = "report.xlsx".GetContentType(); // "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
string mime = ContentTypes.GetContentTypeByExtension(".mp4"); // "video/mp4"

// Use a constant directly
string jsonMime = ContentTypes.Json; // "application/json"
```

</details>

---

## PascalCaseJsonNamingPolicy

A `JsonNamingPolicy` implementation that capitalizes the first letter of every property name. Use it when your JSON consumer expects PascalCase names but your C# models use camelCase conventions.

### PascalCaseJsonNamingPolicy Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

```cs
using CommonNetFuncs.Web.Common;

JsonSerializerOptions options = new()
{
    PropertyNamingPolicy = new PascalCaseJsonNamingPolicy()
};

// { "Name": "Alice", "Age": 30 } instead of { "name": "Alice", "age": 30 }
string json = JsonSerializer.Serialize(new { name = "Alice", age = 30 }, options);
```

</details>

---

## SecurityHeadersStore

Provides a frozen dictionary of recommended security response headers (`X-Xss-Protection`, `X-Frame-Options`, `Referrer-Policy`, `X-Content-Type-Options`, `X-Permitted-Cross-Domain-Policies`, `Content-Security-Policy`) and a list of server-identifying headers to remove (`Server`, `X-Powered-By`). Intended to be used with `UseCustomHeadersMiddleware` from `CommonNetFuncs.Web.Middleware`.

### SecurityHeadersStore Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

```cs
using CommonNetFuncs.Web.Common;
using CommonNetFuncs.Web.Middleware;

WebApplication app = builder.Build();

app.UseCustomHeaders(
    addHeaders: SecurityHeadersStore.SecurityHeaders,
    removeHeaders: SecurityHeadersStore.HeadersToRemove
);
```

</details>

---

## UriHelpers

Extension methods for building and parsing query strings and URIs, including converting lists and key-value pairs into well-formed query parameter strings.

### UriHelpers Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ListToQueryParameters

```cs
using CommonNetFuncs.Web.Common;

// From a list of values with a shared key
IEnumerable<int> ids = [1, 2, 3];
string qs = ids.ListToQueryParameters("id"); // "id=1&id=2&id=3"

// From a collection of key-value pairs
IEnumerable<KeyValuePair<string, string>> filters =
[
    new("status", "active"),
    new("role", "admin")
];
string qs = filters.ListToQueryParameters(); // "status=active&role=admin"
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Web.Common
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
