using System.Data;
using System.IO.Packaging;
using System.Reflection;
using CommonNetFuncs.Core;
using CommonNetFuncs.Excel.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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
			document.CompressionOption = CompressionOption.Maximum;
			uint newSheetId = document.InitializeExcelFile(sheetName);
			Worksheet? worksheet = document.GetWorksheetById(newSheetId);

			if ((worksheet != null) && !ExportFromTable(document, worksheet, dataList, createTable, tableName, skipColumnNames, wrapText))
			{
				return null;
			}

			document.Save();
			document.Dispose();

			return memoryStream;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Export)}.{nameof(GenericExcelExport)} Error");
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

			return memoryStream;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Export)}.{nameof(GenericExcelExport)} Error");
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
	private static bool AddGenericTableInternal<T>(this SpreadsheetDocument document, object? data, Type dataType, string sheetName, bool createTable = false, string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false)
	{
		bool success = false;
		try
		{
			int i = 1;
			string actualSheetName = sheetName;
			while (document.GetWorksheetByName(actualSheetName) != null)
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
				else
				{
					throw new("Invalid type for data parameter. Parameter must be either an IEnumerable or DataTable class");
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Export)}.{nameof(AddGenericTableInternal)} Error");
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
				SheetData? sheetData = worksheet.GetFirstChild<SheetData>() ?? throw new ArgumentException("The worksheet does not contain sheetData, which is required for this operation.");

				uint headerStyleId = document.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
				uint bodyStyleId = document.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

				uint x = 1;
				uint y = 1;

				PropertyInfo[] properties = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => (skipColumnNames == null) || (skipColumnNames.Count == 0) || !skipColumnNames.Contains(x.Name, StringComparer.InvariantCultureIgnoreCase)).ToArray();

				// Write headers
				foreach (PropertyInfo prop in properties)
				{
					sheetData.InsertCellValue(x, y, new CellValue(prop.Name), CellValues.SharedString, headerStyleId);
					x++;
				}
				x = 1;
				y++;

				// Write data
				foreach (T item in data.Where(x => !x.ToNString().IsNullOrEmpty()))
				{
					cancellationToken.ThrowIfCancellationRequested();
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
		catch (OperationCanceledException)
		{
			throw new TaskCanceledException($"{nameof(Export)}.{nameof(ExportFromTable)} was canceled");
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"Error in {nameof(Export)}.{nameof(ExportFromTable)}");
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
				SheetData? sheetData = worksheet.GetFirstChild<SheetData>() ?? throw new ArgumentException("The worksheet does not contain sheetData, which is required for this operation.");

				uint headerStyleId = document.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
				uint bodyStyleId = document.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

				uint y = 1;
				uint x = 1;

				List<uint> skipColumns = [];
				foreach (DataColumn column in data.Columns)
				{
					if (skipColumnNames?.Contains(column.ColumnName, StringComparer.InvariantCultureIgnoreCase) != true)
					{
						sheetData.InsertCellValue(x, y, new(column.ColumnName), CellValues.SharedString, headerStyleId);
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
					cancellationToken.ThrowIfCancellationRequested();
					foreach (object? value in row.ItemArray)
					{
						if ((value != null) && !skipColumns.Contains(x))
						{
							sheetData.InsertCellValue(x, y, new(value.ToString() ?? string.Empty), CellValues.SharedString, bodyStyleId);
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
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"Error in {nameof(Export)}.{nameof(ExportFromTable)}");
			return false;
		}
	}
}
