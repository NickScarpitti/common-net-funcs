# CommonNetFuncs.Core

[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Core)](https://www.nuget.org/packages/CommonNetFuncs.Core/)

This lightweight project contains helper methods for several common functions required by applications.

## Contents

- [Async](#async)
- [Collections](#collections)
- [Copy](#copy)
- [DateHelpers](#datehelpers)
- [DimensionScale](#dimensionscale)
- [ExceptionLocation](#exceptionlocation)
- [FileHelpers](#filehelpers)
- [Inspect](#inspect)
- [MathHelpers](#mathhelpers)
- [Random](#random)
- [RunBatches](#runbatches)
- [Streams](#streams)
- [Strings](#strings)
- [TypeChecks](#typechecks)
- [UnitConversion](#unitconversion)

---

## Async

Helper methods for dealing with asynchronous processes.
<details>
<summary><h3>Usage Examples</h3></summary>

#### ObjectUpdate

Asynchronously update properties of a class using ObjectUpdate

```cs
//Fill the Name and Address property using async tasks
Person person = new();

//AsyncIntString helper class is used for int and string types since they can't otherwise be gotten asynchronously like this
AsyncIntString personPhotoLocation = new();

person.Id = 1;
List<Task> tasks =
[
    person.ObjectUpdate(nameof(Person.Name), GetPersonNameByIdAsync(person.Id)), //Fills person.Name with results of GetPersonNameByIdAsync(person.Id)

    person.ObjectUpdate(nameof(Person.Address), GetPersonAddressByIdAsync(person.Id)), //Fills person.Address with results of GetPersonAddressByIdAsync(person.Id)

    personPhotoLocation.ObjectUpdate(nameof(AsyncIntString.AsyncString), GetPersonPhotoLocationById(person.Id)) //Fills personPhotoLocation.AsyncString with the results of GetPersonPhotoLocationById(person.Id)
]
await Task.WhenAll(tasks);
```

#### ObjectFill

Object fill can be used to asynchronously fill classes and lists with.

```cs
Person person = new();
ConcurrentBag<Person> people = [];
List<Task> tasks =
[
    person.ObjectUpdate(GetPersonById(1)), //person is filled by results of GetPersonById(1) which returns type Person

    //people is filled by the results of all three calls to GetPeopleByState additively (all results will be present in people)
    people.ObjectUpdate(GetPeopleByState("Ohio")),
    people.ObjectUpdate(GetPeopleByState("California")),
    people.ObjectUpdate(GetPeopleByState("Texas"))
]
await Task.WhenAll(tasks);
```

</details>

---

## Collections

### Collections Usage Examples

Helper methods that work with collections such as IEnumerable, List, IDictionary, ConcurrentBag, and DataTable

<details>
<summary><h3>Usage Examples</h3></summary>

#### AnyFast

Used to address issue CA1860 where it suggests using .Count for performance in an easier to type extension method

```cs
bool x = collection?.Any() == true;
//Or
collection?.Count > 0;
//Becomes
bool x = collection.AnyFast();
```

#### AddDictionaryItem & AddDictionaryItems

Used to directly TryAdd a KeyValuePair object(s) to a dictionary

```cs
KeyValuePair<string, string> JsonContentHeader = new("Content-Type", "application/json");

//Single addition
ConcurrentDictionary<string, string>? httpHeaders = [];
httpHeaders.AddDictionaryItem(JsonContentHeader);

//Add multiples
List<KeyValuePair<string, string>> keyValuePairs = [new("Accept-Encoding", "br"), new("Accept-Encoding", "gzip")];
httpHeaders.AddDictionaryItems(keyValuePairs);
```

#### AddRange & AddRangeParallel

[Description]

```cs
```

#### SetValue & SetValueParallel

[Description]

```cs
```

#### SelectNonEmpty

[Description]

```cs
```

#### SelectNonNull

[Description]

```cs
```

#### SingleToList

[Description]

```cs
```

#### GetObjectByPartial

[Description]

```cs
```

#### ToList

[Description]

```cs
```

#### ToListParallel

[Description]

```cs
```

#### ToDataTable

[Description]

```cs
```

#### ToDataTableReflection

[Description]

```cs
```

#### CombineExpressions

[Description]

```cs
```

#### StringAggProps

[Description]

```cs
```

#### IndexOf

[Description]

```cs
```

</details>

---

## Copy

### Copy Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## DateHelpers

### DateHelpers Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## DimensionScale

### DimensionScale Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## ExceptionLocation

### ExceptionLocation Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## FileHelpers

### FileHelpers Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## Inspect

### Inspect Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## MathHelpers

### MathHelpers Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## Random

### Random Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## RunBatches

### RunBatches Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## Streams

### Streams Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## Strings

### Strings Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## TypeChecks

### TypeChecks Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```

---

</details>

## UnitConversion

### UnitConversion Usage Examples

[Description here]

<details>
<summary><h3>Usage Examples</h3></summary>

#### [MethodNameHere]

```cs
//Code here
```
