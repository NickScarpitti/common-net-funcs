# CommonNetFuncs.Web.Api

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Web.Api)](https://www.nuget.org/packages/CommonNetFuncs.Web.Api/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Web.Api)](https://www.nuget.org/packages/CommonNetFuncs.Web.Api/)

This project contains helper methods for several common functions required by API applications that interact with databases using Entity Framework Core. Works in combination with the BaseDbContextActions class in CommonNetFuncs.EFCore package.

## Contents

- [CommonNetFuncs.Web.Api](#commonnetfuncswebapi)
  - [Contents](#contents)
  - [GenericEndpoints](#genericendpoints)
    - [GenericEndpoints Usage Examples](#genericendpoints-usage-examples)
      - [CreateMany](#createmany)
      - [Delete](#delete)
      - [Patch](#patch)
  - [GenericMinimalEndpoints](#genericminimalendpoints)
    - [GenericMinimalEndpoints Usage Examples](#genericminimalendpoints-usage-examples)
      - [CreateMany Minimal API](#createmany-minimal-api)
      - [Patch Minimal API](#patch-minimal-api)
  - [GenericMinimalDtoEndpoints](#genericminimaldtoendpoints)
    - [GenericMinimalDtoEndpoints Usage Examples](#genericminimaldtoendpoints-usage-examples)
      - [CreateManyDto](#createmanydto)
      - [Update](#update)
  - [MsgPack](#msgpack)
    - [MsgPackRequestMiddleware](#msgpackrequestmiddleware)
      - [UseMsgPackRequestBody](#usemsgpackrequestbody)
    - [MsgPackOutputFilter](#msgpackoutputfilter)
      - [WithMsgPackOutput](#withmsgpackoutput)
    - [FlexibleDecimalResolver](#flexibledecimalresolver)
      - [Registering with a CompositeResolver](#registering-with-a-compositeresolver)
      - [Registering with AddMvc / AddControllers](#registering-with-addmvc--addcontrollers)
  - [Installation](#installation)
  - [License](#license)

---

## GenericEndpoints

Provides a set of reusable `ControllerBase` methods for common CRUD and patch operations in MVC controller-based API endpoints. Each method accepts an `IBaseDbContextActions` instance and returns an `ActionResult<T>`, making them easy to delegate to from thin controller actions.

### GenericEndpoints Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### CreateMany

```cs
[HttpPost("many")]
public Task<ActionResult<IEnumerable<MyEntity>>> CreateMany(IEnumerable<MyEntity> models)
    => _endpoints.CreateMany(models, _db);
```

#### Delete

```cs
[HttpDelete]
public Task<ActionResult<MyEntity>> Delete(MyEntity model)
    => _endpoints.Delete(model, _db);
```

#### Patch

Applies a JSON Patch document to an entity located by primary key. Returns `Ok` with the patched entity or `NoContent` if not found.

```cs
[HttpPatch("{id}")]
public Task<ActionResult<MyEntity>> Patch(int id, JsonPatchDocument<MyEntity> patch)
    => _endpoints.Patch<MyEntity, MyDbContext>(id, patch, _db);
```

</details>

## GenericMinimalEndpoints

Provides static methods for common CRUD and patch operations designed for use in ASP.NET Core minimal API endpoints. Each method accepts an `IBaseDbContextActions` instance and returns strongly-typed `Microsoft.AspNetCore.Http.HttpResults` results (`Results<Ok<T>, NoContent>` or `Results<Ok<T>, NoContent, ValidationProblem>`), making them directly usable as minimal API route handlers.

### GenericMinimalEndpoints Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### CreateMany Minimal API

Creates multiple entities and saves them to the database. Returns `Ok` with the created entities on success, or `NoContent` on failure.

```cs
app.MapPost("/entities", (IEnumerable<MyEntity> models, IBaseDbContextActions<MyEntity, MyDbContext> db) =>
    GenericMinimalEndpoints.CreateMany(models, db));
```

#### Patch Minimal API

Applies a JSON Patch document to an existing entity located by primary key. Validates the patched model and returns `Ok` with the updated entity, `ValidationProblem` if validation fails, or `NoContent` if the entity is not found.

```cs
app.MapPatch("/entities/{id}", (int id, JsonPatchDocument<MyEntity> patch, IBaseDbContextActions<MyEntity, MyDbContext> db) =>
    GenericMinimalEndpoints.Patch<MyEntity, MyDbContext>(id, patch, db));
```

</details>

---

## GenericMinimalDtoEndpoints

Provides static methods for common CRUD, patch, and update operations for minimal API endpoints that use separate input and output DTO types. Input DTOs are mapped to the entity model before database operations and the result is mapped to the output DTO before returning. Returns `Results<Ok<TOutDto>, NoContent>` or `Results<Ok<TOutDto>, NoContent, ValidationProblem>`.

### GenericMinimalDtoEndpoints Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### CreateManyDto

Creates multiple entities from input DTOs, saves them, and returns the created records mapped to the output DTO type.

```cs
app.MapPost("/entities", (IEnumerable<MyInDto> models, IBaseDbContextActions<MyEntity, MyDbContext> db) =>
    GenericMinimalDtoEndpoints.CreateMany<MyEntity, MyDbContext, MyInDto, MyOutDto>(models, db));
```

#### Update

Retrieves an existing entity by primary key, overwrites its properties from the input DTO, validates the result, and saves. Returns `Ok` with the updated record mapped to the output DTO, `ValidationProblem` if validation fails, or `NoContent` if the entity is not found.

```cs
app.MapPut("/entities/{id}", (int id, MyInDto dto, IBaseDbContextActions<MyEntity, MyDbContext> db) =>
    GenericMinimalDtoEndpoints.Update<MyEntity, MyDbContext, MyInDto, MyOutDto>(id, dto, db));
```

</details>

---

---

## MsgPack

A set of focused components for adding MessagePack support to ASP.NET Core minimal API applications. Unlike a single monolithic middleware, the functionality is split into a request middleware and an endpoint output filter so each can be applied independently.

### MsgPackRequestMiddleware

Converts a MessagePack-encoded request body to JSON before the endpoint handler runs, allowing standard `[FromBody]` parameter binding to work unchanged. Register it in the pipeline **before routing**.

<details>
<summary><h3>Usage Examples</h3></summary>

#### UseMsgPackRequestBody

Registers `MsgPackRequestMiddleware` globally so every endpoint accepts MessagePack request bodies.

```cs
// Program.cs
app.UseMsgPackRequestBody();

app.MapPost("/entities", (MyEntity entity) => Results.Ok(entity));
```

</details>

---

### MsgPackOutputFilter

An endpoint filter that intercepts the handler's return value before System.Text.Json serializes it. When the client's `Accept` header includes `application/x-msgpack`, the value is serialized directly to MessagePack with no JSON intermediate. Results that carry no body (204, 404 without body, redirects) and problem-detail results (`application/problem+json`) are passed through unchanged.

<details>
<summary><h3>Usage Examples</h3></summary>

#### WithMsgPackOutput

Attaches `MsgPackOutputFilter` to an endpoint or route group. Optionally accepts custom `MessagePackSerializerOptions`; defaults to `MessagePackSerializer.DefaultOptions` when `null`.

```cs
// Apply to a single endpoint
app.MapGet("/entities/{id}", (int id, IBaseDbContextActions<MyEntity, MyDbContext> db) =>
    GenericMinimalEndpoints.GetById(id, db))
    .WithMsgPackOutput();

// Apply to an entire route group
RouteGroupBuilder group = app.MapGroup("/entities").WithMsgPackOutput();
group.MapGet("/{id}", (int id) => Results.Ok(myEntity));
```

</details>

---

### FlexibleDecimalResolver

`FlexibleDecimalResolver` is a MessagePack `IFormatterResolver` that intercepts `decimal` and `decimal?` serialization and substitutes `FlexibleDecimalFormatter` / `FlexibleNullableDecimalFormatter` in place of the built-in `DecimalFormatter`.

**Why you need it:** The standard `DecimalFormatter` only accepts the string msgpack encoding that C# produces. JavaScript clients using [msgpackr](https://github.com/kriszyp/msgpackr) always encode JS `number` values as msgpack integers or floats, never as strings. Without this resolver those payloads throw a deserialization exception.

- `FlexibleDecimalFormatter` — handles `decimal`; deserializes from msgpack string, integer, or float.
- `FlexibleNullableDecimalFormatter` — handles `decimal?`; additionally handles the msgpack nil token.
- `FlexibleDecimalResolver` — resolver that routes `decimal` / `decimal?` to the two formatters above and returns `null` for everything else.

Register `FlexibleDecimalResolver` **before** `StandardResolver` (or any other resolver that handles decimals) in a `CompositeResolver`.

<details>
<summary><h3>Usage Examples</h3></summary>

#### Registering with a CompositeResolver

Use this approach for minimal API applications or any scenario where you supply `MessagePackSerializerOptions` directly (e.g. to `WithMsgPackOutput`).

```cs
using MessagePack;
using MessagePack.Resolvers;
using CommonNetFuncs.Web.Api.MsgPack;

// Build options that accept both C# string-encoded decimals and JS numeric decimals.
MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard
    .WithResolver(CompositeResolver.Create(
        FlexibleDecimalResolver.Instance,  // must come first
        StandardResolver.Instance));

// Pass to WithMsgPackOutput (optional – omit to use DefaultOptions)
app.MapGet("/entities/{id}", (int id) => Results.Ok(myEntity))
   .WithMsgPackOutput(options);

// Or set as the global default
MessagePackSerializer.DefaultOptions = options;
```

#### Registering with AddMvc / AddControllers

When using MVC controllers with the [MessagePack-CSharp ASP.NET Core formatter](https://github.com/MessagePack-CSharp/MessagePack-CSharp#aspnet-core-mvc-formatters), supply the options when adding the formatters.

```cs
using MessagePack;
using MessagePack.Resolvers;
using CommonNetFuncs.Web.Api.MsgPack;

MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard
    .WithResolver(CompositeResolver.Create(
        FlexibleDecimalResolver.Instance,
        StandardResolver.Instance));

builder.Services.AddControllers()
    .AddMessagePackFormatters(o =>
    {
        o.SerializerOptions = options;
    });
```

> **Note:** `FlexibleDecimalResolver.Instance` must appear **before** `StandardResolver.Instance` (or `ContractlessStandardResolver.Instance`) so that its `decimal` / `decimal?` registrations take precedence.

</details>

---

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Web.Api
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
