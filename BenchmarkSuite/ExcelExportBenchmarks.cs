using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;
using CommonNetFuncs.Excel.Common;
using CommonNetFuncs.Excel.OpenXml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using static CommonNetFuncs.Core.ReflectionCaches;
using static CommonNetFuncs.Excel.OpenXml.Common;

namespace BenchmarkSuite;

/// <summary>
/// Benchmarks comparing the original (pre-optimization) ExportFromTable implementation against
/// the new optimized one for both the generic IEnumerable&lt;T&gt; and DataTable overloads.
///
/// Root causes of the original being slow at 2000 rows × 36 columns (~72 000 cells):
///   1. InsertSharedStringItem   – O(n) linear scan of the shared-string table per cell +
///                                 SharedStringTable.Save() called 72 000 times.
///   2. GetWorkbookFromCell()    – XML-tree ancestor traversal on every cell insertion.
///   3. InsertCell()             – Elements&lt;Row&gt;().FirstOrDefault() linear scan per cell.
///   4. AutoFitColumns()         – second full pass that repeats tree traversal + ElementAt()
///                                 shared-string lookup per cell.
/// </summary>
[RankColumn]
[MemoryDiagnoser]
public class ExcelExportBenchmarks
{
	private List<ExportTestRow> GenericData = [];
	private DataTable Dt = new();

	[Params(500, 2000)]
	public int RowCount { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		System.Random rng = new(42);
		string[] statuses = ["Active", "Inactive", "Pending", "Closed"];
		string[] categories = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon"];

		GenericData = Enumerable.Range(1, RowCount).Select(i => new ExportTestRow
		{
			Id = i,
			FirstName = $"First{i}",
			LastName = $"Last{i}",
			Email = $"user{i}@example.com",
			Phone = $"555-{i:D4}",
			Status = statuses[i % statuses.Length],
			Category = categories[i % categories.Length],
			SubCategory = $"Sub{i % 10}",
			Amount = (decimal)(rng.NextDouble() * 10000),
			Price = (decimal)(rng.NextDouble() * 500),
			Cost = (decimal)(rng.NextDouble() * 200),
			Total = (decimal)(rng.NextDouble() * 12000),
			Quantity = rng.Next(1, 100),
			Count = rng.Next(0, 1000),
			IsActive = i % 2 == 0,
			IsEnabled = i % 3 != 0,
			CreatedDate = DateTime.Now.AddDays(-i),
			UpdatedDate = DateTime.Now.AddDays(-i / 2),
			ProcessedDate = DateTime.Now.AddDays(-i / 4),
			Field01 = $"F01_{i}",
			Field02 = $"F02_{i}",
			Field03 = $"F03_{i}",
			Field04 = $"F04_{i}",
			Field05 = $"F05_{i}",
			Field06 = $"F06_{i}",
			Field07 = $"F07_{i}",
			Field08 = $"F08_{i}",
			Field09 = $"F09_{i}",
			Field10 = $"F10_{i}",
			Field11 = $"F11_{i}",
			Field12 = $"F12_{i}",
			Field13 = $"F13_{i}",
			Field14 = $"F14_{i}",
			Field15 = $"F15_{i}",
			Field16 = $"F16_{i}",
			Field17 = $"F17_{i}"
		}).ToList();

		// Mirror the generic data in a DataTable for the DataTable overload benchmarks
		Dt = new DataTable();
		PropertyInfo[] props = GetOrAddPropertiesFromReflectionCache(typeof(ExportTestRow));
		foreach (PropertyInfo p in props)
		{
			Dt.Columns.Add(p.Name, typeof(string));
		}
		foreach (ExportTestRow row in GenericData)
		{
			DataRow dr = Dt.NewRow();
			foreach (PropertyInfo p in props)
			{
				dr[p.Name] = p.GetValue(row)?.ToString() ?? string.Empty;
			}
			Dt.Rows.Add(dr);
		}
	}

	// -----------------------------------------------------------------------
	// Generic IEnumerable<T> benchmarks
	// -----------------------------------------------------------------------

	[Benchmark(Baseline = true, Description = "Original IEnumerable<T>")]
	public void Original_Generic()
	{
		using MemoryStream ms = new();
		using SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true);
		document.CompressionOption = CompressionOption.Normal;
		uint newSheetId = document.InitializeExcelFile("Data");
		Worksheet? worksheet = document.GetWorksheetById(newSheetId);
		if (worksheet != null)
		{
			OriginalExportFromTable(document, worksheet, GenericData);
		}
		document.Save();
	}

	[Benchmark(Description = "Optimized IEnumerable<T>")]
	public void Optimized_Generic()
	{
		using MemoryStream ms = new();
		using SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true);
		document.CompressionOption = CompressionOption.Normal;
		uint newSheetId = document.InitializeExcelFile("Data");
		Worksheet? worksheet = document.GetWorksheetById(newSheetId);
		if (worksheet != null)
		{
			Export.ExportFromTable(document, worksheet, GenericData);
		}
		document.Save();
	}

	// -----------------------------------------------------------------------
	// DataTable benchmarks
	// -----------------------------------------------------------------------

	[Benchmark(Description = "Original DataTable")]
	public void Original_DataTable()
	{
		using MemoryStream ms = new();
		using SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true);
		document.CompressionOption = CompressionOption.Normal;
		uint newSheetId = document.InitializeExcelFile("Data");
		Worksheet? worksheet = document.GetWorksheetById(newSheetId);
		if (worksheet != null)
		{
			OriginalExportFromTable(document, worksheet, Dt);
		}
		document.Save();
	}

	[Benchmark(Description = "Optimized DataTable")]
	public void Optimized_DataTable()
	{
		using MemoryStream ms = new();
		using SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true);
		document.CompressionOption = CompressionOption.Normal;
		uint newSheetId = document.InitializeExcelFile("Data");
		Worksheet? worksheet = document.GetWorksheetById(newSheetId);
		if (worksheet != null)
		{
			Export.ExportFromTable(document, worksheet, Dt);
		}
		document.Save();
	}

	// -----------------------------------------------------------------------
	// Verbatim copy of the ORIGINAL ExportFromTable<T> logic (pre-optimization)
	// so that the baseline is not affected by any changes made to Export.cs.
	// -----------------------------------------------------------------------

	private static bool OriginalExportFromTable<T>(SpreadsheetDocument document, Worksheet worksheet, IEnumerable<T> data,
			bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
	{
		if (data?.Any() == true)
		{
			SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
			if (sheetData == null)
			{
				return false;
			}

			uint headerStyleId = document.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
			uint bodyStyleId = document.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

			uint x = 1;
			uint y = 1;

			PropertyInfo[] properties = GetOrAddPropertiesFromReflectionCache(typeof(T))
					.Where(p => (skipColumnNames == null) || (skipColumnNames.Count == 0) || !skipColumnNames.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase))
					.ToArray();

			foreach (PropertyInfo prop in properties)
			{
				sheetData.InsertCellValue(x, y, new CellValue(prop.Name), CellValues.SharedString, headerStyleId);
				x++;
			}
			x = 1;
			y++;

			foreach (T item in data.Where(i => !i.ToNString().IsNullOrEmpty()))
			{
				foreach (PropertyInfo prop in properties)
				{
					sheetData.InsertCellValue(x, y, new CellValue(prop.GetValue(item)?.ToString() ?? string.Empty), CellValues.SharedString, bodyStyleId);
					x++;
				}
				x = 1;
				y++;
			}

			if (createTable)
			{
				worksheet.CreateTable(1, 1, y - 1, (uint)properties.Length, tableName);
			}
			else
			{
				worksheet.SetAutoFilter(1, 1, y - 1, (uint)properties.Length);
			}

			worksheet.AutoFitColumns();
		}
		ClearStandardFormatCacheForWorkbook(document);
		return true;
	}

	private static bool OriginalExportFromTable(SpreadsheetDocument document, Worksheet worksheet, DataTable data,
			bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
	{
		if (data?.Rows.Count > 0)
		{
			SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
			if (sheetData == null)
			{
				return false;
			}

			uint headerStyleId = document.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
			uint bodyStyleId = document.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

			uint y = 1;
			uint x = 1;

			List<uint> skipColumns = [];
			foreach (DataColumn column in data.Columns)
			{
				if (skipColumnNames?.Contains(column.ColumnName, StringComparer.InvariantCultureIgnoreCase) != true)
				{
					sheetData.InsertCellValue(x, y, new CellValue(column.ColumnName), CellValues.SharedString, headerStyleId);
				}
				else
				{
					skipColumns.Add(x);
				}
				x++;
			}

			x = 1;
			y++;

			foreach (DataRow row in data.Rows)
			{
				foreach (object? value in row.ItemArray)
				{
					if ((value != null) && !skipColumns.Contains(x))
					{
						sheetData.InsertCellValue(x, y, new CellValue(value.ToString() ?? string.Empty), CellValues.SharedString, bodyStyleId);
					}
					x++;
				}
				x = 1;
				y++;
			}

			if (createTable)
			{
				worksheet.CreateTable(1, 1, y - 1, (uint)data.Columns.Count, tableName);
			}
			else
			{
				worksheet.SetAutoFilter(1, 1, y - 1, (uint)data.Columns.Count);
			}

			worksheet.AutoFitColumns();
		}
		return true;
	}
}
