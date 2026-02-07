# CommonNetFuncs.Core

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Core)](https://www.nuget.org/packages/CommonNetFuncs.Core/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Core)](https://www.nuget.org/packages/CommonNetFuncs.Core/)

This lightweight project contains helper methods for several common functions required by applications.

## Contents

- [CommonNetFuncs.Core](#commonnetfuncscore)
	- [Contents](#contents)
	- [Async](#async)
		- [Async Usage Examples](#async-usage-examples)
			- [ObjectFill](#objectfill)
			- [ObjectUpdate](#objectupdate)
	- [Collections](#collections)
		- [Collections Usage Examples](#collections-usage-examples)
			- [AnyFast](#anyfast)
			- [AddDictionaryItem \& AddDictionaryItems](#adddictionaryitem--adddictionaryitems)
			- [AddRange \& AddRangeParallel](#addrange--addrangeparallel)
			- [SetValue \& SetValueParallel](#setvalue--setvalueparallel)
			- [SelectNonEmpty](#selectnonempty)
			- [SelectNonNull](#selectnonnull)
			- [SingleToList](#singletolist)
			- [GetObjectByPartial](#getobjectbypartial)
			- [ToList](#tolist)
			- [ToListParallel](#tolistparallel)
			- [ToDataTable](#todatatable)
			- [ToDataTableReflection (Obsolete)](#todatatablereflection-obsolete)
			- [CombineExpressions](#combineexpressions)
			- [StringAggProps](#stringaggprops)
			- [IndexOf](#indexof)
			- [IsIn](#isin)
			- [GetCombinations](#getcombinations)
			- [GetRandomCombinations](#getrandomcombinations)
			- [GetEnumeratedCombinations](#getenumeratedcombinations)
	- [Copy](#copy)
		- [Copy Usage Examples](#copy-usage-examples)
			- [CopyPropertiesTo](#copypropertiesto)
			- [CopyPropertiesToNew](#copypropertiestonew)
			- [CopyPropertiesToNew\<T, U\>](#copypropertiestonewt-u)
			- [CopyPropertiesToNewRecursive](#copypropertiestonewrecursive)
			- [MergeInstances](#mergeinstances)
	- [DateOnlyHelpers](#dateonlyhelpers)
		- [DateHelpers Usage Examples](#datehelpers-usage-examples)
			- [GetBusinessDays (DateOnly)](#getbusinessdays-dateonly)
			- [GetDayOfWeek (DateOnly)](#getdayofweek-dateonly)
			- [GetMonthBoundaries (DateOnly)](#getmonthboundaries-dateonly)
			- [GetFirstDayOfMonth (DateOnly)](#getfirstdayofmonth-dateonly)
			- [GetLastDayOfMonth (DateOnly)](#getlastdayofmonth-dateonly)
			- [GetToday (DateOnly)](#gettoday-dateonly)
	- [DateTimeHelpers](#datetimehelpers)
		- [DateTimeHelpers Usage Examples](#datetimehelpers-usage-examples)
			- [GetBusinessDays (DateTime)](#getbusinessdays-datetime)
			- [GetDayOfWeek (DateTime)](#getdayofweek-datetime)
			- [GetMonthBoundaries (DateTime)](#getmonthboundaries-datetime)
			- [GetFirstDayOfMonth (DateTime)](#getfirstdayofmonth-datetime)
			- [GetLastDayOfMonth (DateTime)](#getlastdayofmonth-datetime)
			- [IsValidOaDate](#isvalidoadate)
	- [DimensionScale](#dimensionscale)
		- [DimensionScale Usage Examples](#dimensionscale-usage-examples)
			- [ScaleDimensionsToConstraint](#scaledimensionstoconstraint)
	- [ExceptionLocation](#exceptionlocation)
		- [ExceptionLocation Usage Examples](#exceptionlocation-usage-examples)
			- [GetLocationOfException](#getlocationofexception)
	- [FileHelpers](#filehelpers)
		- [FileHelpers Usage Examples](#filehelpers-usage-examples)
			- [GetSafeSaveName](#getsafesavename)
			- [ValidateFileExtension](#validatefileextension)
			- [GetHashFromFile](#gethashfromfile)
			- [GetHashFromStream](#gethashfromstream)
			- [GetAllFilesRecursive](#getallfilesrecursive)
			- [CleanFileName](#cleanfilename)
			- [ReadFileFromPipe](#readfilefrompipe)
	- [Inspect](#inspect)
		- [Inspect Usage Examples](#inspect-usage-examples)
			- [GetDefaultValue](#getdefaultvalue)
			- [CountDefaultProps](#countdefaultprops)
			- [ObjectHasAttribute](#objecthasattribute)
			- [IsEqualR](#isequalr)
			- [IsEqual](#isequal)
			- [GetHashForObject](#gethashforobject)
			- [GetHashForObjectAsync](#gethashforobjectasync)
	- [MathHelpers](#mathhelpers)
		- [MathHelpers Usage Examples](#mathhelpers-usage-examples)
			- [Ceiling](#ceiling)
			- [Floor](#floor)
			- [GetPrecision](#getprecision)
			- [GenerateRange](#generaterange)
			- [GreatestCommonDenominator](#greatestcommondenominator)
	- [Random](#random)
		- [Random Usage Examples](#random-usage-examples)
			- [GetRandomInt](#getrandomint)
			- [GetRandomInts](#getrandomints)
			- [GetRandomDouble](#getrandomdouble)
			- [GetRandomDoubles](#getrandomdoubles)
			- [GetRandomDecimal](#getrandomdecimal)
			- [GetRandomDecimals](#getrandomdecimals)
			- [ShuffleListInPlace](#shufflelistinplace)
			- [Shuffle](#shuffle)
			- [ShuffleLinq](#shufflelinq)
			- [GetRandomElement](#getrandomelement)
			- [GetRandomElements](#getrandomelements)
			- [GenerateRandomString](#generaterandomstring)
			- [GenerateRandomStrings](#generaterandomstrings)
			- [GenerateRandomStringByCharSet](#generaterandomstringbycharset)
	- [RunBatches](#runbatches)
		- [RunBatches Usage Examples](#runbatches-usage-examples)
			- [RunBatchedProcessAsync](#runbatchedprocessasync)
			- [RunBatchedProcess](#runbatchedprocess)
	- [Streams](#streams)
		- [Streams Usage Examples](#streams-usage-examples)
			- [ReadStreamAsync](#readstreamasync)
			- [WriteStreamToStream](#writestreamtostream)
	- [Strings](#strings)
		- [Strings Usage Examples](#strings-usage-examples)
			- [Left](#left)
			- [Right](#right)
			- [ExtractBetween](#extractbetween)
			- [MakeNullNull](#makenullnull)
			- [ParsePascalCase](#parsepascalcase)
			- [ToTitleCase](#totitlecase)
			- [TrimFull](#trimfull)
			- [IsNullOrWhiteSpace](#isnullorwhitespace)
			- [IsNullOrEmpty](#isnullorempty)
			- [ContainsInvariant](#containsinvariant)
			- [StartsWithInvariant](#startswithinvariant)
			- [EndsWithInvariant](#endswithinvariant)
			- [IndexOfInvariant](#indexofinvariant)
			- [Contains](#contains)
			- [ReplaceInvariant](#replaceinvariant)
			- [StrEq](#streq)
			- [StrComp](#strcomp)
			- [IsAlphanumeric](#isalphanumeric)
			- [IsAlphaOnly](#isalphaonly)
			- [IsNumericOnly](#isnumericonly)
			- [ExtractToLastInstance](#extracttolastinstance)
			- [ExtractFromLastInstance](#extractfromlastinstance)
			- [TrimObjectStringsR](#trimobjectstringsr)
			- [TrimObjectStrings](#trimobjectstrings)
			- [NormalizeObjectStringsR](#normalizeobjectstringsr)
			- [NormalizeObjectStrings](#normalizeobjectstrings)
			- [MakeObjectNullNullR](#makeobjectnullnullr)
			- [MakeObjectNullNull](#makeobjectnullnull)
			- [ToNString](#tonstring)
			- [ToListInt](#tolistint)
			- [ToNInt](#tonint)
			- [ToNDouble](#tondouble)
			- [ToNDecimal](#tondecimal)
			- [ToNDateTime](#tondatetime)
			- [ToNDateOnly](#tondateonly)
			- [YesNoToBool](#yesnotobool)
			- [YNToBool](#yntobool)
			- [BoolToYesNo](#booltoyesno)
			- [BoolToYN](#booltoyn)
			- [BoolToInt](#booltoint)
			- [GetSafeDate](#getsafedate)
			- [MakeExportNameUnique](#makeexportnameunique)
			- [TimespanToShortForm](#timespantoshortform)
			- [GetHash](#gethash)
			- [NormalizeWhiteSpace](#normalizewhitespace)
			- [FormatDateString](#formatdatestring)
			- [ReplaceInverse](#replaceinverse)
			- [UrlEncodeReadable](#urlencodereadable)
			- [FormatPhoneNumber](#formatphonenumber)
			- [SplitLines](#splitlines)
			- [ToFractionString](#tofractionstring)
			- [FractionToDecimal](#fractiontodecimal)
			- [TryFractionToDecimal](#tryfractiontodecimal)
			- [TryStringToDecimal](#trystringtodecimal)
			- [FractionToDouble](#fractiontodouble)
			- [TryFractionToDouble](#tryfractiontodouble)
			- [TryStringToDouble](#trystringtodouble)
			- [RemoveLetters](#removeletters)
			- [RemoveNumbers](#removenumbers)
			- [GetOnlyLetters](#getonlyletters)
			- [GetOnlyNumbers](#getonlynumbers)
			- [RemoveLeadingNonAlphanumeric](#removeleadingnonalphanumeric)
			- [RemoveTrailingNonAlphanumeric](#removetrailingnonalphanumeric)
			- [TrimOuterNonAlphanumeric](#trimouternonalphanumeric)
			- [CountChars](#countchars)
			- [HasNoMoreThanNumberOfChars](#hasnomorethannumberofchars)
			- [HasNoLessThanNumberOfChars](#hasnolessthannumberofchars)
	- [TypeChecks](#typechecks)
		- [TypeChecks Usage Examples](#typechecks-usage-examples)
			- [IsDelegate](#isdelegate)
			- [IsArray](#isarray)
			- [IsDictionary](#isdictionary)
			- [IsEnumerable](#isenumerable)
			- [IsClassOtherThanString](#isclassotherthanstring)
			- [IsNumeric](#isnumeric)
			- [IsNumericType](#isnumerictype)
			- [IsSimpleType](#issimpletype)
			- [IsReadOnlyCollectionType](#isreadonlycollectiontype)
	- [UnitConversion](#unitconversion)
		- [UnitConversion Usage Examples](#unitconversion-usage-examples)
			- [LbsToKg](#lbstokg)
			- [KgToLbs](#kgtolbs)
			- [InsToFt](#instoft)
			- [InsToMm](#instomm)
			- [MmToIns](#mmtoins)
			- [FtToIns](#fttoins)
			- [BytesToKb](#bytestokb)
			- [KbToBytes](#kbtobytes)
			- [BytesToMb](#bytestomb)
			- [MbToBytes](#mbtobytes)
			- [BytesToGb](#bytestogb)
			- [GbToBytes](#gbtobytes)
			- [BytesToTb](#bytestotb)
			- [TbToBytes](#tbtobytes)
			- [KbToMb](#kbtomb)
			- [MbToKb](#mbtokb)
			- [KbToGb](#kbtogb)
			- [GbToKb](#gbtokb)
			- [KbToTb](#kbtotb)
			- [TbToKb](#tbtokb)
			- [MbToGb](#mbtogb)
			- [GbToMb](#gbtomb)
			- [MbToTb](#mbtotb)
			- [TbToMb](#tbtomb)
			- [GbToTb](#gbtotb)
			- [TbToGb](#tbtogb)
			- [GetFileSizeFromBytesWithUnits](#getfilesizefrombyteswithunits)
			- [MetersToMiles](#meterstomiles)
			- [MilesToMeters](#milestometers)
	- [Validation](#validation)
		- [Validation Usage Examples](#validation-usage-examples)
			- [SetInvalidPropertiesToDefault](#setinvalidpropertiestodefault)
	- [Installation](#installation)
	- [License](#license)

<!-- - [CommonNetFuncs.Core](#commonnetfuncscore)
  - [Contents](#contents)
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
  - [Validation](#validation) -->

---

## Async

Helper methods for dealing with asynchronous processes.

### Async Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

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

---

## Collections

Helper methods that work with collections such as IEnumerable, List, IDictionary, ConcurrentBag, and DataTable

### Collections Usage Examples

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
Expression<Func<Person, bool>>[] expressions = [x => x.Age > 18, x => x.Name.StartsWith("N")];
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

#### GetCombinations

Get all unique combinations of items from 2+ collections, or optionally, limit to the first # of combinations with the maxCombinations parameter.
Can use custom separator between items as well as replace null values with a custom value.

```cs
List<List<string>> sources = new() { new List<string> { "A", "B", "C" }, new List<string> { "1", "2", "3" } };
HashSet<string> result = sources.GetCombinations(maxCombinations: 5); // ["A|1", "A|2", "A|3", "B|1", "B|2"] - First 5 combinations
```

#### GetRandomCombinations

Get all or a limited quantity of all possible combinations from 2+ collections in a randomized order.
Can use custom separator between items as well as replace null values with a custom value.

```cs
List<List<string>> sources = new() { new List<string> { "A", "B", "C" }, new List<string> { "1", "2", "3" } };
HashSet<string> result = sources.GetRandomCombinations(maxCombinations: 5); // ["B|3", "C|1", "C|3", "B|1", "A|3"] - Random 5 combinations
```

#### GetEnumeratedCombinations

Get an enumerable of all unique combinations of items from 2+ collections, or optionally, limit to the first # of combinations with the maxCombinations parameter.
Can use custom separator between items as well as replace null values with a custom value.
Useful for scenarios where a large quantity of values will be generated and consumed since values aren't all collected into one object before being returned.

```cs
List<List<string>> sources = new() { new List<string> { "A", "B", "C" }, new List<string> { "1", "2", "3" } };
IEnumerable<string> result = sources.GetCombinations(maxCombinations: 5); // ["A|1", "A|2", "A|3", "B|1", "B|2"] - First 5 combinations
```

</details>

---

## Copy

Helper methods for copying properties between objects, including deep and shallow copy.

### Copy Usage Examples

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

Helper methods for working with `DateOnly` values.

### DateHelpers Usage Examples

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

Helper methods for working with `DateTime` values.

### DateTimeHelpers Usage Examples

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

Helpers for scaling 2D and 3D dimensions proportionally to maximally fit within constraint dimensions.

### DimensionScale Usage Examples

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

Helpers for extracting location information from exceptions.

### ExceptionLocation Usage Examples

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

Helpers for working with files and file paths.

### FileHelpers Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetSafeSaveName

Get a unique file name if the file already exists.

```cs
string fileName = "test.txt";
string safeName = fileName.GetSafeSaveName(); // If "test.txt" exists, returns "test0.txt"
```

#### ValidateFileExtension

Validates file extension based on list of valid extensions

```cs
string fileName1 = "/some/file/path/test.txt";
string fileName2 = "/some/other/file/path.pdf";
string[] validExtensions = [".txt", ".doc", ".docx"];
bool fileName1Valid = fileName1.ValidateFileExtension(validExtensions); // True
bool fileName2Valid = fileName2.ValidateFileExtension(validExtensions); // false
```

#### GetHashFromFile

Gets the hash of a file's contents using the specified hashing algorithm.

```cs
string filePath = "document.txt";
string hash = await filePath.GetHashFromFile(EHashAlgorithm.SHA512); // Gets SHA512 hash of file
```

#### GetHashFromStream

Generates a hash based on the contents of a stream using the designated algorithm.

```cs
using FileStream stream = File.OpenRead("document.txt");
string hash = await stream.GetHashFromStream(EHashAlgorithm.SHA256); // Gets SHA256 hash of stream contents
```

#### GetAllFilesRecursive

Returns the full file path of all files contained under the specified directory.

```cs
string directory = @"C:\Documents"; // Get all files in directory and subdirectories
List<string> allFiles = FileHelpers.GetAllFilesRecursive(directory); // Returns all files
List<string> textFiles = FileHelpers.GetAllFilesRecursive(directory, "*.txt"); // Returns only .txt files
```

#### CleanFileName

Cleans a filename by removing or replacing invalid characters with safe alternatives.

```cs
string unsafeName = "file:with*invalid/chars?.txt";
string safeName = FileHelpers.CleanFileName(unsafeName); // Returns "file.with_invalid-chars_.txt"
```

#### ReadFileFromPipe

Reads data from a `PipeReader` and writes it to a stream, with optional file size limit and error handling. Designed for efficiently handling file uploads without unnecessary memory allocations. Returns a tuple with a success flag and an optional return value. Automatically resets stream position to 0 after reading. Handles both single-segment and multi-segment pipe data.

**With Size Limit:**

```cs
using System.IO.Pipelines;

// Example with size limit (e.g., for file uploads)
PipeReader pipeReader = GetPipeReader(); // From your source
MemoryStream fileStream = new();
const long maxFileSize = 5 * 1024 * 1024; // 5 MB limit

string? ErrorHandler(Exception ex)
{
    logger.Error(ex, "Failed to read file");
    return "Upload failed";
}

(bool success, string? result) = await pipeReader.ReadFileFromPipe(
    fileStream,
    maxFileSize,
    successReturn: "File uploaded successfully",
    fileTooLargeReturn: "File exceeds 5 MB limit",
    errorReturn: ErrorHandler,
    cancellationToken: cancellationToken);

if (success)
{
    // File successfully read, stream is at position 0 and ready to use
    await SaveFile(fileStream);
}
else
{
    // Handle error using the result message
    return BadRequest(result);
}
```

**Without Size Limit:**

```cs
using System.IO.Pipelines;

// Example without size limit
PipeReader pipeReader = GetPipeReader();
MemoryStream fileStream = new();

(bool success, int? statusCode) = await pipeReader.ReadFileFromPipe(
    fileStream,
    successReturn: 200,
    errorReturn: (ex) => 500,
    cancellationToken: cancellationToken);

if (success)
{
    // Process file stream (stream is at position 0)
    ProcessFile(fileStream);
}
```

</details>

---

## Inspect

Helpers for inspecting types and objects.

### Inspect Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetDefaultValue

Get the default value for a type.

```cs
object? def = typeof(int).GetDefaultValue(); // 0
object? defDate = typeof(DateTime).GetDefaultValue(); // 01/01/0001 00:00:00
```

#### CountDefaultProps

Count the number of properties with default values.

```cs
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
}

MyClass obj1 = new MyClass(); // IntProp = 0, StringProp = null int count1 = obj1.CountDefaultProps(); // 2
MyClass obj2 = new MyClass { IntProp = 1, StringProp = "not default" }; int count2 = obj2.CountDefaultProps(); // 0
```

#### ObjectHasAttribute

Check if a type has a specific attribute by name.

```cs
[Description("desc")]
public class ClassWithDescription { }
bool hasDescription = typeof(ClassWithDescription).ObjectHasAttribute("DescriptionAttribute"); // True
bool hasDescription2 = typeof(MyClass).ObjectHasAttribute("DescriptionAttribute"); // False
```

#### IsEqualR

Compares two objects for value equality using reflection, optionally exempting certain properties. Nested classes are not compared

```cs
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
}

MyClass a = new MyClass { IntProp = 5, StringProp = "abc" };
MyClass b = new MyClass { IntProp = 5, StringProp = "abc" };
bool eq = a.IsEqualR(b); // True
MyClass c = new MyClass { IntProp = 6, StringProp = "abc" };
bool eq2 = a.IsEqualR(c); // False

// Exempt IntProp from comparison
bool eq3 = a.IsEqualR(c, new[] { "IntProp" }); // True
```

#### IsEqual

Compares two objects for value equality, with options for exempting properties, ignoring string case, and recursive comparison.

```cs
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
}

MyClass a = new MyClass { IntProp = 1, StringProp = "abc" };
MyClass b = new MyClass { IntProp = 1, StringProp = "ABC" };
bool isAEqualToBNoCase = a.IsEqual(b, ignoreStringCase: true); // True
bool isAEqualToB = a.IsEqual(b, ignoreStringCase: false); // False
// Exempt IntProp from comparison
MyClass c = new MyClass { IntProp = 2, StringProp = "abc" };
bool isAEqualToC = a.IsEqual(c, exemptProps: new[] { "IntProp" }); // True

// Recursive comparison for nested objects
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
}

public class Nested
{
    public int Id { get; set; }
    public MyClass? Child { get; set; }
}

Nested nested1 = new Nested { Id = 1, Child = new MyClass { IntProp = 2, StringProp = "x" } };
Nested nested2 = new Nested { Id = 1, Child = new MyClass { IntProp = 2, StringProp = "x" } };
bool is1EqualTo2 = nested1.IsEqual(nested2); // True
```

#### GetHashForObject

Gets a hash string representing the object's value, using the specified algorithm (default MD5). Order of collection elements does not affect the hash.

```cs
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
}

MyClass a = new MyClass { IntProp = 1, StringProp = "abc" };
MyClass b = new MyClass { IntProp = 1, StringProp = "abc" };
string hashA = a.GetHashForObject(); // e.g. "e99a18c428cb38d5f260853678922e03"
string hashB = b.GetHashForObject(); // same as hashA

MyClass c = new MyClass { IntProp = 2, StringProp = "abc" };
string hashC = c.GetHashForObject(); // different from hashA/hashB

// For null objects
string hashNull = ((MyClass?)null).GetHashForObject(); // "null"
```

#### GetHashForObjectAsync

Asynchronously gets a hash string representing the object's value, using the specified algorithm (default MD5).

```cs
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
}

MyClass a = new MyClass { IntProp = 1, StringProp = "abc" };
MyClass b = new MyClass { IntProp = 1, StringProp = "abc" };
string hashA = a.GetHashForObjectAsync(); // e.g. "e99a18c428cb38d5f260853678922e03"
string hashB = b.GetHashForObjectAsync(); // same as hashA

MyClass c = new MyClass { IntProp = 2, StringProp = "abc" };
string hashC = c.GetHashForObjectAsync(); // different from hashA/hashB

// For null objects
string hashNull = ((MyClass?)null).GetHashForObjectAsync(); // "null"
```

</details>

---

## MathHelpers

Helpers for common math operations.

### MathHelpers Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Ceiling

Rounds a value up to the next multiple of the specified significance.

```cs
double? d1 = 10.5;
double up1 = d1.Ceiling(5.0); // 15.0
decimal? d2 = 4.1m;
decimal up2 = d2.Ceiling(2.0m); // 6.0
double? d3 = null;
double up3 = d3.Ceiling(5.0); // 0.0

// If significance is 0, rounds up to the next integer
double up4 = 10.5.Ceiling(0.0); // 11.0
```

#### Floor

Rounds a value down to the previous multiple of the specified significance.

```cs
double? d1 = 12.0; double down1 = d1.Floor(5.0); // 10.0
decimal? d2 = 4.1m; decimal down2 = d2.Floor(2.0m); // 4.0
double? d3 = null; double down3 = d3.Floor(5.0); // 0.0

// If significance is 0, rounds down to the previous integer
double down4 = 10.5.Floor(0.0); // 10.0
```

#### GetPrecision

Gets the number of decimal places in a double or decimal value.

```cs
double? d1 = 123.12; int p1 = d1.GetPrecision(); // 2
decimal? d2 = 123.123m; int p2 = d2.GetPrecision(); // 3
double? d3 = 123.0; int p3 = d3.GetPrecision(); // 0
decimal? d4 = null; int p4 = d4.GetPrecision(); // 0

// Respects current culture's decimal separator
System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
double d5 = 123.45;
int p5 = d5.GetPrecision(); // 2
```

#### GenerateRange

Generates a continuous range of integers between start and end (inclusive).

```cs
var range1 = MathHelpers.GenerateRange(1, 5); // [1, 2, 3, 4, 5]
var range2 = MathHelpers.GenerateRange(-2, 2); // [-2, -1, 0, 1, 2]
var range3 = MathHelpers.GenerateRange(0, 0); // [0]
```

#### GreatestCommonDenominator

Reduces a fraction to its lowest terms and returns the greatest common denominator.

```cs
long num = 12, den = 8;
MathHelpers.GreatestCommonDenominator(ref num, ref den, out long gcd); // num == 3, den == 2, gcd == 4
num = 25; den = 15;
MathHelpers.GreatestCommonDenominator(ref num, ref den, out gcd); // num == 5, den == 3, gcd == 5
num = 7; den = 13;
MathHelpers.GreatestCommonDenominator(ref num, ref den, out gcd); // num == 7, den == 13, gcd == 1
num = 0; den = 5;
MathHelpers.GreatestCommonDenominator(ref num, ref den, out gcd); // num == 0, den == 1, gcd == 5
```

</details>

---

## Random

Helpers for generating randomness.

### Random Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetRandomInt

Generates a random integer within a specified range.

```cs
int n1 = Random.GetRandomInt(); // 0 <= n1 < int.MaxValue
int n2 = Random.GetRandomInt(100); // 0 <= n2 < 100
int n3 = Random.GetRandomInt(-5, 5); // -5 <= n3 < 5
```

#### GetRandomInts

Generates a number of random integers within a specified range.

```cs
IEnumerable<int> values = Random.GetRandomInts(5, 0, 100); // 5 random ints, each 0 <= x < 100
IEnumerable<int> values2 = Random.GetRandomInts(10, -50, 50); // 10 random ints, -50 <= x < 50
```

#### GetRandomDouble

Generates a random double in the range [0, 1), optionally with a specified number of decimal places.

```cs
double d1 = Random.GetRandomDouble(); // 0 <= d1 < 1, with 15 decimal places
double d2 = Random.GetRandomDouble(3); // 0 <= d2 < 1, with 3 decimal places
```

#### GetRandomDoubles

Generates a number of random doubles in the range [0, 1), each with the specified number of decimal places.

```cs
IEnumerable<double> doubles = Random.GetRandomDoubles(5, 3); // 5 random doubles, with 3 decimal places
```

#### GetRandomDecimal

Generates a random decimal in the range [0, 1), optionally with a specified number of decimal places.

```cs
decimal dec1 = Random.GetRandomDecimal(); // 0 <= dec1 < 1, with 28 decimal places
decimal dec2 = Random.GetRandomDecimal(5); // 0 <= dec2 < 1, with 5 decimal places
```

#### GetRandomDecimals

Generates a number of random decimals in the range [0, 1), each with the specified number of decimal places.

```cs
IEnumerable<decimal> decimals = Random.GetRandomDecimals(5, 3); // 5 random decimals, with 3 decimal places
```

#### ShuffleListInPlace

Randomly shuffles a list in place.

```cs
List<int> list = Enumerable.Range(1, 10).ToList();
list.ShuffleListInPlace(); // list is now shuffled in place eg. [4, 6, 1, 9, 7, 8, 3, 2, 10, 5]
```

#### Shuffle

Randomly shuffles a collection and returns a new collection.

```cs
int[] arr = Enumerable.Range(1, 10).ToArray();
int[] shuffledArr = arr.Shuffle(); // returns a new shuffled IEnumerable<int> eg. [4, 6, 1, 9, 7, 8, 3, 2, 10, 5]
arr.Shuffle(); // shuffles the array in place
List<int> list = Enumerable.Range(1, 10).ToList();
List<int> shuffledList = list.Shuffle(); // returns a new shuffled List<int> eg. [4, 6, 1, 9, 7, 8, 3, 2, 10, 5]
```

#### ShuffleLinq

Randomly shuffles a collection using LINQ.

```cs
IEnumerable<int> shuffled = Enumerable.Range(1, 10).ShuffleLinq(); // returns a new shuffled IEnumerable<int> eg. [4, 6, 1, 9, 7, 8, 3, 2, 10, 5]
```

#### GetRandomElement

Selects a single random element from a collection.

```cs
List<int> items = Enumerable.Range(1, 100).ToList();
int? randomItem = items.GetRandomElement(); // randomItem is one of the items in the list eg. 42
```

#### GetRandomElements

Selects a specified number of random elements from a collection.

```cs
List<int> items = Enumerable.Range(1, 100).ToList();
IEnumerable<int> randomItems = items.GetRandomElements(5); // 5 random elements from the list eg. [11, 97, 47, 38, 3]
```

#### GenerateRandomString

Generates a random string of the specified length and ASCII range, with optional blacklist.

```cs
string s1 = Random.GenerateRandomString(10); // Up to 10 random printable ASCII characters eg. "d5FimP2aL"
string s2 = Random.GenerateRandomString(10, 5); // Between 5 and 10 random printable ASCII characters characters eg. "d5Fm"
string s3 = Random.GenerateRandomString(10, -1, 65, 90); // Up to 10 uppercase letters only eg. "ARLDFKGNV"
string s4 = Random.GenerateRandomString(10, blacklistedCharacters: new[] { 'a', 'e', 'i', 'o', 'u' }); // Up to 10 random characters with no vowels eg. "d5FmP2L"
```

#### GenerateRandomStrings

Generates multiple random strings of the specified length and ASCII range.

```cs
IEnumerable<string> strings = Random.GenerateRandomStrings(3, 10); // 3 random strings, each up to 10 characters long eg. ["d5FimP2aL", "jk3n452l3s", "P3c"]
```

#### GenerateRandomStringByCharSet

Generates a random string of the specified length using a custom character set.

```cs
char[] charset = { 'A', 'B', 'C', '1', '2', '3' };
string s = Random.GenerateRandomStringByCharSet(10, charset); // 10 characters, only from charset eg. 3BCA11CA23
```

</details>

---

## RunBatches

Helpers for running tasks in batches.

### RunBatches Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### RunBatchedProcessAsync

Processes a collection in batches asynchronously using the provided batch processor delegate. Supports breaking on failure, custom batch sizes, and works with both `IEnumerable<T>` and `List<T>` batch processors.

```cs
// Process a list of items in batches of 30 asynchronously
List<int> items = Enumerable.Range(1, 100).ToList();
List<int> processed = [];
async Task<bool> BatchProcessor(IEnumerable<int> batch)
{
    foreach (int item in batch)
    {
        // Simulate async work
        await Task.Delay(1);
        processed.Add(item);
    }
    return true; // Return false here to indicate failure and optionally break
}
bool result = await items.RunBatchedProcessAsync(BatchProcessor, batchSize: 30); // result == true, processed contains all items

//Using a List<T> batch processor
async Task<bool> BatchProcessorList(List<int> batch)
{
    processed.AddRange(batch);
    await Task.Yield();
    return true;
}
bool result2 = await items.RunBatchedProcessAsync(BatchProcessorList, batchSize: 20); // result2 == true, processed contains all items
// Break on first failed batch
int failAfter = 1;
async Task<bool> FailingBatchProcessor(IEnumerable<int> batch)
{
    return failAfter-- > 0;
}
bool result3 = await items.RunBatchedProcessAsync(FailingBatchProcessor, batchSize: 30, breakOnFail: true); // result3 == false, only first batch processed
```

#### RunBatchedProcess

Processes a collection in batches synchronously using the provided batch processor delegate. Supports breaking on failure, custom batch sizes, and works with both `IEnumerable<T>` and `List<T>` batch processors.

```cs
// Process a list of items in batches of 30 synchronously
List<int> items = Enumerable.Range(1, 100).ToList(); List<int> processed = [];
bool BatchProcessor(IEnumerable<int> batch)
{
    processed.AddRange(batch);
    return true; // Return false here to indicate failure and optionally break
}
bool result = items.RunBatchedProcess(BatchProcessor, batchSize: 30); // result == true, processed contains all items

// Using a List<T> batch processor
bool BatchProcessorList(List<int> batch)
{
    processed.AddRange(batch);
    return true;
}
bool result2 = items.RunBatchedProcess(BatchProcessorList, batchSize: 20); // result2 == true, processed contains all items

// Break on first failed batch
int failAfter = 1;
bool FailingBatchProcessor(IEnumerable<int> batch)
{
    return failAfter-- > 0;
}
bool result3 = items.RunBatchedProcess(FailingBatchProcessor, batchSize: 30, breakOnFail: true); // result3 == false, only first batch processed
```

</details>

---

## Streams

Helpers for working with streams.

### Streams Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ReadStreamAsync

Reads the entire contents of a stream asynchronously into a byte array. Optionally specify a buffer size.

```cs
// Read all bytes from a MemoryStream
byte[] data = { 1, 2, 3, 4, 5 };
using MemoryStream stream = new(data);
byte[] result = await stream.ReadStreamAsync(); // result.ToArray() == data

// Specify a custom buffer size
byte[] result2 = await stream.ReadStreamAsync(bufferSize: 8192); // result2.ToArray() == data

// Handles empty streams
using MemoryStream emptyStream = new();
byte[] emptyResult = await emptyStream.ReadStreamAsync(); // emptyResult.Length == 0
```

#### WriteStreamToStream

Copies the contents of a source stream to a target stream asynchronously, resetting positions and ensuring all data is copied.

```cs
// Copy from MemoryStream to MemoryStream
byte[] data = { 10, 20, 30, 40 };
using MemoryStream source = new(data);
using MemoryStream target = new();
await target.WriteStreamToStream(source); // target.ToArray() == data, target.Position == 0, source.Position == 0

// Copy from FileStream to MemoryStream
using FileStream fileSource = new("TestData/test.png", FileMode.Open, FileAccess.Read, FileShare.Read);
using MemoryStream memTarget = new();
await memTarget.WriteStreamToStream(fileSource); // memTarget.ToArray() == await fileSource.ReadStreamAsync(), memTarget.Position == 0, fileSource.Position == 0
```

</details>

---

## Strings

Helpers for string manipulation.

### Strings Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Left

Returns the leftmost `n` characters of a string (like VBA's `Left`).

```cs
"Hello".Left(3); // "Hel"
"Test".Left(5);  // "Test"
((string?)null).Left(3); // null
```

#### Right

Returns the rightmost `n` characters of a string (like VBA's `Right`).

```cs
"Hello".Right(3); // "llo"
"Test".Right(5);  // "Test"
((string?)null).Right(3); // null
```

#### ExtractBetween

Extracts the substring between two delimiters.

```cs
"Start[Middle]End".ExtractBetween("[", "]"); // "Middle"
"Hello World".ExtractBetween("Hello", "World"); // " "
"NoDelimiters".ExtractBetween("[", "]"); // null
```

#### MakeNullNull

Converts the string "null" (case-insensitive, with or without whitespace) to null.

```cs
"null".MakeNullNull(); // null
" not null ".MakeNullNull(); // " not null "
```

#### ParsePascalCase

Inserts spaces before each uppercase letter in PascalCase or camelCase strings.

```cs
"HelloWorld".ParsePascalCase(); // "Hello World"
"camelCase".ParsePascalCase();  // "camel Case"
```

#### ToTitleCase

Converts a string to title case, with options for handling all-uppercase words.

```cs
"THE QUICK BROWN FOX".ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertAllUppercase); // "The Quick Brown Fox"
"THE QUICK BROWN FOX".ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.IgnoreUppercase); // "THE QUICK BROWN FOX"
```

#### TrimFull

Removes leading/trailing whitespace and reduces multiple spaces to a single space.

```cs
"   extra   spaces   ".TrimFull(); // "extra spaces"
"hello  world   test".TrimFull();  // "hello world test"
```

#### IsNullOrWhiteSpace

Checks if a string is null, empty, or whitespace.

```cs
null.IsNullOrWhiteSpace(); // true
"".IsNullOrWhiteSpace(); // true
"   ".IsNullOrWhiteSpace(); // true
"Test".IsNullOrWhiteSpace(); // false
```

#### IsNullOrEmpty

Checks if a string is null or empty.

```cs
null.IsNullOrWhiteSpace(); // true
"".IsNullOrWhiteSpace(); // true
"   ".IsNullOrWhiteSpace(); // false
"Test".IsNullOrWhiteSpace(); // false
```

#### ContainsInvariant

Case-insensitive, culture-invariant substring search, or checks if the given string contains at least one or all of the strings in a collection of strings, regardless of culture or case.

```cs
"Hello WORLD".ContainsInvariant("world"); // true
"Test".ContainsInvariant("no match"); // false

"Hello WORLD".ContainsInvariant(["world", "Not Present"], true); // true
"Hello WORLD".ContainsInvariant(["world", "Not Present"], false); // false
"Hello WORLD".ContainsInvariant(["hello", "world"], false); // true
"Test".ContainsInvariant("no match"); // false
```

#### StartsWithInvariant

Checks if the given string begins with a specific string regardless of culture or case

```cs
"Hello WORLD".StartsWithInvariant("hello w"); // true
"Test".StartsWithInvariant("no match"); // false
```

#### EndsWithInvariant

Checks if the given string ends with a specific string regardless of culture or case

```cs
"Hello WORLD".EndsWithInvariant(" world"); // true
"Test".EndsWithInvariant("no match"); // false
```

#### IndexOfInvariant

Gets the index of a character in a string, ignoring culture and case

```cs
"Hello WORLD".IndexOfInvariant('w'); // 6
```

#### Contains

Checks if the given string contains at least one or all of the strings in a collection of strings (case sensitive).

```cs
"Hello WORLD".Contains(["Hello", "Not Present"], true); // true
"Hello WORLD".Contains(["Hello", "Not Present"], false); // false
"Hello WORLD".Contains(["Hello", "WORLD"], false); // true
"Test".Contains("no match"); // false
```

#### ReplaceInvariant

Case-insensitive, culture-invariant string replacement.

```cs
"Hello WORLD".ReplaceInvariant("hello", "Test"); // "Test WORLD"
```

#### StrEq

Case-insensitive, culture-invariant string equality.

```cs
"string1".StrEq("STRING1"); // true
"string1".StrEq("string2"); // false
```

#### StrComp

Compare two strings with optional stringComparison parameter

```cs
"string1".StrComp("STRING1"); // false
"string1".StrComp("string1"); // false
"string1".StrComp("string2"); // false
"string1".StrComp("STRING1", StringComparison.InvariantCultureIgnoreCase); // true
"string1".StrComp("string2", StringComparison.InvariantCultureIgnoreCase); // false
```

#### IsAlphanumeric

Checks if a string contains only letters and numbers (optionally spaces).

```cs
"abc123".IsAlphanumeric(); // true
"abc 123".IsAlphanumeric(true); // true
"abc 123".IsAlphanumeric(false); // false
"abc@123".IsAlphanumeric(); // false
```

#### IsAlphaOnly

Checks if a string contains only letters (optionally spaces).

```cs
"abcDEF".IsAlphaOnly(); // true
"abc DEF".IsAlphaOnly(true); // true
"abc DEF".IsAlphaOnly(false); // false
"abc@DEF".IsAlphaOnly(); // false
"abc123".IsAlphaOnly(); // false
```

#### IsNumericOnly

Checks if a string contains only numbers (optionally spaces).

```cs
"123456".IsNumericOnly(); // true
"123 456".IsNumericOnly(true); // true
"123 456".IsNumericOnly(false); // false
"123@456".IsNumericOnly(); // false
"abc123".IsNumericOnly(); // false
```

#### ExtractToLastInstance

Gets string up until before the last instance of a character (exclusive)

```cs
"Hello World Hello World".ExtractToLastInstance('W') // "Hello World Hello "
```

#### ExtractFromLastInstance

Gets string remaining after the last instance of a character (exclusive)

```cs
"Hello World Hello World".ExtractFromLastInstance('W') // "orld"
```

#### TrimObjectStringsR

Trims top level string property values retrieved with reflection in an object using [TrimFull](#trimfull).

```cs
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
    public NestedClass? NestedClass { get; set; }
}

public class NestedClass
{
    public int NestedIntProp { get; set; }
    public string? NestedStringProp { get; set; }
}

MyClass myClass = new()
{
    IntProp = 1,
    StringProp = " Test    string ",
    NestedClass = new()
    {
        NestedIntProp = 1,
        NestedStringProp = " Test    string "
    }
};

myClass = myClass.TrimObjectStringsR();
// myClass =
// {
//     IntProp = 1,
//     StringProp = "Test string",
//     NestedClass =
//     {
//         NestedIntProp = 1,
//         NestedStringProp = " Test    string "
//     }
// };
```

#### TrimObjectStrings

Trims top level string property values in a class, and optionally nested class properties using expression trees in an object using [TrimFull](#trimfull).

```cs
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
    public NestedClass? NestedClass { get; set; }
}

public class NestedClass
{
    public int NestedIntProp { get; set; }
    public string? NestedStringProp { get; set; }
}

MyClass myClass = new()
{
    IntProp = 1,
    StringProp = " Test    string ",
    NestedClass = new()
    {
        NestedIntProp = 1,
        NestedStringProp = " Test    string "
    }
};

myClass = myClass.TrimObjectStrings(true);
// myClass =
// {
//     IntProp = 1,
//     StringProp = "Test string",
//     NestedClass =
//     {
//         NestedIntProp = 1,
//         NestedStringProp = "Test string" <== Nested string is trimmed as well here>
//     }
// };
```

#### NormalizeObjectStringsR

Apply normalization form and optionally [TrimFull](#trimfull) to all top level string properties in a class using reflection.

```cs
public class TestObject
{
    public string? StringProp { get; set; }
    public NestedObject NestedObject { get; set; } = new();
    public string? StringPropWithSpaces { get; set; }
    public NestedObject NestedStringPropWithSpaces { get; set; } = new();
}

TestObject testObject = new()
{
    StringProp = "test\u0300", // Combining grave accent
    NestedObject = new() { InnerString = "e\u0301" }, // Combining acute accent
    StringPropWithSpaces = "  test  ",
    NestedStringPropWithSpaces = new() { InnerString = "  test  " }
};

testObject = testObject.NormalizeObjectStringsR(true, NormalizationForm.FormD);
// testObject =
{
    StringProp = "test̀", // Combining grave accent
    NestedObject = new() { InnerString = "e\u0301" }, // Combining acute accent
    StringPropWithSpaces = "test",
    NestedStringPropWithSpaces = new() { InnerString = "  test  " }
};
```

#### NormalizeObjectStrings

Apply normalization form and optionally [TrimFull](#trimfull) to all top level string properties in a class and optionally string properties of nested classes using expression trees.

```cs
public class TestObject
{
    public string? StringProp { get; set; }
    public NestedObject NestedObject { get; set; } = new();
    public string? StringPropWithSpaces { get; set; }
    public NestedObject NestedStringPropWithSpaces { get; set; } = new();
}

TestObject testObject = new()
{
    StringProp = "test\u0300", // Combining grave accent
    NestedObject = new() { InnerString = "e\u0301" }, // Combining acute accent
    StringPropWithSpaces = "  test  ",
    NestedStringPropWithSpaces = new() { InnerString = "  test  " }
};

testObject = testObject.NormalizeObjectStrings(true, NormalizationForm.FormD, true);
// testObject =
{
    StringProp = "test̀",
    NestedObject = new() { InnerString = "é" },
    StringPropWithSpaces = "test",
    NestedStringPropWithSpaces = new() { InnerString = "test" }
};
```

#### MakeObjectNullNullR

Apply [MakeNullNull](#makenullnull) to all top level string properties in a class.

```cs
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
    public NestedClass? NestedClass { get; set; }
}

public class NestedClass
{
    public int NestedIntProp { get; set; }
    public string? NestedStringProp { get; set; }
}

MyClass myClass = new()
{
    IntProp = 1,
    StringProp = "null",
    NestedClass = new()
    {
        NestedIntProp = 1,
        NestedStringProp = "null"
    }
};

myClass = myClass.MakeObjectNullNullR();
// myClass =
// {
//     IntProp = 1,
//     StringProp = null,
//     NestedClass =
//     {
//         NestedIntProp = 1,
//         NestedStringProp = "null"
//     }
// };
```

#### MakeObjectNullNull

Apply [MakeNullNull](#makenullnull) to all top level string properties in a class, optionally applying to nested classes as well.

```cs
public class MyClass
{
    public int IntProp { get; set; }
    public string? StringProp { get; set; }
    public NestedClass? NestedClass { get; set; }
}

public class NestedClass
{
    public int NestedIntProp { get; set; }
    public string? NestedStringProp { get; set; }
}

MyClass myClass = new()
{
    IntProp = 1,
    StringProp = "null",
    NestedClass = new()
    {
        NestedIntProp = 1,
        NestedStringProp = "null"
    }
};

myClass = myClass.MakeObjectNullNullR();
// myClass =
// {
//     IntProp = 1,
//     StringProp = null,
//     NestedClass =
//     {
//         NestedIntProp = 1,
//         NestedStringProp = null
//     }
// };
```

#### ToNString

Convert nullable value into a nullable string, retaining null value if the source was null. Works with `DateTime?`, `DateOnly?`, `TimeSpan?`, `int?`, `long?`, `double?`, `decimal?`, `bool?`, `object?`

```cs
string? test = (int)null.ToNString(); // null
string? test = 1.ToNString(); // "1"
```

#### ToListInt

Converts list of string representations of integers into list of integers.

```cs
List<int> numbers = ["1", "2", "3"].ToListInt(); // [1, 2, 3]
```

#### ToNInt

Parses nullable string into a nullable integer where null string results in a returned null value.

```cs
"1".ToNInt(); // 1
"invalid".ToNInt(); // null
null.ToNInt(); // null
```

#### ToNDouble

Parses nullable string into a nullable double where null string results in a returned null value.

```cs
"1".ToNDouble(); // 1.0d
"invalid".ToNDouble(); // null
null.ToNDouble(); // null
```

#### ToNDecimal

Parses nullable string into a nullable decimal where null string results in a returned null value.

```cs
"1".ToNDecimal(); // 1.0m
"invalid".ToNDecimal(); // null
null.ToNDecimal(); // null
```

#### ToNDateTime

Parses nullable string into a nullable DateTime where null string results in a returned null value.

```cs
"2024-05-12".ToNDateTime(); // {5/12/2024 12:00:00 AM}
"invalid".ToNDateTime(); // null
null.ToNDateTime(); // null
```

#### ToNDateOnly

Parses nullable string into a nullable DateOnly where null string results in a returned null value.

```cs
"2024-05-12".ToNDateOnly(); // {5/12/2024}
"invalid".ToNDateOnly(); // null
null.ToNDateOnly(); // null
```

#### YesNoToBool

Converts string "Yes" and "No" values into true and false using invariant text comparison on trimmed version of the original string.

```cs
"YES".YesNoToBool(); // true
"YES   ".YesNoToBool(); // true
"yes".YesNoToBool(); // true
"No".YesNoToBool(); // false
"SomeRandomText".YesNoToBool(); // false
```

#### YNToBool

Converts string "Y" and "N" values into true and false using invariant text comparison on trimmed version of the original string.

```cs
"Y".YNToBool(); // true
"Y   ".YNToBool(); // true
"y".YNToBool(); // true
"N".YNToBool(); // false
"SomeRandomText".YNToBool(); // false
```

#### BoolToYesNo

Converts a boolean into a "Yes" or "No" string value

```cs
true.BoolToYesNo(); // "Yes"
false.BoolToYesNo(); // "No"
```

#### BoolToYN

Converts a boolean into a "Y" or "N" string value

```cs
true.BoolToYN(); // "Y"
false.BoolToYN(); // "N"
```

#### BoolToInt

Convert bool to 1 or 0

```cs
true.BoolToInt(); // 1
false.BoolToInt(); // 0
```

#### GetSafeDate

Get file name safe date in the chosen format

```cs
"5/12/2025".GetSafeDate("yyyy/MM/dd"); //2025-05-12
Strings.GetSafeDate("yyyy/MM/dd"); // Current date in yyyy-MM-dd format
```

#### MakeExportNameUnique

Adds number in () at the end of a file name if it would create a duplicate in the savePath

```cs
string safeFileName = Strings.MakeExportNameUnique(@"C:\Some\Test\Path", "test", "txt"); // "test (0).txt" -- Assuming test.txt is already in C:\Some\Test\Path
```

#### TimespanToShortForm

"Description"

```cs
<Example Code>
```

#### GetHash

Gets a hash of a string using the specified algorithm.

```cs
string hashValue = "test string".GetHash(EHashAlgorithm.SHA256); // 64-char SHA256 hex string
```

#### NormalizeWhiteSpace

Removes excess whitespace, preserving single spaces and line breaks.

```cs
"Hello   World\t\nTest".NormalizeWhiteSpace(); // "Hello World\nTest"
```

#### FormatDateString

Take any format of a date time string and convert it to a different format.

```cs
string formattedDateString = "2023-01-01".FormatDateString("yyyy-MM-dd", "yyyy.MM.dd"); // "2023.01.01"
```

#### ReplaceInverse

Replaces any characters that don't match the provided regexPattern with specified replacement string.

```cs
string result = "Example_Test_Text".ReplaceInverse("Test_Text", ""); // "Test_Text"
```

#### UrlEncodeReadable

URL Encodes a string but then replaces specific escape sequences with their decoded character.

```cs
string urlEncode = UrlEncode("Hello World"); // "Hello%20World"
string urlEncode = UrlEncodeReadable("Hello World"); // "Hello World"
```

#### FormatPhoneNumber

Formats a string as a phone number.

```cs
"1234567890".FormatPhoneNumber(); // "123-456-7890"
"11234567890".FormatPhoneNumber(); // "+1 123-456-7890"
"1234567890".FormatPhoneNumber("-", true); // "(123)-456-7890"
```

#### SplitLines

Splits a string into lines.

```cs
"hello\nworld\ntest".SplitLines(); // ["hello", "world", "test"]
```

#### ToFractionString

Converts decimals to fraction strings.

```cs
2.5m.ToFractionString(3); // "2 1/2"
```

#### FractionToDecimal

Parses fraction strings to decimals

```cs
"2 1/2".FractionToDecimal(); // 2.5m
"2.5".FractionToDecimal(); // 2.5m
```

#### TryFractionToDecimal

Attempts to convert a fraction represented as a string into its decimal equivalent

```cs
if("3 1/4".TryStringToDecimal(out decimal result))
{
    Console.WriteLine(result); // 3.25m
}
if("3.25".TryStringToDecimal(out decimal result))
{
    Console.WriteLine(result); // 3.25m
}
```

#### TryStringToDecimal

Attempts to convert a decimal or fraction represented as a string into its decimal equivalent

```cs
if("3 1/4".TryStringToDecimal(out decimal result))
{
    Console.WriteLine(result); // 3.25m
}

if("3.25".TryStringToDecimal(out decimal result))
{
    Console.WriteLine(result); // 3.25m
}

// Will ignore any text that does not fit the regex [0-9]*\.?[0-9]+
if("3.25 Some other text".TryStringToDecimal(out decimal result))
{
    Console.WriteLine(result); // 3.25m
}
```

#### FractionToDouble

Parses fraction strings to doubles

```cs
"2 1/2".FractionToDecimal(); // 2.5d
"2.5".FractionToDecimal(); // 2.5d
```

#### TryFractionToDouble

Attempts to convert a fraction represented as a string into its double equivalent

```cs
if("3 1/4".TryStringToDouble(out double result))
{
    Console.WriteLine(result); // 3.25d
}
if("3.25".TryStringToDouble(out double result))
{
    Console.WriteLine(result); // 3.25d
}
```

#### TryStringToDouble

Attempts to convert a decimal or fraction represented as a string into its double equivalent

```cs
if("3 1/4".TryStringToDouble(out double result))
{
    Console.WriteLine(result); // 3.25d
}

if("3.25".TryStringToDouble(out double result))
{
    Console.WriteLine(result); // 3.25d
}

// Will ignore any text that does not fit the regex [0-9]*\.?[0-9]+
if("3.25 Some other text".TryStringToDouble(out double result))
{
    Console.WriteLine(result); // 3.25d
}
```

#### RemoveLetters

Removes all letters from a string.

```cs
"123hello123".RemoveLetters(); // "123123"
```

#### RemoveNumbers

Removes all numbers from a string.

```cs
"123hello123".RemoveNumbers(); // "hello"
```

#### GetOnlyLetters

Extracts only the letters from a string.

```cs
"hello123".GetOnlyLetters(); // "hello"
```

#### GetOnlyNumbers

Extracts only the numbers from a string.

```cs
"hello123".GetOnlyNumbers(); // "123"
"123 1/2".GetOnlyNumbers(true); // "123 1/2"
```

#### RemoveLeadingNonAlphanumeric

Removes non-alphanumeric characters from the start of a string.

```cs
"!@#abc123".RemoveLeadingNonAlphanumeric(); // "abc123"
```

#### RemoveTrailingNonAlphanumeric

Removes non-alphanumeric characters from the end of a string.

```cs
"abc123!@#".RemoveTrailingNonAlphanumeric(); // "abc123"
```

#### TrimOuterNonAlphanumeric

Chains [RemoveLeadingNonAlphanumeric](#removeleadingnonalphanumeric) and [RemoveTrailingNonAlphanumeric](#removeleadingnonalphanumeric) calls

```cs
"!@#abc123!@#".TrimOuterNonAlphanumeric(); // "abc123"
```

#### CountChars

Counts the number of times a character appears in a string.

```cs
"hello".CountChars('l'); // 2
```

#### HasNoMoreThanNumberOfChars

Checks if a string contains no more than a specified number of a given character.

```cs
"hello".HasNoMoreThanNumberOfChars('l', 2); // true
"hello".HasNoMoreThanNumberOfChars('l', 1); // false
```

#### HasNoLessThanNumberOfChars

Checks if a string contains at least a specified number of a given character.

```cs
"hello".HasNoLessThanNumberOfChars('l', 2); // true
"hello".HasNoLessThanNumberOfChars('l', 3); // false
```

</details>

---

## TypeChecks

Helpers for checking types.

### TypeChecks Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### IsDelegate

Checks if a type is a delegate.

```cs
typeof(Action).IsDelegate(); // true
typeof(Func<int>).IsDelegate(); // true
typeof(string).IsDelegate(); // false
typeof(int).IsDelegate(); // false
```

#### IsArray

Checks if a type is an array.

```cs
typeof(int[]).IsArray(); // true
typeof(string[]).IsArray(); // true
typeof(List<int>).IsArray(); // false
typeof(int).IsArray(); // false
```

#### IsDictionary

Checks if a type implements `IDictionary`.

```cs
typeof(Dictionary<int, string>).IsDictionary(); // true
typeof(System.Collections.Hashtable).IsDictionary(); // true
typeof(List<int>).IsDictionary(); // false
typeof(int).IsDictionary(); // false
```

#### IsEnumerable

Checks if a type implements `IEnumerable` and is not a string.

```cs
typeof(List<int>).IsEnumerable(); // true
typeof(int[]).IsEnumerable(); // true
typeof(string).IsEnumerable(); // false
typeof(int).IsEnumerable(); // false
```

#### IsClassOtherThanString

Checks if a type is a class other than string. Returns true for null.

```cs
typeof(List<int>).IsClassOtherThanString(); // true
typeof(string).IsClassOtherThanString(); // false
typeof(int).IsClassOtherThanString(); // false
((Type?)null).IsClassOtherThanString(); // true
```

#### IsNumeric

Checks if an object is a numeric type.

```cs
123.IsNumeric(); // true
123.45.IsNumeric(); // true
"string".IsNumeric(); // false
((object?)null).IsNumeric(); // false
```

#### IsNumericType

Checks if a type is a numeric type (including nullable numeric types).

```cs
typeof(int).IsNumericType(); // true
typeof(double).IsNumericType(); // true
typeof(string).IsNumericType(); // false
typeof(int?).IsNumericType(); // true
((Type?)null).IsNumericType(); // false
```

#### IsSimpleType

Returns true if the type is a primitive, enum, string, decimal, DateTime, DateTimeOffset, TimeSpan, or Guid.

```cs
typeof(int).IsSimpleType(); // true
typeof(string).IsSimpleType(); // true
typeof(decimal).IsSimpleType(); // true
typeof(DateTime).IsSimpleType(); // true
typeof(Guid).IsSimpleType(); // true
typeof(List<int>).IsSimpleType(); // false
```

#### IsReadOnlyCollectionType

Checks if the type is a read-only collection type, such as `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, or `ReadOnlyCollection<T>`.

```cs
typeof(IReadOnlyCollection<int>).IsReadOnlyCollectionType(); // true .
typeof(IReadOnlyList<string>).IsReadOnlyCollectionType(); // true
typeof(System.Collections.ObjectModel.ReadOnlyCollection<int>).IsReadOnlyCollectionType(); // true
typeof(List<int>).IsReadOnlyCollectionType(); // false
typeof(int[]).IsReadOnlyCollectionType(); // false
typeof(System.Collections.Immutable.ImmutableArray<int>).IsReadOnlyCollectionType(); // true
typeof(Dictionary<int, string>).IsReadOnlyCollectionType(); // false
typeof(IReadOnlyDictionary<int, string>).IsReadOnlyCollectionType(); // true
typeof(object).IsReadOnlyCollectionType(); // false
```

</details>

---

## UnitConversion

Helpers for converting between units.

### UnitConversion Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### LbsToKg

Converts pounds (lbs) to kilograms (kg).

```cs
decimal kg = 10m.LbsToKg(); // 4.53592
decimal kg2 = ((decimal?)10.0).LbsToKg(); // 4.53592
decimal kg3 = ((decimal?)null).LbsToKg(); // 0

```

#### KgToLbs

Converts kilograms (kg) to pounds (lbs).

```cs
decimal lbs = 10m.KgToLbs(); // 22.0462
decimal lbs2 = ((decimal?)10.0).KgToLbs(); // 22.0462
decimal lbs3 = ((decimal?)null).KgToLbs(); // 0
```

#### InsToFt

Converts inches to feet.

```cs
decimal feet = 12m.InsToFt(); // 1.0
decimal feet2 = ((decimal?)12.0).InsToFt(); // 1.0
decimal feet3 = ((decimal?)null).InsToFt(); // 0
```

#### InsToMm

Converts inches to millimeters.

```cs
decimal mm = 1m.InsToMm(); // 25.4
decimal mm2 = ((decimal?)1.0).InsToMm(1); // 25.4
decimal mm3 = ((decimal?)null).InsToMm(1); // 0
```

#### MmToIns

Converts millimeters to inches.

```cs
decimal inches = 25.4m.MmToIns(1); // 1.0
decimal inches2 = ((decimal?)25.4).MmToIns(1); // 1.0
decimal inches3 = ((decimal?)null).MmToIns(1); // 0
```

#### FtToIns

Converts feet to inches.

```cs
decimal inches = 1m.FtToIns(); // 12.0
decimal inches2 = ((decimal?)1.0).FtToIns(); // 12.0
decimal inches3 = ((decimal?)null).FtToIns(); // 0
```

#### BytesToKb

Converts bytes to kilobytes (KB).

```cs
decimal kb = 1024.BytesToKb(1); // 1.0
decimal kb2 = 1024L.BytesToKb(1); // 1.0
```

#### KbToBytes

Converts kilobytes (KB) to bytes.

```cs
long bytes = 1m.KbToBytes(); // 1024
```

#### BytesToMb

Converts bytes to megabytes (MB).

```cs
decimal mb = 1048576.BytesToMb(1); // 1.0
decimal mb2 = 1048576L.BytesToMb(1); // 1.0
```

#### MbToBytes

Converts megabytes (MB) to bytes.

```cs
long bytes = 1m.MbToBytes(); // 1048576
```

#### BytesToGb

Converts bytes to gigabytes (GB).

```cs
decimal gb = 1073741824.BytesToGb(1); // 1.0
decimal gb2 = 1073741824L.BytesToGb(1); // 1.0
```

#### GbToBytes

Converts gigabytes (GB) to bytes.

```cs
long bytes = 1m.GbToBytes(); // 1073741824
```

#### BytesToTb

Converts bytes to terabytes (TB).

```cs
decimal tb = 1099511627776.BytesToTb(1); // 1.0
decimal tb2 = 1099511627776L.BytesToTb(1); // 1.0
```

#### TbToBytes

Converts terabytes (TB) to bytes.

```cs
long bytes = 1m.TbToBytes(); // 1099511627776
```

#### KbToMb

Converts kilobytes (KB) to megabytes (MB).

```cs
decimal mb = 1024m.KbToMb(1); // 1.0
```

#### MbToKb

Converts megabytes (MB) to kilobytes (KB).

```cs
decimal mb = 1024m.KbToMb(1); // 1.0
```

#### KbToGb

Converts kilobytes (KB) to gigabytes (GB).

```cs
decimal gb = 1048576m.KbToGb(1); // 1.0
```

#### GbToKb

Converts gigabytes (GB) to kilobytes (KB).

```cs
decimal kb = 1m.GbToKb(1); // 1048576.0
```

#### KbToTb

Converts kilobytes (KB) to terabytes (TB).

```cs
decimal tb = 1073741824m.KbToTb(1); // 1.0
```

#### TbToKb

Converts terabytes (TB) to kilobytes (KB).

```cs
decimal kb = 1m.TbToKb(1); // 1073741824.0
```

#### MbToGb

Converts megabytes (MB) to gigabytes (GB).

```cs
decimal gb = 1024m.MbToGb(1); // 1.0
```

#### GbToMb

Converts gigabytes (GB) to megabytes (MB).

```cs
decimal mb = 1m.GbToMb(1); // 1024.0
```

#### MbToTb

Converts megabytes (MB) to terabytes (TB).

```cs
decimal tb = 1048576m.MbToTb(1); // 1.0
```

#### TbToMb

Converts terabytes (TB) to megabytes (MB).

```cs
decimal mb = 1m.TbToMb(1); // 1048576.0
```

#### GbToTb

Converts gigabytes (GB) to terabytes (TB).

```cs
decimal tb = 1024m.GbToTb(1); // 1.0
```

#### TbToGb

Converts terabytes (TB) to gigabytes (GB).

```cs
decimal gb = 1m.TbToGb(1); // 1024.0
```

#### GetFileSizeFromBytesWithUnits

Returns a human-readable string representation of the number of bytes, with units.

```cs
string size1 = 1024L.GetFileSizeFromBytesWithUnits(); // "1 KB"
string size2 = 1048576L.GetFileSizeFromBytesWithUnits(); // "1 MB"
string size3 = 1073741824L.GetFileSizeFromBytesWithUnits(); // "1 GB"
string size4 = 1099511627776L.GetFileSizeFromBytesWithUnits(); // "1 TB"
string size5 = 0L.GetFileSizeFromBytesWithUnits(); // "0 B"
string size6 = ((long?)null).GetFileSizeFromBytesWithUnits(); // "-0"
string size7 = 1024.GetFileSizeFromBytesWithUnits(); // "1 KB"
string size8 = ((int?)null).GetFileSizeFromBytesWithUnits(); // "-0"
```

#### MetersToMiles

Converts meters to miles. Overloads support decimal, double, int, and nullable types.

```cs
decimal miles = 1609.34m.MetersToMiles(); // 1.0
decimal miles2 = ((decimal?)1609.34).MetersToMiles(); // 1.0
decimal miles3 = 1609.34.MetersToMiles(); // 1.0 (double overload)
decimal miles4 = ((double?)1609.34).MetersToMiles(); // 1.0
decimal miles5 = 1609.MetersToMiles(); // 1.0 (int overload)
decimal miles6 = ((int?)1609).MetersToMiles(); // 1.0
```

#### MilesToMeters

Converts miles to meters. Overloads support decimal and nullable types.

```cs
decimal meters = 1m.MilesToMeters(); // 1609.34
decimal meters2 = ((decimal?)1.0).MilesToMeters(); // 1609.34
decimal meters3 = 1.MilesToMeters(); // 1609.34 (int overload)
decimal meters4 = ((int?)1).MilesToMeters(); // 1609.34
```

</details>

---

## Validation

Helpers for validating objects and properties.

### Validation Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### SetInvalidPropertiesToDefault

Sets all properties that are invalid based on their validation decorators / attributes to the default value for that property

```cs
    public sealed class TestModel
    {
        [Required]
        public string? RequiredString { get; set; }

        [StringLength(5)]
        public string? MaxLengthString { get; set; }

        [Range(1, 10)]
        public int RangeNumber { get; set; }

        public string? UnvalidatedProperty { get; set; }
    }

    TestModel testModel = new()
    {
        RequiredString = "Required",
        MaxLengthString = "StringTooLong",
        RangeNumber = 55,
        UnvalidatedProperty = "Whatever you want here"
    };
    testModel = testModel.SetInvalidPropertiesToDefault();
    // testModel =
    // {
    //     RequiredString = "Required",
    //     MaxLengthString = null,
    //     RangeNumber = 0,
    //     UnvalidatedProperty = "Whatever you want here"
    // }
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Core
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
