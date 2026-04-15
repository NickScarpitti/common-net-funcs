using System.Data;
using System.Reflection;
using ClosedXML.Excel;
using CommonNetFuncs.Core;
using CommonNetFuncs.Excel.Common;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Excel.ClosedXml;

/// <summary>
/// Export data to an excel file using ClosedXML
/// </summary>
public static class Export
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	private const string TableNameLengthError = "Table name cannot be longer than 255 characters";
	private const string ErrorLocationTemplate = "{Class}.{Method} Error";

	/// <summary>
	/// Convert a list of data objects into a MemoryStream containing an excel file with a tabular representation of the data
	/// </summary>
	/// <typeparam name="T">Type of data inside of list to be exported</typeparam>
	/// <param name="dataList">Data to export as a table.</param>
	/// <param name="memoryStream">Output memory stream (will be created if one is not provided)</param>
	/// <param name="createTable">If <see langword="true"/>, will format the exported data into an Excel table.</param>
	/// <param name="sheetName">Name of the sheet to export data to</param>
	/// <param name="tableName">Name of the table in Excel when createTable is true</param>
	/// <param name="skipColumnNames">List of columns to not include in export</param>
	/// <param name="wrapText">Whether to wrap text in cells</param>
	/// <param name="tableStyle">Table style to use when createTable is true</param>
	/// <returns>MemoryStream containing an excel file with a tabular representation of dataList</returns>
	public static async Task<MemoryStream?> GenericExcelExport<T>(this IEnumerable<T> dataList, MemoryStream? memoryStream = null, bool createTable = false,
		string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false, ETableStyle tableStyle = ETableStyle.TableStyleMedium1, CancellationToken cancellationToken = default)
	{
		try
		{
			if (sheetName.IsNullOrWhiteSpace())
			{
				sheetName = "Data";
			}

			if (tableName.IsNullOrWhiteSpace())
			{
				tableName = "Data";
			}

			if (sheetName.Length > 31)
			{
				throw new ArgumentOutOfRangeException(nameof(sheetName), "Sheet name cannot be longer than 31 characters");
			}

			if (tableName.Length > 255)
			{
				throw new ArgumentOutOfRangeException(nameof(tableName), TableNameLengthError);
			}

			memoryStream ??= new();

			using XLWorkbook wb = new();
			IXLWorksheet ws = wb.AddWorksheet(sheetName);
			if (!dataList.ExcelExport(wb, ws, createTable, tableName, skipColumnNames, wrapText, tableStyle, cancellationToken))
			{
				return null;
			}

			await memoryStream.WriteFileToMemoryStreamAsync(wb, cancellationToken).ConfigureAwait(false);

			return memoryStream;
		}
		catch (OperationCanceledException)
		{
			throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExport)} was canceled");
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, nameof(Export), nameof(GenericExcelExport));
		}

		return new();
	}

	/// <summary>
	/// Convert a DataTable into a MemoryStream containing an excel file with a tabular representation of the data
	/// </summary>
	/// <param name="datatable">Data to export as a table.</param>
	/// <param name="memoryStream">Output memory stream (will be created if one is not provided)</param>
	/// <param name="createTable">If <see langword="true"/>, will format the exported data into an Excel table.</param>
	/// <param name="sheetName">Name of the sheet to export data to</param>
	/// <param name="tableName">Name of the table in Excel when createTable is true</param>
	/// <param name="skipColumnNames">List of columns to not include in export</param>
	/// <param name="wrapText">Whether to wrap text in cells</param>
	/// <param name="tableStyle">Table style to use when createTable is true</param>
	/// <returns>MemoryStream containing an excel file with a tabular representation of datatable</returns>
	public static async Task<MemoryStream?> GenericExcelExport(this DataTable datatable, MemoryStream? memoryStream = null, bool createTable = false,
		string sheetName = "Data", string tableName = "Data", List<string>? skipColumnNames = null, bool wrapText = false, ETableStyle tableStyle = ETableStyle.TableStyleMedium1, CancellationToken cancellationToken = default)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(sheetName))
			{
				sheetName = "Data";
			}

			if (string.IsNullOrWhiteSpace(tableName))
			{
				tableName = "Data";
			}

			if (sheetName.Length > 31)
			{
				throw new ArgumentOutOfRangeException(nameof(sheetName), "Sheet name cannot be longer than 31 characters");
			}

			if (tableName.Length > 255)
			{
				throw new ArgumentOutOfRangeException(nameof(tableName), TableNameLengthError);
			}

			memoryStream ??= new();

			using XLWorkbook wb = new();
			IXLWorksheet ws = wb.AddWorksheet(sheetName);
			if (!datatable.ExcelExport(wb, ws, createTable, tableName, skipColumnNames, wrapText, tableStyle, cancellationToken))
			{
				return null;
			}

			await memoryStream.WriteFileToMemoryStreamAsync(wb, cancellationToken).ConfigureAwait(false);

			return memoryStream;
		}
		catch (OperationCanceledException)
		{
			throw new TaskCanceledException($"{nameof(Export)}.{nameof(GenericExcelExport)} was canceled");
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, nameof(Export), nameof(GenericExcelExport));
		}

		return new();
	}

	/// <summary>
	/// Add data from a typed list to a new sheet in an existing workbook
	/// </summary>
	/// <typeparam name="T">Type of data inside of list to be exported</typeparam>
	/// <param name="wb">Workbook to add the sheet to</param>
	/// <param name="data">Data to insert into workbook</param>
	/// <param name="sheetName">Name of sheet to add data into</param>
	/// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
	/// <param name="tableName">Name of the table in Excel</param>
	/// <param name="skipColumnNames">List of columns to not include in export</param>
	/// <param name="wrapText">Whether to wrap text in cells</param>
	/// <param name="tableStyle">Table style to use when createTable is true</param>
	/// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
	public static bool AddGenericTable<T>(this IXLWorkbook wb, IEnumerable<T> data, string sheetName, bool createTable = false, string tableName = "Data",
		List<string>? skipColumnNames = null, bool wrapText = false, ETableStyle tableStyle = ETableStyle.TableStyleMedium1)
	{
		return wb.AddGenericTableInternal<T>(data, typeof(IEnumerable<T>), sheetName, createTable, tableName, skipColumnNames, wrapText, tableStyle);
	}

	/// <summary>
	/// Add data from a DataTable to a new sheet in an existing workbook
	/// </summary>
	/// <param name="wb">Workbook to add the sheet to</param>
	/// <param name="data">Data to insert into workbook</param>
	/// <param name="sheetName">Name of sheet to add data into</param>
	/// <param name="createTable">If <see langword="true"/>, will format the inserted data into an Excel table.</param>
	/// <param name="tableName">Name of the table in Excel</param>
	/// <param name="skipColumnNames">List of columns to not include in export</param>
	/// <param name="wrapText">Whether to wrap text in cells</param>
	/// <param name="tableStyle">Table style to use when createTable is true</param>
	/// <returns><see langword="true"/> if data was successfully added to the workbook</returns>
	public static bool AddGenericTable(this IXLWorkbook wb, DataTable data, string sheetName, bool createTable = false, string tableName = "Data",
		List<string>? skipColumnNames = null, bool wrapText = false, ETableStyle tableStyle = ETableStyle.TableStyleMedium1)
	{
		return wb.AddGenericTableInternal<char>(data, typeof(DataTable), sheetName, createTable, tableName, skipColumnNames, wrapText, tableStyle);
	}

	/// <summary>
	/// Internal helper that adds a new sheet with data to an IXLWorkbook, handling duplicate sheet names automatically.
	/// </summary>
	private static bool AddGenericTableInternal<T>(this IXLWorkbook wb, object? data, Type dataType, string sheetName, bool createTable = false, string tableName = "Data",
		List<string>? skipColumnNames = null, bool wrapText = false, ETableStyle tableStyle = ETableStyle.TableStyleMedium1)
	{
		bool success = false;
		try
		{
			if (string.IsNullOrWhiteSpace(sheetName))
			{
				sheetName = "Data";
			}

			if (string.IsNullOrWhiteSpace(tableName))
			{
				tableName = "Data";
			}

			if (sheetName.Length > 31)
			{
				throw new ArgumentOutOfRangeException(nameof(sheetName), "Sheet name cannot be longer than 31 characters");
			}

			if (tableName.Length > 255)
			{
				throw new ArgumentOutOfRangeException(nameof(tableName), TableNameLengthError);
			}

			int i = 1;
			string actualSheetName = sheetName;
			while (wb.Worksheets.Any(ws => string.Equals(ws.Name, actualSheetName, StringComparison.OrdinalIgnoreCase)))
			{
				actualSheetName = $"{sheetName} ({i})";
				i++;
			}

			IXLWorksheet ws = wb.AddWorksheet(actualSheetName);
			if (data != null)
			{
				if (dataType == typeof(IEnumerable<T>))
				{
					success = ((IEnumerable<T>)data).ExcelExport(wb, ws, createTable, tableName, skipColumnNames, wrapText, tableStyle);
				}
				else if (dataType == typeof(DataTable))
				{
					success = ((DataTable)data).ExcelExport(wb, ws, createTable, tableName, skipColumnNames, wrapText, tableStyle);
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, nameof(Export), nameof(AddGenericTableInternal));
		}
		return success;
	}

	/// <summary>
	/// Generates a simple excel file containing the passed in data in a tabular format
	/// </summary>
	/// <typeparam name="T">Type of data inside of list to be inserted into the workbook</typeparam>
	/// <param name="data">Data to be inserted into the workbook</param>
	/// <param name="wb">Workbook to insert the data into</param>
	/// <param name="ws">Worksheet to insert the data into</param>
	/// <param name="createTable">Turn the output into an Excel table.</param>
	/// <param name="tableName">Name of the table when createTable is true</param>
	/// <param name="skipColumnNames">List of columns to not include in export</param>
	/// <param name="wrapText">Whether to wrap text in cells</param>
	/// <param name="tableStyle">Table style to use when createTable is true</param>
	/// <returns><see langword="true"/> if excel file was created successfully</returns>
	public static bool ExcelExport<T>(this IEnumerable<T> data, IXLWorkbook wb, IXLWorksheet ws, bool createTable = false, string tableName = "Data",
		List<string>? skipColumnNames = null, bool wrapText = false, ETableStyle tableStyle = ETableStyle.TableStyleMedium1, CancellationToken cancellationToken = default)
	{
		skipColumnNames ??= [];
		try
		{
			if (string.IsNullOrWhiteSpace(tableName))
			{
				tableName = "Data";
			}

			if (tableName.Length > 255)
			{
				throw new ArgumentOutOfRangeException(nameof(tableName), TableNameLengthError);
			}

			if (data?.Any() == true)
			{
				IXLStyle? headerStyle = wb.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
				IXLStyle? bodyStyle = wb.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

				int x = 1;
				int y = 1;

				PropertyInfo[] props = GetOrAddPropertiesFromReflectionCache(typeof(T))
					.Where(p => skipColumnNames.Count == 0 || !skipColumnNames.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase))
					.ToArray();

				List<string> columnNames = [];

				foreach (string propName in props.Select(x => x.Name))
				{
					IXLCell cell = ws.Cell(y, x);
					cell.Value = propName;
					columnNames.Add(propName);
					if (!createTable)
					{
						if (headerStyle != null)
						{
							cell.Style = headerStyle;
						}
					}
					else
					{
						// Use body style for header row - the table style will apply its own header formatting
						if (bodyStyle != null) { cell.Style = bodyStyle; }
					}
					x++;
				}
				x = 1;
				y++;

				foreach (T item in data)
				{
					if (item.ToNString().IsNullOrEmpty())
					{
						continue;
					}

					cancellationToken.ThrowIfCancellationRequested();
					foreach (PropertyInfo prop in props)
					{
						object value = prop.GetValue(item) ?? string.Empty;
						IXLCell cell = ws.Cell(y, x);
						cell.Value = value.ToString();
						if (bodyStyle != null) { cell.Style = bodyStyle; }
						x++;
					}
					x = 1;
					y++;
				}

				if (!createTable)
				{
					ws.Range(1, 1, 1, props.Length).SetAutoFilter();
				}
				else
				{
					IXLTable table = ws.Range(1, 1, y - 1, props.Length).CreateTable();
					table.ShowTotalsRow = false;
					table.ShowRowStripes = true;
					table.Theme = XLTableTheme.FromName(tableStyle.ToString());
					table.ShowAutoFilter = true;
					table.Name = tableName;
				}

				try
				{
					for (int i = 1; i <= props.Length; i++)
					{
						ws.Column(i).AdjustToContents();
					}
				}
				catch (Exception ex)
				{
					logger.Error(ex, "Error using ClosedXML AdjustToContents in {Class}.{Method}", nameof(Export), nameof(ExcelExport));
					logger.Warn("libgdiplus library required to use ClosedXML AdjustToContents method");
				}
			}
			return true;
		}
		catch (OperationCanceledException)
		{
			throw new TaskCanceledException($"{nameof(Export)}.{nameof(ExcelExport)} was canceled");
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, nameof(Export), nameof(ExcelExport));
			return false;
		}
	}

	/// <summary>
	/// Generates a simple excel file containing the passed in DataTable data in a tabular format
	/// </summary>
	/// <param name="data">Data as DataTable to be inserted into the workbook</param>
	/// <param name="wb">Workbook to insert the data into</param>
	/// <param name="ws">Worksheet to insert the data into</param>
	/// <param name="createTable">Turn the output into an Excel table.</param>
	/// <param name="tableName">Name of the table when createTable is true</param>
	/// <param name="skipColumnNames">List of columns to not include in export</param>
	/// <param name="wrapText">Whether to wrap text in cells</param>
	/// <param name="tableStyle">Table style to use when createTable is true</param>
	/// <returns><see langword="true"/> if excel file was created successfully</returns>
	public static bool ExcelExport(this DataTable data, IXLWorkbook wb, IXLWorksheet ws, bool createTable = false, string tableName = "Data",
		List<string>? skipColumnNames = null, bool wrapText = false, ETableStyle tableStyle = ETableStyle.TableStyleMedium1, CancellationToken cancellationToken = default)
	{
		skipColumnNames ??= [];
		try
		{
			if (string.IsNullOrWhiteSpace(tableName))
			{
				tableName = "Data";
			}

			if (tableName.Length > 255)
			{
				throw new ArgumentOutOfRangeException(nameof(tableName), TableNameLengthError);
			}

			if (data?.Rows.Count > 0)
			{
				IXLStyle? headerStyle = wb.GetStandardCellStyle(EStyle.Header, wrapText: wrapText);
				IXLStyle? bodyStyle = wb.GetStandardCellStyle(EStyle.Body, wrapText: wrapText);

				int x = 1;
				int y = 1;

				HashSet<int> skipColumns = [];
				List<string> columnNames = [];
				int colNum = 1;

				foreach (DataColumn column in data.Columns)
				{
					if (!skipColumnNames.Contains(column.ColumnName, StringComparer.InvariantCultureIgnoreCase))
					{
						IXLCell cell = ws.Cell(y, x);
						cell.Value = column.ColumnName;
						columnNames.Add(column.ColumnName);
						if (!createTable)
						{
							if (headerStyle != null) { cell.Style = headerStyle; }
						}
						else
						{
							if (bodyStyle != null) { cell.Style = bodyStyle; }
						}
						x++;
					}
					else
					{
						skipColumns.Add(colNum);
					}
					colNum++;
				}

				x = 1;
				y++;

				foreach (DataRow row in data.Rows)
				{
					cancellationToken.ThrowIfCancellationRequested();
					int rawColIndex = 1;
					foreach (object? value in row.ItemArray)
					{
						if (!skipColumns.Contains(rawColIndex))
						{
							IXLCell cell = ws.Cell(y, x);
							cell.Value = value?.ToString() ?? string.Empty;
							if (bodyStyle != null) { cell.Style = bodyStyle; }
							x++;
						}
						rawColIndex++;
					}
					x = 1;
					y++;
				}

				int totalCols = columnNames.Count;

				if (!createTable)
				{
					ws.Range(1, 1, 1, totalCols).SetAutoFilter();
				}
				else
				{
					IXLTable table = ws.Range(1, 1, y - 1, totalCols).CreateTable();
					table.ShowTotalsRow = false;
					table.ShowRowStripes = true;
					table.Theme = XLTableTheme.FromName(tableStyle.ToString());
					table.ShowAutoFilter = true;
					table.Name = tableName;
				}

				try
				{
					for (int i = 1; i <= totalCols; i++)
					{
						ws.Column(i).AdjustToContents();
					}
				}
				catch (Exception ex)
				{
					logger.Error(ex, "Error using ClosedXML AdjustToContents in {Class}.{Method}", nameof(Export), nameof(ExcelExport));
					logger.Warn("libgdiplus library required to use ClosedXML AdjustToContents method");
				}
			}
			return true;
		}
		catch (OperationCanceledException)
		{
			throw new TaskCanceledException($"{nameof(Export)}.{nameof(ExcelExport)} was canceled");
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, nameof(Export), nameof(ExcelExport));
			return false;
		}
	}

	/// <summary>
	/// Generates a simple excel file containing the passed in data in a tabular format.
	/// This overload is kept for backwards compatibility. Prefer using ExcelExport overloads instead.
	/// </summary>
	/// <typeparam name="T">Object to transform into a table</typeparam>
	/// <param name="wb">IXLWorkbook object to place data into</param>
	/// <param name="ws">IXLWorksheet object to place data into</param>
	/// <param name="data">Data to be exported</param>
	/// <param name="createTable">Make the exported data into an Excel table.</param>
	/// <returns><see langword="true"/> if excel file was created successfully</returns>
	public static bool ExportFromTable<T>(IXLWorkbook wb, IXLWorksheet ws, IEnumerable<T>? data, bool createTable = false, bool wrapText = false, CancellationToken cancellationToken = default) where T : class
	{
		try
		{
			return data?.ExcelExport(wb, ws, createTable, wrapText: wrapText, cancellationToken: cancellationToken) ?? true;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
	}

	/// <summary>
	/// Generates a simple excel file containing the passed in data in a tabular format.
	/// This overload is kept for backwards compatibility. Prefer using ExcelExport overloads instead.
	/// </summary>
	/// <param name="wb">IXLWorkbook object to place data into</param>
	/// <param name="ws">IXLWorksheet object to place data into</param>
	/// <param name="data">Data to be exported</param>
	/// <param name="createTable">Make the exported data into an Excel table.</param>
	/// <returns><see langword="true"/> if excel file was created successfully</returns>
	public static bool ExportFromTable(IXLWorkbook wb, IXLWorksheet ws, DataTable? data, bool createTable = false, bool wrapText = false, CancellationToken cancellationToken = default)
	{
		try
		{
			return data?.ExcelExport(wb, ws, createTable, wrapText: wrapText, cancellationToken: cancellationToken) ?? true;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
	}
}
