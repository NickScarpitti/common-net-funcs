# CommonNetFuncs.Core

[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Core)](https://www.nuget.org/packages/CommonNetFuncs.Core/)

This lightweight project contains helper methods for several common functions required by applications.

## Contents

- [Async](#async)
- [Collections](#collections)
- [Copy](#copy)
- [DateOnlyHelpers](#dateonlyhelpers)
- [DateTimeHelpers](#datetimehelpers)
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
- [Validation](#Validation)

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

Used to directly TryAdd a KeyValuePair object(s) to a dictionary. Does nothing if add fails

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

Add multiple items to a collection at once, optionally in parallel.

```cs
List<int> numbers = [1, 2, 3];
numbers.AddRange([4, 5, 6]); // [1, 2, 3, 4, 5, 6]
//Or
numbers.AddRangeParallel([4, 5, 6]); // [1, 2, 3, 4, 5, 6]
```

#### SetValue & SetValueParallel

Set all items in a collection to a specific value, optionally in parallel.

```cs
int[,] array = new int[,] { { 1, 2 }, { 3, 4 } };
array.SetValue((arr, indices) => arr.SetValue(((int)arr.GetValue(indices)!) * 2, indices)); //  { 2, 4 }, { 6, 8 }

var people = new List<Person> { new() { Name = "Nick", Age = 32, NameAge = null }, new() { Name = "John", Age = 40, NameAge = null } };
names.SetValue(x => {
    x.Age++;
    NameAge = x.Name + x.Age.ToString();
}); // NameAge will be Nick33 and John41
```

#### SelectNonEmpty

Select only non-empty + non-null strings from a collection.

```cs
var nonEmpty = new[] { "a", "", "b", null }.SelectNonEmpty(); // ["a", "b"]
```

#### SelectNonNull

Select only non-null items from a collection.

```cs
var nonNull = new string?[] { "a", "", "b", null }.SelectNonNull(); // ["a", "", "b"]
```

#### SingleToList

Wrap a single object in a list.

```cs
var list = 42.SingleToList(); // [42]
```

#### GetObjectByPartial

Find an object in a collection by matching non-null properties.

```cs
var people = new List<Person> { new() { Name = "Nick", Age = 32 }, new() { Name = "John", Age = 40 } };
var result = people.GetObjectByPartial(new Person() { Name = "Nick" ); // Person with Name "Nick"
```

#### ToList

Convert DataTable to a List.

```cs
using DataTable dataTable = new();
dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));

dataTable.Rows.Add(1, "test1");
dataTable.Rows.Add(2, "test2");

List<TestClass?> result = dataTable.ToList<TestClass>(); // [{ Id = 1, Name = "test1"}, { Id = 2, Name = "test2"}]
```

#### ToListParallel

Convert a DataTable to a List in parallel.

```cs
using DataTable dataTable = new();
dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));

dataTable.Rows.Add(1, "test1");
dataTable.Rows.Add(2, "test2");
List<TestClass?> result = dataTable.ToListParallel<TestClass>(); // [{ Id = 1, Name = "test1"}, { Id = 2, Name = "test2"}]
```

#### ToDataTable

Convert a collection to a DataTable.

```cs
List<Person> people = [new() { Name = "Nick" }];
using DataTable table = people.ToDataTable(); // DataTable object containing People
```

#### ToDataTableReflection (Obsolete)

Convert a collection to a DataTable using reflection.

```cs
List<Person> people = [new() { Name = "Nick" }];
using DataTable table = people.ToDataTableReflection(); // DataTable object containing People
```

#### CombineExpressions

Combine multiple expressions into one.

```cs
Expression<Func<Person, bool>>[] expressions = [p => p.Age > 18, p => p.Name.StartsWith("N")];
var combinedExpression = CombineExpressions(expressions); // combinedExpression will do both > 18 and Name starts with "N"
```

#### StringAggProps

Aggregate string properties of objects.

```cs
List<Person> collection = new()
{
    new Person { Id = 1, Name = "Nick", Description = "desc1" },
    new Person { Id = 1, Name = "John", Description = "desc2" },
    new Person { Id = 1, Name = "Chris", Description = "Desc1" }
};
List<Person> aggregatedCollection = people.StringAggProps("Name", "Description"); // [{ Id = 1, Name = Nick;John;Chris, Description = "desc1;desc2" }]
```

#### IndexOf

Get the index of an item in a collection.

```cs
List<string> list = ["a", "b", "c"];
int idx = list.IndexOf("b"); // 1
```

#### IsIn

Check if a given value is valid for an enum.

```cs
public enum TestEnum
{
    Monday = DayOfWeek.Monday,
    Tuesday = DayOfWeek.Tuesday,
    Wednesday = DayOfWeek.Wednesday,
    Thursday = DayOfWeek.Thursday,
    Friday = DayOfWeek.Friday
}

bool result = "Monday".IsIn<TestEnum>(); // True
bool result = "Not A Day".IsIn<TestEnum>(); // False
```

</details>

---

## Copy

### Copy Usage Examples

Helper methods for copying properties between objects, including deep and shallow copy.

<details>
<summary><h3>Usage Examples</h3></summary>

#### CopyPropertiesTo

Copy matching properties from one object to another by name

```cs
SourceClass source = new() { Id = 1, Name = "Test" };
DestinationClass dest = new() { Id = 5, Name = "Nick", OtherParm = "Some Value" };
source.CopyPropertiesTo(dest); // { Id = 1, Name = "Test", OtherParm = "Some Value" }
```

#### CopyPropertiesToNew

Create a new instance of the same type with copied properties.

```cs
SourceClass source = new() { Id = 1, Name = "Test" };
SourceClass copy = source.CopyPropertiesToNew(); // { Id = 1, Name = "Test" }
```

#### CopyPropertiesToNew<T, U>

Copy properties to a new instance of a different type.

```cs
SourceClass source = new() { Id = 1, Name = "Test" };
DestinationClass dest = source.CopyPropertiesToNew<SourceClass, DestinationClass>(); // { Id = 1, Name = "Test", OtherParm = null }
```

#### CopyPropertiesToNewRecursive

Deep copy properties, including nested objects and collections.

```cs
ParentClass parent = new() { Name = "Nick" Child = new() { Value = 42 } };
ParentClass copy = parent.CopyPropertiesToNewRecursive(); // { Name = "Nick" Child = { Value = 42 } }
```

#### MergeInstances

Merge the field values from one instance into another of the same object.
Only default values will be overridden by mergeFromObjects.

```cs
Person target = new()
{
    Id = 0, // Default value
    Name = null // Default value
};

Person person1 = new()
{
    Id = 1, // Non-default value
    Name = null // Default value
};

Person person2 = new()
{
    Id = 0, // Default value
    Name = "Test" // Non-default value
};

Person result = target.MergeInstances(new[] { person1, person2 }); // { Id = 1, Name = "Test" }
```

</details>

---

## DateOnlyHelpers

### DateHelpers Usage Examples

Helper methods for working with `DateOnly` values.

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetBusinessDays (DateOnly)

Get the number of business days found within a date range (inclusive).

```cs
 DateOnly start = new(2024, 5, 6); // Monday
 DateOnly end = new(2024, 5, 12); // Sunday
 List<DateOnly> holidays = new() { new(2024, 5, 11), new(2024, 5, 8) }; // Sat, Wed
 int businessDays = DateOnlyHelpers.GetBusinessDays(start, end, holidays); //4
```

#### GetDayOfWeek (DateOnly)

Get the date of the day requested given the week provided via the date parameter.

```cs
DateOnly date = DateOnly.Parse("2024-05-08"); // Wednesday
DateOnly dayOfWeekDate = date.GetDayOfWeek(DayOfWeek.Monday); // 2024-05-06 = Monday of that week
```

#### GetMonthBoundaries (DateOnly)

Gets the first and last day of the month provided.

```cs
(DateOnly firstDay, DateOnly lastDay) = DateOnlyHelpers.GetMonthBoundaries(2, 2024); // (2024-02-01, 2024-02-29) *Accounts for leap year
```

#### GetFirstDayOfMonth (DateOnly)

Gets the first day of the month provided.

```cs
DateOnly date = new(2024, 5, 15);
DateOnly firstDay = date.GetFirstDayOfMonth(); // 2024-05-01
```

#### GetLastDayOfMonth (DateOnly)

Gets the last day of the month provided.

```cs
DateOnly date = new(2024, 5, 15);
DateOnly lastDay = date.GetLastDayOfMonth(); // 2024-05-31
```

#### GetToday (DateOnly)

Get the current date in DateOnly format.

```cs
DateOnly today = DateOnlyHelpers.GetToday(); // Gets current date
```

</details>

---

## DateTimeHelpers

### DateTimeHelpers Usage Examples

Helper methods for working with `DateTime` values.

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetBusinessDays (DateTime)

Get the number of business days found within a date range (inclusive).

```cs
 DateTime start = new(2024, 5, 6); // Monday
 DateTime end = new(2024, 5, 12); // Sunday
 List<DateTime> holidays = new() { new(2024, 5, 11), new(2024, 5, 8) }; // Sat, Wed
 int businessDays = DateTimeHelpers.GetBusinessDays(start, end, holidays); //4
```

#### GetDayOfWeek (DateTime)

Get the date of the day requested given the week provided via the date parameter.

```cs
DateTime date = DateOnly.Parse("2024-05-08"); // Wednesday
DateTime dayOfWeekDate = date.GetDayOfWeek(DayOfWeek.Monday); // 2024-05-06 = Monday of that week
```

#### GetMonthBoundaries (DateTime)

Gets the first and last day of the month provided.

```cs
(DateTime firstDay, DateTime lastDay) = DateTimeHelpers.GetMonthBoundaries(2, 2024); // (2024-02-01, 2024-02-29) *Accounts for leap year
```

#### GetFirstDayOfMonth (DateTime)

Gets the first day of the month provided.

```cs
DateTime date = new(2024, 5, 15);
DateTime firstDay = date.GetFirstDayOfMonth(); // 2024-05-01
```

#### GetLastDayOfMonth (DateTime)

Gets the last day of the month provided.

```cs
DateTime date = new(2024, 5, 15);
DateTime lastDay = date.GetLastDayOfMonth(); // 2024-05-31
```

#### IsValidOaDate

Checks if the double provided is a valid OADate (used in Excel).

```cs
Double validOdDate = 657435.0;
Double inValidOdDate = 657434.999;
bool isValid = validOdDate.IsValidOaDate(); // True
bool isNotValid = inValidOdDate.IsValidOaDate(); // False
```

</details>

---

## DimensionScale

### DimensionScale Usage Examples

Helpers for scaling 2D and 3D dimensions proportionally to maximally fit within constraint dimensions.

<details>
<summary><h3>Usage Examples</h3></summary>

#### ScaleDimensionsToConstraint

Scale a size to fit within a bounding box.

```cs
// 2D example
(decimal newWidth, decimal newHeight) = DimensionScale.ScaleDimensionsToConstraint(200.0, 100.0, 100.0, 100.0, true, 2); //( 100.00, 50.00 )

//3D example
(int newWidth, int newHeight, int newDepth) = DimensionScale.ScaleDimensionsToConstraint(100, 100, 100, 200, 200, 200, true); // ( 200, 200, 200 )
```

</details>

---

## ExceptionLocation

### ExceptionLocation Usage Examples

Helpers for extracting location information from exceptions.

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetLocationOfException

Get the file and line number from an exception.

```cs
public class TestClass;
public void TestMethod()
{
    try
    {
        throw new InvalidOperationException();
    }
    catch (Exception ex)
    {
        string location = ex.GetLocationOfException();// "TestClass.TestMethod"
    }
}
```

</details>

---

## FileHelpers

### FileHelpers Usage Examples

Helpers for working with files and file paths.

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetSafeSaveName

Get a unique file name if the file already exists.

```cs
string fileName = "test.txt";
string safeName = fileName.GetSafeSaveName(); // If "test.txt" exists, returns "test0.txt"
```

TODO: Pickup Here

#### ValidateFileExtension

"Description"

```cs
<Example Code>
```

#### GetHashFromFile

"Description"

```cs
<Example Code>
```

#### GetHashFromStream

"Description"

```cs
<Example Code>
```

#### GetAllFilesRecursive

"Description"

```cs
<Example Code>
```

#### CleanFileName


"Description"

```cs
<Example Code>
```

</details>

---

## Inspect

### Inspect Usage Examples

Helpers for inspecting types and objects.

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetDefaultValue

Get the default value for a type.

```cs
object? def = typeof(int).GetDefaultValue(); // 0
```

#### CountDefaultProps

Count the number of properties with default values.

```cs
var obj = new MyClass();
int count = obj.CountDefaultProps();
```

#### ObjectHasAttribute

"Description"

```cs
<Example Code>
```

#### IsEqualR

"Description"

```cs
<Example Code>
```

#### IsEqual

"Description"

```cs
<Example Code>
```

#### GetHashForObject

"Description"

```cs
<Example Code>
```

#### GetHashForObjectAsync

"Description"

```cs
<Example Code>
```

</details>

---

## MathHelpers

### MathHelpers Usage Examples

Helpers for common math operations.

<details>
<summary><h3>Usage Examples</h3></summary>

#### Ceiling

"Description"

```cs
<Example Code>
```

#### Floor

"Description"

```cs
<Example Code>
```

#### GetPrecision

"Description"

```cs
<Example Code>
```

#### GenerateRange

"Description"

```cs
<Example Code>
```

#### GreatestCommonDenominator

"Description"

```cs
<Example Code>
```

</details>

---

## Random

### Random Usage Examples

Helpers for generating random values.

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetRandomInt

"Description"

```cs
<Example Code>
```

#### GetRandomInts

"Description"

```cs
<Example Code>
```

#### GetRandomDouble

"Description"

```cs
<Example Code>
```

#### GetRandomDoubles

"Description"

```cs
<Example Code>
```

#### GetRandomDecimal

"Description"

```cs
<Example Code>
```

#### GetRandomDecimals

"Description"

```cs
<Example Code>
```

#### ShuffleListInPlace

"Description"

```cs
<Example Code>
```

#### Shuffle

"Description"

```cs
<Example Code>
```

#### ShuffleLinq

"Description"

```cs
<Example Code>
```

#### GetRandomElement

"Description"

```cs
<Example Code>
```

#### GetRandomElements

"Description"

```cs
<Example Code>
```

#### GenerateRandomString

"Description"

```cs
<Example Code>
```

#### GenerateRandomStrings

"Description"

```cs
<Example Code>
```

#### GenerateRandomStringByCharSet

"Description"

```cs
<Example Code>
```

</details>

---

## RunBatches

### RunBatches Usage Examples

Helpers for running tasks in batches.

<details>
<summary><h3>Usage Examples</h3></summary>

#### RunBatchedProcessAsync

"Description"

```cs
<Example Code>
```

#### RunBatchedProcess

"Description"

```cs
<Example Code>
```

</details>

---

## Streams

### Streams Usage Examples

Helpers for working with streams.

<details>
<summary><h3>Usage Examples</h3></summary>

#### ReadStreamAsync

"Description"

```cs
<Example Code>
```

#### WriteStreamToStream

"Description"

```cs
<Example Code>
```

</details>

---

## Strings

### Strings Usage Examples

Helpers for string manipulation.

<details>
<summary><h3>Usage Examples</h3></summary>

#### Left

"Description"

```cs
<Example Code>
```

#### Right

"Description"

```cs
<Example Code>
```

#### ExtractBetween

"Description"

```cs
<Example Code>
```

#### MakeNullNull

"Description"

```cs
<Example Code>
```

#### ParsePascalCase

"Description"

```cs
<Example Code>
```

#### ToTitleCase

"Description"

```cs
<Example Code>
```

#### TrimFull

"Description"

```cs
<Example Code>
```

#### IsNullOrWhiteSpace

"Description"

```cs
<Example Code>
```

#### IsNullOrEmpty

"Description"

```cs
<Example Code>
```

#### ContainsInvariant

"Description"

```cs
<Example Code>
```

#### StartsWithInvariant

"Description"

```cs
<Example Code>
```

#### EndsWithInvariant

"Description"

```cs
<Example Code>
```

#### IndexOfInvariant

"Description"

```cs
<Example Code>
```

#### Contains

"Description"

```cs
<Example Code>
```

#### ReplaceInvariant

"Description"

```cs
<Example Code>
```

#### StrEq

"Description"

```cs
<Example Code>
```

#### StrComp

"Description"

```cs
<Example Code>
```

#### IsAlphanumeric

"Description"

```cs
<Example Code>
```

#### IsAlphaOnly

"Description"

```cs
<Example Code>
```

#### IsNumericOnly

"Description"

```cs
<Example Code>
```

#### ExtractToLastInstance

"Description"

```cs
<Example Code>
```

#### ExtractFromLastInstance

"Description"

```cs
<Example Code>
```

#### TrimObjectStringsR

"Description"

```cs
<Example Code>
```

#### TrimObjectStrings

"Description"

```cs
<Example Code>
```

#### NormalizeObjectStringsR

"Description"

```cs
<Example Code>
```

#### NormalizeObjectStrings

"Description"

```cs
<Example Code>
```

#### MakeObjectNullNullR

"Description"

```cs
<Example Code>
```

#### MakeObjectNullNull

"Description"

```cs
<Example Code>
```

#### CreateMakeObjectNullNullExpression

"Description"

```cs
<Example Code>
```

#### ToNString

"Description"

```cs
<Example Code>
```

#### ToListInt

"Description"

```cs
<Example Code>
```

#### ToNInt

"Description"

```cs
<Example Code>
```

#### ToNDouble

"Description"

```cs
<Example Code>
```

#### ToNDecimal

"Description"

```cs
<Example Code>
```

#### ToNDateTime

"Description"

```cs
<Example Code>
```

#### ToNDateOnly

"Description"

```cs
<Example Code>
```

#### YesNoToBool

"Description"

```cs
<Example Code>
```

#### YNToBool

"Description"

```cs
<Example Code>
```

#### BoolToYesNo

"Description"

```cs
<Example Code>
```

#### BoolToYN

"Description"

```cs
<Example Code>
```

#### BoolToInt

"Description"

```cs
<Example Code>
```

#### GetSafeDate

"Description"

```cs
<Example Code>
```

#### MakeExportNameUnique

"Description"

```cs
<Example Code>
```

#### TimespanToShortForm

"Description"

```cs
<Example Code>
```

#### GetHash

"Description"

```cs
<Example Code>
```

#### NormalizeWhiteSpace

"Description"

```cs
<Example Code>
```

#### FormatDateString

"Description"

```cs
<Example Code>
```

#### ReplaceInverse

"Description"

```cs
<Example Code>
```

#### UrlEncodeReadable

"Description"

```cs
<Example Code>
```

#### FormatPhoneNumber

"Description"

```cs
<Example Code>
```

#### SplitLines

"Description"

```cs
<Example Code>
```

#### ToFractionString

"Description"

```cs
<Example Code>
```

#### FractionToDecimal

"Description"

```cs
<Example Code>
```

#### TryStringToDecimal

"Description"

```cs
<Example Code>
```

#### TryStringToDecimal

"Description"

```cs
<Example Code>
```

#### FractionToDouble

"Description"

```cs
<Example Code>
```

#### RemoveLetters

"Description"

```cs
<Example Code>
```

#### RemoveNumbers

"Description"

```cs
<Example Code>
```

#### GetOnlyLetters

"Description"

```cs
<Example Code>
```

#### GetOnlyNumbers

"Description"

```cs
<Example Code>
```

#### RemoveLeadingNonAlphanumeric

"Description"

```cs
<Example Code>
```

#### RemoveTrailingNonAlphanumeric

"Description"

```cs
<Example Code>
```

#### TrimOuterNonAlphanumeric

"Description"

```cs
<Example Code>
```

#### CountChars

"Description"

```cs
<Example Code>
```

#### HasNoMoreThanNumberOfChars

"Description"

```cs
<Example Code>
```

#### HasNoLessThanNumberOfChars

"Description"

```cs
<Example Code>
```

</details>

---

## TypeChecks

### TypeChecks Usage Examples

Helpers for checking types.

<details>
<summary><h3>Usage Examples</h3></summary>

#### IsDelegate

"Description"

```cs
<Example Code>
```

#### IsArray

"Description"

```cs
<Example Code>
```

#### IsDictionary

"Description"

```cs
<Example Code>
```

#### IsEnumerable

"Description"

```cs
<Example Code>
```

#### IsClassOtherThanString

"Description"

```cs
<Example Code>
```

#### IsNumeric

"Description"

```cs
<Example Code>
```

#### IsNumericType

Check if a type is numeric.

```cs
bool isNumeric = typeof(int).IsNumericType(); // True
```

#### IsSimpleType

"Description"

```cs
<Example Code>
```

#### IsReadOnlyCollectionType

"Description"

```cs
<Example Code>
```

</details>

---

## UnitConversion

### UnitConversion Usage Examples

Helpers for converting between units.

<details>
<summary><h3>Usage Examples</h3></summary>

#### <MethodName>

"Description"

```cs
<Example Code>
```

</details>

---

## Validation

### Validation Usage Examples

Helpers for validating objects and properties.

<details>
<summary><h3>Usage Examples</h3></summary>

#### SetInvalidPropertiesToDefault

Sets all properties that are invalid based on their validation decorators / attributes to the default value for that property

```cs
<Example Code>
```
