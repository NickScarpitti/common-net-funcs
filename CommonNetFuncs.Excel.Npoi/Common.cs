using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using CommonNetFuncs.Core;
using CommonNetFuncs.Excel.Common;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.POIFS.FileSystem;
using NPOI.SS;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.Util;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static System.Convert;
using static System.Math;

namespace CommonNetFuncs.Excel.Npoi;

public sealed class NpoiBorderStyles
{
	public NpoiBorderStyles(ICellStyle? cellStyle)
	{
		if (cellStyle != null)
		{
			ExtractBorderStyles(cellStyle);
		}
	}

	public NpoiBorderStyles(BorderStyle? borderTop = null, BorderStyle? borderLeft = null, BorderStyle? borderRight = null, BorderStyle? borderBottom = null,
		short? borderTopColor = null, short? borderLeftColor = null, short? borderRightColor = null, short? borderBottomColor = null)
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

	public BorderStyle? BorderTop { get; set; }

	public BorderStyle? BorderLeft { get; set; }

	public BorderStyle? BorderRight { get; set; }

	public BorderStyle? BorderBottom { get; set; }

	public short? BorderTopColor { get; set; }

	public short? BorderLeftColor { get; set; }

	public short? BorderRightColor { get; set; }

	public short? BorderBottomColor { get; set; }

	public void ExtractBorderStyles(ICellStyle cellStyle)
	{
		BorderTop = cellStyle.BorderTop;
		BorderLeft = cellStyle.BorderLeft;
		BorderRight = cellStyle.BorderRight;
		BorderBottom = cellStyle.BorderBottom;

		BorderTopColor = cellStyle.TopBorderColor;
		BorderLeftColor = cellStyle.LeftBorderColor;
		BorderRightColor = cellStyle.RightBorderColor;
		BorderBottomColor = cellStyle.BottomBorderColor;
	}
}

public sealed class CellStyle : ICellStyle
{
	public bool ShrinkToFit { get; set; }

	public short Index { get; }

	public short DataFormat { get; set; }

	public short FontIndex { get; private set; }

	public bool IsHidden { get; set; }

	public bool IsLocked { get; set; }

	public bool IsQuotePrefixed { get; set; }

	public HorizontalAlignment Alignment { get; set; }

	public bool WrapText { get; set; }

	public VerticalAlignment VerticalAlignment { get; set; }

	public short Rotation { get; set; }

	public short Indention { get; set; }

	public BorderStyle BorderLeft { get; set; }

	public BorderStyle BorderRight { get; set; }

	public BorderStyle BorderTop { get; set; }

	public BorderStyle BorderBottom { get; set; }

	public short LeftBorderColor { get; set; }

	public short RightBorderColor { get; set; }

	public short TopBorderColor { get; set; }

	public short BottomBorderColor { get; set; }

	public FillPattern FillPattern { get; set; }

	public short FillBackgroundColor { get; set; }

	public short FillForegroundColor { get; set; }

	public short BorderDiagonalColor { get; set; }

	public BorderStyle BorderDiagonalLineStyle { get; set; }

	public BorderDiagonal BorderDiagonal { get; set; }

	public IColor? FillBackgroundColorColor { get; }

	public IColor? FillForegroundColorColor { get; }

	public ReadingOrder ReadingOrder { get; set; }

	public string? HexColor { get; set; }

	public short? HssfColor { get; set; }

	public void CloneStyleFrom(ICellStyle source)
	{
		source.CopyPropertiesTo(this);
	}

	public string GetDataFormatString()
	{
		throw new NotImplementedException();
	}

	public IFont GetFont(IWorkbook parentWorkbook)
	{
		return parentWorkbook.GetFontAt(FontIndex);
	}

	public void SetFont(IFont font)
	{
		FontIndex = font.Index;
	}
}

public sealed class CellFont : IFont
{
	public string FontName { get; set; } = string.Empty;

	public double FontHeight { get; set; }

	public double FontHeightInPoints { get; set; }

	public bool IsItalic { get; set; }

	public bool IsStrikeout { get; set; }

	public short Color { get; set; }

	public FontSuperScript TypeOffset { get; set; }

	public FontUnderlineType Underline { get; set; }

	public short Charset { get; set; }

	public short Index { get; }

	public bool IsBold { get; set; }

	public void CloneStyleFrom(IFont src)
	{
		src.CopyPropertiesTo(this);
	}

	public void CopyProperties(IFont dest)
	{
		dest.FontName = FontName;
		if (FontHeight != default)
		{
			dest.FontHeight = FontHeight;
		}
		else
		{
			dest.FontHeightInPoints = FontHeightInPoints;
		}
		dest.IsItalic = IsItalic;
		dest.IsStrikeout = IsStrikeout;
		dest.Color = Color;
		dest.TypeOffset = TypeOffset;
		dest.Underline = Underline;
		dest.Charset = Charset;
		dest.IsBold = IsBold;
	}
}

/// <summary>
/// Methods to make reading and writing to an excel file easier using NPOI
/// </summary>
public static partial class Common
{
	[GeneratedRegex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
	private static partial Regex HexColorRegex();

	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	// Cache: one dictionary per workbook, automatically cleaned up
	private static readonly ConditionalWeakTable<IWorkbook, Dictionary<string, ICellStyle>> StyleCacheTable = new();

	private static Dictionary<string, ICellStyle> GetStyleCache(IWorkbook wb)
	{
		return StyleCacheTable.GetOrCreateValue(wb);
	}

	private static ICellStyle GetOrCreateStyle(this IWorkbook wb, CellStyle style, int cachedColorLimit = 100)
	{
		Dictionary<string, ICellStyle> cache = GetStyleCache(wb);
		string key = GetStyleKey(style);
		if (cache.TryGetValue(key, out ICellStyle? cachedStyle))
		{
			return cachedStyle; // Return existing style
		}
		ICellStyle newCellStyle = wb.CreateCellStyle();
		cache[key] = newCellStyle;
		style.CopyPropertiesTo(newCellStyle);

		if (style.HexColor != null)
		{
			if (wb.IsXlsx())
			{
				Regex regex = HexColorRegex();
				if ((style.HexColor?.Length == 7) && regex.IsMatch(style.HexColor))
				{
					byte[] rgb = [ToByte(style.HexColor.Substring(1, 2), 16), ToByte(style.HexColor.Substring(3, 2), 16), ToByte(style.HexColor.Substring(5, 2), 16)];
					((XSSFCellStyle)newCellStyle).SetFillForegroundColor(new XSSFColor(new Rgb24(rgb[0], rgb[1], rgb[2])));
				}
				else
				{
					throw new ArgumentException("Invalid hex color format. Expected format: #RRGGBB");
				}
			}
			else
			{
				HSSFColor hexHssfColor = GetClosestHssfColor(style.HexColor, cachedColorLimit);
				if (hexHssfColor != null)
				{
					newCellStyle.FillForegroundColor = hexHssfColor.Indexed;
				}
			}
		}
		else if (style.HssfColor != null)
		{
			newCellStyle.FillForegroundColor = (short)style.HssfColor;
		}

		return newCellStyle;
	}

	private static readonly ConditionalWeakTable<IWorkbook, Dictionary<string, IFont>> FontCacheTable = new();

	private static Dictionary<string, IFont> GetFontCache(IWorkbook wb)
	{
		return FontCacheTable.GetOrCreateValue(wb);
	}

	private static IFont GetOrCreateFont(this IWorkbook wb, CellFont font)
	{
		Dictionary<string, IFont> cache = GetFontCache(wb);
		string key = GetFontKey(font);
		if (cache.TryGetValue(key, out IFont? cachedFont))
		{
			return cachedFont; // Return existing font
		}
		IFont newFont = wb.CreateFont();
		cache[key] = newFont;
		font.CopyProperties(newFont);
		return newFont;
	}

	private static string GetStyleKey(CellStyle style)
	{
		// Build a unique key based on all relevant properties
		StringBuilder stringBuilder = new();
		stringBuilder.Append(style.Alignment);
		stringBuilder.Append('|');
		stringBuilder.Append(style.BorderBottom);
		stringBuilder.Append('|');
		stringBuilder.Append(style.BorderDiagonal);
		stringBuilder.Append('|');
		stringBuilder.Append(style.BorderDiagonalColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.BorderDiagonalLineStyle);
		stringBuilder.Append('|');
		stringBuilder.Append(style.BorderLeft);
		stringBuilder.Append('|');
		stringBuilder.Append(style.BorderRight);
		stringBuilder.Append('|');
		stringBuilder.Append(style.BorderTop);
		stringBuilder.Append('|');
		stringBuilder.Append(style.BottomBorderColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.DataFormat);
		stringBuilder.Append('|');
		stringBuilder.Append(style.FillBackgroundColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.FillBackgroundColorColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.FillForegroundColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.FillForegroundColorColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.FillPattern);
		stringBuilder.Append('|');
		stringBuilder.Append(style.FontIndex);
		stringBuilder.Append('|');
		stringBuilder.Append(style.HexColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.HssfColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.Indention);
		stringBuilder.Append('|');
		stringBuilder.Append(style.IsHidden);
		stringBuilder.Append('|');
		stringBuilder.Append(style.IsLocked);
		stringBuilder.Append('|');
		stringBuilder.Append(style.IsQuotePrefixed);
		stringBuilder.Append('|');
		stringBuilder.Append(style.LeftBorderColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.ReadingOrder);
		stringBuilder.Append('|');
		stringBuilder.Append(style.RightBorderColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.Rotation);
		stringBuilder.Append('|');
		stringBuilder.Append(style.ShrinkToFit);
		stringBuilder.Append('|');
		stringBuilder.Append(style.TopBorderColor);
		stringBuilder.Append('|');
		stringBuilder.Append(style.VerticalAlignment);
		stringBuilder.Append('|');
		stringBuilder.Append(style.WrapText);
		return stringBuilder.ToString();
	}

	private static string GetFontKey(CellFont font)
	{
		StringBuilder stringBuilder = new();
		stringBuilder.Append(font.Charset);
		stringBuilder.Append('|');
		stringBuilder.Append(font.Color);
		stringBuilder.Append('|');
		stringBuilder.Append(font.FontHeight);
		stringBuilder.Append('|');
		stringBuilder.Append(font.FontHeightInPoints);
		stringBuilder.Append('|');
		stringBuilder.Append(font.FontName);
		stringBuilder.Append('|');
		stringBuilder.Append(font.IsBold);
		stringBuilder.Append('|');
		stringBuilder.Append(font.IsItalic);
		stringBuilder.Append('|');
		stringBuilder.Append(font.IsStrikeout);
		stringBuilder.Append('|');
		stringBuilder.Append(font.TypeOffset);
		stringBuilder.Append('|');
		stringBuilder.Append(font.Underline);
		return stringBuilder.ToString();
	}

	/// <summary>
	/// Checks if cell is empty
	/// </summary>
	/// <param name="cell">Cell to check if it is empty</param>
	/// <returns><see langword="true"/> if cell is empty</returns>
	public static bool IsCellEmpty(this ICell cell)
	{
		return string.IsNullOrWhiteSpace(cell.GetStringValue());
	}

	/// <summary>
	/// Get ICell offset from cellReference
	/// </summary>
	/// <param name="ws">Worksheet that cell is in</param>
	/// <param name="cellReference">Cell reference in A1 notation. If a range is provided, the top left cell of the range will be used</param>
	/// <param name="colOffset">X axis offset from the named cell reference</param>
	/// <param name="rowOffset">Y axis offset from the named cell reference</param>
	/// <returns>ICell object of the specified offset of the named cell</returns>
	public static ICell? GetCellFromReference(this ISheet ws, string cellReference, int colOffset = 0, int rowOffset = 0)
	{
		try
		{
			CellRangeAddress cellRangeAddress = CellRangeAddress.ValueOf(cellReference);
			IRow row = ws.GetRow(cellRangeAddress.FirstRow + rowOffset) ?? ws.CreateRow(cellRangeAddress.FirstRow + rowOffset);
			return row.GetCell(cellRangeAddress.FirstColumn + colOffset, MissingCellPolicy.CREATE_NULL_AS_BLANK);

			// CellReference cr = new(cellReference);
			// IRow? row = ws.GetRow(cr.Row + rowOffset);
			// row ??= ws.CreateRow(cr.Row + rowOffset);
			// return row.GetCell(cr.Col + colOffset, MissingCellPolicy.CREATE_NULL_AS_BLANK);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Common)}.{nameof(GetCellFromReference)} Error");
			return null;
		}
	}

	/// <summary>
	/// Get ICell offset from the startCell
	/// </summary>
	/// <param name="startCell">Cell to get offset from</param>
	/// <param name="colOffset">X axis offset from the named cell reference</param>
	/// <param name="rowOffset">Y axis offset from the named cell reference</param>
	/// <returns>ICell object of the specified offset of the startCell</returns>
	public static ICell? GetCellOffset(this ICell startCell, int colOffset = 0, int rowOffset = 0)
	{
		try
		{
			ISheet ws = startCell.Sheet;
			IRow? row = ws.GetRow(startCell.RowIndex + rowOffset) ?? ws.CreateRow(startCell.RowIndex + rowOffset);
			return row.GetCell(startCell.ColumnIndex + colOffset, MissingCellPolicy.CREATE_NULL_AS_BLANK);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Common)}.{nameof(GetCellOffset)} Error");
			return null;
		}
	}

	/// <summary>
	/// Get ICell offset from the cell indicated with the x and y coordinates
	/// </summary>
	/// <param name="ws">Worksheet that cell is in</param>
	/// <param name="colIndex">0 based X coordinate of starting cell</param>
	/// <param name="rowIndex">0 based Y coordinate of starting cell</param>
	/// <param name="colOffset">X axis offset from the cell reference</param>
	/// <param name="rowOffset">Y axis offset from the cell reference</param>
	/// <returns>ICell object of the specified offset of the cell indicated with the x and y coordinates</returns>
	public static ICell? GetCellFromCoordinates(this ISheet ws, int colIndex, int rowIndex, int colOffset = 0, int rowOffset = 0)
	{
		try
		{
			IRow row = ws.GetRow(rowIndex + rowOffset) ?? ws.CreateRow(rowIndex + rowOffset);
			return row.GetCell(colIndex + colOffset, MissingCellPolicy.CREATE_NULL_AS_BLANK);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Common)}.{nameof(GetCellFromCoordinates)} Error");
			return null;
		}
	}

	/// <summary>
	/// Gets the 0 based index of the last row with a non-blank value
	/// </summary>
	/// <param name="ws">Worksheet that contains the column to get the last populated row from</param>
	/// <param name="colIndex">0 based index of the column to find the last populated row in</param>
	/// <returns>0 based index of the last row with a non-blank value</returns>
	public static int GetLastPopulatedRowInColumn(this ISheet ws, int colIndex)
	{
		//int i = 0;
		//ICell? currentCell = ws.GetCellFromCoordinates(colIndex, i);
		//while (currentCell?.IsCellEmpty() == false)
		//{
		//    i++;
		//    currentCell = ws.GetCellFromCoordinates(colIndex, i);
		//}
		//return i - 1;

		// Iterate backwards through the rows to find the last populated row (faster on large sheets than top down method)
		for (int i = ws.LastRowNum; i >= 0; i--)
		{
			IRow row = ws.GetRow(i);
			ICell? cell = row?.GetCell(colIndex);
			if (cell?.IsCellEmpty() == false)
			{
				return i;
			}
		}
		return -1;
	}

	/// <summary>
	/// Gets the 0 based index of the last row with a non-blank value
	/// </summary>
	/// <param name="ws">Worksheet that contains the column to get the last populated row from</param>
	/// <param name="colName">Column name of the column to find the last populated row in</param>
	/// <returns>0 based index of the last row with a non-blank value</returns>
	public static int GetLastPopulatedRowInColumn(this ISheet ws, string colName)
	{
		return ws.GetLastPopulatedRowInColumn(colName.ColumnNameToNumber());
	}

	/// <summary>
	/// Get ICell offset from the cell with named reference cellName
	/// </summary>
	/// <param name="wb">Workbook that cell is in</param>
	/// <param name="cellName">Name of cell being looked for</param>
	/// <param name="colOffset">X axis offset from the named cell reference<</param>
	/// <param name="rowOffset">Y axis offset from the named cell reference<</param>
	/// <returns>ICell object of the specified offset of the cell with named reference cellName</returns>
	public static ICell? GetCellFromName(this IWorkbook wb, string cellName, int colOffset = 0, int rowOffset = 0)
	{
		try
		{
			IName? name = wb.GetName(cellName);
			CellReference[] crs;
			if (name != null)
			{
				crs = new AreaReference(name.RefersToFormula, SpreadsheetVersion.EXCEL2007).GetAllReferencedCells();
			}
			else
			{
				logger.Warn("{msg}", $"Unable to locate cell with name {cellName}");
				return null;
			}

			ISheet? ws = null;
			int rowNum = -1;
			int colNum = -1;
			for (int i = 0; i < crs.Length; i++)
			{
				ws ??= wb.GetSheet(crs[i].SheetName);

				if ((rowNum == -1) || (rowNum > crs[i].Row))
				{
					rowNum = crs[i].Row;
				}

				if ((colNum == -1) || (colNum > crs[i].Col))
				{
					colNum = crs[i].Col;
				}
			}

			if ((ws != null) && (colNum > -1) && (rowNum > -1))
			{
				IRow row = ws.GetRow(rowNum + rowOffset);
				row ??= ws.CreateRow(rowNum + rowOffset);
				return row.GetCell(colNum + colOffset, MissingCellPolicy.CREATE_NULL_AS_BLANK);
			}
			else
			{
				return null;
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Common)}.{nameof(GetCellFromName)} Error");
			return null;
		}
	}

	/// <summary>
	/// Clear contents from cell with named reference cellName
	/// </summary>
	/// <param name="wb">Workbook that cell is in</param>
	/// <param name="cellName">Name of cell to clear contents from</param>
	public static void ClearAllFromName(this IWorkbook wb, string cellName)
	{
		try
		{
			IName name = wb.GetName(cellName);
			CellReference[] crs;
			try
			{
				crs = new AreaReference(name.RefersToFormula, SpreadsheetVersion.EXCEL2007).GetAllReferencedCells();
			}
			catch (Exception ex)
			{
				logger.Warn("{msg}", $"Unable to locate cell with name {cellName}");
				logger.Warn(ex);
				return;
			}
			ISheet ws = wb.GetSheet(crs[0].SheetName);

			if ((ws == null) || (crs.Length == 0) || (name == null))
			{
				return;
			}

			for (int i = 0; i < crs.Length; i++)
			{
				IRow row = ws.GetRow(crs[i].Row);
				if (row != null)
				{
					ICell cell = row.GetCell(crs[i].Col);
					if (cell != null)
					{
						row.RemoveCell(cell);
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Common)}.{nameof(ClearAllFromName)} Error");
		}
	}

	/// <summary>
	/// Initializes cell at indicated row and column
	/// </summary>
	/// <param name="row">Row to create cell in</param>
	/// <param name="columnIndex">0 based column index of the cell to create</param>
	/// <returns>ICell object of the cell that was created</returns>
	public static ICell CreateCell(this IRow row, int columnIndex)
	{
		return row.CreateCell(columnIndex);
	}

	/// <summary>
	/// Writes an excel file to the specified path
	/// </summary>
	/// <param name="wb">SXSSFWorkbook object to write to a file</param>
	/// <param name="path">Full file path (including file name) to write wb object to</param>
	/// <returns><see langword="true"/> if write was successful</returns>
	public static bool WriteExcelFile(this SXSSFWorkbook wb, string path)
	{
		try
		{
			using (FileStream fs = new(path, FileMode.Create, FileAccess.Write))
			{
				wb.Write(fs);
			}
			wb.Close();
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Common)}.{nameof(WriteExcelFile)} Error");
			return false;
		}
	}

	/// <summary>
	/// Writes an excel file to the specified path
	/// </summary>
	/// <param name="wb">HSSFWorkbook object to write to a file</param>
	/// <param name="path">Full file path (including file name) to write wb object to</param>
	/// <returns><see langword="true"/> if write was successful</returns>
	public static bool WriteExcelFile(this HSSFWorkbook wb, string path)
	{
		try
		{
			using (FileStream fs = new(path, FileMode.Create, FileAccess.Write))
			{
				wb.Write(fs);
			}
			wb.Close();
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{nameof(Common)}.{nameof(WriteExcelFile)} Error");
			return false;
		}
	}

	/// <summary>
	/// Get cell style based on enum EStyle options
	/// </summary>
	/// <param name="wb">Workbook the style will be used in</param>
	/// <param name="cellLocked">True if the cell should be locked / disabled for user input</param>
	/// <param name="font">NPOI.SS.UserModel.IFont object defining the cell font to be used (only used for custom font)</param>
	/// <param name="alignment">NPOI.SS.UserModel.HorizontalAlignment enum indicating text alignment in the cell (only used for custom font)</param>
	/// <returns>ICellStyle object containing all of the styling associated with the input EStyles option</returns>
	private static ICellStyle GetCustomStyle(this IWorkbook wb, bool cellLocked = false, string? hexColor = null, short? hssfColor = null, IFont? font = null, HorizontalAlignment? alignment = null,
				FillPattern? fillPattern = null, NpoiBorderStyles? borderStyles = null, int cachedColorLimit = 100, bool wrapText = false)
	{
		//ICellStyle cellStyle;
		CellStyle cellStyle = new();
		if (wb.IsXlsx())
		{
			//ICellStyle xssfStyle = (XSSFCellStyle)wb.CreateCellStyle();
			//CellStyle cellStyle = new();

			//cellStyle = (CellStyle)xssfStyle;

			if (borderStyles != null)
			{
				if (borderStyles.BorderTop != null)
				{
					cellStyle.BorderTop = (BorderStyle)borderStyles.BorderTop;
					if (borderStyles.BorderTopColor != null)
					{
						cellStyle.TopBorderColor = (short)borderStyles.BorderTopColor;
					}
				}

				if (borderStyles.BorderLeft != null)
				{
					cellStyle.BorderLeft = (BorderStyle)borderStyles.BorderLeft;
					if (borderStyles.BorderLeftColor != null)
					{
						cellStyle.LeftBorderColor = (short)borderStyles.BorderLeftColor;
					}
				}

				if (borderStyles.BorderRight != null)
				{
					cellStyle.BorderRight = (BorderStyle)borderStyles.BorderRight;
					if (borderStyles.BorderRightColor != null)
					{
						cellStyle.RightBorderColor = (short)borderStyles.BorderRightColor;
					}
				}

				if (borderStyles.BorderBottom != null)
				{
					cellStyle.BorderBottom = (BorderStyle)borderStyles.BorderBottom;
					if (borderStyles.BorderBottomColor != null)
					{
						cellStyle.BottomBorderColor = (short)borderStyles.BorderBottomColor;
					}
				}
			}
		}
		else
		{
			//ICellStyle hssfStyle = (HSSFCellStyle)wb.CreateCellStyle();
			//if (alignment != null)
			//{
			//    cellStyle.Alignment = (HorizontalAlignment)alignment;
			//}

			//if (fillPattern != null)
			//{
			//    cellStyle.FillPattern = (FillPattern)fillPattern;
			//}
			//if (font != null)
			//{
			//    cellStyle.SetFont(font);
			//}

			//cellStyle = (CellStyle)hssfStyle;
		}

		if (alignment != null)
		{
			cellStyle.Alignment = (HorizontalAlignment)alignment;
		}

		if (fillPattern != null)
		{
			cellStyle.FillPattern = (FillPattern)fillPattern;
		}

		if (font != null)
		{
			cellStyle.SetFont(font);
		}

		if (hssfColor != null)
		{
			cellStyle.HssfColor = hssfColor;
		}

		if (hexColor != null)
		{
			cellStyle.HexColor = hexColor;
		}

		cellStyle.IsLocked = cellLocked;

		cellStyle.WrapText = wrapText;

		return wb.GetOrCreateStyle(cellStyle, cachedColorLimit);
	}

	/// <summary>
	/// Get cell style based on enum EStyle options
	/// </summary>
	/// <param name="wb">Workbook the style will be used in</param>
	/// <param name="cellLocked">True if the cell should be locked / disabled for user input</param>
	/// <param name="font">NPOI.SS.UserModel.IFont object defining the cell font to be used (only used for custom font)</param>
	/// <param name="alignment">NPOI.SS.UserModel.HorizontalAlignment enum indicating text alignment in the cell (only used for custom font)</param>
	/// <returns>ICellStyle object containing all of the styling associated with the input EStyles option</returns>
	public static ICellStyle GetCustomStyle(this IWorkbook wb, bool cellLocked = false, IFont? font = null, HorizontalAlignment? alignment = null,
				FillPattern? fillPattern = null, NpoiBorderStyles? borderStyles = null, bool wrapText = false)
	{
		return GetCustomStyle(wb, cellLocked, null, null, font, alignment, fillPattern, borderStyles, wrapText: wrapText);
	}

	/// <summary>
	/// Get cell style based on enum EStyle options
	/// </summary>
	/// <param name="wb">Workbook the style will be used in</param>
	/// <param name="hexColor">Cell background color to be used (only used for custom font)</param>
	/// <param name="cellLocked">True if the cell should be locked / disabled for user input</param>
	/// <param name="font">NPOI.SS.UserModel.IFont object defining the cell font to be used (only used for custom font)</param>
	/// <param name="alignment">NPOI.SS.UserModel.HorizontalAlignment enum indicating text alignment in the cell (only used for custom font)</param>
	/// <returns>IXLStyle object containing all of the styling associated with the input EStyles option</returns>
	public static ICellStyle GetCustomStyle(this IWorkbook wb, string hexColor, bool cellLocked = false, IFont? font = null, HorizontalAlignment? alignment = null,
				FillPattern? fillPattern = null, NpoiBorderStyles? borderStyles = null, int cachedColorLimit = 100, bool wrapText = false)
	{
		return wb.GetCustomStyle(cellLocked, hexColor, null, font, alignment, fillPattern, borderStyles, cachedColorLimit, wrapText);
	}

	/// <summary>
	/// Get cell style based on enum EStyle options
	/// </summary>
	/// <param name="wb">Workbook the style will be used in</param>
	/// <param name="hssfColor">Cell background color to be used (only used for custom font)</param>
	/// <param name="cellLocked">True if the cell should be locked / disabled for user input</param>
	/// <param name="font">NPOI.SS.UserModel.IFont object defining the cell font to be used (only used for custom font)</param>
	/// <param name="alignment">NPOI.SS.UserModel.HorizontalAlignment enum indicating text alignment in the cell (only used for custom font)</param>
	/// <returns>IXLStyle object containing all of the styling associated with the input EStyles option</returns>
	public static ICellStyle GetCustomStyle(this IWorkbook wb, short? hssfColor, bool cellLocked = false, IFont? font = null, HorizontalAlignment? alignment = null,
				FillPattern? fillPattern = null, NpoiBorderStyles? borderStyles = null, bool wrapText = false)
	{
		return wb.GetCustomStyle(cellLocked, null, hssfColor, font, alignment, fillPattern, borderStyles, wrapText: wrapText);
	}

	/// <summary>
	/// Gets the standard ICellStyle corresponding to the style enum passed in
	/// </summary>
	/// <param name="wb">Workbook to add the standard cell style to</param>
	/// <param name="style">Enum value indicating which style to create</param>
	/// <param name="cellLocked">Optional: Whether or not the cells with this style should be locked or not. Default = false</param>
	/// <param name="borderStyles">Optional: Border styling overrides</param>
	/// <returns>The ICellStyle that was created</returns>
	public static ICellStyle GetStandardCellStyle(this IWorkbook wb, EStyle style, bool cellLocked = false, bool wrapText = false, NpoiBorderStyles? borderStyles = null)
	{
		//ICellStyle cellStyle = wb.CreateCellStyle();
		CellStyle cellStyle = new();
		IFont cellFont;
		switch (style)
		{
			case EStyle.Header:
				cellStyle.Alignment = HorizontalAlignment.Center;
				cellStyle.BorderBottom = borderStyles?.BorderBottom ?? BorderStyle.Thin;
				cellStyle.BorderLeft = borderStyles?.BorderLeft ?? BorderStyle.Thin;
				cellStyle.BorderRight = borderStyles?.BorderRight ?? BorderStyle.Thin;
				cellStyle.BorderTop = borderStyles?.BorderTop ?? BorderStyle.Thin;
				cellStyle.FillForegroundColor = HSSFColor.Grey25Percent.Index;
				cellStyle.FillPattern = FillPattern.SolidForeground;
				cellStyle.SetFont(wb.GetFont(EFont.Header));
				break;

			case EStyle.HeaderThickTop:
				cellStyle.Alignment = HorizontalAlignment.Center;
				cellStyle.BorderBottom = borderStyles?.BorderBottom ?? BorderStyle.Thin;
				cellStyle.BorderLeft = borderStyles?.BorderLeft ?? BorderStyle.Thin;
				cellStyle.BorderRight = borderStyles?.BorderRight ?? BorderStyle.Thin;
				cellStyle.BorderTop = borderStyles?.BorderTop ?? BorderStyle.Medium;
				cellStyle.FillForegroundColor = HSSFColor.Grey25Percent.Index;
				cellStyle.FillPattern = FillPattern.SolidForeground;
				cellStyle.SetFont(wb.GetFont(EFont.Header));
				break;

			case EStyle.Body:
				cellStyle.Alignment = HorizontalAlignment.Center;
				cellStyle.BorderBottom = borderStyles?.BorderBottom ?? BorderStyle.Thin;
				cellStyle.BorderLeft = borderStyles?.BorderLeft ?? BorderStyle.Thin;
				cellStyle.BorderRight = borderStyles?.BorderRight ?? BorderStyle.Thin;

				if (borderStyles?.BorderTop != null)
				{
					cellStyle.BorderTop = (BorderStyle)borderStyles.BorderTop;
				}

				cellStyle.FillForegroundColor = HSSFColor.COLOR_NORMAL;
				cellStyle.SetFont(wb.GetFont(EFont.Default));
				break;

			case EStyle.Error:
				cellStyle.FillForegroundColor = HSSFColor.Red.Index;
				cellStyle.FillPattern = FillPattern.SolidForeground;

				if (borderStyles != null)
				{
					if (borderStyles.BorderBottom != null)
					{
						cellStyle.BorderBottom = (BorderStyle)borderStyles.BorderBottom;
					}

					if (borderStyles.BorderLeft != null)
					{
						cellStyle.BorderLeft = (BorderStyle)borderStyles.BorderLeft;
					}

					if (borderStyles.BorderRight != null)
					{
						cellStyle.BorderRight = (BorderStyle)borderStyles.BorderRight;
					}

					if (borderStyles.BorderTop != null)
					{
						cellStyle.BorderTop = (BorderStyle)borderStyles.BorderTop;
					}
				}
				break;

			case EStyle.Blackout:
				cellFont = wb.CreateFont();
				cellFont.Color = HSSFColor.Black.Index;
				cellStyle.SetFont(cellFont);
				cellStyle.FillForegroundColor = HSSFColor.Black.Index;
				cellStyle.FillPattern = FillPattern.SolidForeground;

				if (borderStyles != null)
				{
					if (borderStyles.BorderBottom != null)
					{
						cellStyle.BorderBottom = (BorderStyle)borderStyles.BorderBottom;
					}

					if (borderStyles.BorderLeft != null)
					{
						cellStyle.BorderLeft = (BorderStyle)borderStyles.BorderLeft;
					}

					if (borderStyles.BorderRight != null)
					{
						cellStyle.BorderRight = (BorderStyle)borderStyles.BorderRight;
					}

					if (borderStyles.BorderTop != null)
					{
						cellStyle.BorderTop = (BorderStyle)borderStyles.BorderTop;
					}
				}
				break;

			case EStyle.Whiteout:
				cellFont = wb.CreateFont();
				cellFont.Color = HSSFColor.White.Index;
				cellStyle.SetFont(cellFont);
				cellStyle.FillForegroundColor = HSSFColor.White.Index;
				cellStyle.FillPattern = FillPattern.SolidForeground;

				if (borderStyles != null)
				{
					if (borderStyles.BorderBottom != null)
					{
						cellStyle.BorderBottom = (BorderStyle)borderStyles.BorderBottom;
					}

					if (borderStyles.BorderLeft != null)
					{
						cellStyle.BorderLeft = (BorderStyle)borderStyles.BorderLeft;
					}

					if (borderStyles.BorderRight != null)
					{
						cellStyle.BorderRight = (BorderStyle)borderStyles.BorderRight;
					}

					if (borderStyles.BorderTop != null)
					{
						cellStyle.BorderTop = (BorderStyle)borderStyles.BorderTop;
					}
				}
				break;

			case EStyle.ImageBackground:
				cellStyle.SetFont(wb.GetFont(EFont.ImageBackground));
				cellStyle.FillForegroundColor = HSSFColor.COLOR_NORMAL;
				cellStyle.Alignment = HorizontalAlignment.Center;
				cellStyle.VerticalAlignment = VerticalAlignment.Center;

				if (borderStyles != null)
				{
					if (borderStyles.BorderBottom != null)
					{
						cellStyle.BorderBottom = (BorderStyle)borderStyles.BorderBottom;
					}

					if (borderStyles.BorderLeft != null)
					{
						cellStyle.BorderLeft = (BorderStyle)borderStyles.BorderLeft;
					}

					if (borderStyles.BorderRight != null)
					{
						cellStyle.BorderRight = (BorderStyle)borderStyles.BorderRight;
					}

					if (borderStyles.BorderTop != null)
					{
						cellStyle.BorderTop = (BorderStyle)borderStyles.BorderTop;
					}
				}
				break;
		}

		if (borderStyles != null)
		{
			if (borderStyles.BorderBottomColor != null)
			{
				cellStyle.BottomBorderColor = (short)borderStyles.BorderBottomColor;
			}

			if (borderStyles.BorderLeftColor != null)
			{
				cellStyle.LeftBorderColor = (short)borderStyles.BorderLeftColor;
			}

			if (borderStyles.BorderRightColor != null)
			{
				cellStyle.RightBorderColor = (short)borderStyles.BorderRightColor;
			}

			if (borderStyles.BorderTopColor != null)
			{
				cellStyle.TopBorderColor = (short)borderStyles.BorderTopColor;
			}
		}

		cellStyle.IsLocked = cellLocked;
		cellStyle.WrapText = wrapText;

		return wb.GetOrCreateStyle(cellStyle);
	}

	/// <summary>
	/// Get font styling based on EFonts option
	/// </summary>
	/// <param name="wb">Workbook the font will be used in</param>
	/// <param name="font">Enum for preset fonts</param>
	/// <returns>IXLFont object containing all of the styling associated with the input EFonts option</returns>
	public static IFont GetFont(this IWorkbook wb, EFont font)
	{
		//IFont cellFont = wb.CreateFont();
		CellFont cellFont = new();
		switch (font)
		{
			case EFont.Default:
				cellFont.IsBold = false;
				cellFont.FontHeightInPoints = 10;
				cellFont.FontName = nameof(EFontName.Calibri);
				break;

			case EFont.Header:
				cellFont.IsBold = true;
				cellFont.FontHeightInPoints = 10;
				cellFont.FontName = nameof(EFontName.Calibri);
				break;

			case EFont.Whiteout:
				cellFont.IsBold = false;
				cellFont.FontHeight = 10;
				cellFont.FontName = nameof(EFontName.Calibri);
				break;

			case EFont.ImageBackground:
				cellFont.FontName = nameof(EFontName.Calibri);
				cellFont.FontHeightInPoints = 11;
				break;
		}

		return wb.GetOrCreateFont(cellFont);
	}

	/// <summary>
	/// Create a table for the specified sheet in an XSSFWorkbook
	/// </summary>
	/// <param name="xssfWorkbook">Workbook to add table to</param>
	/// <param name="sheetName">Name of the sheet to add the table to</param>
	/// <param name="tableName">Name of the table to add</param>
	/// <param name="firstColIndex">Zero based index of the first column of the table.</param>
	/// <param name="lastColIndex">Zero based index of the last column of the table.</param>
	/// <param name="firstRowIndex">Zero based index of the first row of the table.</param>
	/// <param name="lastRowIndex">Zero based index of the last row of the table.</param>
	/// <param name="columnNames">
	/// Optional: Ordered list of names for each column in the table. Will use Column# if not provided or list has fewer
	/// elements than there are columns in the table
	/// </param>
	/// <param name="tableStyle">Optional: Style to use for table, defaults to TableStyleMedium1</param>
	/// <param name="showRowStripes">Optional: Styles the table to show row stripes or not</param>
	/// <param name="showColStripes">Optional: Styles the table to show column stripes or not</param>
	public static void CreateTable(this XSSFWorkbook xssfWorkbook, string sheetName, string tableName, int firstColIndex, int lastColIndex, int firstRowIndex, int lastRowIndex, List<string>? columnNames = null,
				ETableStyle tableStyle = ETableStyle.TableStyleMedium1, bool showRowStripes = true, bool showColStripes = false)
	{
		if (tableName.Length > 255)
		{
			throw new ArgumentOutOfRangeException(nameof(tableName), "Table name cannot be longer than 255 characters");
		}

		XSSFSheet xssfSheet = (XSSFSheet)xssfWorkbook.GetSheet(sheetName);
		XSSFTable table = xssfSheet.CreateTable();
		CT_Table ctTable = table.GetCTTable();
		AreaReference dataRange = new(new CellReference(firstRowIndex, firstColIndex), new CellReference(lastRowIndex, lastColIndex));

		ctTable.@ref = dataRange.FormatAsString();
		ctTable.id = (uint)xssfSheet.GetTables().Count;
		ctTable.name = tableName;
		ctTable.displayName = tableName;
		ctTable.autoFilter = new() { @ref = dataRange.FormatAsString() };
		ctTable.tableStyleInfo = new() { name = tableStyle.ToString(), showRowStripes = showRowStripes, showColumnStripes = showColStripes };
		ctTable.tableColumns = new() { tableColumn = [] };

		IRow headerRow = xssfSheet.GetRow(firstRowIndex) ?? xssfSheet.CreateRow(firstRowIndex);
		for (int i = 0; i < lastColIndex - firstColIndex + 1; i++)
		{
			string? cellValue = ((columnNames?.Count > 0) && (columnNames.Count - 1 >= i)) ? columnNames[i] : $"Column{i + 1}";
			ctTable.tableColumns.tableColumn.Add(new() { id = ((uint)i) + 1, name = cellValue });

			ICell cell = headerRow.GetCell(firstColIndex + i) ?? headerRow.CreateCell(firstColIndex + i);
			cell.SetCellValue(cellValue);
		}
	}

	/// <summary>
	/// Gets string value contained in cell
	/// </summary>
	/// <param name="cell">Cell to get the string value from</param>
	/// <returns>String representation of the value in cell</returns>
	[return: NotNullIfNotNull(nameof(cell))]
	public static string? GetStringValue(this ICell? cell)
	{
		if (cell == null)
		{
			return null;
		}

		return cell.CellType switch
		{
			CellType.Unknown => string.Empty,
			CellType.Numeric => cell.NumericCellValue.ToString(),
			CellType.String => cell.StringCellValue,
			CellType.Formula => cell.CachedFormulaResultType switch
			{
				CellType.Unknown => string.Empty,
				CellType.Numeric => cell.NumericCellValue.ToString(),
				CellType.String => cell.StringCellValue,
				CellType.Blank => string.Empty,
				CellType.Boolean => cell.BooleanCellValue.ToString(),
				CellType.Error => string.Empty, // cell.ErrorCellValue.ToString(), <-- Returns the NPOI error code
				_ => string.Empty,
			},
			CellType.Blank => string.Empty,
			CellType.Boolean => cell.BooleanCellValue.ToString(),
			CellType.Error => string.Empty, //cell.ErrorCellValue.ToString(), <-- Returns the NPOI error code
			_ => string.Empty,
		};
	}

	/// <summary>
	/// Writes excel file to a MemoryStream object
	/// </summary>
	/// <param name="memoryStream">MemoryStream object to write SXSSFWorkbook object to</param>
	/// <param name="wb">XSSFWorkbook object to write into a MemoryStream</param>
	public static async Task WriteFileToMemoryStreamAsync(this MemoryStream memoryStream, IWorkbook wb, CancellationToken cancellationToken = default)
	{
		await using MemoryStream tempStream = new();
		wb.Write(tempStream, true);
		await tempStream.FlushAsync(cancellationToken).ConfigureAwait(false);
		tempStream.Position = 0;
		await tempStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
		await tempStream.DisposeAsync().ConfigureAwait(false);
		await memoryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
		memoryStream.Position = 0;
	}

	/// <summary>
	/// Adds images into a workbook at the designated named ranges
	/// </summary>
	/// <param name="wb">Workbook to insert images into</param>
	/// <param name="imageData">Image byte array</param>
	/// <param name="cellName">Named range to insert image at</param>
	public static void AddImage(this IWorkbook wb, byte[] imageData, string cellName, AnchorType anchorType = AnchorType.MoveAndResize)
	{
		wb.AddImages([imageData], [cellName], anchorType);
	}


	/// <summary>
	/// Adds images into a workbook at the designated named ranges
	/// </summary>
	/// <param name="wb">Workbook to insert images into</param>
	/// <param name="imageData">Image byte array</param>
	/// <param name="range">Range to insert image at</param>
	public static void AddImage(this IWorkbook wb, ISheet ws, byte[] imageData, string range, AnchorType anchorType = AnchorType.MoveAndResize)
	{
		CellRangeAddress? cellRange = ws.GetCellFromReference(range).GetRangeOfMergedCells() ?? throw new ArgumentException($"Unable to get range from reference {range}", nameof(range));
		wb.AddImages(ws, [imageData], [cellRange], anchorType);
	}

	/// <summary>
	/// Adds images into a workbook at the designated named ranges
	/// </summary>
	/// <param name="wb">Workbook to insert images into</param>
	/// <param name="imageData">Image byte array</param>
	/// <param name="range">Range to insert image at</param>
	public static void AddImage(this IWorkbook wb, ISheet ws, byte[] imageData, CellRangeAddress range, AnchorType anchorType = AnchorType.MoveAndResize)
	{
		wb.AddImages(ws, [imageData], [range], anchorType);
	}

	/// <summary>
	/// Adds images into a workbook at the designated named ranges
	/// </summary>
	/// <param name="wb">Workbook to insert images into</param>
	/// <param name="imageData">Image byte array</param>
	/// <param name="cell">Cell in range to insert image at</param>
	public static void AddImage(this IWorkbook wb, ISheet ws, byte[] imageData, ICell cell, AnchorType anchorType = AnchorType.MoveAndResize)
	{
		CellRangeAddress? cellRange = cell.GetRangeOfMergedCells() ?? throw new ArgumentException("Unable to get range from cell", nameof(cell));
		wb.AddImages(ws, [imageData], [cellRange], anchorType);
	}

	/// <summary>
	/// Adds images into a workbook at the designated named ranges
	/// </summary>
	/// <param name="wb">Workbook to insert images into</param>
	/// <param name="imageData">List of image byte arrays. Must be equal in length to cellNames parameter</param>
	/// <param name="cellNames">List of named ranges to insert images at. Must be equal in length to imageData parameter</param>
	public static void AddImages(this IWorkbook wb, List<byte[]> imageData, List<string> cellNames, AnchorType anchorType = AnchorType.MoveAndResize)
	{
		if ((wb != null) && (imageData.Count > 0) && (cellNames.Count > 0) && (imageData.Count == cellNames.Count))
		{
			ISheet? ws;
			ICreationHelper helper = wb.GetCreationHelper();
			Dictionary<string, IDrawing<IShape>> worksheetDrawings = [];
			for (int i = 0; i < imageData.Count; i++)
			{
				if ((imageData[i].Length > 0) && (wb != null) && (cellNames[i] != null))
				{
					ICell? cell = wb.GetCellFromName(cellNames[i]);
					ICellStyle cellStyle = wb.GetStandardCellStyle(EStyle.ImageBackground, borderStyles: new(cell?.CellStyle)); // Need to do this to keep borders consistent
					CellRangeAddress? area = cell.GetRangeOfMergedCells();
					ws = cell?.Sheet;
					if ((ws != null) && (area != null))
					{
						if (!worksheetDrawings.TryGetValue(ws.SheetName, out IDrawing<IShape>? drawing))
						{
							drawing = ws.CreateDrawingPatriarch();
							worksheetDrawings.Add(ws.SheetName, drawing);
						}
						wb.AddPicture(ws, area, imageData[i], drawing, anchorType, helper, cellStyle);
					}
				}
			}
		}
	}

	/// <summary>
	/// Adds images into a workbook at the designated named ranges
	/// </summary>
	/// <param name="wb">Workbook to insert images into</param>
	/// <param name="imageData">List of image byte arrays. Must be equal in length to cellNames parameter</param>
	/// <param name="ranges">List of ranges to insert images at. Must be equal in length to imageData parameter</param>
	public static void AddImages(this IWorkbook wb, ISheet ws, List<byte[]> imageData, List<CellRangeAddress> ranges, AnchorType anchorType = AnchorType.MoveAndResize)
	{
		if ((wb != null) && (imageData.Count > 0) && (ranges.Count > 0) && (imageData.Count == ranges.Count))
		{
			ICreationHelper helper = wb.GetCreationHelper();
			IDrawing<IShape>? drawing = ws.CreateDrawingPatriarch();
			ICellStyle cellStyle = wb.GetStandardCellStyle(EStyle.ImageBackground);
			for (int i = 0; i < imageData.Count; i++)
			{
				if ((imageData[i].Length > 0) && (wb != null) && (ranges[i] != null))
				{
					CellRangeAddress area = ranges[i];
					wb.AddPicture(ws, area, imageData[i], drawing, anchorType, helper, cellStyle);
				}
			}
		}
	}

	/// <summary>
	/// Adds picture element to specified CellRangeAddress
	/// </summary>
	/// <param name="wb">Workbook to insert image into</param>
	/// <param name="ws">Worksheet to insert image into</param>
	/// <param name="area">CellRangeAddress where the image is to be inserted</param>
	/// <param name="imageData">Byte array containing the image to be inserted</param>
	/// <param name="drawing">Drawing patriarch to create the picture with</param>
	/// <param name="anchorType">Optional: Anchor type to define the behavior of the inserted image</param>
	/// <param name="helper">Optional: Creation helper to make anchor with</param>
	/// <param name="cellStyle">
	/// Optional: Cell style to use in cells where pasting image. Using the image background font is strongly
	/// recommended as it ensures proper measurements when sizing the picture
	/// </param>
	public static void AddPicture(this IWorkbook wb, ISheet ws, CellRangeAddress area, byte[] imageData, IDrawing<IShape> drawing, AnchorType anchorType = AnchorType.MoveAndResize, ICreationHelper? helper = null, ICellStyle? cellStyle = null)
	{
		ICell? cell = ws.GetCellFromCoordinates(area.FirstColumn, area.FirstRow);
		if (cell != null)
		{
			helper ??= wb.GetCreationHelper();

			cell.CellStyle = cellStyle ?? wb.GetCustomStyle(false, null, HSSFColor.COLOR_NORMAL, wb.GetFont(EFont.ImageBackground), null, null, new(cell.CellStyle));//Ensure consistent cell style to ensure images are sized correctly

			IClientAnchor anchor = helper.CreateClientAnchor();

			int imgWidth;
			int imgHeight;

			// Using old GDI+ System.Drawing
			// using (MemoryStream ms = new(imageData[i]))
			// {
			// using Image img = Image.FromStream(ms);
			// imgWidth = img?.Width ?? 0;
			// imgHeight = img?.Height ?? 0;
			// }

			using Image image = Image.Load(imageData);
			imgWidth = image.Width;
			imgHeight = image.Height;

			decimal imgAspect = ((decimal)imgWidth) / imgHeight;

			int rangeWidth = ws.GetRangeWidthInPx(area.FirstColumn, area.LastColumn);
			int rangeHeight = ws.GetRangeHeightInPx(area.FirstRow, area.LastRow);
			decimal rangeAspect = ((decimal)rangeWidth) / rangeHeight;

			decimal scale = (rangeAspect < imgAspect) ? ((rangeWidth - 3m) / imgWidth) : (rangeHeight - 3m) / imgHeight;

			int resizeWidth = (int)Round(imgWidth * scale, 0, MidpointRounding.ToZero);
			int resizeHeight = (int)Round(imgHeight * scale, 0, MidpointRounding.ToZero);
			int xMargin = (int)Round((rangeWidth - resizeWidth) * Units.EMU_PER_PIXEL / 2.0, 0, MidpointRounding.ToZero);
			int yMargin = (int)Round((rangeHeight - resizeHeight) * Units.EMU_PER_PIXEL * 1.75 / 2.0, 0, MidpointRounding.ToZero);

			anchor.AnchorType = anchorType;
			anchor.Col1 = area.FirstColumn;
			anchor.Row1 = area.FirstRow;
			anchor.Col2 = area.LastColumn + 1;
			anchor.Row2 = area.LastRow + 1;
			anchor.Dx1 = xMargin;
			anchor.Dy1 = yMargin;
			anchor.Dx2 = -xMargin;
			anchor.Dy2 = -yMargin;

			int pictureIndex = wb.AddPicture(imageData, PictureType.PNG);
			drawing?.CreatePicture(anchor, pictureIndex);
		}
	}

	/// <summary>
	/// Gets CellRangeAddress of merged cells
	/// </summary>
	/// <param name="cell">Cell to get CellRangeAddress from</param>
	/// <returns>CellRangeAddress of merged cells</returns>
	public static CellRangeAddress? GetRangeOfMergedCells(this ICell? cell)
	{
		if (cell?.IsMergedCell == true)
		{
			ISheet sheet = cell.Sheet;
			for (int i = 0; i < sheet.NumMergedRegions; i++)
			{
				CellRangeAddress region = sheet.GetMergedRegion(i);
				if (region.ContainsRow(cell.RowIndex) && region.ContainsColumn(cell.ColumnIndex))
				{
					return region;
				}
			}
			return null;
		}
		else if (cell != null)
		{
			return CellRangeAddress.ValueOf($"{cell.Address}:{cell.Address}");
		}
		return null;
	}

	/// <summary>
	/// Get the width of a specified range in pixels
	/// </summary>
	/// <param name="ws">Worksheet containing range to get width of</param>
	/// <param name="startCol">0 based index of the first column in range being measured</param>
	/// <param name="endCol">0 based index of the last column in range being measured</param>
	/// <returns>Double representation of the width of the column range in pixels</returns>
	public static int GetRangeWidthInPx(this ISheet ws, int startCol, int endCol)
	{
		if (startCol > endCol)
		{
			(endCol, startCol) = (startCol, endCol);
		}

		double totalWidth = 0;
		for (int i = startCol; i < endCol + 1; i++)
		{
			double columnWidth = ws.GetColumnWidthInPixels(i);
			if (columnWidth == 0.0)
			{
				logger.Warn("{msg}", $"Width of Column {i} is 0! Check referenced excel sheet: {ws.SheetName}");
			}
			totalWidth += columnWidth;
		}
		return (int)Round(totalWidth, 0, MidpointRounding.ToZero);
	}

	/// <summary>
	/// Get the height of a specified range in pixels
	/// </summary>
	/// <param name="ws">Worksheet containing range to get height of</param>
	/// <param name="startRow">0 based index of the first row in range being measured</param>
	/// <param name="endRow">0 based index of the last row in range being measured</param>
	/// <returns>Double representation of the height of the rows range in pixels</returns>
	public static int GetRangeHeightInPx(this ISheet ws, int startRow, int endRow)
	{
		if (startRow > endRow)
		{
			(endRow, startRow) = (startRow, endRow); //Swap values with tuple assignment
		}

		float totalHeight = 0;
		for (int i = startRow; i < endRow + 1; i++)
		{
			totalHeight += ws.GetRow(i)?.HeightInPoints ?? 0;
		}

		return (int)Round(totalHeight * Units.EMU_PER_POINT / Units.EMU_PER_PIXEL, 0, MidpointRounding.ToZero); //Approximation of point to px
	}

	/// <summary>
	/// Get cells contained within a range
	/// </summary>
	/// <param name="sheet">Sheet to get range from</param>
	/// <param name="range">String cell / range reference in A1 notation</param>
	/// <returns>Array of cells contained within the range specified</returns>
	public static ICell[,] GetRange(this ISheet sheet, string range)
	{
		string[] cellStartStop = range.Split(':');

		CellReference cellRefStart = new(cellStartStop[0]);
		CellReference cellRefStop = new(cellStartStop[1]);

		ICell[,] cells = new ICell[cellRefStop.Row - cellRefStart.Row + 1, cellRefStop.Col - cellRefStart.Col + 1];

		for (int i = cellRefStart.Row; i < cellRefStop.Row + 1; i++)
		{
			IRow row = sheet.GetRow(i);
			for (int j = cellRefStart.Col; j < cellRefStop.Col + 1; j++)
			{
				cells[i - cellRefStart.Row, j - cellRefStart.Col] = row.GetCell(j);
			}
		}

		return cells;
	}

	/// <summary>
	/// Adds list validation to all cells specified by cellRangeAddressList
	/// </summary>
	/// <param name="ws">ISheet object to add data validation to</param>
	/// <param name="cellRangeAddressList">Cells to add data validation to</param>
	/// <param name="options">Options to be used as the valid choices in the drop down</param>
	public static void AddDataValidation(this ISheet ws, CellRangeAddressList cellRangeAddressList, List<string> options)
	{
		IDataValidationHelper validationHelper = ws.GetDataValidationHelper();
		IDataValidationConstraint constraint = validationHelper.CreateExplicitListConstraint(options.ToArray());
		IDataValidation dataValidation = validationHelper.CreateValidation(constraint, cellRangeAddressList);

		dataValidation.ShowErrorBox = true;
		dataValidation.ErrorStyle = 0;
		dataValidation.CreateErrorBox("InvalidValue", "Selected value must be in list");
		dataValidation.ShowErrorBox = true;
		dataValidation.ShowPromptBox = false;

		ws.AddValidationData(dataValidation);
	}

	/// <summary>
	/// Reads tabular data from an unformatted excel sheet to a DataTable object similar to Python Pandas
	/// </summary>
	/// <param name="fileStream">Stream of Excel file being read</param>
	/// <param name="hasHeaders">
	/// Does the data being read have headers. Will be used for data table column names instead of default 'Column0',
	/// 'Column1'... if true. If no headers specified, first row of data must have a value for all columns in order to
	/// read all columns correctly./>
	/// </param>
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
			IWorkbook? wb = null;
			if (fileStream.IsXlsx())
			{
				wb = new XSSFWorkbook(fileStream);
			}
			else
			{
				wb = new HSSFWorkbook(fileStream);
			}

			if (wb != null)
			{
				ISheet? ws = null;

				if (!string.IsNullOrWhiteSpace(sheetName))
				{
					ws = wb.GetSheet(sheetName);
				}
				else
				{
					ws = wb.GetSheetAt(0); //Get first sheet if not specified
				}

				if (ws != null)
				{
					int startColIndex = 0;
					int? endColIndex = null;
					int startRowIndex = 0;
					int? endRowIndex = null;
					ICell? startCell;
					ICell? endCell;

					if (string.IsNullOrWhiteSpace(startCellReference))
					{
						startCellReference = "A1";
					}

					startCell = ws.GetCellFromReference(startCellReference) ?? ws.GetCellFromReference("A1"); //Default to A1 if invalid cell referenced
					startColIndex = startCell!.ColumnIndex;
					startRowIndex = startCell!.RowIndex;

					if (!string.IsNullOrWhiteSpace(endCellReference))
					{
						endCell = ws.GetCellFromReference(endCellReference);
						if (endCell != null)
						{
							endColIndex = endCell.ColumnIndex;
							endRowIndex = endCell.RowIndex;
						}
					}

					// Add headers to table
					if (hasHeaders)
					{
						if ((endColIndex ?? 0) != 0)
						{
							for (int colIndex = startColIndex; colIndex < endColIndex + 1; colIndex++)
							{
								dataTable.Columns.Add(ws.GetCellFromCoordinates(colIndex, startRowIndex).GetStringValue());
							}
						}
						else
						{
#pragma warning disable S1994 // "for" loop increment clauses should modify the loops' counters
							string? currentCellVal = startCell.GetStringValue();
							for (int colIndex = 1; !string.IsNullOrWhiteSpace(currentCellVal); colIndex++)
							{
								endColIndex = colIndex - 1;
								dataTable.Columns.Add(currentCellVal);
								currentCellVal = startCell.GetCellOffset(colIndex, 0).GetStringValue();
							}
						}
#pragma warning restore S1994 // "for" loop increment clauses should modify the loops' counters
					}
					else
					{
						if ((endColIndex ?? 0) != 0)
						{
							for (int colIndex = startColIndex; colIndex < endColIndex + 1; colIndex++)
							{
								dataTable.Columns.Add($"Column{colIndex - startColIndex}");
							}
						}
						else
						{
#pragma warning disable S1994 // "for" loop increment clauses should modify the loops' counters
							string? currentCellVal = startCell.GetStringValue();
							for (int colIndex = 1; !string.IsNullOrWhiteSpace(currentCellVal); colIndex++)
							{
								endColIndex = colIndex - 1;
								dataTable.Columns.Add($"Column{colIndex - 1}");
								currentCellVal = startCell.GetCellOffset(colIndex, 0).GetStringValue();
							}
#pragma warning restore S1994 // "for" loop increment clauses should modify the loops' counters
						}
					}

					// Add rows to table
					if (dataTable.Columns.Count > 0)
					{
						if (endRowIndex != null)
						{
							for (int rowIndex = startRowIndex + (hasHeaders ? 1 : 0); rowIndex < endRowIndex + 1; rowIndex++)
							{
								IRow row = ws.GetRow(rowIndex) ?? ws.CreateRow(rowIndex);
								string?[] newRowData = new string?[((int)endColIndex!) + 1 - startColIndex];

								for (int colIndex = startColIndex; colIndex < endColIndex + 1; colIndex++)
								{
									//newRowData[colIndex - startColIndex] = ws.GetCellFromCoordinates(colIndex, rowIndex).GetStringValue();
									newRowData[colIndex - startColIndex] = row.GetCell(colIndex).GetStringValue();
								}
								dataTable.Rows.Add(newRowData);
							}
						}
						else
						{
							int rowIndex = startRowIndex + (hasHeaders ? 1 : 0);
							bool rowIsNotNull = true;

							while (rowIsNotNull)
							{
								cancellationToken.ThrowIfCancellationRequested();

								IRow? row = ws.GetRow(rowIndex);
								rowIsNotNull = row != null;

								if (rowIsNotNull)
								{
									string?[] newRowData = new string?[((int)endColIndex!) + 1 - startColIndex];

									for (int colIndex = startColIndex; colIndex < endColIndex + 1; colIndex++)
									{
										//string? cellValue = ws.GetCellFromCoordinates(colIndex, rowIndex).GetStringValue();
										string? cellValue = row!.GetCell(colIndex).GetStringValue();
										rowIsNotNull = rowIsNotNull ? rowIsNotNull : (!string.IsNullOrWhiteSpace(cellValue));
										newRowData[colIndex - startColIndex] = cellValue;
									}

									if (rowIsNotNull)
									{
										dataTable.Rows.Add(newRowData);
									}
								}
								rowIndex++;
							}
						}
					}
				}
			}

			wb?.Dispose();
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"Unable to read excel data. Location: {nameof(Common)}.{nameof(ReadExcelFileToDataTable)}");
		}

		return dataTable;
	}

	/// <summary>
	/// Reads an Excel table into a DataTable object similar to Python Pandas
	/// </summary>
	/// <param name="fileStream">Stream of Excel file being read</param>
	/// <param name="tableName">Name of table to read. If not specified, this function will read the first table it finds in the workbook</param>
	/// <returns><see cref="DataTable"/> object containing the data read from Excel stream</returns>
	public static DataTable ReadExcelTableToDataTable(this Stream fileStream, string? tableName = null, CancellationToken cancellationToken = default)
	{
		DataTable dataTable = new();

		try
		{
			if (fileStream.IsXlsx()) //Only .xlsx files can have tables
			{
				fileStream.Position = 0;
				using XSSFWorkbook wb = new(fileStream);
				ISheet? ws = null;
				XSSFTable? table = null;
				if (!string.IsNullOrWhiteSpace(tableName))
				{
					table = wb.GetTable(tableName);
				}

				// Get first table name if not specified or not found
				if (string.IsNullOrWhiteSpace(tableName) || (table == null))
				{
					int numberOfSheets = wb.NumberOfSheets;
					for (int sheetIndex = 0; sheetIndex < numberOfSheets; sheetIndex++)
					{
						ws = wb.GetSheetAt(sheetIndex);
						foreach (XSSFTable sheetTable in ((XSSFSheet)ws).GetTables())
						{
							tableName = sheetTable.Name;
							if (!string.IsNullOrWhiteSpace(tableName))
							{
								table = wb.GetTable(tableName);
								break;
							}
						}

						if (table != null)
						{
							break;
						}
					}
				}

				if (table != null)
				{
					ws ??= wb.GetSheet(table.SheetName);

					// Get headers
					IRow currentRow = ws.GetRow(table.StartRowIndex) ?? ws.CreateRow(table.StartRowIndex);
					for (int i = table.StartColIndex; i < table.EndColIndex + 1; i++)
					{
						//dataTable.Columns.Add(ws.GetCellFromCoordinates(i, table.StartRowIndex).GetStringValue());
						dataTable.Columns.Add(currentRow.GetCell(i).GetStringValue());
					}

					// Get body data
					for (int i = table.StartRowIndex + 1; i < table.EndRowIndex + 1; i++)
					{
						cancellationToken.ThrowIfCancellationRequested();
						currentRow = ws.GetRow(i) ?? ws.CreateRow(i);
						string?[] newRowData = new string?[table.EndColIndex + 1 - table.StartColIndex];

						for (int n = table.StartColIndex; n < table.EndColIndex + 1; n++)
						{
							//newRowData[n - table.StartColIndex] = ws.GetCellFromCoordinates(n, i).GetStringValue();
							newRowData[n - table.StartColIndex] = currentRow.GetCell(n).GetStringValue();
						}

						dataTable.Rows.Add(newRowData);
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"Unable to read excel table data. Location {nameof(Common)}.{nameof(ReadExcelTableToDataTable)}");
		}

		return dataTable;
	}

	/// <summary>
	/// Gets whether or not the stream passed in represents an XLSX type file or not
	/// </summary>
	/// <param name="fileStream">Stream representation of a file</param>
	/// <returns><see langword="true"/> if stream is an XLSX file</returns>
	public static bool IsXlsx(this Stream fileStream)
	{
		fileStream.Position = 0;
		return FileMagicContainer.ValueOf(fileStream) == FileMagic.OOXML;
		//return DocumentFactoryHelper.HasOOXMLHeader(fileStream); // Deprecated method
	}

	/// <summary>
	/// Gets whether or not the stream passed in represents an XLSX type file or not
	/// </summary>
	/// <param name="workbook">NPOI Workbook Object</param>
	/// <returns><see langword="true"/> if stream is an XLSX file</returns>
	public static bool IsXlsx(this IWorkbook workbook)
	{
		return !string.Equals(workbook.GetType().Name, typeof(HSSFWorkbook).Name, StringComparison.InvariantCultureIgnoreCase);
	}

	private static readonly Dictionary<string, HSSFColor> HssfColorCache = [];

	private static readonly Lazy<IEnumerable<HSSFColor>> HssfColors = new(() => HSSFColor.GetIndexHash().Select(x => x.Value));

	/// <summary>
	/// Converts a hex color to the closest available HSSFColor
	/// </summary>
	/// <param name="hexColor">Hex color to convert</param>
	/// <param name="cachedColorLimit">
	/// Maximum number of colors to cache. Once cache reaches this limit, oldest cached value will be removed when a new
	/// value is added
	/// </param>
	/// <returns>The closest HSSFColor to the provided hex color</returns>
	public static HSSFColor GetClosestHssfColor(string hexColor, int cachedColorLimit = 100)
	{
		if (HssfColorCache.TryGetValue(hexColor, out HSSFColor? hSSFColor))
		{
			return hSSFColor!;
		}

		HSSFColor outputColor;
		Regex regex = HexColorRegex();
		if ((hexColor.Length == 7) && regex.IsMatch(hexColor))
		{
			byte[] rgb = [ToByte(hexColor.Substring(1, 2), 16), ToByte(hexColor.Substring(3, 2), 16), ToByte(hexColor.Substring(5, 2), 16)];
			outputColor = HssfColors.Value.MinBy(hssfColor => ColorDistance(rgb, hssfColor.RGB)) ?? new HSSFColor();

			// Old way to do this
			// Span<byte> rgb =
			// [
			// ToByte(hexColor.Substring(1, 2), 16),
			// ToByte(hexColor.Substring(3, 2), 16),
			// ToByte(hexColor.Substring(5, 2), 16),
			// ];

			// int deviation = int.MaxValue;
			// foreach (HSSFColor hssfColor in HSSFColor.GetIndexHash().Select(x => x.Value))
			// {
			// byte[] hssfRgb = hssfColor.RGB;
			// int totalDeviation = (int)Pow((double)rgb[0] - hssfRgb[0], 2) + (int)Pow((double)rgb[1] - hssfRgb[1], 2) + (int)Pow((double)rgb[2] - hssfRgb[2], 2);
			// if (totalDeviation < deviation)
			// {
			// outputColor = hssfColor;
			// deviation = totalDeviation;
			// if (deviation == 0)
			// {
			// break;
			// }
			// }
			// }
		}
		else
		{
			throw new ArgumentException("Invalid hex color format. Expected format: #RRGGBB", nameof(hexColor));
		}

		if (HssfColorCache.Count >= cachedColorLimit)
		{
			while (HssfColorCache.Count > cachedColorLimit)
			{
				HssfColorCache.Remove(HssfColorCache.First().Key);
			}
		}
		HssfColorCache[hexColor] = outputColor;
		return outputColor;
	}

	private static double ColorDistance(ReadOnlySpan<byte> rgb1, ReadOnlySpan<byte> rgb2)
	{
		double rMean = (rgb1[0] + rgb2[0]) / 2.0;
		double r = rgb1[0] - rgb2[0];
		double g = rgb1[1] - rgb2[1];
		double b = rgb1[2] - rgb2[2];
		return Sqrt(((2 + (rMean / 256)) * r * r) + (4 * g * g) + ((2 + ((255 - rMean) / 256)) * b * b));
	}

	/// <summary>
	/// Get the 0 based column number for the column name provided (0 = A)
	/// </summary>
	/// <param name="columnName">The column name to get the 0 based (0 = A) column index of</param>
	/// <returns>The 0 based column index (0 = A) corresponding to the value of columnName</returns>
	public static int ColumnNameToNumber(this string? columnName)
	{
		if (string.IsNullOrEmpty(columnName))
		{
			throw new ArgumentException("Column name cannot be null or empty.");
		}

		columnName = columnName.ToUpperInvariant();
		int index = 0;

		for (int i = 0; i < columnName.Length; i++)
		{
			index *= 26;
			index += columnName[i] - 'A' + 1;
		}

		return index - 1; // Subtract 1 to make it 0-based
	}

	/// <summary>
	/// Get the column name corresponding to the provided 0 based column number (A = 0)
	/// </summary>
	/// <param name="columnNumber">0 based column number (A = 0) to get name of</param>
	/// <returns>Column name corresponding to the value of columnNumber</returns>
	public static string ColumnIndexToName(this int? columnNumber)
	{
		if (columnNumber is null or < 0)
		{
			throw new ArgumentException("Index cannot be null or negative.");
		}

		return ((int)columnNumber).ColumnIndexToName();
	}

	/// <summary>
	/// Get the column name corresponding to the provided 0 based column number (A = 0)
	/// </summary>
	/// <param name="columnNumber">0 based column number (A = 0) to get name of</param>
	/// <returns>Column name corresponding to the value of columnNumber</returns>
	public static string ColumnIndexToName(this int columnNumber)
	{
		if (columnNumber < 0)
		{
			throw new ArgumentException("Index cannot be negative.");
		}

		columnNumber++; // Convert to 1-based index because we're working backwards
		StringBuilder columnName = new();
		while (columnNumber > 0)
		{
			columnNumber--;
			columnName.Insert(0, (char)('A' + (columnNumber % 26)));
			columnNumber /= 26;
		}

		return columnName.ToString();
	}
}
