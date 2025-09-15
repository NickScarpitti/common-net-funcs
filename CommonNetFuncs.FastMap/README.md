# CommonNetFuncs.FastMap

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.FastMap)](https://www.nuget.org/packages/CommonNetFuncs.FastMap/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.FastMap)](https://www.nuget.org/packages/CommonNetFuncs.FastMap/)

This lightweight project contains a helper method for fast mapping of properties between different objects in .NET applications based on property names.

## Contents

- [CommonNetFuncs.FastMap](#commonnetfuncsfastmap)
  - [Contents](#contents)
  - [FastMapper](#fastmapper)
    - [FastMapper Usage Examples](#fastmapper-usage-examples)
      - [FastMap](#fastmap)

---

## FastMapper

A utility class for fast mapping of properties between different objects based on property names.

### FastMapper Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### FastMap

Maps properties from one object to another based on matching property names using expression trees. Speed is comparable in performance to other mapping libraries like AutoMapper and Mapperly, but requires less (no) configuration, and does not rely on source generators to work.

```cs
public sealed class SimpleSource
{
    public required string StringProp { get; set; }
    public int IntProp { get; set; }
}

public sealed class SimpleDestination
{
    public required string StringProp { get; set; }
    public string? ExtraStringProp { get; set; }
    public int IntProp { get; set; }
}

SimpleSource source = new()
{
    StringProp = "Test",
    IntProp = 17,
};

SimpleDestination destination = source.FastMap<SimpleSource, SimpleDestination>();
// destination =
//{
//    StringProp = "Test",
//    ExtraStringProp = null,
//    IntProp = 17
//}
```

</details>
