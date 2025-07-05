# CommonNetFuncs.Web.Interface

[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Web.Interface)](https://www.nuget.org/packages/CommonNetFuncs.Web.Interface/)

This lightweight project contains helper methods related to web interface functionality (specifically designed for MVC)

## Contents

- [CommonNetFuncs.Web.Interface](#commonnetfuncswebinterface)
  - [Contents](#contents)
  - [DataTableHelpers](#datatablehelpers)
    - [DataTableHelpers Usage Examples](#datatablehelpers-usage-examples)
      - [GetDataTableRequest](#getdatatablerequest)
      - [GetSortAndLimitPostModel](#getsortandlimitpostmodel)
  - [ModelErrorHelpers](#modelerrorhelpers)
    - [ModelErrorHelpers Usage Examples](#modelerrorhelpers-usage-examples)
      - [ParseModelStateErrors](#parsemodelstateerrors)
  - [SelectListItemCreation](#selectlistitemcreation)
    - [SelectListItemCreation Usage Examples](#selectlistitemcreation-usage-examples)
      - [ToSelectListItem](#toselectlistitem)

---

## DataTableHelpers

Helpers related to handling requests from the DataTables.net JavaScript plugin.

### DataTableHelpers Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetDataTableRequest

Parses the `HttpRequest` passed by the DataTables.net API into a DataTableRequest object (included in the CommonNetFuncs.Web.Interface package)

```cs
 HttpRequest request = A.Fake<HttpRequest>();

 Dictionary<string, StringValues> formData = new()
 {
     { "draw", new StringValues("1") },
     { "start", new StringValues("0") },
     { "length", new StringValues("10") },
     { "order[0][column]", new StringValues("1") },
     { "order[0][dir]", new StringValues("asc") },
     { "columns[1][data]", new StringValues("name") },
     { "search[value]", new StringValues("field1=value1,field2=value2") }
 };

 IFormCollection formCollection = new FormCollection(formData);

 A.CallTo(() => request.ContentType).Returns("application/x-www-form-urlencoded");
 A.CallTo(() => request.Form).Returns(formCollection);

// Using the above form data as the HttpRequest's form data...
DataTableRequest result = request.GetDataTableRequest();

// result =
// {
//    Draw = 1,
//    PageSize = 10,
//    Skip = 0,
//    SortColumns[0] = "name",
//    SortColumnsDir[0] = "asc",
//    SearchValues["field1"] = "value1",
//    SearchValues["field2"] = "value2"
// }

```

#### GetSortAndLimitPostModel

Converts a DataTableRequest object into a SortAndLimitPostModel object (included in the CommonNetFuncs.Web.Interface package) to make REST calls from the controller easier.

```cs
DataTableRequest result = request.GetDataTableRequest();
SortAndLimitPostModel sortAndLimitPostModel = DataTableHelpers.GetSortAndLimitPostModel(result);
// Assuming the same request as before:
// sortAndLimitPostModel =
// {
//   SortColumns = ["name"],
//   SortColumnDir = ["asc"],
//   Skip = 0,
//   PageSize = 10
// }
```

---

## ModelErrorHelpers

Helper for dealing with model errors captured in ASP.NET Core applications.

### ModelErrorHelpers Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ParseModelStateErrors

Convert ModelStateDictionary used by ASP.NET Core into a standard dictionary.

```cs
if (!ModelState.IsValid)
{
  Dictionary<string, string?> errors = ModelErrorHelpers.ParseModelStateErrors(ModelState); // Result is a dictionary containing all of the model state errors inside of a controller call
}
```

---

## SelectListItemCreation

Helpers for creating SelectListItem objects for populating select lists.

### SelectListItemCreation Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ToSelectListItem

Creates a SelectListItem based on the input values.

```cs
public class Person
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
}

// Id and Value are different
List<Person> people = [ new() { Id = 1, Name = "Nick", CreatedDate = DateTime.Now }, new() { Id = 2, Name = "Chris", CreatedDate = DateTime.Now } ];
List<SelectListItem> peopleOptions = people.Select(x => x.Id.ToSelectListItem(x.Name)); // [ { Value = "1", Text = "Nick" }, { Value = "2", Text = "Chris" } ]

// Value only or Id and Value are the same
List<string> colors = [ "red", "green", "blue" ];
List<SelectListItem> colorOptions = colors.Select(x => x.ToSelectListItem()); // [ { Value = "red", Text = "red" }, { Value = "green", Text = "green" }, { Value = "blue", Text = "blue" } ]
```

</details>
