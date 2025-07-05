# CommonNetFuncs.Csv

[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Csv)](https://www.nuget.org/packages/CommonNetFuncs.Csv/)

This lightweight project contains helper methods for several common functions required by applications.

## Contents

- [CommonNetFuncs.Csv](#commonnetfuncscsv)
  - [Contents](#contents)
  - [CsvExportHelpers](#csvexporthelpers)
    - [CsvExportHelpers Usage Examples](#csvexporthelpers-usage-examples)
      - [ExportToCsv](#exporttocsv)
  - [CsvReadHelpers](#csvreadhelpers)
    - [CsvReadHelpers Usage Examples](#csvreadhelpers-usage-examples)
      - [ReadCsv](#readcsv)
      - [ReadCsvAsync](#readcsvasync)
      - [ReadCsvAsyncEnumerable](#readcsvasyncenumerable)
      - [ReadCsvToDataTable](#readcsvtodatatable)

---

## CsvExportHelpers

Helpers for exporting data to a CSV file.

### CsvExportHelpers Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ExportToCsv

Export data from an IEnumerable or DataTable

```cs
public class Person
{
  public string Name { get; set; }
  public int Age { get; set; }
}

Person[] testData = [ new() { Name = "Chris", Age = 32 }, new() { Name = "Nick", Age = 43 } ];
await using MemoryStream ms = new();
await testData.ExportToCsv(ms); // ms contains CSV data for testData
```

</details>

---

## CsvReadHelpers

Helpers for ingesting data from CSV files.

### CsvReadHelpers Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ReadCsv

Read CSV data directly from a physical CSV file or stream containing its data into a List. CSV data should match the type of T.

```cs
public class Person
{
  public string Name { get; set; }
  public int Age { get; set; }
}

List<Person> csvPeople = CsvReadHelpers.ReadCsv(@"C:\Documents\People.csv"); // csvPeople contains list of values from People.csv
```

#### ReadCsvAsync

Asynchronously read CSV data directly from a physical CSV file or stream containing its data into a List. CSV data should match the type of T.

```cs
public class Person
{
  public string Name { get; set; }
  public int Age { get; set; }
}

List<Person> csvPeople = await CsvReadHelpers.ReadCsvAsync(@"C:\Documents\People.csv"); // csvPeople contains list of values from People.csv
```

#### ReadCsvAsyncEnumerable

Asynchronously read CSV data directly from a physical CSV file or stream containing its data into an IAsyncEnumerable. CSV data should match the type of T.

```cs
public class Person
{
  public string Name { get; set; }
  public int Age { get; set; }
}

List<Person> result = new();
await foreach (Person record in CsvReadHelpers.ReadCsvAsyncEnumerable<Person>(@"C:\Documents\People.csv"))
{
    result.Add(record);
}
// Result contains list of all records within People.csv
```

#### ReadCsvToDataTable

Read CSV data directly from a physical CSV file or stream containing its data into a DataTable. DataTable can be constructed with a definite or indefinite type

```cs
public class Person
{
  public string Name { get; set; }
  public int Age { get; set; }
}

using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(@"C:\Documents\People.csv"); // Indeterminate type in People.csv, dataTable contains all records from People.csv with all values as strings

using DataTable dataTable = CsvReadHelpers.ReadCsvToDataTable(@"C:\Documents\People.csv", typeof(Person)); // Known type in People.csv, dataTable contains all records from People.csv with all values typed per Person class
```

</details>
