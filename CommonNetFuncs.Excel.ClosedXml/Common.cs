using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using CommonNetFuncs.Excel.Common;
using SixLabors.ImageSharp;

namespace CommonNetFuncs.Excel.ClosedXml;

/// <summary>
/// Encapsulates border styles for ClosedXML cells, mirroring the NPOI NpoiBorderStyles class.
/// </summary>
public sealed class ClosedXmlBorderStyles
{
	public ClosedXmlBorderStyles(IXLStyle? cellStyle)
	{
		if (cellStyle != null)
		{
			ExtractBorderStyles(cellStyle);
		}
	}

	public ClosedXmlBorderStyles(XLBorderStyleValues? borderTop = null, XLBorderStyleValues? borderLeft = null, XLBorderStyleValues? borderRight = null, XLBorderStyleValues? borderBottom = null,
		XLColor? borderTopColor = null, XLColor? borderLeftColor = null, XLColor? borderRightColor = null, XLColor? borderBottomColor = null)
	{
		BorderTop = borderTop;
		BorderLeft = borderLeft;
		BorderRight = borderRight;
		BorderBottom = borderBottom;

		BorderTopColor = borderTopColor;
		BorderLeftColor = borderLeftColor;
		BorderRightColor = borderRightColor;
		BorderBottomColor = borderBottomColor;
	}

	public XLBorderStyleValues? BorderTop { get; set; }

	public XLBorderStyleValues? BorderLeft { get; set; }

	public XLBorderStyleValues? BorderRight { get; set; }

	public XLBorderStyleValues? BorderBottom { get; set; }

	public XLColor? BorderTopColor { get; set; }

	public XLColor? BorderLeftColor { get; set; }

	public XLColor? BorderRightColor { get; set; }

	public XLColor? BorderBottomColor { get; set; }

	public void ExtractBorderStyles(IXLStyle cellStyle)
	{
		BorderTop = cellStyle.Border.TopBorder;
		BorderLeft = cellStyle.Border.LeftBorder;
		BorderRight = cellStyle.Border.RightBorder;
		BorderBottom = cellStyle.Border.BottomBorder;

		BorderTopColor = cellStyle.Border.TopBorderColor;
		BorderLeftColor = cellStyle.Border.LeftBorderColor;
		BorderRightColor = cellStyle.Border.RightBorderColor;
		BorderBottomColor = cellStyle.Border.BottomBorderColor;
	}
}

/// <summary>
/// Methods to make reading and writing to an excel file easier using ClosedXML
/// </summary>
public static class Common
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	private const string ErrorLocationFormat = "{Class}.{Method} Error";

	/// <summary>
	/// Checks if cell is empty
	/// </summary>
	/// <param name="cell">Cell to check if empty</param>
	/// <returns><see langword="true"/> if cell is empty</returns>
	public static bool IsCellEmpty(this IXLCell cell)
	{
		return string.IsNullOrWhiteSpace(cell.Value.ToString());
	}

	/// <summary>
	/// Gets the string value of a cell, returning null if the cell is null
	/// </summary>
	/// <param name="cell">Cell to get the string value from</param>
	/// <returns>String representation of the value in the cell</returns>
	[return: NotNullIfNotNull(nameof(cell))]
	public static string? GetStringValue(this IXLCell? cell)
	{
		if (cell == null)
		{
			return null;
		}

		return cell.DataType == XLDataType.Blank ? string.Empty : cell.Value.ToString();
	}

	/// <summary>
	/// Get IXLCell offset from a cell reference in A1 notation
	/// </summary>
	/// <param name="ws">Worksheet that cell is in</param>
	/// <param name="cellReference">Cell reference in A1 notation (e.g. "B3"). If a range is provided, the top-left cell is used.</param>
	/// <param name="colOffset">X axis offset from the named cell reference</param>
	/// <param name="rowOffset">Y axis offset from the named cell reference</param>
	/// <returns>IXLCell object at the specified offset from the named cell reference</returns>
	public static IXLCell? GetCellFromReference(this IXLWorksheet ws, string cellReference, int colOffset = 0, int rowOffset = 0)
	{
		try
		{
			// Parse the top-left cell of a potential range (e.g. "A1:B2" -> "A1")
			string singleRef = cellReference.Contains(':') ? cellReference.Split(':')[0] : cellReference;
			IXLCell refCell = ws.Cell(singleRef);
			return ws.Cell(refCell.Address.RowNumber + rowOffset, refCell.Address.ColumnNumber + colOffset);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationFormat, nameof(Common), nameof(GetCellFromReference));
			return null;
		}
	}

	/// <summary>
	/// Get IXLCell offset from the startCell
	/// </summary>
	/// <param name="startCell">Cell to get offset from</param>
	/// <param name="colOffset">X axis offset from the start cell</param>
	/// <param name="rowOffset">Y axis offset from the start cell</param>
	/// <returns>IXLCell object at the specified offset from startCell</returns>
	public static IXLCell? GetCellOffset(this IXLCell startCell, int colOffset = 0, int rowOffset = 0)
	{
		try
		{
			return startCell.Worksheet.Cell(startCell.Address.RowNumber + rowOffset, startCell.Address.ColumnNumber + colOffset);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationFormat, nameof(Common), nameof(GetCellOffset));
			return null;
		}
	}

	/// <summary>
	/// Get IXLCell at the given 1-based column and row coordinates plus optional offsets
	/// </summary>
	/// <param name="ws">Worksheet that cell is in</param>
	/// <param name="colIndex">1 based X coordinate of starting cell</param>
	/// <param name="rowIndex">1 based Y coordinate of starting cell</param>
	/// <param name="colOffset">X axis offset from the cell reference</param>
	/// <param name="rowOffset">Y axis offset from the cell reference</param>
	/// <returns>IXLCell object at the specified coordinates with offset applied</returns>
	public static IXLCell? GetCellFromCoordinates(this IXLWorksheet ws, int colIndex, int rowIndex, int colOffset = 0, int rowOffset = 0)
	{
		try
		{
			return ws.Cell(rowIndex + rowOffset, colIndex + colOffset);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationFormat, nameof(Common), nameof(GetCellFromCoordinates));
			return null;
		}
	}

	/// <summary>
	/// Gets the 1 based index of the last row with a non-blank value in the specified column
	/// </summary>
	/// <param name="ws">Worksheet that contains the column to get the last populated row from</param>
	/// <param name="colIndex">1 based index of the column to find the last populated row in</param>
	/// <returns>1 based index of the last row with a non-blank value, or 0 if no populated rows found</returns>
	public static int GetLastPopulatedRowInColumn(this IXLWorksheet ws, int colIndex)
	{
		int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
		// Iterate backwards through rows to find the last populated row (faster on large sheets)
		for (int i = lastRow; i >= 1; i--)
		{
			IXLCell cell = ws.Cell(i, colIndex);
			if (!cell.IsEmpty())
			{
				return i;
			}
		}
		return 0;
	}

	/// <summary>
	/// Gets the 1 based index of the last row with a non-blank value in the specified column
	/// </summary>
	/// <param name="ws">Worksheet that contains the column to get the last populated row from</param>
	/// <param name="colName">Column name (e.g. "A", "B") of the column to find the last populated row in</param>
	/// <returns>1 based index of the last row with a non-blank value, or 0 if no populated rows found</returns>
	public static int GetLastPopulatedRowInColumn(this IXLWorksheet ws, string colName)
	{
		return ws.GetLastPopulatedRowInColumn(XLHelper.GetColumnNumberFromLetter(colName));
	}

	/// <summary>
	/// Get IXLCell offset from the cell with named range cellName
	/// </summary>
	/// <param name="wb">Workbook that cell is in</param>
	/// <param name="cellName">Name of cell/range being looked for</param>
	/// <param name="colOffset">X axis offset from the named cell reference</param>
	/// <param name="rowOffset">Y axis offset from the named cell reference</param>
	/// <returns>IXLCell object at the specified offset of the cell with named reference cellName</returns>
	public static IXLCell? GetCellFromName(this IXLWorkbook wb, string cellName, int colOffset = 0, int rowOffset = 0)
	{
		try
		{
			IXLDefinedName? name = wb.DefinedName(cellName);
			if (name == null)
			{
				logger.Warn("Unable to locate cell with name {cellName}", cellName);
				return null;
			}

			IXLCell? topLeftCell = null;
			foreach (IXLRange range in name.Ranges)
			{
				IXLCell candidate = range.FirstCell();
				if (topLeftCell == null ||
					candidate.Address.RowNumber < topLeftCell.Address.RowNumber ||
					(candidate.Address.RowNumber == topLeftCell.Address.RowNumber && candidate.Address.ColumnNumber < topLeftCell.Address.ColumnNumber))
				{
					topLeftCell = candidate;
				}
			}

			if (topLeftCell == null)
			{
				return null;
			}

			return topLeftCell.Worksheet.Cell(topLeftCell.Address.RowNumber + rowOffset, topLeftCell.Address.ColumnNumber + colOffset);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationFormat, nameof(Common), nameof(GetCellFromName));
			return null;
		}
	}

	/// <summary>
	/// Clear contents from cells covered by the named range cellName
	/// </summary>
	/// <param name="wb">Workbook that cell is in</param>
	/// <param name="cellName">Name of the range to clear contents from</param>
	public static void ClearAllFromName(this IXLWorkbook wb, string cellName)
	{
		try
		{
			IXLDefinedName? name = wb.DefinedName(cellName);
			if (name == null)
			{
				logger.Warn("Unable to locate cell with name {cellName}", cellName);
				return;
			}

			foreach (IXLRange range in name.Ranges)
			{
				range.Clear();
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationFormat, nameof(Common), nameof(ClearAllFromName));
		}
	}

	/// <summary>
	/// Gets the merged cell range that contains the specified cell, or a single-cell range if not merged
	/// </summary>
	/// <param name="cell">Cell to get the range of</param>
	/// <returns>IXLRange representing the merged region, or a single-cell range if not merged</returns>
	public static IXLRange? GetRangeOfMergedCells(this IXLCell? cell)
	{
		if (cell == null)
		{
			return null;
		}

		if (cell.IsMerged())
		{
			return cell.MergedRange();
		}

		return cell.AsRange();
	}

	/// <summary>
	/// Get cells contained within a range
	/// </summary>
	/// <param name="ws">Worksheet to get range from</param>
	/// <param name="range">String cell/range reference in A1 notation (e.g. "A1:C3")</param>
	/// <returns>2D array of cells contained within the range specified</returns>
	public static IXLCell[,] GetRange(this IXLWorksheet ws, string range)
	{
		IXLRange xlRange = ws.Range(range);
		int rowCount = xlRange.RowCount();
		int colCount = xlRange.ColumnCount();
		IXLCell[,] cells = new IXLCell[rowCount, colCount];

		for (int r = 0; r < rowCount; r++)
		{
			for (int c = 0; c < colCount; c++)
			{
				cells[r, c] = xlRange.Cell(r + 1, c + 1);
			}
		}

		return cells;
	}

	/// <summary>
	/// Adds list validation (dropdown) to all cells specified by the range
	/// </summary>
	/// <param name="ws">IXLWorksheet object to add data validation to</param>
	/// <param name="range">Range address string to apply validation to (e.g. "B2:B100")</param>
	/// <param name="options">Options to be used as the valid choices in the drop down</param>
	public static void AddDataValidation(this IXLWorksheet ws, string range, List<string> options)
	{
		IXLDataValidation validation = ws.Range(range).CreateDataValidation();
		validation.List(string.Join(",", options.Select(o => $"\"{o}\"")), true);
		validation.ErrorStyle = XLErrorStyle.Stop;
		validation.ErrorTitle = "InvalidValue";
		validation.ErrorMessage = "Selected value must be in list";
		validation.ShowErrorMessage = true;
		validation.ShowInputMessage = false;
	}

	/// <summary>
	/// Adds list validation (dropdown) to all cells specified by the range
	/// </summary>
	/// <param name="ws">IXLWorksheet object to add data validation to</param>
	/// <param name="range">IXLRange to apply validation to</param>
	/// <param name="options">Options to be used as the valid choices in the drop down</param>
	public static void AddDataValidation(this IXLWorksheet ws, IXLRange range, List<string> options)
	{
		IXLDataValidation validation = range.CreateDataValidation();
		validation.List(string.Join(",", options.Select(o => $"\"{o}\"")), true);
		validation.ErrorStyle = XLErrorStyle.Stop;
		validation.ErrorTitle = "InvalidValue";
		validation.ErrorMessage = "Selected value must be in list";
		validation.ShowErrorMessage = true;
		validation.ShowInputMessage = false;
	}

	/// <summary>
	/// Reads tabular data from an unformatted Excel sheet to a DataTable object
	/// </summary>
	/// <param name="fileStream">Stream of Excel file being read</param>
	/// <param name="hasHeaders">Whether the data being read has headers. Used for column names instead of default 'Column0', 'Column1'... if true.</param>
	/// <param name="sheetName">Name of sheet to read data from. Will use lowest index sheet if not specified.</param>
	/// <param name="startCellReference">Top left corner containing data to read in A1 notation. Will use A1 if not specified.</param>
	/// <param name="endCellReference">Bottom right cell containing data to read in A1 notation. Will read to first full empty row if not specified.</param>
	/// <returns><see cref="DataTable"/> representation of the data read from the excel file</returns>
	public static DataTable ReadExcelFileToDataTable(this Stream fileStream, bool hasHeaders = true, string? sheetName = null, string? startCellReference = null, string? endCellReference = null, CancellationToken cancellationToken = default)
	{
		DataTable dataTable = new();

		try
		{
			fileStream.Position = 0;
			using XLWorkbook wb = new(fileStream);

			IXLWorksheet? ws = null;
			if (!string.IsNullOrWhiteSpace(sheetName))
			{
				ws = wb.Worksheet(sheetName);
			}
			else
			{
				ws = wb.Worksheets.FirstOrDefault();
			}

			if (ws == null)
			{
				return dataTable;
			}

			if (string.IsNullOrWhiteSpace(startCellReference))
			{
				startCellReference = "A1";
			}

			IXLCell startCell = ws.Cell(startCellReference);
			int startColIndex = startCell.Address.ColumnNumber;
			int startRowIndex = startCell.Address.RowNumber;
			int? endColIndex = null;
			int? endRowIndex = null;

			if (!string.IsNullOrWhiteSpace(endCellReference))
			{
				IXLCell endCell = ws.Cell(endCellReference);
				endColIndex = endCell.Address.ColumnNumber;
				endRowIndex = endCell.Address.RowNumber;
			}

			// Determine columns
			if (endColIndex == null)
			{
				int col = startColIndex;
				while (!ws.Cell(startRowIndex, col).IsEmpty())
				{
					col++;
				}
				endColIndex = col - 1;
			}

			// Add headers / column definitions
			if (hasHeaders)
			{
				for (int colIndex = startColIndex; colIndex <= endColIndex; colIndex++)
				{
					dataTable.Columns.Add(ws.Cell(startRowIndex, colIndex).GetStringValue());
				}
			}
			else
			{
				for (int colIndex = startColIndex; colIndex <= endColIndex; colIndex++)
				{
					dataTable.Columns.Add($"Column{colIndex - startColIndex}");
				}
			}

			// Add rows
			if (dataTable.Columns.Count > 0)
			{
				int dataStartRow = startRowIndex + (hasHeaders ? 1 : 0);

				if (endRowIndex != null)
				{
					for (int rowIndex = dataStartRow; rowIndex <= endRowIndex; rowIndex++)
					{
						cancellationToken.ThrowIfCancellationRequested();
						string?[] rowData = new string?[(int)endColIndex - startColIndex + 1];
						for (int colIndex = startColIndex; colIndex <= endColIndex; colIndex++)
						{
							rowData[colIndex - startColIndex] = ws.Cell(rowIndex, colIndex).GetStringValue();
						}
						dataTable.Rows.Add(rowData);
					}
				}
				else
				{
					int rowIndex = dataStartRow;
					while (true)
					{
						cancellationToken.ThrowIfCancellationRequested();
						IXLRow? row = ws.Row(rowIndex);
						if (row == null || row.IsEmpty())
						{
							break;
						}

						string?[] rowData = new string?[(int)endColIndex - startColIndex + 1];
						bool rowHasData = false;
						for (int colIndex = startColIndex; colIndex <= endColIndex; colIndex++)
						{
							string? cellValue = ws.Cell(rowIndex, colIndex).GetStringValue();
							rowData[colIndex - startColIndex] = cellValue;
							if (!string.IsNullOrWhiteSpace(cellValue))
							{
								rowHasData = true;
							}
						}

						if (!rowHasData)
						{
							break;
						}

						dataTable.Rows.Add(rowData);
						rowIndex++;
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Unable to read excel data. Location: {Class}.{Method}", nameof(Common), nameof(ReadExcelFileToDataTable));
		}

		return dataTable;
	}

	/// <summary>
	/// Reads an Excel table into a DataTable object
	/// </summary>
	/// <param name="fileStream">Stream of Excel file being read</param>
	/// <param name="tableName">Name of table to read. If not specified, reads the first table found in the workbook.</param>
	/// <returns><see cref="DataTable"/> object containing the data read from Excel stream</returns>
	public static DataTable ReadExcelTableToDataTable(this Stream fileStream, string? tableName = null, CancellationToken cancellationToken = default)
	{
		DataTable dataTable = new();

		try
		{
			fileStream.Position = 0;
			using XLWorkbook wb = new(fileStream);
			IXLTable? table = null;

			// ClosedXML tables are scoped per-worksheet; enumerate all sheets to find a matching table
			IEnumerable<IXLTable> allTables = wb.Worksheets.SelectMany(ws => ws.Tables);

			if (!string.IsNullOrWhiteSpace(tableName))
			{
				table = allTables.FirstOrDefault(t => string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));
			}

			// Get first table if not specified or not found
			if (table == null)
			{
				table = allTables.FirstOrDefault();
			}

			if (table != null)
			{
				// Get headers
				foreach (IXLTableField field in table.Fields)
				{
					dataTable.Columns.Add(field.Name);
				}

				// Get body data (skip header row)
				foreach (IXLTableRow row in table.DataRange.Rows())
				{
					cancellationToken.ThrowIfCancellationRequested();
					string?[] rowData = new string?[dataTable.Columns.Count];
					for (int i = 0; i < dataTable.Columns.Count; i++)
					{
						rowData[i] = row.Cell(i + 1).GetStringValue();
					}
					dataTable.Rows.Add(rowData);
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Unable to read excel table data. Location: {Class}.{Method}", nameof(Common), nameof(ReadExcelTableToDataTable));
		}

		return dataTable;
	}

	/// <summary>
	/// Converts a column letter name to its 1-based column number (e.g. "A" = 1, "Z" = 26, "AA" = 27)
	/// </summary>
	/// <param name="columnName">The column name to convert</param>
	/// <returns>The 1-based column number corresponding to the column name</returns>
	public static int ColumnNameToNumber(this string? columnName)
	{
		if (string.IsNullOrEmpty(columnName))
		{
			throw new ArgumentException("Column name cannot be null or empty.");
		}

		return XLHelper.GetColumnNumberFromLetter(columnName.ToUpperInvariant());
	}

	/// <summary>
	/// Get the column letter name corresponding to the provided 1-based column number (1 = A)
	/// </summary>
	/// <param name="columnNumber">1 based column number to get name of</param>
	/// <returns>Column name corresponding to the value of columnNumber</returns>
	public static string ColumnIndexToName(this int? columnNumber)
	{
		if (columnNumber is null or < 1)
		{
			throw new ArgumentException("Index cannot be null or less than 1.");
		}

		return XLHelper.GetColumnLetterFromNumber((int)columnNumber);
	}

	/// <summary>
	/// Get the column letter name corresponding to the provided 1-based column number (1 = A)
	/// </summary>
	/// <param name="columnNumber">1 based column number to get name of</param>
	/// <returns>Column name corresponding to the value of columnNumber</returns>
	public static string ColumnIndexToName(this int columnNumber)
	{
		if (columnNumber < 1)
		{
			throw new ArgumentException("Index cannot be less than 1.");
		}

		return XLHelper.GetColumnLetterFromNumber(columnNumber);
	}

	/// <summary>
	/// Get the width of a specified range in pixels. Uses a character-unit to pixel approximation (~7 px per character unit) for Calibri 11pt at 96 DPI.
	/// </summary>
	/// <param name="ws">Worksheet containing range to get width of</param>
	/// <param name="startCol">1 based index of the first column in range being measured</param>
	/// <param name="endCol">1 based index of the last column in range being measured</param>
	/// <returns>Width of the column range in pixels</returns>
	public static int GetRangeWidthInPx(this IXLWorksheet ws, int startCol, int endCol)
	{
		const double maxDigitWidthPx = 7.0; // character units to pixels at 96 DPI, Calibri 11pt
		if (startCol > endCol)
		{
			(endCol, startCol) = (startCol, endCol);
		}

		double totalWidth = 0;
		for (int i = startCol; i <= endCol; i++)
		{
			double columnWidth = ws.Column(i).Width * maxDigitWidthPx;
			if (columnWidth.Equals(0))
			{
				logger.Warn("Width of Column {Column} is 0! Check referenced excel sheet: {SheetName}", i, ws.Name);
			}
			totalWidth += columnWidth;
		}
		return (int)Math.Round(totalWidth, 0, MidpointRounding.ToZero);
	}

	/// <summary>
	/// Get the height of a specified range in pixels.
	/// </summary>
	/// <param name="ws">Worksheet containing range to get height of</param>
	/// <param name="startRow">1 based index of the first row in range being measured</param>
	/// <param name="endRow">1 based index of the last row in range being measured</param>
	/// <returns>Height of the row range in pixels</returns>
	public static int GetRangeHeightInPx(this IXLWorksheet ws, int startRow, int endRow)
	{
		if (startRow > endRow)
		{
			(endRow, startRow) = (startRow, endRow);
		}

		double totalHeight = 0;
		for (int i = startRow; i <= endRow; i++)
		{
			totalHeight += ws.Row(i).Height; // in points
		}
		return (int)Math.Round(totalHeight * 96.0 / 72.0, 0, MidpointRounding.ToZero); // points to px at 96 DPI
	}

	/// <summary>
	/// Create a table for the specified sheet in a workbook.
	/// </summary>
	/// <param name="wb">Workbook to add table to</param>
	/// <param name="sheetName">Name of the sheet to add the table to</param>
	/// <param name="tableName">Name of the table to add</param>
	/// <param name="firstColIndex">1 based index of the first column of the table</param>
	/// <param name="lastColIndex">1 based index of the last column of the table</param>
	/// <param name="firstRowIndex">1 based index of the first row of the table</param>
	/// <param name="lastRowIndex">1 based index of the last row of the table</param>
	/// <param name="columnNames">
	/// Optional: Ordered list of names for each column in the table. Will use Column# if not provided or list has fewer
	/// elements than there are columns in the table
	/// </param>
	/// <param name="tableStyle">Optional: Style to use for table, defaults to TableStyleMedium1</param>
	/// <param name="showRowStripes">Optional: Styles the table to show row stripes or not</param>
	/// <param name="showColStripes">Optional: Styles the table to show column stripes or not</param>
	public static void CreateTable(this IXLWorkbook wb, string sheetName, string tableName, int firstColIndex, int lastColIndex, int firstRowIndex, int lastRowIndex, List<string>? columnNames = null,
		ETableStyle tableStyle = ETableStyle.TableStyleMedium1, bool showRowStripes = true, bool showColStripes = false)
	{
		if (tableName.Length > 255)
		{
			throw new ArgumentOutOfRangeException(nameof(tableName), "Table name cannot be longer than 255 characters");
		}

		IXLWorksheet ws = wb.Worksheet(sheetName);

		// Set header row values if column names provided
		if (columnNames?.Count > 0)
		{
			for (int i = 0; i < lastColIndex - firstColIndex + 1; i++)
			{
				string cellValue = (columnNames.Count > i) ? columnNames[i] : $"Column{i + 1}";
				ws.Cell(firstRowIndex, firstColIndex + i).Value = cellValue;
			}
		}

		IXLRange range = ws.Range(firstRowIndex, firstColIndex, lastRowIndex, lastColIndex);
		IXLTable table = range.CreateTable();
		table.Name = tableName;
		table.Theme = XLTableTheme.FromName(tableStyle.ToString());
		table.ShowRowStripes = showRowStripes;
		table.ShowColumnStripes = showColStripes;
		table.ShowAutoFilter = true;
	}

	/// <summary>
	/// Adds an image into a workbook at the designated named range
	/// </summary>
	/// <param name="wb">Workbook to insert image into</param>
	/// <param name="imageData">Image byte array</param>
	/// <param name="cellName">Named range to insert image at</param>
	/// <param name="placement">Optional: Placement type defining the behavior of the inserted image</param>
	public static void AddImage(this IXLWorkbook wb, byte[] imageData, string cellName, XLPicturePlacement placement = XLPicturePlacement.MoveAndSize)
	{
		wb.AddImages([imageData], [cellName], placement);
	}

	/// <summary>
	/// Adds an image into a workbook at the designated range reference string
	/// </summary>
	/// <param name="wb">Workbook to insert image into</param>
	/// <param name="ws">Worksheet to insert image into</param>
	/// <param name="imageData">Image byte array</param>
	/// <param name="range">Range reference string (e.g. "B2:D5") to insert image at</param>
	/// <param name="placement">Optional: Placement type defining the behavior of the inserted image</param>
	public static void AddImage(this IXLWorkbook wb, IXLWorksheet ws, byte[] imageData, string range, XLPicturePlacement placement = XLPicturePlacement.MoveAndSize)
	{
		IXLRange? area = ws.GetCellFromReference(range)?.GetRangeOfMergedCells()
			?? throw new ArgumentException($"Unable to get range from reference {range}", nameof(range));
		wb.AddImages(ws, [imageData], [area], placement);
	}

	/// <summary>
	/// Adds an image into a workbook at the designated range
	/// </summary>
	/// <param name="wb">Workbook to insert image into</param>
	/// <param name="ws">Worksheet to insert image into</param>
	/// <param name="imageData">Image byte array</param>
	/// <param name="range">Range to insert image at</param>
	/// <param name="placement">Optional: Placement type defining the behavior of the inserted image</param>
	public static void AddImage(this IXLWorkbook wb, IXLWorksheet ws, byte[] imageData, IXLRange range, XLPicturePlacement placement = XLPicturePlacement.MoveAndSize)
	{
		wb.AddImages(ws, [imageData], [range], placement);
	}

	/// <summary>
	/// Adds an image into a workbook at the range occupied by the specified cell
	/// </summary>
	/// <param name="wb">Workbook to insert image into</param>
	/// <param name="ws">Worksheet to insert image into</param>
	/// <param name="imageData">Image byte array</param>
	/// <param name="cell">Cell (or merged cell) to insert image at</param>
	/// <param name="placement">Optional: Placement type defining the behavior of the inserted image</param>
	public static void AddImage(this IXLWorkbook wb, IXLWorksheet ws, byte[] imageData, IXLCell cell, XLPicturePlacement placement = XLPicturePlacement.MoveAndSize)
	{
		IXLRange? area = cell.GetRangeOfMergedCells()
			?? throw new ArgumentException($"Unable to get range from cell at {cell.Address}", nameof(cell));
		wb.AddImages(ws, [imageData], [area], placement);
	}

	/// <summary>
	/// Adds images into a workbook at the designated named ranges
	/// </summary>
	/// <param name="wb">Workbook to insert images into</param>
	/// <param name="imageData">List of image byte arrays. Must be equal in length to cellNames parameter</param>
	/// <param name="cellNames">List of named ranges to insert images at. Must be equal in length to imageData parameter</param>
	/// <param name="placement">Optional: Placement type defining the behavior of the inserted images</param>
	public static void AddImages(this IXLWorkbook wb, List<byte[]> imageData, List<string> cellNames, XLPicturePlacement placement = XLPicturePlacement.MoveAndSize)
	{
		if ((wb != null) && (imageData.Count > 0) && (cellNames.Count > 0) && (imageData.Count == cellNames.Count))
		{
			for (int i = 0; i < imageData.Count; i++)
			{
				if ((imageData[i].Length > 0) && (cellNames[i] != null))
				{
					IXLCell? cell = wb.GetCellFromName(cellNames[i]);
					IXLStyle? cellStyle = wb.GetStandardCellStyle(EStyle.ImageBackground, borderStyles: cell?.Style != null ? new(cell.Style) : null);

					IXLRange? area = cell?.GetRangeOfMergedCells();
					IXLWorksheet? ws = cell?.Worksheet;
					if ((ws != null) && (area != null))
					{
						if (cellStyle != null && cell != null)
						{
							cell.Style = cellStyle;
						}
						wb.AddPicture(ws, area, imageData[i], placement, cellStyle);
					}
				}
			}
		}
	}

	/// <summary>
	/// Adds images into a workbook at the designated ranges
	/// </summary>
	/// <param name="wb">Workbook to insert images into</param>
	/// <param name="ws">Worksheet to insert images into</param>
	/// <param name="imageData">List of image byte arrays. Must be equal in length to ranges parameter</param>
	/// <param name="ranges">List of ranges to insert images at. Must be equal in length to imageData parameter</param>
	/// <param name="placement">Optional: Placement type defining the behavior of the inserted images</param>
	public static void AddImages(this IXLWorkbook wb, IXLWorksheet ws, List<byte[]> imageData, List<IXLRange> ranges, XLPicturePlacement placement = XLPicturePlacement.MoveAndSize)
	{
		if ((wb != null) && (imageData.Count > 0) && (ranges.Count > 0) && (imageData.Count == ranges.Count))
		{
			IXLStyle? cellStyle = wb.GetStandardCellStyle(EStyle.ImageBackground);
			for (int i = 0; i < imageData.Count; i++)
			{
				if ((imageData[i].Length > 0) && (ranges[i] != null))
				{
					wb.AddPicture(ws, ranges[i], imageData[i], placement, cellStyle);
				}
			}
		}
	}

	/// <summary>
	/// Adds a picture element to the specified range with aspect-ratio-preserving centering
	/// </summary>
	/// <param name="wb">Workbook to insert image into</param>
	/// <param name="ws">Worksheet to insert image into</param>
	/// <param name="area">Range where the image is to be inserted</param>
	/// <param name="imageData">Byte array containing the image to be inserted</param>
	/// <param name="placement">Optional: Placement type defining the behavior of the inserted image</param>
	/// <param name="cellStyle">
	/// Optional: Cell style to use in cells where pasting image. Using the image background font is strongly
	/// recommended as it ensures proper measurements when sizing the picture
	/// </param>
	public static void AddPicture(this IXLWorkbook wb, IXLWorksheet ws, IXLRange area, byte[] imageData, XLPicturePlacement placement = XLPicturePlacement.MoveAndSize, IXLStyle? cellStyle = null)
	{
		IXLCell? cell = area.FirstCell();
		if (cell != null)
		{
			cell.Style = cellStyle ?? wb.GetStandardCellStyle(EStyle.ImageBackground) ?? ws.Style;

			using Image image = Image.Load(imageData);
			int imgWidth = image.Width;
			int imgHeight = image.Height;

			decimal imgAspect = ((decimal)imgWidth) / imgHeight;

			int firstColIndex = area.FirstCell().Address.ColumnNumber;
			int lastColIndex = area.LastCell().Address.ColumnNumber;
			int firstRowIndex = area.FirstCell().Address.RowNumber;
			int lastRowIndex = area.LastCell().Address.RowNumber;

			int rangeWidth = ws.GetRangeWidthInPx(firstColIndex, lastColIndex);
			int rangeHeight = ws.GetRangeHeightInPx(firstRowIndex, lastRowIndex);
			decimal rangeAspect = ((decimal)rangeWidth) / rangeHeight;

			decimal scale = (rangeAspect < imgAspect) ? ((rangeWidth - 3m) / imgWidth) : ((rangeHeight - 3m) / imgHeight);

			int resizeWidth = (int)Math.Round(imgWidth * scale, 0, MidpointRounding.ToZero);
			int resizeHeight = (int)Math.Round(imgHeight * scale, 0, MidpointRounding.ToZero);
			int xMargin = (int)Math.Round((rangeWidth - resizeWidth) / 2.0, 0, MidpointRounding.ToZero);
			int yMargin = (int)Math.Round((rangeHeight - resizeHeight) / 2.0, 0, MidpointRounding.ToZero);

			using MemoryStream ms = new(imageData);
			IXLPicture picture = ws.AddPicture(ms);
			picture.MoveTo(area.FirstCell(), xMargin, yMargin).WithSize(resizeWidth, resizeHeight);
			picture.Placement = placement;
		}
	}

	/// <summary>
	/// Writes an excel file to the specified path
	/// </summary>
	/// <param name="wb">Workbook to write to disk</param>
	/// <param name="path">Path to save the workbook to</param>
	/// <returns><see langword="true"/> if write was successful</returns>
	public static bool WriteExcelFile(IXLWorkbook wb, string path)
	{
		try
		{
			using (FileStream fs = new(path, FileMode.Create, FileAccess.Write))
			{
				wb.SaveAs(fs);
			}
			wb.Dispose();
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationFormat, nameof(Common), nameof(WriteExcelFile));
			return false;
		}
	}

	/// <summary>
	/// Gets the standard IXLStyle corresponding to the style enum passed in
	/// </summary>
	/// <param name="wb">Workbook to create the style from</param>
	/// <param name="style">Enum value indicating which style to create</param>
	/// <param name="cellLocked">Optional: Whether the cells with this style should be locked. Default = false</param>
	/// <param name="wrapText">Optional: Whether to wrap text in cells. Default = false</param>
	/// <param name="borderStyles">Optional: Border styling overrides</param>
	/// <returns>The IXLStyle that was created</returns>
	public static IXLStyle? GetStandardCellStyle(this IXLWorkbook wb, EStyle style, bool cellLocked = false, bool wrapText = false, ClosedXmlBorderStyles? borderStyles = null)
	{
		IXLStyle? cellStyle = CreateEmptyStyle();
		if (cellStyle == null)
		{
			return null;
		}

		switch (style)
		{
			case EStyle.Header:
				cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				cellStyle.Border.BottomBorder = borderStyles?.BorderBottom ?? XLBorderStyleValues.Thin;
				cellStyle.Border.LeftBorder = borderStyles?.BorderLeft ?? XLBorderStyleValues.Thin;
				cellStyle.Border.RightBorder = borderStyles?.BorderRight ?? XLBorderStyleValues.Thin;
				cellStyle.Border.TopBorder = borderStyles?.BorderTop ?? XLBorderStyleValues.Thin;
				cellStyle.Fill.BackgroundColor = XLColor.LightGray;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				cellStyle.Font = GetFont(EFont.Header, wb);
				break;

			case EStyle.HeaderThickTop:
				cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				cellStyle.Border.BottomBorder = borderStyles?.BorderBottom ?? XLBorderStyleValues.Thin;
				cellStyle.Border.LeftBorder = borderStyles?.BorderLeft ?? XLBorderStyleValues.Thin;
				cellStyle.Border.RightBorder = borderStyles?.BorderRight ?? XLBorderStyleValues.Thin;
				cellStyle.Border.TopBorder = borderStyles?.BorderTop ?? XLBorderStyleValues.Medium;
				cellStyle.Fill.BackgroundColor = XLColor.LightGray;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				cellStyle.Font = GetFont(EFont.Header, wb);
				break;

			case EStyle.Body:
				cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				cellStyle.Border.BottomBorder = borderStyles?.BorderBottom ?? XLBorderStyleValues.Thin;
				cellStyle.Border.LeftBorder = borderStyles?.BorderLeft ?? XLBorderStyleValues.Thin;
				cellStyle.Border.RightBorder = borderStyles?.BorderRight ?? XLBorderStyleValues.Thin;
				if (borderStyles?.BorderTop != null)
				{
					cellStyle.Border.TopBorder = (XLBorderStyleValues)borderStyles.BorderTop;
				}
				cellStyle.Fill.BackgroundColor = XLColor.NoColor;
				cellStyle.Font = GetFont(EFont.Default, wb);
				break;

			case EStyle.Error:
				cellStyle.Fill.BackgroundColor = XLColor.Red;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				ApplyOptionalBorderStyles(cellStyle, borderStyles);
				break;

			case EStyle.Blackout:
				cellStyle.Font.FontColor = XLColor.Black;
				cellStyle.Fill.BackgroundColor = XLColor.Black;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				ApplyOptionalBorderStyles(cellStyle, borderStyles);
				break;

			case EStyle.Whiteout:
				cellStyle.Font.FontColor = XLColor.White;
				cellStyle.Fill.BackgroundColor = XLColor.White;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				ApplyOptionalBorderStyles(cellStyle, borderStyles);
				break;

			case EStyle.ImageBackground:
				cellStyle.Font = GetFont(EFont.ImageBackground, wb);
				cellStyle.Fill.BackgroundColor = XLColor.NoColor;
				cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				cellStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
				ApplyOptionalBorderStyles(cellStyle, borderStyles);
				break;
		}

		if (borderStyles != null)
		{
			if (borderStyles.BorderBottomColor != null)
			{
				cellStyle.Border.BottomBorderColor = borderStyles.BorderBottomColor;
			}
			if (borderStyles.BorderLeftColor != null)
			{
				cellStyle.Border.LeftBorderColor = borderStyles.BorderLeftColor;
			}
			if (borderStyles.BorderRightColor != null)
			{
				cellStyle.Border.RightBorderColor = borderStyles.BorderRightColor;
			}
			if (borderStyles.BorderTopColor != null)
			{
				cellStyle.Border.TopBorderColor = borderStyles.BorderTopColor;
			}
		}

		cellStyle.Protection.Locked = cellLocked;
		cellStyle.Alignment.WrapText = wrapText;

		return cellStyle;
	}

	/// <summary>
	/// Get cell style based on enum EStyle options
	/// </summary>
	/// <param name="style">Cell style to retrieve</param>
	/// <param name="wb">Workbook to add cell style to</param>
	/// <param name="cellLocked">Whether or not to lock the cells this style applies to</param>
	/// <param name="wrapText">Whether to wrap text in cells</param>
	/// <param name="htmlColor">Background color in HTML format (only used for EStyle.Custom)</param>
	/// <param name="font">Font to use for the cells this style applies to (only used for EStyle.Custom)</param>
	/// <param name="alignment">Text alignment for the cells this style applies to (only used for EStyle.Custom)</param>
	/// <returns>IXLStyle object containing all of the styling associated with the input EStyle option</returns>
	public static IXLStyle? GetStyle(EStyle style, IXLWorkbook wb, bool cellLocked = false, bool wrapText = false, string? htmlColor = null, IXLFont? font = null, XLAlignmentHorizontalValues? alignment = null)
	{
		IXLStyle? cellStyle = CreateEmptyStyle();
		if (cellStyle == null)
		{
			return null;
		}

		switch (style)
		{
			case EStyle.Header:
				cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				cellStyle.Border.BottomBorder = XLBorderStyleValues.Thin;
				cellStyle.Border.LeftBorder = XLBorderStyleValues.Thin;
				cellStyle.Border.RightBorder = XLBorderStyleValues.Thin;
				cellStyle.Border.TopBorder = XLBorderStyleValues.Thin;
				cellStyle.Fill.BackgroundColor = XLColor.LightGray;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				cellStyle.Font = GetFont(EFont.Header, wb);
				break;

			case EStyle.HeaderThickTop:
				cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				cellStyle.Border.BottomBorder = XLBorderStyleValues.Thin;
				cellStyle.Border.LeftBorder = XLBorderStyleValues.Thin;
				cellStyle.Border.RightBorder = XLBorderStyleValues.Thin;
				cellStyle.Border.TopBorder = XLBorderStyleValues.Medium;
				cellStyle.Fill.BackgroundColor = XLColor.LightGray;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				cellStyle.Font = GetFont(EFont.Header, wb);
				break;

			case EStyle.Body:
				cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				cellStyle.Border.BottomBorder = XLBorderStyleValues.Thin;
				cellStyle.Border.LeftBorder = XLBorderStyleValues.Thin;
				cellStyle.Border.RightBorder = XLBorderStyleValues.Thin;
				cellStyle.Fill.BackgroundColor = XLColor.NoColor;
				cellStyle.Font = GetFont(EFont.Default, wb);
				break;

			case EStyle.Error:
				cellStyle.Fill.BackgroundColor = XLColor.Red;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				break;

			case EStyle.Blackout:
				cellStyle.Font.FontColor = XLColor.Black;
				cellStyle.Fill.BackgroundColor = XLColor.Black;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				break;

			case EStyle.Whiteout:
				cellStyle.Font.FontColor = XLColor.White;
				cellStyle.Fill.BackgroundColor = XLColor.White;
				cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
				break;

			case EStyle.ImageBackground:
				cellStyle.Font = GetFont(EFont.ImageBackground, wb);
				cellStyle.Fill.BackgroundColor = XLColor.NoColor;
				cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				cellStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
				break;

			case EStyle.Custom:
				IXLStyle xStyle = wb.Style;
				if (alignment != null)
				{
					xStyle.Alignment.Horizontal = (XLAlignmentHorizontalValues)alignment;
				}
				if (!string.IsNullOrWhiteSpace(htmlColor))
				{
					xStyle.Fill.BackgroundColor = XLColor.FromHtml(htmlColor);
				}
				xStyle.Fill.PatternType = XLFillPatternValues.Solid;
				if (font != null)
				{
					xStyle.Font = font;
				}
				cellStyle = xStyle;
				break;
		}

		cellStyle.Protection.Locked = cellLocked;
		cellStyle.Alignment.WrapText = wrapText;

		return cellStyle;
	}

	/// <summary>
	/// Creates a new instance of a IXLStyle object with reflection to avoid using the same reference to the existing workbook style.
	/// Private because it relies on ClosedXML internal API that may change between library versions.
	/// </summary>
	/// <returns>Empty IXLStyle object</returns>
	private static IXLStyle? CreateEmptyStyle()
	{
		Type type = typeof(XLConstants).Assembly.GetType("ClosedXML.Excel.XLStyle")!;
		MethodInfo methodInfo = type.GetMethod("CreateEmptyStyle", BindingFlags.Static | BindingFlags.NonPublic)!;
		return methodInfo?.Invoke(null, null) as IXLStyle;
	}

	/// <summary>
	/// Applies optional border styles to a cell style when the borderStyles object specifies non-null overrides.
	/// </summary>
	/// <param name="cellStyle">The style to apply borders to</param>
	/// <param name="borderStyles">Optional border styles to apply</param>
	private static void ApplyOptionalBorderStyles(IXLStyle cellStyle, ClosedXmlBorderStyles? borderStyles)
	{
		if (borderStyles == null)
		{
			return;
		}

		if (borderStyles.BorderBottom != null)
		{
			cellStyle.Border.BottomBorder = (XLBorderStyleValues)borderStyles.BorderBottom;
		}
		if (borderStyles.BorderLeft != null)
		{
			cellStyle.Border.LeftBorder = (XLBorderStyleValues)borderStyles.BorderLeft;
		}
		if (borderStyles.BorderRight != null)
		{
			cellStyle.Border.RightBorder = (XLBorderStyleValues)borderStyles.BorderRight;
		}
		if (borderStyles.BorderTop != null)
		{
			cellStyle.Border.TopBorder = (XLBorderStyleValues)borderStyles.BorderTop;
		}
	}

	/// <summary>
	/// Get font styling based on EFont option
	/// </summary>
	/// <param name="font">Font type to get</param>
	/// <param name="wb">Workbook to add font to</param>
	/// <returns>IXLFont object containing all of the styling associated with the input EFont option</returns>
	public static IXLFont GetFont(EFont font, IXLWorkbook wb)
	{
		IXLFont cellFont = wb.Style.Font;
		switch (font)
		{
			case EFont.Default:
			case EFont.Whiteout:
				cellFont.Bold = false;
				cellFont.FontSize = 10;
				cellFont.FontName = "Calibri";
				break;

			case EFont.Header:
				cellFont.Bold = true;
				cellFont.FontSize = 10;
				cellFont.FontName = "Calibri";
				break;

			case EFont.ImageBackground:
				cellFont.FontName = "Calibri";
				cellFont.FontSize = 11;
				break;
		}
		return cellFont;
	}

	/// <summary>
	/// Writes excel file to a MemoryStream object
	/// </summary>
	/// <param name="memoryStream">Memory stream to write workbook data to</param>
	/// <param name="wb">Workbook to read into memory stream</param>
	public static async Task WriteFileToMemoryStreamAsync(this MemoryStream memoryStream, IXLWorkbook wb, CancellationToken cancellationToken = default)
	{
		await using MemoryStream tempStream = new();
		SaveOptions options = new()
		{
			EvaluateFormulasBeforeSaving = true,
			ValidatePackage = true,
			GenerateCalculationChain = true
		};

		wb.SaveAs(tempStream, options);
		await tempStream.FlushAsync(cancellationToken).ConfigureAwait(false);
		tempStream.Position = 0;
		await tempStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
		await tempStream.DisposeAsync().ConfigureAwait(false);
		await memoryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
		memoryStream.Position = 0;
	}
}
