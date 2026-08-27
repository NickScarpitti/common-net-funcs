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
			- [WriteAndClose / WriteAndCloseAsync](#writeandclose--writeandcloseasync)
			- [Performance-Optimized Shared String APIs](#performance-optimized-shared-string-apis)
			- [Reading Cell Values](#reading-cell-values)
			- [Reading to DataTable](#reading-to-datatable)
			- [Writing Cell Values](#writing-cell-values)
			- [Cell Styles](#cell-styles)
			- [Column Sizing](#column-sizing)
			- [Table Helpers](#table-helpers)
			- [Worksheet Utilities](#worksheet-utilities)
	- [Export](#export)
		- [Export Usage Examples](#export-usage-examples)
			- [GenericExcelExport](#genericexcelexport)
			- [AddGenericTable](#addgenerictable)
			- [ExportFromTable](#exportfromtable)
	- [Installation](#installation)
	- [License](#license)

---

## Common

Low-level helpers for building and manipulating `SpreadsheetDocument` objects with the OpenXML SDK. Covers creating workbooks and sheets, reading and writing cell values, managing shared strings, applying cell styles, reading data into `DataTable` objects, inserting images, and finalizing documents.

### Common Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### InitializeExcelFile / CreateNewSheet

`InitializeExcelFile()` (no sheet name) only sets up the `WorkbookPart`/`Workbook`/`Sheets` scaffolding — with no sheets — and returns the `WorkbookPart` that was created or reused. `InitializeExcelFile(sheetName)` goes a step further and also creates the first sheet, returning its sheet ID.

```cs
using DocumentFormat.OpenXml.Packaging;
using CommonNetFuncs.Excel.OpenXml;

using MemoryStream ms = new();
using SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true);

uint sheetId = document.InitializeExcelFile("Sheet1"); // creates the workbook and first sheet
uint sheet2Id = document.CreateNewSheet("Sheet2");     // appends a second sheet

Worksheet? ws = document.GetWorksheetById(sheetId);

// Or set up the workbook scaffolding without creating any sheet yet
using MemoryStream ms2 = new();
using SpreadsheetDocument emptyDocument = SpreadsheetDocument.Create(ms2, SpreadsheetDocumentType.Workbook, true);
WorkbookPart workbookPart = emptyDocument.InitializeExcelFile(); // Workbook + empty Sheets collection, no sheets yet
uint firstSheetId = emptyDocument.CreateNewSheet("Sheet1");      // add sheets whenever you're ready
```

#### WriteAndClose / WriteAndCloseAsync

`WriteAndClose` saves the workbook, disposes the document, and resets the stream position to 0 so the stream is ready to be read or returned immediately.

Use the single-`Stream` overload when the document was created in-memory:

```cs
using DocumentFormat.OpenXml.Packaging;
using CommonNetFuncs.Excel.OpenXml;

using MemoryStream ms = new();
SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook);
document.InitializeExcelFile("Sheet1");
// ... write data ...

document.WriteAndClose(ms); // saves, disposes, resets ms.Position to 0
// ms is now ready to read / return as a file download
```

Use the `filePath` overload (or its async counterpart) when the document was created against a file path and you want to return the result as a `MemoryStream`:

```cs
using DocumentFormat.OpenXml.Packaging;
using CommonNetFuncs.Excel.OpenXml;

string path = Path.GetTempFileName();
MemoryStream ms = new();
SpreadsheetDocument document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
document.InitializeExcelFile("Sheet1");
// ... write data ...

// sync
document.WriteAndClose(ms, path);

// async
await document.WriteAndCloseAsync(ms, path);

// ms.Position == 0 and contains the full .xlsx content
return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "output.xlsx");
```

Both overloads accept an optional `clearCachedStyles` parameter. Set it to `true` when the document is a template that will be reused — this prevents stale style indices from leaking into subsequent documents created from the same template.

#### Performance-Optimized Shared String APIs

When reading many cells from a workbook the default `GetCellValue` / `GetStringValue` methods perform an O(n) scan of the shared-string table for every shared-string cell. Build a single O(1) index up front and pass it through to avoid this:

```cs
using DocumentFormat.OpenXml.Packaging;
using CommonNetFuncs.Excel.OpenXml;

using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
WorkbookPart wbp = document.WorkbookPart!;

// Build the index once — O(n) scan performed exactly once regardless of how many cells are read
SharedStringTablePart? shareStringTablePart = wbp.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
IReadOnlyDictionary<int, string>? index = shareStringTablePart?.BuildSharedStringIndex();

Worksheet ws = document.GetWorksheetByName("Sheet1")!;
SheetData sheetData = ws.GetFirstChild<SheetData>()!;

// O(1) per cell — no tree traversal, no linear scan
string val = sheetData.GetCellValue(row: 2, col: 1, index);
string? str = someCell.GetStringValue(index);
double width = someCell.CalculateWidth(index);
```

When **writing** many shared strings use the cache-based overload of `InsertSharedStringItem` to avoid the O(n) duplicate scan and the expensive per-insert `Save()` call of the standard overload. Call `SharedStringTable.Save()` once when all insertions are done:

```cs
// Writer side — O(1) duplicate detection, single Save() at the end
Dictionary<string, int> cache = new(StringComparer.Ordinal);

foreach (string text in values)
{
    int idx = workbook.InsertSharedStringItem(text, cache);
    cell.CellValue = new CellValue(idx.ToString());
    cell.DataType = CellValues.SharedString;
}

// Save once after all insertions — NOT once per insert
sharedStringTablePart.SharedStringTable!.Save();
```

#### Reading Cell Values

```cs
// Standard read (O(n) shared-string lookup)
string value = cell.GetCellValue();
string? formatted = cell.GetStringValue(); // handles Boolean/Error/SharedString formatting

// High-performance read using a pre-built index
string value = cell.GetCellValue(sharedStringIndex);
string? formatted = cell.GetStringValue(sharedStringIndex);

// Read from SheetData by coordinate
string v1 = sheetData.GetCellValue(row: 3, col: 2);
string v2 = sheetData.GetCellValue(row: 3, col: 2, sharedStringIndex);
string v3 = sheetData.GetCellValue(new CellReference("B3"));
string v4 = worksheet.GetCellValue(new CellReference("B3"));
string? str = worksheet.GetStringValue(new CellReference("B3"));
```

#### Reading to DataTable

`ReadExcelFileToDataTable` and `ReadExcelTableToDataTable` both build a shared-string index internally so all per-cell lookups are O(1):

```cs
using CommonNetFuncs.Excel.OpenXml;

// Read a flat sheet (optional header row, optional range limits)
DataTable dt = fileStream.ReadExcelFileToDataTable(hasHeaders: true);
DataTable dt2 = fileStream.ReadExcelFileToDataTable(hasHeaders: true, sheetName: "Sales", startCellReference: "B2", endCellReference: "E50");

// Read a named Excel table
DataTable table = fileStream.ReadExcelTableToDataTable("MyTable");
DataTable first = fileStream.ReadExcelTableToDataTable(); // reads first table found
```

#### Writing Cell Values

A family of `SetCellStringValue`, `SetCellDateValue`, and `SetCellNumericValue` extension methods write typed values to cells. Every method is available on `Cell?`, `SheetData` (by row/col or `CellReference`), and `Worksheet` (by `CellReference`). All null-cell overloads are no-ops.

```cs
// String — Cell overload
cell.SetCellStringValue("Hello");
cell.SetCellStringValue(true); // writes "True" / "False"
cell.SetCellStringValue(42);
cell.SetCellStringValue(3.14);
cell.SetCellStringValue(9.99m);
cell.SetCellStringValue(new DateOnly(2024, 6, 15)); // default format "MM/dd/yyyy"
cell.SetCellStringValue(new DateOnly(2024, 6, 15), "yyyy-MM-dd");
cell.SetCellStringValue(DateTime.Now); // default format "g"
cell.SetCellStringValue(DateTime.Now, "yyyy-MM-dd");

// String — SheetData overload (by row/col index or CellReference)
sheetData.SetCellStringValue(row: 1, col: 2, "Hello");
sheetData.SetCellStringValue(new CellReference("B1"), "Hello");

// String — Worksheet overload (by CellReference)
worksheet.SetCellStringValue(new CellReference("B1"), "Hello");

// Date value (stores as CellValues.Date)
cell.SetCellDateValue(new DateOnly(2024, 6, 15));
cell.SetCellDateValue(DateTime.Now);
sheetData.SetCellDateValue(row: 1, col: 1, new DateOnly(2024, 6, 15));
worksheet.SetCellDateValue(new CellReference("A1"), DateTime.Now);

// Numeric value (stores as CellValues.Number)
cell.SetCellNumericValue(42);
cell.SetCellNumericValue(3.14);
cell.SetCellNumericValue(9.99m);
sheetData.SetCellNumericValue(row: 1, col: 1, 42);
worksheet.SetCellNumericValue(new CellReference("A1"), 3.14m);
```

#### Cell Styles

`GetStandardCellStyle` returns (or creates) one of the built-in preset styles. Both the resolved format ID and the underlying Border/Fill/Font element indices are cached per document inside a `ConditionalWeakTable`, so they are freed automatically when the document is garbage collected — no explicit cleanup is required after a normal export.

```cs
// Preset styles: Header, HeaderThickTop, Body, Error, Blackout, Whiteout
uint headerId = document.GetStandardCellStyle(EStyle.Header);
uint bodyId   = document.GetStandardCellStyle(EStyle.Body, cellLocked: false, wrapText: true);

// Per-document: clear the format-ID cache only (element-index cache is preserved so that
// multi-sheet exports still deduplicate CellFormats without duplicating stylesheet elements).
document.ClearStandardFormatCache();

// Per-document: clear the element-index cache (Border/Fill/Font indices). Rarely needed —
// only when you explicitly want to force fresh elements on the next GetStandardCellStyle call.
document.ClearStyleElementCache();

// Global: replace the StandardCacheTable so all live documents start fresh on next access.
ClearStandardFormatCache();
```

`GetCustomStyle` creates a fully custom `CellFormat` and caches it per document via a `ConditionalWeakTable`. The cache is freed automatically on GC. Use `document.ClearCustomFormatCache()` to remove it explicitly (e.g. after writing a template-based document). `document.GetCustomFormatCache()` returns the live `WorkbookStyleCache` for inspection.

`WorkbookStyleCache` is shared with `GetOrAddFont`, `GetOrAddFill`, and `GetOrAddBorder` to prevent duplicate style elements:

```cs
// Custom style with font, fill, border, alignment, and protection
uint styleId = document.GetCustomStyle(
    cellLocked: false,
    font:   new Font { Bold = new Bold(), FontSize = new FontSize { Val = 11 } },
    fill:   new Fill { PatternFill = new PatternFill { PatternType = PatternValues.Solid } },
    border: new Border { LeftBorder = new LeftBorder { Style = BorderStyleValues.Thin } },
    alignment: HorizontalAlignmentValues.Center,
    wrapText: true
);

// Inspect or clear the per-document custom format cache
WorkbookStyleCache? cache = document.GetCustomFormatCache(); // null if no custom styles yet
document.ClearCustomFormatCache();                           // removes the entry explicitly

// Global: replace the CustomCacheTable for all documents
ClearCustomFormatCache();

// Or manage style elements individually using WorkbookStyleCache
Stylesheet stylesheet = document.GetStylesheet()!;
WorkbookStyleCache manualCache = new();
uint fontId   = stylesheet.GetOrAddFont(manualCache, new Font { Bold = new Bold() });
uint fillId   = stylesheet.GetOrAddFill(manualCache, myFill);
uint borderId = stylesheet.GetOrAddBorder(manualCache, myBorder);
```

#### Column Sizing

```cs
// Auto-fit all columns to their content (max 100 chars)
worksheet.AutoFitColumns();
worksheet.AutoFitColumns(maxWidth: 50);

// Size or create a specific column
worksheet.SizeColumn(colIndex: 3, columnWidth: 20.5);
Column? col = worksheet.GetOrCreateColumn(colIndex: 3, columnWidth: 20.5);

// Calculate the display width of a single cell's content
double width = cell.CalculateWidth();                  // standard O(n) shared-string lookup
double width = cell.CalculateWidth(sharedStringIndex); // O(1) with pre-built index
double width = CalculateWidth("Hello World", styleIndex: 1);
```

#### Table Helpers

```cs
// Get the top-left cell reference of a table's range
CellReference start = table.GetTableStart(); // e.g. new CellReference("B3")

// Get the 1-based worksheet column index for a named table column
int colIndex = table.GetColumnIndex("CustomerName");
int colIndex = table.GetColumnIndex("CustomerName", explicitTableStart);
```

#### Worksheet Utilities

```cs
// Force Excel to recalculate all formulas on next open
document.ForceFormulaRecalculation();
workbookPart.ForceFormulaRecalculation();
workbook.ForceFormulaRecalculation();

// Add a dropdown data-validation list to a cell or range
AddDropDownValidation(worksheet, cellReference: "B2", formula: "\"Option1,Option2,Option3\"");
AddDropDownValidation(worksheet, cellReference: "C2", formula: "Sheet2!$A$1:$A$5");
```

</details>

---

## Export

Provides methods to convert an `IEnumerable<T>` or `DataTable` into a `.xlsx` `MemoryStream`, or to append tabular data as a new sheet inside an existing `SpreadsheetDocument`. The export engine writes directly to the OpenXML tree (no `InsertCell`/`InsertCellValue` per cell) and uses a single shared-string `Dictionary` cache plus one `SharedStringTable.Save()` at the end, making it suitable for large datasets.

### Export Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GenericExcelExport

Converts an `IEnumerable<T>` or `DataTable` to a standalone `.xlsx` `MemoryStream`.

```cs
using CommonNetFuncs.Excel.OpenXml;

List<MyRecord> data = GetData();

// IEnumerable<T> — basic export
MemoryStream? stream = data.GenericExcelExport();

// IEnumerable<T> — formatted Excel table, skipping a column, with text wrapping
MemoryStream? stream = data.GenericExcelExport(
    createTable: true,
    sheetName: "Report",
    tableName: "ReportTable",
    skipColumnNames: ["InternalId"],
    wrapText: true
);

// DataTable overload
DataTable dt = BuildDataTable();
MemoryStream? stream = dt.GenericExcelExport(createTable: true, sheetName: "Sales");

// Return as a file download from an ASP.NET Core endpoint
return File(stream!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report.xlsx");
```

#### AddGenericTable

Appends a new sheet containing tabular data to an **existing** `SpreadsheetDocument`. If a sheet with the given name already exists, a suffix `(1)`, `(2)`, … is added automatically.

```cs
using DocumentFormat.OpenXml.Packaging;
using CommonNetFuncs.Excel.OpenXml;

using MemoryStream ms = new();
using SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true);
document.InitializeExcelFile("Summary");

// IEnumerable<T> overload
bool ok = document.AddGenericTable(salesData, sheetName: "Sales", createTable: true, tableName: "SalesTable");

// DataTable overload
bool ok = document.AddGenericTable(myDataTable, sheetName: "Detail");

document.Save();
```

#### ExportFromTable

Lower-level method that writes data directly into a `Worksheet` that already exists in the provided `SpreadsheetDocument`. This is what `GenericExcelExport` and `AddGenericTable` call internally and can be used when you need full control over document creation.

```cs
using DocumentFormat.OpenXml.Packaging;
using CommonNetFuncs.Excel.OpenXml;

using MemoryStream ms = new();
using SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true);
uint sheetId = document.InitializeExcelFile("Data");
Worksheet worksheet = document.GetWorksheetById(sheetId)!;

// IEnumerable<T> overload — supports CancellationToken
bool ok = ExportFromTable(document, worksheet, myList, createTable: true, tableName: "MyTable");

// DataTable overload
bool ok = ExportFromTable(document, worksheet, myDataTable);

document.Save();
ms.Position = 0;
```

Both `ExportFromTable` overloads:

- Apply `EStyle.Header` and `EStyle.Body` standard cell styles.
- Track column widths inline during the write pass — no second `AutoFitColumns` pass is required.
- Use a single `Dictionary<string, int>` shared-string cache for O(1) duplicate detection.
- Call `SharedStringTable.Save()` exactly once after all rows are written.
- Append an `AutoFilter` or create a named Excel table depending on the `createTable` flag.

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Excel.OpenXml
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
