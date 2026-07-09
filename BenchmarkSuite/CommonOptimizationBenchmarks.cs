using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Excel.OpenXml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using static CommonNetFuncs.Excel.OpenXml.Common;

namespace BenchmarkSuite;

// ============================================================
// Benchmark 1: CalculateWidth — static HashSets vs per-call
// ============================================================
/// <summary>
/// Issue: CalculateWidth allocates two new HashSet&lt;uint&gt; instances on every single call.
/// With 18,000 cells (500 rows × 36 cols) this creates 36,000 throwaway HashSets per export.
/// Fix: Promote them to static readonly fields.
/// </summary>
[RankColumn]
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5)]
public class CalculateWidthBenchmarks
{
	// Pre-built static sets — the OPTIMIZED approach
	private static readonly HashSet<uint> NumberStyles = [5, 6, 7, 8];
	private static readonly HashSet<uint> BoldStyles = [1, 2, 3, 4, 6, 7, 8];

	private readonly string[] Texts = ["Hello World", "123456789.00", "SomeVeryLongColumnHeaderName", "Active", "2026-07-06 12:00"];
	private readonly uint[] Styles = [0, 1, 2, 3, 4, 5, 6, 7, 8];

	// Verbatim copy of the CURRENT CalculateWidth body (HashSets created each call)
	private static double OriginalCalculateWidth(string text, uint? styleIndex)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}

		const int padding = 1;
		HashSet<uint> numberStyles = [5, 6, 7, 8];
		HashSet<uint> boldStyles = [1, 2, 3, 4, 6, 7, 8];
		double width = text.Length + padding;
		if (double.TryParse(text, out _))
		{
			width++;
		}

		if ((styleIndex != null) && numberStyles.Contains((uint)styleIndex))
		{
			int thousandCount = (int)Math.Truncate(width / 4);
			width += 3 + thousandCount;
		}

		if ((styleIndex != null) && boldStyles.Contains((uint)styleIndex))
		{
			width++;
		}
		const double maxCharWidth = 5;
		return Math.Truncate(((width * maxCharWidth) + 5) / maxCharWidth * 256) / 256;
	}

	// Same logic but using static readonly HashSets
	private static double OptimizedCalculateWidth(string text, uint? styleIndex)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}

		const int padding = 1;
		double width = text.Length + padding;
		if (double.TryParse(text, out _))
		{
			width++;
		}

		if ((styleIndex != null) && NumberStyles.Contains((uint)styleIndex))
		{
			int thousandCount = (int)Math.Truncate(width / 4);
			width += 3 + thousandCount;
		}

		if ((styleIndex != null) && BoldStyles.Contains((uint)styleIndex))
		{
			width++;
		}
		const double maxCharWidth = 5;
		return Math.Truncate(((width * maxCharWidth) + 5) / maxCharWidth * 256) / 256;
	}

	[Benchmark(Baseline = true, Description = "Original (new HashSet each call)")]
	public double Original()
	{
		double sum = 0;
		for (int t = 0; t < Texts.Length; t++)
		{
			for (int s = 0; s < Styles.Length; s++)
			{
				sum += OriginalCalculateWidth(Texts[t], Styles[s]);
			}
		}
		return sum;
	}

	[Benchmark(Description = "Optimized (static readonly HashSets)")]
	public double Optimized()
	{
		double sum = 0;
		for (int t = 0; t < Texts.Length; t++)
		{
			for (int s = 0; s < Styles.Length; s++)
			{
				sum += OptimizedCalculateWidth(Texts[t], Styles[s]);
			}
		}
		return sum;
	}
}

// ============================================================
// Benchmark 2: InsertSharedStringItem — Save() per insert
// ============================================================
/// <summary>
/// Issue: InsertSharedStringItem calls SharedStringTable.Save() after every insertion,
/// serializing the entire XML part to the ZIP entry each time.  For 200 inserts the table
/// grows by ~4 KB per save, causing hundreds of redundant ZIP writes.
/// Fix A: Remove per-insertion Save() — the document-level Save() handles persistence.
/// Fix B: Additionally use a Dictionary for O(1) duplicate detection instead of O(n) scan.
/// </summary>
[RankColumn]
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class InsertSharedStringBenchmarks
{
	private const int InsertCount = 200;
	private string[] TestStrings = [];

	[GlobalSetup]
	public void Setup()
	{
		TestStrings = Enumerable.Range(1, InsertCount).Select(i => $"SharedString_{i:D5}").ToArray();
	}

	// Verbatim copy of the CURRENT InsertSharedStringItem (O(n) scan + Save per insert)
	private static int OriginalInsertSharedStringItem(Workbook workbook, string text)
	{
		SharedStringTablePart shareStringTablePart = workbook.WorkbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault()
				?? workbook.WorkbookPart?.AddNewPart<SharedStringTablePart>()
				?? throw new InvalidOperationException("WorkbookPart is missing.");
		shareStringTablePart.SharedStringTable ??= new SharedStringTable();
		int i = 0;
		foreach (SharedStringItem item in shareStringTablePart.SharedStringTable.Elements<SharedStringItem>())
		{
			if (string.Equals(item.InnerText, text))
			{
				return i;
			}
			i++;
		}
		shareStringTablePart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
		shareStringTablePart.SharedStringTable.Save(); // <-- THE BOTTLENECK
		return i;
	}

	// Fix A: O(n) scan but no per-insert Save
	private static int OptimizedInsertNoSave(SharedStringTablePart ssp, string text)
	{
		ssp.SharedStringTable ??= new SharedStringTable();
		int i = 0;
		foreach (SharedStringItem item in ssp.SharedStringTable.Elements<SharedStringItem>())
		{
			if (string.Equals(item.InnerText, text))
			{
				return i;
			}
			i++;
		}
		ssp.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
		return i;
	}

	private static (SpreadsheetDocument doc, WorkbookPart wbp) CreateDoc(MemoryStream ms)
	{
		SpreadsheetDocument doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true);
		WorkbookPart wbp = doc.AddWorkbookPart();
		wbp.Workbook = new Workbook();
		return (doc, wbp);
	}

	[Benchmark(Baseline = true, Description = "Original (O(n) scan + Save per insert)")]
	public void Original_WithSavePerInsert()
	{
		using MemoryStream ms = new();
		(SpreadsheetDocument doc, WorkbookPart workbookPart) = CreateDoc(ms);
		using (doc)
		{
			foreach (string str in TestStrings)
			{
				OriginalInsertSharedStringItem(workbookPart.Workbook!, str);
			}
			doc.Save();
		}
	}

	[Benchmark(Description = "Fix A: O(n) scan, single Save at end")]
	public void OptimizedA_NoSavePerInsert()
	{
		using MemoryStream ms = new();
		(SpreadsheetDocument doc, WorkbookPart wbp) = CreateDoc(ms);
		using (doc)
		{
			SharedStringTablePart ssp = wbp.AddNewPart<SharedStringTablePart>();
			foreach (string str in TestStrings)
			{
				OptimizedInsertNoSave(ssp, str);
			}
			ssp.SharedStringTable?.Save();
			doc.Save();
		}
	}

	[Benchmark(Description = "Fix B: O(1) dict lookup, single Save at end")]
	public void OptimizedB_DictAndNoSavePerInsert()
	{
		using MemoryStream ms = new();
		(SpreadsheetDocument doc, WorkbookPart wbp) = CreateDoc(ms);
		using (doc)
		{
			SharedStringTablePart sharedStringPart = wbp.AddNewPart<SharedStringTablePart>();
			sharedStringPart.SharedStringTable = new SharedStringTable();
			Dictionary<string, int> cache = new(StringComparer.Ordinal);
			int count = 0;
			foreach (string str in TestStrings)
			{
				if (!cache.ContainsKey(str))
				{
					sharedStringPart.SharedStringTable.AppendChild(new SharedStringItem(new Text(str)));
					cache[str] = count++;
				}
			}
			sharedStringPart.SharedStringTable.Save();
			doc.Save();
		}
	}
}

// ============================================================
// Benchmark 3: NumberToColumnName — string concatenation vs Span
// ============================================================
/// <summary>
/// Issue: NumberToColumnName uses "$"{char}{columnName}"" to prepend a character, which
/// allocates a new string on every loop iteration.  For a 3-char column name that is 3
/// allocations; over all 16 384 Excel columns this is thousands of short-lived strings.
/// Fix:  Fill a stackalloc Span&lt;char&gt; in reverse and return a single new string.
/// </summary>
[RankColumn]
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5)]
public class NumberToColumnNameBenchmarks
{
	// Verbatim copy of current implementation
	private static string OriginalNumberToColumnName(uint columnNumber)
	{
		int number = ((int)columnNumber) - 1;
		string columnName = string.Empty;
		while (number >= 0)
		{
			int remainder = number % 26;
			columnName = $"{Convert.ToChar('A' + remainder)}{columnName}";
			number = (number / 26) - 1;
			if (number < 0)
			{
				break;
			}
		}
		return columnName;
	}

	// Span-based replacement — fills chars in reverse, single string allocation
	private static string OptimizedNumberToColumnName(uint columnNumber)
	{
		// Excel max column is XFD (16 384) — at most 3 characters
		Span<char> chars = stackalloc char[3];
		int pos = 2;
		int number = (int)columnNumber - 1;
		while (number >= 0)
		{
			chars[pos--] = (char)('A' + number % 26);
			number = number / 26 - 1;
		}
		return new string(chars[(pos + 1)..]);
	}

	// Benchmark: iterate ALL valid Excel columns (1 … 16 384) to stress-test the full range
	[Benchmark(Baseline = true, Description = "Original (string concat loop)")]
	public static string Original_AllColumns()
	{
		string last = string.Empty;
		for (uint i = 1; i <= 16384; i++)
		{
			last = OriginalNumberToColumnName(i);
		}
		return last;
	}

	[Benchmark(Description = "Optimized (stackalloc Span)")]
	public static string Optimized_AllColumns()
	{
		string last = string.Empty;
		for (uint i = 1; i <= 16384; i++)
		{
			last = OptimizedNumberToColumnName(i);
		}
		return last;
	}
}

// ============================================================
// Benchmark 4: AutoFitColumns — ConcurrentDictionary vs Dictionary
// ============================================================
/// <summary>
/// Issue: AutoFitColumns uses ConcurrentDictionary in a single-threaded context, paying
/// for thread-safety guarantees (volatile reads, Interlocked ops) that are never needed.
/// Fix:  Plain Dictionary&lt;uint, double&gt;.
/// </summary>
[RankColumn]
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class AutoFitColumnsDictionaryBenchmarks
{
	private const int Iterations = 50_000; // simulate a large number of "track width" calls

	[Benchmark(Baseline = true, Description = "Original (ConcurrentDictionary)")]
	public static double Original_ConcurrentDict()
	{
		ConcurrentDictionary<uint, double> columnWidths = [];
		double result = 0;
		for (uint col = 0; col < Iterations; col++)
		{
			double width = col * 0.1 + 1.5;
			if (!columnWidths.TryGetValue(col % 36, out double value) || width > value)
			{
				value = Math.Min(width, 100);
				columnWidths[col % 36] = value;
			}
			result += value;
		}
		return result;
	}

	[Benchmark(Description = "Optimized (Dictionary)")]
	public static double Optimized_PlainDict()
	{
		Dictionary<uint, double> columnWidths = [];
		double result = 0;
		for (uint col = 0; col < Iterations; col++)
		{
			double width = col * 0.1 + 1.5;
			if (!columnWidths.TryGetValue(col % 36, out double value) || width > value)
			{
				value = Math.Min(width, 100);
				columnWidths[col % 36] = value;
			}
			result += value;
		}
		return result;
	}
}

// ============================================================
// Benchmark 5: SharedString read (GetCellValue) — ElementAt vs pre-built index
// ============================================================
/// <summary>
/// Issue: GetCellValue(Cell) resolves a SharedString cell by calling
/// sharedStringPart.SharedStringTable?.ElementAt(index).InnerText — an O(n) LINQ scan
/// through XML child elements for EVERY cell read.  Reading a 500-row × 36-col sheet
/// therefore does 18 000 separate O(n) scans.
/// Fix:  Build a Dictionary&lt;int, string&gt; once and look up in O(1) per cell.
/// New:  BuildSharedStringIndex() helper + GetCellValue overload accepting the dictionary.
/// </summary>
[RankColumn]
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class SharedStringReadBenchmarks
{
	private byte[] ExcelFileBytes = [];

	[GlobalSetup]
	public void Setup()
	{
		// Create a representative test file: 500 rows × 36 columns, all SharedString cells
		List<ExportTestRow> data = Enumerable.Range(1, 500).Select(i => new ExportTestRow
		{
			Id = i,
			FirstName = $"First{i}",
			LastName = $"Last{i}",
			Email = $"user{i}@example.com",
			Phone = $"555-{i:D4}",
			Status = "Active",
			Category = "Alpha",
			SubCategory = $"Sub{i % 10}",
			Amount = i * 1.23m,
			Price = i * 0.99m,
			Cost = i * 0.50m,
			Total = i * 1.73m,
			Quantity = i,
			Count = i * 2,
			IsActive = i % 2 == 0,
			IsEnabled = i % 3 != 0,
			CreatedDate = DateTime.Today,
			UpdatedDate = DateTime.Today,
			ProcessedDate = DateTime.Today,
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

		using MemoryStream ms = new();
		data.GenericExcelExport(ms);
		ExcelFileBytes = ms.ToArray();
	}

	// Reads the file using current ReadExcelFileToDataTable (ElementAt per cell, no cache)
	[Benchmark(Baseline = true, Description = "Original (ElementAt per SharedString cell)")]
	public System.Data.DataTable Original_ReadWithElementAt()
	{
		using MemoryStream ms = new(ExcelFileBytes);
		return ms.ReadExcelFileToDataTable();
	}

	// Reads the file using an optimized path that pre-builds a shared-string index once
	[Benchmark(Description = "Optimized (pre-built Dictionary index)")]
	public System.Data.DataTable Optimized_ReadWithDictionary()
	{
		using MemoryStream ms = new(ExcelFileBytes);
		return ReadWithPrebuiltIndex(ms);
	}

	/// <summary>Inline optimized read — builds the shared-string lookup once.</summary>
	private static System.Data.DataTable ReadWithPrebuiltIndex(Stream fileStream)
	{
		System.Data.DataTable dataTable = new();
		fileStream.Position = 0;
		using SpreadsheetDocument document = SpreadsheetDocument.Open(fileStream, false);
		WorkbookPart? workbookPart = document.WorkbookPart;
		Sheet? sheet = document.GetSheetByName(null);

		if (sheet == null || workbookPart == null)
		{
			return dataTable;
		}

		WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
		SheetData sheetData = worksheetPart.Worksheet?.GetFirstChild<SheetData>() ?? new SheetData();

		// Build O(1) lookup once
		SharedStringTablePart? sharedStringPart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
		IReadOnlyDictionary<int, string>? index = sharedStringPart != null ? BuildIndex(sharedStringPart) : null;

		CellReference startCell = new("A1");
		CellReference endCell = sheetData.GetLastPopulatedCell();

		for (uint col = startCell.ColumnIndex; col <= endCell.ColumnIndex; col++)
		{
			dataTable.Columns.Add(GetCellValueFast(sheetData, startCell.RowIndex, col, index));
		}

		for (uint row = startCell.RowIndex + 1; row <= endCell.RowIndex; row++)
		{
			System.Data.DataRow dataRow = dataTable.NewRow();
			bool rowHasData = false;
			for (uint col = startCell.ColumnIndex; col <= endCell.ColumnIndex; col++)
			{
				string cellValue = GetCellValueFast(sheetData, row, col, index);
				dataRow[(int)(col - startCell.ColumnIndex)] = cellValue;
				if (!string.IsNullOrWhiteSpace(cellValue))
				{
					rowHasData = true;
				}
			}
			if (rowHasData)
			{
				dataTable.Rows.Add(dataRow);
			}
			else
			{
				break;
			}
		}
		return dataTable;
	}

	private static Dictionary<int, string> BuildIndex(SharedStringTablePart sharedStringPart)
	{
		Dictionary<int, string> dict = new();
		int i = 0;
		if (sharedStringPart.SharedStringTable != null)
		{
			foreach (SharedStringItem item in sharedStringPart.SharedStringTable.Elements<SharedStringItem>())
			{
				dict[i++] = item.InnerText;
			}
		}
		return dict;
	}

	private static string GetCellValueFast(SheetData sheetData, uint row, uint col, IReadOnlyDictionary<int, string>? index)
	{
		CellReference cellRef = new(col, row);
		Cell? cell = sheetData.Elements<Row>().FirstOrDefault(x => x.RowIndex != null && x.RowIndex == row)?
				.Elements<Cell>().FirstOrDefault(x => x.CellReference != null && string.Equals(new CellReference(x.CellReference!).ToString(), cellRef.ToString(), StringComparison.OrdinalIgnoreCase));

		if (cell?.CellValue == null)
		{
			return string.Empty;
		}

		string value = cell.CellValue.Text;

		if (cell.DataType?.Value == CellValues.SharedString && index != null && int.TryParse(value, out int idx))
		{
			return index.TryGetValue(idx, out string? s) ? s : string.Empty;
		}
		return value;
	}
}
