# CommonNetFuncs.Web.Api

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Web.Api)](https://www.nuget.org/packages/CommonNetFuncs.Web.Api/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Web.Api)](https://www.nuget.org/packages/CommonNetFuncs.Web.Api/)

This project contains helper methods for several common functions required by API applications that interact with databases using Entity Framework Core. Works in combination with the BaseDbContextActions class in CommonNetFuncs.EFCore package.

## Contents

- [CommonNetFuncs.Web.Api](#commonnetfuncswebapi)
  - [Contents](#contents)
  - [GenericEndpoints](#genericendpoints)
    - [\[Class Name\] Usage Examples](#class-name-usage-examples)
      - [\[MethodNameHere\]](#methodnamehere)
  - [GenericMinimalEndpoints](#genericminimalendpoints)
    - [GenericMinimalEndpoints Usage Examples](#genericminimalendpoints-usage-examples)
      - [CreateMany](#createmany)
      - [Patch](#patch)
  - [GenericMinimalDtoEndpoints](#genericminimaldtoendpoints)
    - [GenericMinimalDtoEndpoints Usage Examples](#genericminimaldtoendpoints-usage-examples)
      - [CreateMany](#createmany-1)
      - [Update](#update)
  - [MinimalMsgPackMiddleware](#minimalmsgpackmiddleware)
    - [MinimalMsgPackMiddleware Usage Examples](#minimalmsgpackmiddleware-usage-examples)
      - [UseContentNegotiationMiddleware](#usecontentnegotiationmiddleware)
  - [Installation](#installation)
  - [License](#license)

---

## GenericEndpoints

[Description here]

### [Class Name] Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

[Method Description here]

```cs
//Code here
```

</details>

## GenericMinimalEndpoints

Provides static methods for common CRUD and patch operations designed for use in ASP.NET Core minimal API endpoints. Each method accepts an `IBaseDbContextActions` instance and returns strongly-typed `Microsoft.AspNetCore.Http.HttpResults` results (`Results<Ok<T>, NoContent>` or `Results<Ok<T>, NoContent, ValidationProblem>`), making them directly usable as minimal API route handlers.

### GenericMinimalEndpoints Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### CreateMany

Creates multiple entities and saves them to the database. Returns `Ok` with the created entities on success, or `NoContent` on failure.

```cs
app.MapPost("/entities", (IEnumerable<MyEntity> models, IBaseDbContextActions<MyEntity, MyDbContext> db) =>
    GenericMinimalEndpoints.CreateMany(models, db));
```

#### Patch

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

#### CreateMany

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

## MinimalMsgPackMiddleware

Middleware that adds transparent MessagePack content negotiation to minimal API endpoints. When a request carries `Content-Type: application/x-msgpack`, the body is converted to JSON before the endpoint handler runs so that standard `[FromBody]` binding works unchanged. When a request carries `Accept: application/x-msgpack`, the JSON response body is converted to MessagePack before it reaches the client.

Register once on a route group or the whole application using the `UseContentNegotiationMiddleware` extension method.

### MinimalMsgPackMiddleware Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### UseContentNegotiationMiddleware

Registers the middleware globally so every endpoint in the application supports MessagePack request and response bodies.

```cs
// Program.cs
app.UseContentNegotiationMiddleware();

app.MapPost("/entities", (MyEntity entity) => Results.Ok(entity));
```

</details>

---

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Web.Api
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
