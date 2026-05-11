# CommonNetFuncs.Excel.Npoi

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Excel.Npoi)](https://www.nuget.org/packages/CommonNetFuncs.Excel.Npoi/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Excel.Npoi)](https://www.nuget.org/packages/CommonNetFuncs.Excel.Npoi/)

This project contains helper methods for reading and writing Excel files using the NPOI library in .NET applications.

## Contents

- [CommonNetFuncs.Excel.Npoi](#commonnetfuncsexcelnpoi)
	- [Contents](#contents)
	- [Export](#export)
		- [Export Usage Examples](#export-usage-examples)
			- [GenericExcelExport](#genericexcelexport)
	- [Installation](#installation)
	- [License](#license)

---

## Export

Provides a `GenericExcelExport` extension method that converts any `IEnumerable<T>` into a `.xlsx` `MemoryStream` using NPOI (Apache POI for .NET). Supports optional table formatting, custom sheet and table names, column exclusion, and text wrapping. Uses the SXSSF streaming workbook for memory-efficient exports of large datasets.

### Export Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GenericExcelExport

```cs
using CommonNetFuncs.Excel.Npoi;

List<MyRecord> data = GetData();

// Basic export
MemoryStream? stream = await data.GenericExcelExport();

// Export as a formatted Excel table with a custom sheet name and skipped column
MemoryStream? stream = await data.GenericExcelExport(
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
dotnet add package CommonNetFuncs.Excel.Npoi
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.