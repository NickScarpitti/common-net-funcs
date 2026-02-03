# CommonNetFuncs.DeepClone

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.DeepClone)](https://www.nuget.org/packages/CommonNetFuncs.DeepClone/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.DeepClone)](https://www.nuget.org/packages/CommonNetFuncs.DeepClone/)

This project contains helper methods for deep cloning objects in .NET applications (i.e. creating a new instance of an object with the same values as an existing instance but without the same references in memory).

## Contents

- [CommonNetFuncs.DeepClone](#commonnetfuncsdeepclone)
  - [Contents](#contents)
  - [ExpressionTrees](#expressiontrees)
    - [ExpressionTrees Usage Examples](#expressiontrees-usage-examples)
      - [DeepClone](#deepclone)
  - [Reflection](#reflection)
    - [Reflection Usage Examples](#reflection-usage-examples)
      - [DeepCloneR](#deepcloner)
  - [Serialize](#serialize)
    - [Serialize Usage Examples](#serialize-usage-examples)
      - [DeepCloneS](#deepclones)

---

## ExpressionTrees

Use expression trees to deep clone objects. This is by far the fastest of the three deep clone methods in this package with a minor penalty for the first clone of each unique type to construct the expression tree.

### ExpressionTrees Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### DeepClone

Deep clone an object using expression trees.

```cs
public class Person
{
  public string Name { get; set; }
  public int Age { get; set; }
}

Person original = new() { Name = "Chris", Age = "34" };
Person clone = original.DeepClone();
clone.Name = "Nick"; // Clone's Name property == "Nick" while original's Name property remains "Chris"
```

</details>

---

## Reflection

Use reflection to deep clone objects. In most cases this is the second fastest option of the three deep clone methods in this package.

### Reflection Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### DeepCloneR

Deep clone an object using reflection.

```cs
public class Person
{
  public string Name { get; set; }
  public int Age { get; set; }
}

Person original = new() { Name = "Chris", Age = "34" };
Person clone = original.DeepCloneR();
clone.Name = "Nick"; // Clone's Name property == "Nick" while original's Name property remains "Chris"
```

</details>

---

## Serialize

Use JSON serialization to deep clone objects. In most cases this is the slowest option of the three deep clone methods in this package.

### Serialize Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### DeepCloneS

Deep clone an object using JSON serialization.

```cs
public class Person
{
  public string Name { get; set; }
  public int Age { get; set; }
}

Person original = new() { Name = "Chris", Age = "34" };
Person clone = original.DeepCloneS();
clone.Name = "Nick"; // Clone's Name property == "Nick" while original's Name property remains "Chris"
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.DeepClone
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.