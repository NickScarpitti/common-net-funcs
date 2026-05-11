# CommonNetFuncs.Excel.OpenXml

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Excel.OpenXml)](https://www.nuget.org/packages/CommonNetFuncs.Excel.OpenXml)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Excel.OpenXml)](https://www.nuget.org/packages/CommonNetFuncs.Excel.OpenXml/)

This project contains helper methods for reading and writing Excel files using the OpenXML SDK in .NET applications.

## Contents

- [CommonNetFuncs.Excel.OpenXml](#commonnetfuncsexcelopenxml)
	- [Contents](#contents)
	- [Common](#common)
		- [Common Usage Examples](#common-usage-examples)
			- [InitializeExcelFile / CreateNewSheet](#initializeexcelfile--createnewsheet)
	- [Export](#export)
		- [Export Usage Examples](#export-usage-examples)
			- [GenericExcelExport](#genericexcelexport)
	- [Installation](#installation)
	- [License](#license)

---

## Common

Low-level helpers for building and manipulating `SpreadsheetDocument` objects with the OpenXML SDK. Covers creating workbooks and sheets, reading and writing cell values, managing number formats, inserting images, and applying cell styles.

### Common Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### InitializeExcelFile / CreateNewSheet

```cs
using DocumentFormat.OpenXml.Packaging;
using CommonNetFuncs.Excel.OpenXml;

using MemoryStream ms = new();
using SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true);

uint sheetId = document.InitializeExcelFile("Sheet1"); // creates the workbook and first sheet
uint sheet2Id = document.CreateNewSheet("Sheet2");     // appends a second sheet

Worksheet? ws = document.GetWorksheetById(sheetId);
```

</details>

---

## Export

Provides a `GenericExcelExport` extension method that converts any `IEnumerable<T>` into a `.xlsx` `MemoryStream` using the OpenXML SDK directly. Supports optional table formatting, custom sheet and table names, and column exclusion.

### Export Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GenericExcelExport

```cs
using CommonNetFuncs.Excel.OpenXml;

List<MyRecord> data = GetData();

// Basic export
MemoryStream? stream = data.GenericExcelExport();

// Export as a formatted Excel table, skipping a column
MemoryStream? stream = data.GenericExcelExport(
    createTable: true,
    sheetName: "Report",
    tableName: "ReportTable",
    skipColumnNames: ["InternalId"]
);

// Return as a file download from an ASP.NET Core endpoint
return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report.xlsx");
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Excel.OpenXml
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.