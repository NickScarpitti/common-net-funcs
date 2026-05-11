# CommonNetFuncs.Excel.ClosedXml

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Excel.ClosedXml)](https://www.nuget.org/packages/CommonNetFuncs.Excel.ClosedXml/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Excel.ClosedXml)](https://www.nuget.org/packages/CommonNetFuncs.Excel.ClosedXml/)

This project contains helper methods for reading and writing Excel files using the ClosedXML library in .NET applications.

## Contents

- [CommonNetFuncs.Excel.ClosedXml](#commonnetfuncsexcelclosedxml)
  - [Contents](#contents)
  - [Export](#export)
    - [Export Usage Examples](#export-usage-examples)
      - [GenericExcelExport](#genericexcelexport)
  - [Installation](#installation)
  - [License](#license)

---

## Export

Provides a `GenericExcelExport` extension method that converts any `IEnumerable<T>` into a `.xlsx` `MemoryStream` using ClosedXML. Supports optional Excel table formatting (with configurable table style), custom sheet and table names, column exclusion, and text wrapping.

### Export Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GenericExcelExport

```cs
using CommonNetFuncs.Excel.ClosedXml;

List<MyRecord> data = GetData();

// Basic export
MemoryStream? stream = await data.GenericExcelExport();

// Export as a formatted Excel table, skipping a column, with a custom sheet name
MemoryStream? stream = await data.GenericExcelExport(
    createTable: true,
    sheetName: "Report",
    tableName: "ReportTable",
    skipColumnNames: ["InternalId"],
    tableStyle: ETableStyle.TableStyleMedium9
);

// Return as a file download from an ASP.NET Core endpoint
return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report.xlsx");
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Excel.ClosedXml
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
