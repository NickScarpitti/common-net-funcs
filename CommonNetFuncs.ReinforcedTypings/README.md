# CommonNetFuncs.ReinforcedTypings

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.ReinforcedTypings)](https://www.nuget.org/packages/CommonNetFuncs.ReinforcedTypings/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.ReinforcedTypings)](https://www.nuget.org/packages/CommonNetFuncs.ReinforcedTypings/)

This project extends [Reinforced.Typings](https://github.com/reinforced/Reinforced.Typings) with a fluent configuration hook that adds three capabilities on top of RT's normal attribute-based interface/class/enum export:

- **TsConst**: Generates `as const` TypeScript object literals from `public const`/`public static readonly` fields on a static C# class - of any field type.
- **TsCollection**: Generates `as const` TypeScript array-literal collections from `public const`/`public static readonly` array/collection fields on a static C# class, cross-referencing the real generated TypeScript type for any element type that is itself exported via `[TsInterface]`/`[TsClass]`/`[TsEnum]`.
- **Valibot schema generation**: Generates a companion [Valibot](https://valibot.dev/) validation schema (`.schema.ts`) next to the interface RT generates for any class decorated with both `[TsInterface]` and `[GenerateValibotSchema]`, derived from the class's `System.ComponentModel.DataAnnotations` validation attributes.

All of the above honor the resolved `[TsGlobal]`/fluent `Global(...)` settings (`WriteWarningComment`, `TabSymbol`, `NewLine`, `CamelCaseForProperties`, `UnresolvedToUnknown`, `UseModules`, `DiscardNamespacesWhenUsingModules`, `RootNamespace`, `ExportPureTypings`) so the hand-written output matches the same conventions Reinforced.Typings itself would use for the rest of the project.

## Contents

- [CommonNetFuncs.ReinforcedTypings](#commonnetfuncsreinforcedtypings)
  - [Contents](#contents)
  - [Setup](#setup)
  - [TsConst](#tsconst)
    - [TsConst Usage Examples](#tsconst-usage-examples)
      - [Export all fields, suppress one](#export-all-fields-suppress-one)
      - [Export only selected fields](#export-only-selected-fields)
      - [Non-string field types](#non-string-field-types)
  - [TsCollection](#tscollection)
    - [TsCollection Usage Examples](#tscollection-usage-examples)
      - [Export all fields, suppress one](#export-all-fields-suppress-one-1)
      - [Export only selected fields](#export-only-selected-fields-1)
      - [Collections of exported types](#collections-of-exported-types)
  - [Valibot Schema Generation](#valibot-schema-generation)
    - [Valibot Usage Examples](#valibot-usage-examples)
      - [Basic form model](#basic-form-model)
      - [SubsetOf-bound input models](#subsetof-bound-input-models)
  - [Installation](#installation)
  - [License](#license)

---

## Setup

Reinforced.Typings runs an `RtConfigurationMethod` (configured in your consuming project's `Reinforced.Typings.settings.xml`) once per build, right before it exports every `[TsInterface]`/`[TsClass]`/`[TsEnum]` type it discovers. Point that setting at `ReinforcedTypingsFluentConfig.Configure` to enable everything in this package:

```xml
<!-- Reinforced.Typings.settings.xml -->
<PropertyGroup>
  <RtConfigurationMethod>
    CommonNetFuncs.ReinforcedTypings.ReinforcedTypingsFluentConfig.Configure
  </RtConfigurationMethod>
  <RtTargetDirectory>$(ProjectDir)TypeScriptModels</RtTargetDirectory>
  <!-- ...your project's other RT settings (RtAssemblies, RtDivideTypesAmongFiles, etc.)... -->
</PropertyGroup>
```

`Configure` scans whichever assemblies your own RT settings are configured to export from (falling back to the assembly this package lives in if none are configured), so it behaves correctly whether it's referenced by one project or shared as a library across several. Hand-written `TsConst`/`TsCollection` output is written to the same target directory RT itself would use (`RtTargetDirectory` when dividing types among files, otherwise the directory portion of `RtTargetFile`), nested into namespace-mirroring subfolders using the same rules RT applies to its own generated files.

---

## TsConst

Marks a static C# class whose `public const`/`public static readonly` fields - of any type (strings, numbers, booleans, `Guid`/date types, enums, arrays/collections, or class/record instances) - should be exported as a TypeScript `as const` object literal, along with a `Key`/`Value` type alias pair (`keyof typeof X` / union of value types).

By default (`TsConstExportMode.All`) every eligible field is exported unless it's decorated with `[TsIgnoreConst]`. Use `[TsConst(TsConstExportMode.Selected)]` to instead opt fields in individually via `[TsExportConst]`.

### TsConst Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Export all fields, suppress one

```cs
using CommonNetFuncs.ReinforcedTypings.Constants;

[TsConst]
public static class RoleConstants
{
    public const string NmPkgPlAdminName = "NM Pkg PL Admin"; // exported

    [TsIgnoreConst]
    public const string InternalOnly = "internal"; // skipped
}
```

Generates `RoleConstants.ts`:

```ts
export const RoleConstants = {
	NmPkgPlAdminName: "NM Pkg PL Admin",
} as const;

export type RoleConstantsKey = keyof typeof RoleConstants;
export type RoleConstantsValue = (typeof RoleConstants)[RoleConstantsKey];
```

#### Export only selected fields

```cs
[TsConst(TsConstExportMode.Selected)]
public static class PolicyNames
{
    public const string InternalPolicy = "InternalPolicy"; // skipped - no [TsExportConst]

    [TsExportConst]
    public const string EditUsersPolicy = "EditUsersPolicy"; // exported
}
```

#### Non-string field types

`TsConst` isn't limited to strings - `Guid`/date types serialize as ISO strings, enums serialize as numbers (or strings when the enum carries `[TsEnum(UseString = true)]`), and nested arrays/collections and class/record instances serialize recursively as array/object literals (honoring `[TsIgnore]` on nested properties):

```cs
[TsConst]
public static class AppDefaults
{
    public static readonly Guid TenantId = new("11111111-1111-1111-1111-111111111111");
    public static readonly DateOnly LaunchDate = new(2024, 1, 2);
    public static readonly int[] AllowedPageSizes = [10, 25, 50];
}
```

```ts
export const AppDefaults = {
	TenantId: "11111111-1111-1111-1111-111111111111",
	LaunchDate: "2024-01-02",
	AllowedPageSizes: [10, 25, 50],
} as const;
```

</details>

---

## TsCollection

Marks a static C# class whose `public const`/`public static readonly` array/collection fields (`List<T>`, `T[]`, `ICollection<T>`, `IEnumerable<T>`, etc. - excluding `string` and dictionaries) should be exported as a TypeScript array-literal collections object, along with a `CollectionsKey`/`CollectionsValue` type alias pair.

By default (`TsCollectionExportMode.All`) every eligible field is exported unless it's decorated with `[TsIgnoreCollection]`. Use `[TsCollection(TsCollectionExportMode.Selected)]` to instead opt fields in individually via `[TsExportCollection]`. If a class has no eligible collection fields, no file is written for it.

Element types that are themselves exported via `[TsInterface]`/`[TsClass]`/`[TsEnum]` are cross-referenced using their _real_ generated TypeScript name (honoring `[TsInterface(AutoI = ...)]`'s `I`-prefix convention) - imported from the sibling generated file when `UseModules` is enabled, or fully namespace-qualified otherwise. Anything else falls back to `any` (or `unknown` when `UnresolvedToUnknown` is set).

### TsCollection Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Export all fields, suppress one

```cs
using CommonNetFuncs.ReinforcedTypings.Collections;

[TsCollection]
public static class RoleConstants
{
    public static readonly string[] AllRoleNames = ["Admin", "Editor", "Viewer"]; // exported

    [TsIgnoreCollection]
    public static readonly string[] Deprecated = ["LegacyRole"]; // skipped
}
```

Generates `RoleConstants.ts`:

```ts
export const RoleConstantsCollections = {
	AllRoleNames: ["Admin", "Editor", "Viewer"] as string[],
} as const;

export type RoleConstantsCollectionsKey = keyof typeof RoleConstantsCollections;
export type RoleConstantsCollectionsValue =
	(typeof RoleConstantsCollections)[RoleConstantsCollectionsKey];
```

#### Export only selected fields

```cs
[TsCollection(TsCollectionExportMode.Selected)]
public static class PolicyNames
{
    public static readonly string[] NotExported = ["InternalPolicy"];

    [TsExportCollection]
    public static readonly string[] Exported = ["EditUsersPolicy", "ViewReportsPolicy"];
}
```

#### Collections of exported types

```cs
[TsEnum]
public enum Role { Admin, Editor, Viewer }

[TsInterface] // AutoI defaults to true -> generated as IUserSummary
public sealed class UserSummary
{
    public string Name { get; set; } = string.Empty;
}

[TsCollection]
public static class Seed
{
    public static readonly List<Role> DefaultRoles = [Role.Admin, Role.Viewer];
    public static readonly List<UserSummary> SampleUsers = [new() { Name = "Ada" }];
}
```

With `UseModules` enabled, `Seed.ts` imports the real generated types instead of falling back to `any`:

```ts
import type { Role } from "./Role";
import type { IUserSummary } from "./UserSummary";

export const SeedCollections = {
	DefaultRoles: [0, 2] as Role[],
	SampleUsers: [{ Name: "Ada" }] as IUserSummary[],
} as const;

export type SeedCollectionsKey = keyof typeof SeedCollections;
export type SeedCollectionsValue = (typeof SeedCollections)[SeedCollectionsKey];
```

</details>

---

## Valibot Schema Generation

Apply `[GenerateValibotSchema]` alongside `[TsInterface]` on classes that represent form-submission models (i.e. classes with `System.ComponentModel.DataAnnotations` validation attributes that are used as inputs on the frontend). Read-only response/query models that don't need client-side validation should omit this attribute.

`ReinforcedTypingsFluentConfig.Configure` finds every type carrying both attributes and configures RT to run them through `ValibotSchemaGenerator`, a custom RT code generator that produces the standard interface _and_ writes a companion `<TypeName>.schema.ts` file (next to the generated interface) containing:

- `export const <TypeName>Schema = v.object({ ... })` - a Valibot schema derived from the class's properties and their validation attributes.
- `export type <TypeName>Input = v.InferInput<typeof <TypeName>Schema>;`
- `export const <TypeName>Labels: Record<string, string> = { ... }` - a display-label map, using `[DisplayName]` where present and falling back to a space-split version of the property name (e.g. `PhoneNumber` -> `"Phone Number"`).

Recognized `System.ComponentModel.DataAnnotations` attributes: `[Required]`, `[MaxLength]`, `[MinLength]`, `[StringLength]`, `[Range]`, `[RegularExpression]`, `[EmailAddress]`, `[Url]`, `[Phone]`, `[CreditCard]`. Properties marked `[TsIgnore]` are skipped in both the schema and the labels map. Nested complex properties recurse into an inline `v.object({...})` schema (with cycle detection falling back to `v.any()`), and array/list properties emit `v.array(...)`.

### Valibot Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Basic form model

```cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.ReinforcedTypings.Valibot;
using Reinforced.Typings.Attributes;

[TsInterface]
[GenerateValibotSchema]
public sealed class CreateUserModel
{
    [Required]
    [MaxLength(50)]
    [DisplayName("Full Name")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(18, 120)]
    public int? Age { get; set; }
}
```

Generates `CreateUserModel.schema.ts`:

```ts
import * as v from "valibot";

export const CreateUserModelSchema = v.object({
	Name: v.pipe(v.string(), v.maxLength(50)),
	Email: v.pipe(v.string(), v.email()),
	Age: v.optional(
		v.nullable(v.pipe(v.number(), v.minValue(18), v.maxValue(120))),
	),
});

export type CreateUserModelInput = v.InferInput<typeof CreateUserModelSchema>;

export const CreateUserModelLabels: Record<string, string> = {
	Name: "Full Name",
	Email: "Email",
	Age: "Age",
};
```

#### SubsetOf-bound input models

Types decorated with `[SubsetOf(typeof(SourceModel))]` (e.g. via `CommonNetFuncs.SubsetModelBinder`) inherit their validation attributes from the source type when generating the schema, so a trimmed-down "bound" model doesn't need to redeclare them:

```cs
public sealed class User
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}

[TsInterface]
[GenerateValibotSchema]
[SubsetOf(typeof(User))]
public sealed class BoundUser
{
    public string Name { get; set; } = string.Empty; // pulls [Required]/[MaxLength(50)] from User.Name
}
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.ReinforcedTypings
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
