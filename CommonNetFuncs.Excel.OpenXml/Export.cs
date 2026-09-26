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
		document.InitializeExcelFile();

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
				foreach (SharedStringItem item in sharedStringTable.Elements<SharedStringItem>())
				{
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
					headerRow.Append((OpenXmlElement[])[new Cell
					{
						CellReference = colLetters[i] + y,
						StyleIndex = headerStyleId,
						DataType = CellValues.SharedString,
						CellValue = new CellValue(ssIdx.ToString())
					}]);

					double w = CalculateWidth(text, headerStyleId);
					if (w > colWidths[i])
					{
						colWidths[i] = w;
					}
				}
				sheetData.Append((OpenXmlElement[])[headerRow]);
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
						dataRow.Append((OpenXmlElement[])[new Cell
						{
							CellReference = colLetters[i] + y,
							StyleIndex = bodyStyleId,
							DataType = CellValues.SharedString,
							CellValue = new CellValue(ssIdx.ToString())
						}]);
						double w = CalculateWidth(text, bodyStyleId);
						if (w > colWidths[i])
						{
							colWidths[i] = w;
						}
					}
					sheetData.Append((OpenXmlElement[])[dataRow]);
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
						columns.Append((OpenXmlElement[])[new Column { Min = (uint)(i + 1), Max = (uint)(i + 1), Width = Math.Min(colWidths[i], 100), CustomWidth = true }]);
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
					headerRow.Append((OpenXmlElement[])[new Cell
					{
						CellReference = colLetters[i] + y,
						StyleIndex = headerStyleId,
						DataType = CellValues.SharedString,
						CellValue = new CellValue(ssIdx.ToString())
					}]);
					double w = CalculateWidth(text, headerStyleId);
					if (w > colWidths[i])
					{
						colWidths[i] = w;
					}
				}
				sheetData.Append((OpenXmlElement[])[headerRow]);
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
						dataRow.Append((OpenXmlElement[])[new Cell
						{
							CellReference = colLetters[i] + y,
							StyleIndex = bodyStyleId,
							DataType = CellValues.SharedString,
							CellValue = new CellValue(ssIdx.ToString())
						}]);
						double w = CalculateWidth(text, bodyStyleId);
						if (w > colWidths[i])
						{
							colWidths[i] = w;
						}
					}
					sheetData.Append((OpenXmlElement[])[dataRow]);
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
						columns.Append((OpenXmlElement[])[new Column { Min = (uint)(i + 1), Max = (uint)(i + 1), Width = Math.Min(colWidths[i], 100), CustomWidth = true }]);
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

	// ─────────────────────────── SAX streaming exports ───────────────────────────

	/// <summary>
	/// Streams a list of objects to <paramref name="outputStream"/> as an xlsx file using the OpenXML SAX engine.
	/// Unlike <see cref="GenericExcelExport{T}"/>, this never builds an in-memory DOM, so memory stays constant regardless of how many rows are written.
	/// Column widths are estimated from header text only.
	/// </summary>
	public static async Task GenericExcelExportAsync<T>(this IEnumerable<T> dataList, Stream outputStream, bool createTable = false,
		string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false,
		CancellationToken cancellationToken = default)
	{
		try
		{
			using SpreadsheetDocument document = SpreadsheetDocument.Create(outputStream, SpreadsheetDocumentType.Workbook, true);
			document.CompressionOption = CompressionOption.Normal;
			WorkbookPart workbookPart = document.InitializeExcelFile();
			WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
			await ExportFromTableSaxCoreAsync<T>(document, worksheetPart, dataList, null, createTable, tableName, skipColumnNames, wrapText, cancellationToken);
			RegisterSaxSheet(workbookPart, worksheetPart, sheetName);
			workbookPart.Workbook!.Save();
		}
		catch (OperationCanceledException)
		{
			throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExportAsync)} was canceled");
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error", nameof(Export), nameof(GenericExcelExportAsync));
		}
	}

	/// <summary>
	/// Streams data from an <see cref="IAsyncEnumerable{T}"/> source (e.g. EF Core <c>AsAsyncEnumerable()</c>) to
	/// <paramref name="outputStream"/> as an xlsx file using the OpenXML SAX engine.
	/// Rows are written directly from the async source without ever buffering a list in RAM.
	/// /// Column widths are estimated from header text only.
	/// </summary>
	public static async Task GenericExcelExportAsync<T>(this IAsyncEnumerable<T> dataList, Stream outputStream, bool createTable = false,
		string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false,
		CancellationToken cancellationToken = default)
	{
		try
		{
			using SpreadsheetDocument document = SpreadsheetDocument.Create(outputStream, SpreadsheetDocumentType.Workbook, true);
			document.CompressionOption = CompressionOption.Normal;
			WorkbookPart workbookPart = document.InitializeExcelFile();
			WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
			await ExportFromTableSaxCoreAsync<T>(document, worksheetPart, null, dataList, createTable, tableName, skipColumnNames, wrapText, cancellationToken);
			RegisterSaxSheet(workbookPart, worksheetPart, sheetName);
			workbookPart.Workbook!.Save();
		}
		catch (OperationCanceledException)
		{
			throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExportAsync)} was canceled");
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error", nameof(Export), nameof(GenericExcelExportAsync));
		}
	}

	/// <summary>
	/// Streams a <see cref="DataTable"/> to <paramref name="outputStream"/> as an xlsx file using the OpenXML SAX engine.
	/// Column widths are estimated from header text only.
	/// </summary>
	public static async Task GenericExcelExportAsync(this DataTable datatable, Stream outputStream, bool createTable = false,
		string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false,
		CancellationToken cancellationToken = default)
	{
		try
		{
			using SpreadsheetDocument document = SpreadsheetDocument.Create(outputStream, SpreadsheetDocumentType.Workbook, true);
			document.CompressionOption = CompressionOption.Normal;
			WorkbookPart workbookPart = document.InitializeExcelFile();
			WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
			await ExportFromTableSaxAsync(document, worksheetPart, datatable, createTable, tableName, skipColumnNames, wrapText, cancellationToken);
			RegisterSaxSheet(workbookPart, worksheetPart, sheetName);
			workbookPart.Workbook!.Save();
		}
		catch (OperationCanceledException)
		{
			throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExportAsync)} was canceled");
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error", nameof(Export), nameof(GenericExcelExportAsync));
		}
	}

	/// <summary>
	/// Writes <paramref name="data"/> into <paramref name="worksheetPart"/> using the OpenXML SAX engine.
	/// The caller is responsible for registering the sheet in the workbook after this call.
	/// </summary>
	public static async Task ExportFromTableSaxAsync<T>(SpreadsheetDocument document, WorksheetPart worksheetPart, IEnumerable<T> data,
		bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false,
		CancellationToken cancellationToken = default)
	{
		await ExportFromTableSaxCoreAsync<T>(document, worksheetPart, data, null, createTable, tableName, skipColumnNames, wrapText, cancellationToken);
	}

	/// <summary>
	/// Writes data from an <see cref="IAsyncEnumerable{T}"/> source into <paramref name="worksheetPart"/> using the OpenXML SAX engine.
	/// The caller is responsible for registering the sheet in the workbook after this call.
	/// </summary>
	public static async Task ExportFromTableSaxAsync<T>(SpreadsheetDocument document, WorksheetPart worksheetPart, IAsyncEnumerable<T> data,
		bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false,
		CancellationToken cancellationToken = default)
	{
		await ExportFromTableSaxCoreAsync<T>(document, worksheetPart, null, data, createTable, tableName, skipColumnNames, wrapText, cancellationToken);
	}

	/// <summary>
	/// Writes a <see cref="DataTable"/> into <paramref name="worksheetPart"/> using the OpenXML SAX engine.
	/// The caller is responsible for registering the sheet in the workbook after this call.
	/// </summary>
	public static Task ExportFromTableSaxAsync(SpreadsheetDocument document, WorksheetPart worksheetPart, DataTable data,
		bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false,
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (data?.Rows.Count > 0)
			{
				uint headerStyleId = document.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
				uint bodyStyleId = document.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

				int totalCols = data.Columns.Count;
				string[] colLetters = new string[totalCols];
				for (int i = 0; i < totalCols; i++)
					colLetters[i] = CellReference.NumberToColumnName((uint)(i + 1));

				HashSet<int> skipColumnIndices = [];
				for (int i = 0; i < totalCols; i++)
				{
					if (skipColumnNames?.Contains(data.Columns[i].ColumnName, StringComparer.InvariantCultureIgnoreCase) == true)
						skipColumnIndices.Add(i);
				}

				// Pre-add table definition part before opening the SAX writer so the relationship ID is known
				TableDefinitionPart? tableDefPart = null;
				string? tableRId = null;
				if (createTable)
				{
					tableDefPart = worksheetPart.AddNewPart<TableDefinitionPart>();
					tableRId = worksheetPart.GetIdOfPart(tableDefPart);
				}

				uint y = 1;
				using (OpenXmlWriter writer = OpenXmlWriter.Create(worksheetPart))
				{
					writer.WriteStartElement(new Worksheet());

					// <cols> must precede <sheetData> per ECMA-376; widths estimated from header text only
					writer.WriteStartElement(new Columns());
					for (int i = 0; i < totalCols; i++)
					{
						if (skipColumnIndices.Contains(i)) continue;
						double w = CalculateWidth(data.Columns[i].ColumnName, headerStyleId);
						if (w > 0)
							writer.WriteElement(new Column { Min = (uint)(i + 1), Max = (uint)(i + 1), Width = Math.Min(w, 100), CustomWidth = true });
					}
					writer.WriteEndElement(); // Columns

					writer.WriteStartElement(new SheetData());

					// Header row
					writer.WriteStartElement(new Row { RowIndex = y });
					for (int i = 0; i < totalCols; i++)
					{
						if (skipColumnIndices.Contains(i)) continue;
						WriteSaxInlineStringCell(writer, colLetters[i] + y, data.Columns[i].ColumnName, headerStyleId);
					}
					writer.WriteEndElement(); // Row
					y++;

					// Data rows
					foreach (DataRow row in data.Rows)
					{
						cancellationToken.ThrowIfCancellationRequested();
						writer.WriteStartElement(new Row { RowIndex = y });
						object?[] items = row.ItemArray;
						for (int i = 0; i < items.Length; i++)
						{
							if (skipColumnIndices.Contains(i)) continue;
							WriteSaxInlineStringCell(writer, colLetters[i] + y, items[i]?.ToString() ?? string.Empty, bodyStyleId);
						}
						writer.WriteEndElement(); // Row
						y++;
					}

					writer.WriteEndElement(); // SheetData

					string rangeRef = $"{new CellReference(1u, 1u)}:{new CellReference((uint)totalCols, y - 1)}";
					if (createTable && tableRId != null)
					{
						writer.WriteStartElement(new TableParts { Count = 1 });
						writer.WriteElement(new TablePart { Id = tableRId });
						writer.WriteEndElement(); // TableParts
					}
					else
					{
						writer.WriteElement(new AutoFilter { Reference = rangeRef });
					}

					writer.WriteEndElement(); // Worksheet
				}

				// Populate the table definition after the SAX writer is flushed
				if (createTable && tableDefPart != null)
				{
					uint visibleCount = (uint)(totalCols - skipColumnIndices.Count);
					TableColumns tableColumns = new() { Count = visibleCount };
					uint colId = 1;
					for (int i = 0; i < totalCols; i++)
					{
						if (skipColumnIndices.Contains(i)) continue;
						tableColumns.Append(new TableColumn { Id = colId++, Name = data.Columns[i].ColumnName });
					}
					string tableRef = $"{new CellReference(1u, 1u)}:{new CellReference((uint)totalCols, y - 1)}";
					tableDefPart.Table = new Table
					{
						Id = 1,
						Name = tableName,
						DisplayName = tableName,
						Reference = tableRef,
						TotalsRowShown = false,
						HeaderRowCount = 1,
						InsertRow = false,
						InsertRowShift = false,
						Published = false,
						AutoFilter = new AutoFilter { Reference = tableRef },
						TableColumns = tableColumns,
						TableStyleInfo = new TableStyleInfo
						{
							Name = ETableStyle.TableStyleMedium1.ToString(),
							ShowFirstColumn = false,
							ShowLastColumn = false,
							ShowRowStripes = true,
							ShowColumnStripes = false
						}
					};
				}
			}
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error", nameof(Export), nameof(ExportFromTableSaxAsync));
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// Core SAX writer shared by both the sync (<see cref="IEnumerable{T}"/>) and async (<see cref="IAsyncEnumerable{T}"/>) generic overloads.
	/// Writes the worksheet XML directly to <paramref name="worksheetPart"/> stream without building a DOM.
	/// </summary>
	private static async Task ExportFromTableSaxCoreAsync<T>(SpreadsheetDocument document, WorksheetPart worksheetPart,
		IEnumerable<T>? syncData, IAsyncEnumerable<T>? asyncData,
		bool createTable, string tableName, List<string>? skipColumnNames, bool wrapText, CancellationToken cancellationToken)
	{
		try
		{
			uint headerStyleId = document.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
			uint bodyStyleId = document.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

			PropertyInfo[] properties = GetOrAddPropertiesFromReflectionCache(typeof(T))
				.Where(x => (skipColumnNames == null) || (skipColumnNames.Count == 0) || !skipColumnNames.Contains(x.Name, StringComparer.InvariantCultureIgnoreCase))
				.ToArray();
			int colCount = properties.Length;

			string[] colLetters = new string[colCount];
			for (int i = 0; i < colCount; i++)
				colLetters[i] = CellReference.NumberToColumnName((uint)(i + 1));

			// Pre-add the table definition part now to obtain its relationship ID before the SAX writer is opened
			TableDefinitionPart? tableDefPart = null;
			string? tableRId = null;
			if (createTable && colCount > 0)
			{
				tableDefPart = worksheetPart.AddNewPart<TableDefinitionPart>();
				tableRId = worksheetPart.GetIdOfPart(tableDefPart);
			}

			uint y = 1;
			using (OpenXmlWriter writer = OpenXmlWriter.Create(worksheetPart))
			{
				writer.WriteStartElement(new Worksheet());

				// <cols> must precede <sheetData> per ECMA-376; widths estimated from header text only in SAX streaming mode
				if (colCount > 0)
				{
					writer.WriteStartElement(new Columns());
					for (int i = 0; i < colCount; i++)
					{
						double w = CalculateWidth(properties[i].Name, headerStyleId);
						if (w > 0)
							writer.WriteElement(new Column { Min = (uint)(i + 1), Max = (uint)(i + 1), Width = Math.Min(w, 100), CustomWidth = true });
					}
					writer.WriteEndElement(); // Columns
				}

				writer.WriteStartElement(new SheetData());

				if (colCount > 0)
				{
					// Header row
					writer.WriteStartElement(new Row { RowIndex = y });
					for (int i = 0; i < colCount; i++)
						WriteSaxInlineStringCell(writer, colLetters[i] + y, properties[i].Name, headerStyleId);
					writer.WriteEndElement(); // Row
					y++;

					// Data rows
					if (asyncData != null)
					{
						await foreach (T item in asyncData.WithCancellation(cancellationToken))
						{
							if (item.ToNString().IsNullOrEmpty()) continue;
							writer.WriteStartElement(new Row { RowIndex = y });
							for (int i = 0; i < colCount; i++)
								WriteSaxInlineStringCell(writer, colLetters[i] + y, properties[i].GetValue(item)?.ToString() ?? string.Empty, bodyStyleId);
							writer.WriteEndElement(); // Row
							y++;
						}
					}
					else if (syncData != null)
					{
						foreach (T item in syncData.Where(x => !x.ToNString().IsNullOrEmpty()))
						{
							cancellationToken.ThrowIfCancellationRequested();
							writer.WriteStartElement(new Row { RowIndex = y });
							for (int i = 0; i < colCount; i++)
								WriteSaxInlineStringCell(writer, colLetters[i] + y, properties[i].GetValue(item)?.ToString() ?? string.Empty, bodyStyleId);
							writer.WriteEndElement(); // Row
							y++;
						}
					}
				}

				writer.WriteEndElement(); // SheetData

				if (colCount > 0)
				{
					string rangeRef = $"{new CellReference(1u, 1u)}:{new CellReference((uint)colCount, y - 1)}";
					if (createTable && tableRId != null)
					{
						writer.WriteStartElement(new TableParts { Count = 1 });
						writer.WriteElement(new TablePart { Id = tableRId });
						writer.WriteEndElement(); // TableParts
					}
					else
					{
						writer.WriteElement(new AutoFilter { Reference = rangeRef });
					}
				}

				writer.WriteEndElement(); // Worksheet
			}

			// Populate the table definition after the SAX writer is flushed and the worksheet XML is final
			if (createTable && tableDefPart != null && colCount > 0)
			{
				TableColumns tableColumns = new() { Count = (uint)colCount };
				for (int i = 0; i < colCount; i++)
					tableColumns.Append(new TableColumn { Id = (uint)i + 1, Name = properties[i].Name });

				string tableRef = $"{new CellReference(1u, 1u)}:{new CellReference((uint)colCount, y - 1)}";
				tableDefPart.Table = new Table
				{
					Id = 1,
					Name = tableName,
					DisplayName = tableName,
					Reference = tableRef,
					TotalsRowShown = false,
					HeaderRowCount = 1,
					InsertRow = false,
					InsertRowShift = false,
					Published = false,
					AutoFilter = new AutoFilter { Reference = tableRef },
					TableColumns = tableColumns,
					TableStyleInfo = new TableStyleInfo
					{
						Name = ETableStyle.TableStyleMedium1.ToString(),
						ShowFirstColumn = false,
						ShowLastColumn = false,
						ShowRowStripes = true,
						ShowColumnStripes = false
					}
				};
			}
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error", nameof(Export), nameof(ExportFromTableSaxCoreAsync));
		}
	}

	/// <summary>
	/// Registers <paramref name="worksheetPart"/> as a new sheet entry in the workbook.
	/// </summary>
	private static void RegisterSaxSheet(WorkbookPart workbookPart, WorksheetPart worksheetPart, string sheetName)
	{
		Sheets sheets = workbookPart.Workbook!.GetFirstChild<Sheets>() ?? workbookPart.Workbook!.AppendChild(new Sheets());
		string partId = workbookPart.GetIdOfPart(worksheetPart);
		uint sheetId = sheets.Elements<Sheet>().Any()
			? (sheets.Elements<Sheet>().Max(x => x.SheetId?.Value) + 1) ?? ((uint)sheets.Elements<Sheet>().Count() + 1)
			: 1u;
		sheets.Append(new Sheet { Id = partId, SheetId = sheetId, Name = sheetName });
	}

	/// <summary>
	/// Writes a single inline-string cell via the SAX writer. Uses <see cref="CellValues.InlineString"/> to avoid
	/// shared-string table allocations, keeping memory overhead at O(1) per cell.
	/// </summary>
	private static void WriteSaxInlineStringCell(OpenXmlWriter writer, string cellRef, string text, uint styleId)
	{
		writer.WriteStartElement(new Cell { CellReference = cellRef, StyleIndex = styleId, DataType = CellValues.InlineString });
		writer.WriteStartElement(new InlineString());
		writer.WriteElement(new Text(text));
		writer.WriteEndElement(); // InlineString
		writer.WriteEndElement(); // Cell
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
		table.AppendChild(new SharedStringItem((OpenXmlElement[])[new Text(text)]));
		cache[text] = count;
		return count++;
	}
}