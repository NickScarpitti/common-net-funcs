using System.Data;
using System.IO.Packaging;
using System.Reflection;
using CommonNetFuncs.Core;
using CommonNetFuncs.Excel.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Core.ReflectionCaches;
using static CommonNetFuncs.Excel.OpenXml.Common;

namespace CommonNetFuncs.Excel.OpenXml;

/// <summary>
/// Export data to an excel data using NPOI
/// </summary>
public static class Export
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Convert a list of data objects into a MemoryStream containing en excel file with a tabular representation of the data
	/// </summary>
	/// <typeparam name="T">Type of data inside of list to be exported</typeparam>
	/// <param name="dataList">Data to export as a table.</param>
	/// <param name="memoryStream">Output memory stream (will be created if one is not provided)</param>
	/// <param name="createTable">If <see langword="true"/>, will format the exported data into an Excel table.</param>
	/// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
	public static MemoryStream? GenericExcelExport<T>(this IEnumerable<T> dataList, MemoryStream? memoryStream = null, bool createTable = false,
			string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
	{
		try
		{
			memoryStream ??= new();

			using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook, true);
			document.CompressionOption = CompressionOption.Normal;
			//document.CompressionOption = CompressionOption.Maximum;
			uint newSheetId = document.InitializeExcelFile(sheetName);
			Worksheet? worksheet = document.GetWorksheetById(newSheetId);

			if ((worksheet != null) && !ExportFromTable(document, worksheet, dataList, createTable, tableName, skipColumnNames, wrapText))
			{
				return null;
			}

			document.Save();
			document.Dispose();

			memoryStream.Position = 0;
			return memoryStream;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error", nameof(Export), nameof(GenericExcelExport));
		}

		return new();
	}

	/// <summary>
	/// Convert a list of data objects into a MemoryStream containing en excel file with a tabular representation of the data
	/// </summary>
	/// <param name="datatable">Data to export as a table.</param>
	/// <param name="memoryStream">Output memory stream (will be created if one is not provided)</param>
	/// <param name="createTable">If <see langword="true"/>, will format the exported data into an Excel table.</param>
	/// <returns>MemoryStream containing en excel file with a tabular representation of dataList</returns>
	public static MemoryStream? GenericExcelExport(this DataTable datatable, MemoryStream? memoryStream = null, bool createTable = false,
			string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
	{
		try
		{
			memoryStream ??= new();
			using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook, true);
			document.CompressionOption = CompressionOption.Maximum;
			uint newSheetId = document.InitializeExcelFile(sheetName);
			Worksheet? worksheet = document.GetWorksheetById(newSheetId);

			if ((worksheet != null) && !ExportFromTable(document, worksheet, datatable, createTable, tableName, skipColumnNames, wrapText))
			{
				return null;
			}

			document.Save();
			document.Dispose();

			memoryStream.Position = 0;
			return memoryStream;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error", nameof(Export), nameof(GenericExcelExport));
		}

		return new();
	}

	/// <summary>
	/// Add data to a new sheet in a workbook
	/// </summary>
	/// <typeparam name="T">Type of data inside of list to be exported</typeparam>
	/// <param name="document">Workbook to add table to</param>
	/// <param name="data">Data to insert into workbook</param>
	/// <param name="sheetName">Name of sheet to add data into</param>
	/// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
	/// <param name="tableName">Name of the table in Excel</param>
	/// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
	public static bool AddGenericTable<T>(this SpreadsheetDocument document, IEnumerable<T> data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
	{
		return document.AddGenericTableInternal<T>(data, typeof(IEnumerable<T>), sheetName, createTable, tableName, skipColumnNames, wrapText);
	}

	/// <summary>
	/// Add data to a new sheet in a workbook
	/// </summary>
	/// <param name="document">Workbook to add table to</param>
	/// <param name="data">Data to insert into workbook</param>
	/// <param name="sheetName">Name of sheet to add data into</param>
	/// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
	/// <param name="tableName">Name of the table in Excel</param>
	/// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
	public static bool AddGenericTable(this SpreadsheetDocument document, DataTable data, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
	{
		return document.AddGenericTableInternal<char>(data, typeof(DataTable), sheetName, createTable, tableName, skipColumnNames, wrapText);
	}

	/// <summary>
	/// Add data to a new sheet in a workbook
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="document">Workbook to add sheet table to</param>
	/// <param name="data">Data to populate table with (only accepts IEnumerable</param>
	/// <param name="dataType">Type of the data parameter</param>
	/// <param name="sheetName">Name of sheet to add data into</param>
	/// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
	/// <param name="tableName">Name of the table in Excel</param>
	/// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
	private static bool AddGenericTableInternal<T>(this SpreadsheetDocument document, object? data, Type dataType, string sheetName, bool createTable = false,
		string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
	{
		bool success = false;
		try
		{
			int i = 1;
			string actualSheetName = sheetName;
			while (document.GetWorksheetByName(actualSheetName, false) != null)
			{
				actualSheetName = $"{sheetName} ({i})"; //Get safe new sheet name
				i++;
			}

			Worksheet? worksheet = document.GetWorksheetById(document.CreateNewSheet(actualSheetName));
			if ((worksheet != null) && (data != null))
			{
				if (dataType == typeof(IEnumerable<T>))
				{
					success = ExportFromTable(document, worksheet, (IEnumerable<T>)data, createTable, tableName, skipColumnNames, wrapText);
				}
				else if (dataType == typeof(DataTable))
				{
					success = ExportFromTable(document, worksheet, (DataTable)data, createTable, tableName, skipColumnNames, wrapText);
				}
				// else
				// {
				// 	throw new ArgumentException("Invalid type for data parameter. Parameter must be either an IEnumerable or DataTable class", nameof(data));
				// }
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		return success;
	}

	/// <summary>
	/// Generates a simple excel file containing the passed in data in a tabular format
	/// </summary>
	/// <typeparam name="T">Type of data inside of list to be inserted into the workbook</typeparam>
	/// <param name="document">Document to insert data into</param>
	/// <param name="worksheet">Worksheet to insert the data into</param>
	/// <param name="data">Data to be inserted into the workbook</param>
	/// <param name="createTable">Turn the output into an Excel table.</param>
	/// <param name="tableName">Name of the table when createTable is true</param>
	/// <returns><see langword="true"/> if excel file was created successfully</returns>
	/// <exception cref="ArgumentException"></exception>
	public static bool ExportFromTable<T>(SpreadsheetDocument document, Worksheet worksheet, IEnumerable<T> data, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false, CancellationToken cancellationToken = default)
	{
		try
		{
			if (data?.Any() == true)
			{
				SheetData sheetData = worksheet.GetFirstChild<SheetData>() ?? throw new ArgumentException("The worksheet does not contain sheetData, which is required for this operation.");

				uint headerStyleId = document.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
				uint bodyStyleId = document.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

				PropertyInfo[] properties = GetOrAddPropertiesFromReflectionCache(typeof(T))
					.Where(x => (skipColumnNames == null) || (skipColumnNames.Count == 0) || !skipColumnNames.Contains(x.Name, StringComparer.InvariantCultureIgnoreCase))
					.ToArray();
				int colCount = properties.Length;

				// Pre-compute column letter strings (e.g. "A", "B", ..., "AJ") once
				string[] colLetters = new string[colCount];
				for (int i = 0; i < colCount; i++)
				{
					colLetters[i] = CellReference.NumberToColumnName((uint)(i + 1));
				}

				// Set up shared-string table with O(1) dictionary lookup.
				// The original approach called InsertSharedStringItem per cell, which did an O(n) linear scan and called SharedStringTable.Save() after every single insertion
				WorkbookPart workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("WorkbookPart is missing.");
				SharedStringTablePart sharedStringPart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault() ?? workbookPart.AddNewPart<SharedStringTablePart>();
				sharedStringPart.SharedStringTable ??= new SharedStringTable();
				SharedStringTable sharedStringTable = sharedStringPart.SharedStringTable;

				Dictionary<string, int> sharedStringCache = new(StringComparer.Ordinal);
				int ssCount = 0;
				foreach (SharedStringItem item in sharedStringTable.Elements<SharedStringItem>()){
					sharedStringCache[item.InnerText] = ssCount++;
				}

				// Track maximum column widths inline during the write pass so that the second full-cell pass of AutoFitColumns() (which also repeated the tree traversals and shared-string lookups) is avoided entirely.
				double[] colWidths = new double[colCount];

				uint y = 1;

				// Write header row — build the Row/Cell objects directly and Append in one shot
				// rather than calling InsertCell() which did a linear Elements<Row>() scan per cell.
				Row headerRow = new() { RowIndex = y };
				for (int i = 0; i < colCount; i++)
				{
					string text = properties[i].Name;
					int ssIdx = GetOrAddSharedString(text, sharedStringCache, sharedStringTable, ref ssCount);
					headerRow.Append(new Cell
					{
						CellReference = colLetters[i] + y,
						StyleIndex = headerStyleId,
						DataType = CellValues.SharedString,
						CellValue = new CellValue(ssIdx.ToString())
					});

					double w = CalculateWidth(text, headerStyleId);
					if (w > colWidths[i])
					{
						colWidths[i] = w;
					}
				}
				sheetData.Append(headerRow);
				y++;

				// Write data rows
				foreach (T item in data.Where(x => !x.ToNString().IsNullOrEmpty()))
				{
					cancellationToken.ThrowIfCancellationRequested();
					Row dataRow = new() { RowIndex = y };
					for (int i = 0; i < colCount; i++)
					{
						string text = properties[i].GetValue(item)?.ToString() ?? string.Empty;
						int ssIdx = GetOrAddSharedString(text, sharedStringCache, sharedStringTable, ref ssCount);
						dataRow.Append(new Cell
						{
							CellReference = colLetters[i] + y,
							StyleIndex = bodyStyleId,
							DataType = CellValues.SharedString,
							CellValue = new CellValue(ssIdx.ToString())
						});
						double w = CalculateWidth(text, bodyStyleId);
						if (w > colWidths[i])
						{
							colWidths[i] = w;
						}
					}
					sheetData.Append(dataRow);
					y++;
				}

				// Save shared-string table exactly once instead of once per cell
				sharedStringTable.Save();

				// Apply column widths from the inline-tracked array — no second pass needed
				Columns columns = worksheet.GetColumns();
				for (int i = 0; i < colCount; i++)
				{
					if (colWidths[i] > 0)
					{
						columns.Append(new Column { Min = (uint)(i + 1), Max = (uint)(i + 1), Width = Math.Min(colWidths[i], 100), CustomWidth = true });
					}
				}

				if (createTable)
				{
					worksheet.CreateTable(1, 1, y - 1, (uint)colCount, tableName);
				}
				else
				{
					worksheet.SetAutoFilter(1, 1, y - 1, (uint)colCount);
				}
			}
			ClearStandardFormatCacheForWorkbook(document);
			return true;
		}
		catch (OperationCanceledException)
		{
			throw new TaskCanceledException($"{nameof(Export)}.{nameof(ExportFromTable)} was canceled");
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
			return false;
		}
	}

	/// <summary>
	/// Generates a simple excel file containing the passed in data in a tabular format
	/// </summary>
	/// <param name="document">Document to insert data into</param>
	/// <param name="worksheet">Worksheet to insert the data into</param>
	/// <param name="data">Data as DataTable to be inserted into the workbook</param>
	/// <param name="createTable">Turn the output into an Excel table.</param>
	/// <param name="tableName">Name of the table when createTable is true</param>
	/// <returns><see langword="true"/> if excel file was created successfully</returns>
	/// <exception cref="ArgumentException"></exception>
	public static bool ExportFromTable(SpreadsheetDocument document, Worksheet worksheet, DataTable data, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false, CancellationToken cancellationToken = default)
	{
		try
		{
			if (data?.Rows.Count > 0)
			{
				SheetData sheetData = worksheet.GetFirstChild<SheetData>() ?? throw new ArgumentException("The worksheet does not contain sheetData, which is required for this operation.");

				uint headerStyleId = document.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
				uint bodyStyleId = document.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

				int totalCols = data.Columns.Count;

				// Pre-compute column letter strings once
				string[] colLetters = new string[totalCols];
				for (int i = 0; i < totalCols; i++)
				{
					colLetters[i] = CellReference.NumberToColumnName((uint)(i + 1));
				}

				// Set up shared-string table with O(1) dictionary lookup
				WorkbookPart workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("WorkbookPart is missing.");
				SharedStringTablePart sharedStringPart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault() ?? workbookPart.AddNewPart<SharedStringTablePart>();
				sharedStringPart.SharedStringTable ??= new SharedStringTable();
				SharedStringTable sharedStringTable = sharedStringPart.SharedStringTable;

				Dictionary<string, int> sharedStringCache = new(StringComparer.Ordinal);
				int ssCount = 0;
				foreach (SharedStringItem item in sharedStringTable.Elements<SharedStringItem>())
				{
					sharedStringCache[item.InnerText] = ssCount++;
				}

				// Build skip set using 0-based column indices (HashSet for O(1) lookup vs the
				// original List<uint> which was O(n) per Contains call)
				HashSet<int> skipColumnIndices = [];
				for (int i = 0; i < totalCols; i++)
				{
					if (skipColumnNames?.Contains(data.Columns[i].ColumnName, StringComparer.InvariantCultureIgnoreCase) == true)
					{
						skipColumnIndices.Add(i);
					}
				}

				// Track maximum column widths inline
				double[] colWidths = new double[totalCols];

				uint y = 1;

				// Write header row
				Row headerRow = new() { RowIndex = y };
				for (int i = 0; i < totalCols; i++)
				{
					if (skipColumnIndices.Contains(i))
					{
						continue;
					}

					string text = data.Columns[i].ColumnName;
					int ssIdx = GetOrAddSharedString(text, sharedStringCache, sharedStringTable, ref ssCount);
					headerRow.Append(new Cell
					{
						CellReference = colLetters[i] + y,
						StyleIndex = headerStyleId,
						DataType = CellValues.SharedString,
						CellValue = new CellValue(ssIdx.ToString())
					});
					double w = CalculateWidth(text, headerStyleId);
					if (w > colWidths[i])
					{
						colWidths[i] = w;
					}
				}
				sheetData.Append(headerRow);
				y++;

				// Write data rows
				foreach (DataRow row in data.Rows)
				{
					cancellationToken.ThrowIfCancellationRequested();
					Row dataRow = new() { RowIndex = y };
					object?[] items = row.ItemArray;
					for (int i = 0; i < items.Length; i++)
					{
						if (items[i] == null || skipColumnIndices.Contains(i))
						{
							continue;
						}
						string text = items[i]!.ToString() ?? string.Empty;
						int ssIdx = GetOrAddSharedString(text, sharedStringCache, sharedStringTable, ref ssCount);
						dataRow.Append(new Cell
						{
							CellReference = colLetters[i] + y,
							StyleIndex = bodyStyleId,
							DataType = CellValues.SharedString,
							CellValue = new CellValue(ssIdx.ToString())
						});
						double w = CalculateWidth(text, bodyStyleId);
						if (w > colWidths[i])
						{
							colWidths[i] = w;
						}
					}
					sheetData.Append(dataRow);
					y++;
				}

				// Save shared-string table exactly once
				sharedStringTable.Save();

				// Apply column widths from the inline-tracked array
				Columns columns = worksheet.GetColumns();
				for (int i = 0; i < totalCols; i++)
				{
					if (colWidths[i] > 0)
					{
						columns.Append(new Column { Min = (uint)(i + 1), Max = (uint)(i + 1), Width = Math.Min(colWidths[i], 100), CustomWidth = true });
					}
				}

				if (createTable)
				{
					worksheet.CreateTable(1, 1, y - 1, (uint)totalCols, tableName);
				}
				else
				{
					worksheet.SetAutoFilter(1, 1, y - 1, (uint)totalCols);
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error in {Class}.{Method}", nameof(Export), nameof(ExportFromTable));
			return false;
		}
	}

	/// <summary>
	/// Returns the shared-string index for <paramref name="text"/>, adding it to both the
	/// in-memory dictionary cache and the XML table if it is not already present.
	/// </summary>
	/// <param name="text">The string to look up or add.</param>
	/// <param name="cache">The in-memory dictionary cache of shared strings.</param>
	/// <param name="table">The XML shared-string table.</param>
	/// <param name="count">The current count of shared strings, used to assign the next index for a new string.</param>
	/// <returns>The index of the shared string in the table.</returns>
	private static int GetOrAddSharedString(string text, Dictionary<string, int> cache, SharedStringTable table, ref int count)
	{
		if (cache.TryGetValue(text, out int index))
		{
			return index;
		}
		table.AppendChild(new SharedStringItem(new Text(text)));
		cache[text] = count;
		return count++;
	}
}