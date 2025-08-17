# CommonNetFuncs.SubsetModelBinder

[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.SubsetModelBinder)](https://www.nuget.org/packages/CommonNetFuncs.SubsetModelBinder/)

This project provides an attribute that forces a class to only contain properties that are also present in a in another parent class. This is useful for model binding scenarios where you want to restrict the properties that can be bound to a model without causing mapping issues when binding the subset model to the parent model (using FastMap for instance)

## Contents

- [CommonNetFuncs.SubsetModelBinder](#commonnetfuncssubsetmodelbinder)
  - [Contents](#contents)

---

<details>
<summary><h3>Usage Example</h3></summary>

```cs
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// This class will only allow properties that are also present in the Person class with the same name and optionally same type (type can be optionally be different)
[SubsetOf(typeof(Person))]
public class PersonSubset
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[SubsetOf(typeof(Person))]
public class InvalidPersonSubset
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Mail { get; set; } // Throws compiler error because 'Mail' is not a property of the Person class
}
```

</details>
