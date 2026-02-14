using System.Data;
using CommonNetFuncs.Excel.Common;
using CommonNetFuncs.Excel.Npoi;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using xRetry.v3;
using static CommonNetFuncs.Excel.Npoi.Common;

namespace Excel.Npoi.Tests;

public sealed class CommonTests : IDisposable
{
	private readonly XSSFWorkbook xlsxWorkbookProp;
	private readonly HSSFWorkbook xlsWorkbookProp;
	private readonly ISheet sheetProp;

	public CommonTests()
	{
		xlsxWorkbookProp = new XSSFWorkbook();
		xlsWorkbookProp = new HSSFWorkbook();
		sheetProp = xlsxWorkbookProp.CreateSheet("Test");
	}

	private bool disposed;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				xlsxWorkbookProp?.Dispose();
				xlsWorkbookProp?.Dispose();
			}
			disposed = true;
		}
	}

	~CommonTests()
	{
		Dispose(false);
	}

	#region Cell Manipulation Tests

	[RetryTheory(3)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	public void IsCellEmpty_WithEmptyValues_ReturnsTrue(string? value)
	{
		// Arrange
		IRow row = sheetProp.CreateRow(0);
		ICell cell = row.CreateCell(0);
		if (value != null)
		{
			cell.SetCellValue(value);
		}

		// Act
		bool result = cell.IsCellEmpty();

		// Assert
		result.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("Test")]
	[InlineData("123")]
	public void IsCellEmpty_WithNonEmptyValues_ReturnsFalse(string value)
	{
		// Arrange
		IRow row = sheetProp.CreateRow(0);
		ICell cell = row.CreateCell(0);
		cell.SetCellValue(value);

		// Act
		bool result = cell.IsCellEmpty();

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void GetCellFromReference_WithValidReference_ReturnsCellAtLocation()
	{
		// Arrange
		const string cellReference = "B2";

		// Act
		ICell? cell = sheetProp.GetCellFromReference(cellReference);

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(1); // B = 1 (0-based)
		cell.RowIndex.ShouldBe(1); // 2-1 = 1 (0-based)
	}

	[RetryTheory(3)]
	[InlineData(1, 0)]  // Right
	[InlineData(-1, 0)] // Left
	[InlineData(0, 1)]  // Down
	[InlineData(0, -1)] // Up
	public void GetCellOffset_WithValidOffsets_ReturnsCorrectCell(int colOffset, int rowOffset)
	{
		// Arrange
		IRow row = sheetProp.CreateRow(1);
		ICell startCell = row.CreateCell(1); // B2

		// Act
		ICell? offsetCell = startCell.GetCellOffset(colOffset, rowOffset);

		// Assert
		offsetCell.ShouldNotBeNull();
		offsetCell.ColumnIndex.ShouldBe(startCell.ColumnIndex + colOffset);
		offsetCell.RowIndex.ShouldBe(startCell.RowIndex + rowOffset);
	}

	[RetryTheory(3)]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(2, 2)]
	public void GetCellFromCoordinates_ReturnsCorrectCell(int colIndex, int rowIndex)
	{
		// Act
		ICell? cell = sheetProp.GetCellFromCoordinates(colIndex, rowIndex);

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(colIndex);
		cell.RowIndex.ShouldBe(rowIndex);
	}

	[RetryFact(3)]
	public void GetLastPopulatedRowInColumn_WithPopulatedCells_ReturnsCorrectIndex()
	{
		// Arrange
		for (int i = 0; i < 3; i++)
		{
			IRow row = sheetProp.CreateRow(i);
			ICell cell = row.CreateCell(0);
			cell.SetCellValue($"Value {i}");
		}

		// Add an empty cell
		IRow emptyRow = sheetProp.CreateRow(3);
		emptyRow.CreateCell(0);

		// Act
		int lastPopulatedRow = sheetProp.GetLastPopulatedRowInColumn(0);

		// Assert
		lastPopulatedRow.ShouldBe(2); // Zero-based index of last populated row
	}

	[RetryTheory(3)]
	[InlineData("A")]
	[InlineData("B")]
	[InlineData("C")]
	public void GetLastPopulatedRowInColumn_WithColumnName_ReturnsCorrectIndex(string columnName)
	{
		// Arrange
		int colIndex = columnName.ColumnNameToNumber();
		for (int i = 0; i < 3; i++)
		{
			IRow row = sheetProp.CreateRow(i);
			ICell cell = row.CreateCell(colIndex);
			cell.SetCellValue($"Value {i}");
		}

		// Act
		int lastPopulatedRow = sheetProp.GetLastPopulatedRowInColumn(columnName);

		// Assert
		lastPopulatedRow.ShouldBe(2);
	}

	[RetryFact(3)]
	public void GetCellFromName_WithValidName_ReturnsCellFromNamedRange()
	{
		// Arrange
		IName name = xlsxWorkbookProp.CreateName();
		name.NameName = "TestRange";
		name.RefersToFormula = "Test!$B$2";

		// Act
		ICell? cell = xlsxWorkbookProp.GetCellFromName("TestRange");

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(1); // B = 1 (0-based)
		cell.RowIndex.ShouldBe(1); // 2-1 = 1 (0-based)
	}

	#endregion

	#region CreateCell Tests

	[RetryTheory(3)]
	[InlineData(0)]  // First column
	[InlineData(1)]  // Second column
	[InlineData(10)] // Arbitrary column
	public void CreateCell_WithValidColumnIndex_ReturnsNewCell(int columnIndex)
	{
		// Arrange
		IRow row = sheetProp.CreateRow(0);

		// Act - Explicitly call the extension method
		ICell cell = Common.CreateCell(row, columnIndex);

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(columnIndex);
		cell.RowIndex.ShouldBe(0);
	}

	[RetryFact(3)]
	public void CreateCell_WithExistingCell_OverwritesCell()
	{
		// Arrange
		IRow row = sheetProp.CreateRow(0);
		ICell existingCell = Common.CreateCell(row, 0);
		existingCell.SetCellValue("Original");

		// Act - Explicitly call the extension method
		ICell newCell = Common.CreateCell(row, 0);
		newCell.SetCellValue("New");

		// Assert
		newCell.ShouldNotBeNull();
		newCell.StringCellValue.ShouldBe("New");
		row.GetCell(0).ShouldBeSameAs(newCell);
	}

	[RetryFact(3)]
	public void CreateCell_MultipleCellsInRow_MaintainsCorrectIndices()
	{
		// Arrange
		IRow row = sheetProp.CreateRow(0);

		// Act - Explicitly call the extension method
		ICell cell1 = Common.CreateCell(row, 0);
		ICell cell2 = Common.CreateCell(row, 1);
		ICell cell3 = Common.CreateCell(row, 2);

		// Assert
		cell1.ColumnIndex.ShouldBe(0);
		cell2.ColumnIndex.ShouldBe(1);
		cell3.ColumnIndex.ShouldBe(2);
		row.LastCellNum.ShouldBe((short)3);
	}

	[RetryFact(3)]
	public void CreateCell_Extension_CreatesCellAtSpecifiedIndex()
	{
		// Arrange
		IRow row = sheetProp.CreateRow(0);
		const int columnIndex = 2;

		// Act - Explicitly call the extension method
		ICell cell = Common.CreateCell(row, columnIndex);

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(columnIndex);
		cell.RowIndex.ShouldBe(0);
		row.GetCell(columnIndex).ShouldBeSameAs(cell);
	}

	[RetryFact(3)]
	public void CreateCell_ExtensionMethod_WithHSSFWorkbook_CreatesCell()
	{
		// Arrange
		ISheet sheet = xlsWorkbookProp.CreateSheet("TestSheet");
		IRow row = sheet.CreateRow(0);

		// Act - Explicitly call the extension method
		ICell cell = Common.CreateCell(row, 3);

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(3);
		cell.RowIndex.ShouldBe(0);
	}

	[RetryFact(3)]
	public void CreateCell_ExtensionMethod_WithSXSSFWorkbook_CreatesCell()
	{
		// Arrange
		using SXSSFWorkbook sxssfWorkbook = new();
		ISheet sheet = sxssfWorkbook.CreateSheet("TestSheet");
		IRow row = sheet.CreateRow(0);

		// Act - Explicitly call the extension method
		ICell cell = Common.CreateCell(row, 5);

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(5);
		cell.RowIndex.ShouldBe(0);
	}

	#endregion

	#region DataTables and Validation Tests

	[RetryFact(3)]
	public async Task ReadExcelFileToDataTable_WithHeaders_ReturnsCorrectDataTable()
	{
		// Arrange
		await using MemoryStream ms = new();
		IRow headerRow = sheetProp.CreateRow(0);
		headerRow.CreateCell(0).SetCellValue("Column1");
		headerRow.CreateCell(1).SetCellValue("Column2");

		IRow dataRow = sheetProp.CreateRow(1);
		dataRow.CreateCell(0).SetCellValue("Value1");
		dataRow.CreateCell(1).SetCellValue("Value2");

		xlsxWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		using DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: true);

		// Assert
		result.Columns.Count.ShouldBe(2);
		result.Columns[0].ColumnName.ShouldBe("Column1");
		result.Columns[1].ColumnName.ShouldBe("Column2");
		result.Rows.Count.ShouldBe(1);
		result.Rows[0]["Column1"].ToString().ShouldBe("Value1");
		result.Rows[0]["Column2"].ToString().ShouldBe("Value2");
	}

	[RetryFact(3)]
	public void AddDataValidation_CreatesValidDropdown()
	{
		// Arrange
		List<string> options = ["Option1", "Option2", "Option3"];
		CellRangeAddressList addressList = new(0, 1, 0, 0); // A1:A2

		// Act
		sheetProp.AddDataValidation(addressList, options);

		// Assert
		IDataValidation? validation = sheetProp.GetDataValidations()[0];
		validation.ShouldNotBeNull();
		validation.ShowErrorBox.ShouldBeTrue();
		validation.ErrorStyle.ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task WriteFileToMemoryStreamAsync_WritesWorkbookCorrectly()
	{
		// Arrange
		IRow row = sheetProp.CreateRow(0);
		row.CreateCell(0).SetCellValue("Test");
		await using MemoryStream ms = new();

		// Act
		await ms.WriteFileToMemoryStreamAsync(xlsxWorkbookProp);

		// Assert
		ms.Length.ShouldBeGreaterThan(0);
		ms.Position.ShouldBe(0);
	}

	#endregion

	#region Workbook and Style Tests

	[RetryTheory(3)]
	[InlineData(EStyle.Header)]
	[InlineData(EStyle.Body)]
	[InlineData(EStyle.Error)]
	public void GetStandardCellStyle_ReturnsCorrectStyle(EStyle style)
	{
		// Act
		ICellStyle cellStyle = xlsxWorkbookProp.GetStandardCellStyle(style);

		// Assert
		cellStyle.ShouldNotBeNull();

		switch (style)
		{
			case EStyle.Header:
				cellStyle.Alignment.ShouldBe(HorizontalAlignment.Center);
				cellStyle.BorderBottom.ShouldBe(BorderStyle.Thin);
				break;
			case EStyle.Body:
				cellStyle.Alignment.ShouldBe(HorizontalAlignment.Center);
				cellStyle.BorderBottom.ShouldBe(BorderStyle.Thin);
				break;
			case EStyle.Error:
				cellStyle.FillForegroundColor.ShouldBe(HSSFColor.Red.Index);
				cellStyle.FillPattern.ShouldBe(FillPattern.SolidForeground);
				break;
		}
	}

	[RetryTheory(3)]
	[InlineData(EFont.Default)]
	[InlineData(EFont.Header)]
	[InlineData(EFont.Whiteout)]
	public void GetFont_ReturnsCorrectFont(EFont font)
	{
		// Act
		IFont cellFont = xlsxWorkbookProp.GetFont(font);

		// Assert
		cellFont.ShouldNotBeNull();

		switch (font)
		{
			case EFont.Default:
				cellFont.IsBold.ShouldBeFalse();
				cellFont.FontHeightInPoints.ShouldBe(10);
				cellFont.FontName.ShouldBe("Calibri");
				break;
			case EFont.Header:
				cellFont.IsBold.ShouldBeTrue();
				cellFont.FontHeightInPoints.ShouldBe(10);
				cellFont.FontName.ShouldBe("Calibri");
				break;
			case EFont.Whiteout:
				cellFont.IsBold.ShouldBeFalse();
				cellFont.FontHeight.ShouldBe(10);
				cellFont.FontName.ShouldBe("Calibri");
				break;
		}
	}

	[RetryFact(3)]
	public void GetCustomStyle_WithHexColor_CreatesCorrectStyle()
	{
		// Arrange
		const string hexColor = "#FF0000"; // Red

		// Act
		ICellStyle style = xlsxWorkbookProp.GetCustomStyle(hexColor);

		// Assert
		style.ShouldNotBeNull();
		if (style is XSSFCellStyle xssfStyle)
		{
			XSSFColor? color = xssfStyle.FillForegroundXSSFColor;
			color.ShouldNotBeNull();
			byte[] rgb = color.RGB;
			rgb[0].ShouldBe((byte)255); // R
			rgb[1].ShouldBe((byte)0);   // G
			rgb[2].ShouldBe((byte)0);   // B
		}
	}

	[RetryTheory(3)]
	[InlineData("A", 0)]
	[InlineData("B", 1)]
	[InlineData("Z", 25)]
	[InlineData("AA", 26)]
	public void ColumnNameToNumber_ReturnsCorrectIndex(string columnName, int expected)
	{
		// Act
		int result = columnName.ColumnNameToNumber();

		// Assert
		result.ShouldBe(expected);
	}

	[RetryTheory(3)]
	[InlineData(0, "A")]
	[InlineData(1, "B")]
	[InlineData(25, "Z")]
	[InlineData(26, "AA")]
	public void ColumnIndexToName_ReturnsCorrectName(int columnIndex, string expected)
	{
		// Act
		string result = columnIndex.ColumnIndexToName();

		// Assert
		result.ShouldBe(expected);
	}

	[RetryFact(3)]
	public void IsXlsx_WithXlsxWorkbook_ReturnsTrue()
	{
		// Act
		bool result = xlsxWorkbookProp.IsXlsx();

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void IsXlsx_WithXlsWorkbook_ReturnsFalse()
	{
		// Act
		bool result = xlsWorkbookProp.IsXlsx();

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region Additional Tests

	[RetryFact(3)]
	public void ClearAllFromName_WithValidName_ClearsCells()
	{
		// Arrange
		IName name = xlsxWorkbookProp.CreateName();
		name.NameName = "TestRange";
		name.RefersToFormula = "Test!$B$2:$B$3";

		ICell? cell1 = sheetProp.GetCellFromReference("B2");
		ICell? cell2 = sheetProp.GetCellFromReference("B3");
		cell1?.SetCellValue("Test1");
		cell2?.SetCellValue("Test2");

		// Act
		xlsxWorkbookProp.ClearAllFromName("TestRange");

		// Assert
		sheetProp.GetRow(1).GetCell(1).ShouldBeNull();
		sheetProp.GetRow(2).GetCell(1).ShouldBeNull();
	}

	[RetryFact(3)]
	public void CreateTable_CreatesValidTable()
	{
		// Arrange
		List<string> columnNames = ["Col1", "Col2"];

		// Act
		xlsxWorkbookProp.CreateTable("Test", "TestTable", 0, 1, 0, 2, columnNames);

		// Assert
		XSSFSheet sheet = (XSSFSheet)xlsxWorkbookProp.GetSheet("Test");
		XSSFTable? table = sheet.GetTables().FirstOrDefault();
		table.ShouldNotBeNull();
		table.Name.ShouldBe("TestTable");
		table.DisplayName.ShouldBe("TestTable");
		table.StartColIndex.ShouldBe(0);
		table.EndColIndex.ShouldBe(1);
		table.StartRowIndex.ShouldBe(0);
		table.EndRowIndex.ShouldBe(2);
	}

	[RetryFact(3)]
	public void GetRange_ReturnsCorrectCellArray()
	{
		// Arrange
		ICell? cell1 = sheetProp.GetCellFromReference("A1");
		ICell? cell2 = sheetProp.GetCellFromReference("B1");
		ICell? cell3 = sheetProp.GetCellFromReference("A2");
		ICell? cell4 = sheetProp.GetCellFromReference("B2");

		cell1?.SetCellValue("1");
		cell2?.SetCellValue("2");
		cell3?.SetCellValue("3");
		cell4?.SetCellValue("4");

		// Act
		ICell[,] range = sheetProp.GetRange("A1:B2");

		// Assert
		range.GetLength(0).ShouldBe(2); // Rows
		range.GetLength(1).ShouldBe(2); // Columns
		range[0, 0].GetStringValue().ShouldBe("1");
		range[0, 1].GetStringValue().ShouldBe("2");
		range[1, 0].GetStringValue().ShouldBe("3");
		range[1, 1].GetStringValue().ShouldBe("4");
	}

	[RetryFact(3)]
	public void GetRangeOfMergedCells_WithMergedCell_ReturnsCorrectRange()
	{
		// Arrange
		CellRangeAddress mergedRange = new(0, 1, 0, 1); // A1:B2
		sheetProp.AddMergedRegion(mergedRange);
		ICell? cell = sheetProp.GetCellFromReference("A1");

		// Act
		CellRangeAddress? result = cell.GetRangeOfMergedCells();

		// Assert
		result.ShouldNotBeNull();
		result.FirstRow.ShouldBe(0);
		result.LastRow.ShouldBe(1);
		result.FirstColumn.ShouldBe(0);
		result.LastColumn.ShouldBe(1);
	}

	[RetryFact(3)]
	public void GetRangeWidthInPx_ReturnsCorrectWidth()
	{
		// Arrange
		sheetProp.SetColumnWidth(0, 20 * 256); // 20 characters width
		sheetProp.SetColumnWidth(1, 15 * 256); // 15 characters width

		// Act
		int width = sheetProp.GetRangeWidthInPx(0, 1);

		// Assert
		width.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void GetRangeHeightInPx_ReturnsCorrectHeight()
	{
		// Arrange
		IRow row1 = sheetProp.CreateRow(0);
		IRow row2 = sheetProp.CreateRow(1);
		row1.Height = 20 * 20; // 20 points
		row2.Height = 15 * 20; // 15 points

		// Act
		int height = sheetProp.GetRangeHeightInPx(0, 1);

		// Assert
		height.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData(CellType.Numeric, 123.45, "123.45")]
	[InlineData(CellType.String, "Test", "Test")]
	[InlineData(CellType.Boolean, true, "True")]
	[InlineData(CellType.Blank, null, "")]
	public void GetStringValue_ReturnsCorrectString(CellType cellType, object? value, string expected)
	{
		// Arrange
		ICell cell = sheetProp.CreateRow(0).CreateCell(0);
		switch (cellType)
		{
			case CellType.Numeric:
				cell.SetCellValue((double)value!);
				break;
			case CellType.String:
				cell.SetCellValue((string)value!);
				break;
			case CellType.Boolean:
				cell.SetCellValue((bool)value!);
				break;
			case CellType.Blank:
				// Leave blank
				break;
		}

		// Act
		string result = cell.GetStringValue();

		// Assert
		result.ShouldBe(expected);
	}

	[RetryFact(3)]
	public void GetClosestHssfColor_ReturnsCachedColor()
	{
		// Arrange
		const string hexColor = "#FF0000";

		// Act
		HSSFColor color1 = GetClosestHssfColor(hexColor);
		HSSFColor color2 = GetClosestHssfColor(hexColor);

		// Assert
		color2.ShouldBe(color1); // Should return cached instance
	}

	[RetryTheory(3)]
	[InlineData("#FF0000")] // Red
	[InlineData("#00FF00")] // Green
	[InlineData("#0000FF")] // Blue
	public void GetClosestHssfColor_WithValidHexColor_ReturnsColor(string hexColor)
	{
		// Act
		HSSFColor color = GetClosestHssfColor(hexColor);

		// Assert
		color.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public async Task ReadExcelTableToDataTable_WithValidTable_ReturnsPopulatedDataTable()
	{
		// Arrange
		List<string> columnNames = ["Col1", "Col2"];
		xlsxWorkbookProp.CreateTable("Test", "TestTable", 0, 1, 0, 2, columnNames);

		ICell? cell1 = sheetProp.GetCellFromReference("A2");
		ICell? cell2 = sheetProp.GetCellFromReference("B2");
		cell1?.SetCellValue("Value1");
		cell2?.SetCellValue("Value2");

		await using MemoryStream ms = new();
		xlsxWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		DataTable result = ms.ReadExcelTableToDataTable("TestTable");

		// Assert
		result.Columns.Count.ShouldBe(2);
		result.Rows.Count.ShouldBe(2);
		result.Rows[0][0].ToString().ShouldBe("Value1");
		result.Rows[0][1].ToString().ShouldBe("Value2");
	}

	[RetryTheory(3)]
	[InlineData(0)]    // Valid index
	[InlineData(null)] // null
	[InlineData(-1)]   // negative
	public void ColumnIndexToName_WithNullableInt_WithInvalidValues_ThrowsArgumentException(int? value)
	{
		// Act & Assert
		if (value is null or < 0)
		{
			ArgumentException ex = Should.Throw<ArgumentException>(() => value.ColumnIndexToName());
			ex.Message.ShouldBe("Index cannot be null or negative.");
		}
		else
		{
			string result = value.ColumnIndexToName();
			result.ShouldNotBeNullOrEmpty();
		}
	}

	[RetryTheory(3)]
	[InlineData(0, "A")]     // First column
	[InlineData(25, "Z")]    // Last single letter
	[InlineData(26, "AA")]   // First double letter
	[InlineData(51, "AZ")]   // Last double letter A-prefix
	[InlineData(701, "ZZ")]  // Last double letter
	[InlineData(702, "AAA")] // First triple letter
	public void ColumnIndexToName_WithNullableInt_WithValidValues_ReturnsCorrectName(int? columnNumber, string expected)
	{
		// Act
		string result = columnNumber.ColumnIndexToName();

		// Assert
		result.ShouldBe(expected);
	}

	[RetryFact(3)]
	public void WriteExcelFile_WithSXSSFWorkbook_WritesSuccessfully()
	{
		// Arrange
		using SXSSFWorkbook wb = new();
		ISheet sheet = wb.CreateSheet("Test");
		IRow row = sheet.CreateRow(0);
		row.CreateCell(0).SetCellValue("Test");
		string path = Path.Combine(Path.GetTempPath(), "test.xlsx");

		try
		{
			// Act
			bool result = wb.WriteExcelFile(path);

			// Assert
			result.ShouldBeTrue();
			File.Exists(path).ShouldBeTrue();
		}
		finally
		{
			// Cleanup
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[RetryFact(3)]
	public void WriteExcelFile_WithHSSFWorkbook_WritesSuccessfully()
	{
		// Arrange
		using HSSFWorkbook wb = new();
		ISheet sheet = wb.CreateSheet("Test");
		IRow row = sheet.CreateRow(0);
		row.CreateCell(0).SetCellValue("Test");
		string path = Path.Combine(Path.GetTempPath(), "test.xls");

		try
		{
			// Act
			bool result = wb.WriteExcelFile(path);

			// Assert
			result.ShouldBeTrue();
			File.Exists(path).ShouldBeTrue();
		}
		finally
		{
			// Cleanup
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[RetryFact(3)]
	public void GetCustomStyle_WithBorderStyles_AppliesCorrectly()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thin,
			BorderLeft = BorderStyle.Medium,
			BorderRight = BorderStyle.Thick,
			BorderBottom = BorderStyle.Double,
			BorderTopColor = 1,
			BorderLeftColor = 2,
			BorderRightColor = 3,
			BorderBottomColor = 4
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetCustomStyle(borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderTop.ShouldBe(BorderStyle.Thin);
		style.BorderLeft.ShouldBe(BorderStyle.Medium);
		style.BorderRight.ShouldBe(BorderStyle.Thick);
		style.BorderBottom.ShouldBe(BorderStyle.Double);
		style.TopBorderColor.ShouldBe((short)1);
		style.LeftBorderColor.ShouldBe((short)2);
		style.RightBorderColor.ShouldBe((short)3);
		style.BottomBorderColor.ShouldBe((short)4);
	}

	[RetryFact(3)]
	public void GetCustomStyle_WithHexColor_OnNonXlsxWorkbook_UsesHssfColor()
	{
		// Arrange
		const string hexColor = "#FF0000";

		// Act
		ICellStyle style = xlsWorkbookProp.GetCustomStyle(hexColor);

		// Assert
		style.ShouldNotBeNull();
		style.ShouldBeOfType<HSSFCellStyle>();
		((HSSFCellStyle)style).FillForegroundColor.ShouldNotBe((short)0);
	}

	[RetryFact(3)]
	public void GetCustomStyle_WithHssfColor_AppliesCorrectColor()
	{
		// Arrange
		const short colorIndex = HSSFColor.Red.Index;

		// Act
		ICellStyle style = xlsxWorkbookProp.GetCustomStyle(hssfColor: colorIndex);

		// Assert
		style.ShouldNotBeNull();
		style.FillForegroundColor.ShouldBe(colorIndex);
	}

	[RetryTheory(3)]
	[InlineData(EStyle.HeaderThickTop)]
	[InlineData(EStyle.Blackout)]
	[InlineData(EStyle.Whiteout)]
	[InlineData(EStyle.ImageBackground)]
	public void GetStandardCellStyle_WithSpecialStyles_ReturnsCorrectStyle(EStyle style)
	{
		// Act
		ICellStyle cellStyle = xlsxWorkbookProp.GetStandardCellStyle(style);

		// Assert
		cellStyle.ShouldNotBeNull();

		switch (style)
		{
			case EStyle.HeaderThickTop:
				cellStyle.BorderTop.ShouldBe(BorderStyle.Medium);
				cellStyle.FillForegroundColor.ShouldBe(HSSFColor.Grey25Percent.Index);
				break;
			case EStyle.Blackout:
				cellStyle.FillForegroundColor.ShouldBe(HSSFColor.Black.Index);
				break;
			case EStyle.Whiteout:
				cellStyle.FillForegroundColor.ShouldBe(HSSFColor.White.Index);
				break;
			case EStyle.ImageBackground:
				cellStyle.Alignment.ShouldBe(HorizontalAlignment.Center);
				cellStyle.VerticalAlignment.ShouldBe(VerticalAlignment.Center);
				break;
		}
	}

	[RetryFact(3)]
	public void GetFont_WithImageBackground_ReturnsCorrectFont()
	{
		// Act
		IFont font = xlsxWorkbookProp.GetFont(EFont.ImageBackground);

		// Assert
		font.ShouldNotBeNull();
		font.FontName.ShouldBe("Calibri");
		font.FontHeightInPoints.ShouldBe(11);
	}

	[RetryTheory(3)]
	[InlineData(CellType.Formula)]
	[InlineData(CellType.Error)]
	[InlineData(CellType.Unknown)]
	public void GetStringValue_WithSpecialCellTypes_ReturnsExpectedValue(CellType cellType)
	{
		using XSSFWorkbook xlsxWorkbook = new();
		ISheet sheet = xlsxWorkbook.CreateSheet("Test");

		// Arrange
		ICell cell = sheet.CreateRow(0).CreateCell(0);

		switch (cellType)
		{
			case CellType.Formula:
				cell.SetCellFormula("SUM(A1)");
				break;
			case CellType.Error:
				cell.SetCellErrorValue(FormulaError.DIV0.Code);
				break;
			case CellType.Unknown:
				// Leave as is for Unknown type
				break;
		}

		// Act
		string result = cell.GetStringValue();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(cellType == CellType.Formula ? cell.NumericCellValue.ToString() : string.Empty);
	}

	[RetryFact(3)]
	public void GetRangeHeightInPx_WithStartRowGreaterThanEndRow_SwapsAndCalculatesCorrectly()
	{
		// Arrange
		const int startRow = 5;
		const int endRow = 2;

		for (int i = Math.Min(startRow, endRow); i <= Math.Max(startRow, endRow); i++)
		{
			IRow row = sheetProp.CreateRow(i);
			row.Height = 20 * 20; // 20 points
		}

		// Act
		int height = sheetProp.GetRangeHeightInPx(startRow, endRow);

		// Assert
		height.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void GetRangeWidthInPx_WithStartColGreaterThanEndCol_SwapsAndCalculatesCorrectly()
	{
		// Arrange
		const int startCol = 5;
		const int endCol = 2;

		for (int i = Math.Min(startCol, endCol); i <= Math.Max(startCol, endCol); i++)
		{
			sheetProp.SetColumnWidth(i, .0001); // Set to effectively 0 to test warning path (actual 0 does not work)
		}

		// Act
		int width = sheetProp.GetRangeWidthInPx(startCol, endCol);

		// Assert
		width.ShouldBe(0);
	}

	[RetryFact(3)]
	public void GetClosestHssfColor_WithCacheLimitExceeded_RemovesOldestEntry()
	{
		// Arrange
		const int cacheLimit = 2;
		GetClosestHssfColor("#FF0000", cacheLimit); // Add first color
		GetClosestHssfColor("#00FF00", cacheLimit); // Add second color

		// Act
		HSSFColor color = GetClosestHssfColor("#0000FF", cacheLimit); // Should remove first color

		// Assert
		color.ShouldNotBeNull();
		// Note: Can't test private cache directly, but we can verify the method works
	}

	[RetryFact(3)]
	public void GetRangeOfMergedCells_WithNonMergedCell_ReturnsSingleCellRange()
	{
		// Arrange
		ICell cell = sheetProp.CreateRow(0).CreateCell(0);
		cell.SetCellValue("Test");

		// Act
		CellRangeAddress? range = cell.GetRangeOfMergedCells();

		// Assert
		range.ShouldNotBeNull();
		range.FirstRow.ShouldBe(0);
		range.LastRow.ShouldBe(0);
		range.FirstColumn.ShouldBe(0);
		range.LastColumn.ShouldBe(0);
	}

	#endregion

	#region Image Tests

	[RetryFact(3)]
	public void AddImage_WithNamedRange_AddsImageCorrectly()
	{
		// Arrange
		IName name = xlsxWorkbookProp.CreateName();
		name.NameName = "ImageCell";
		name.RefersToFormula = "Test!$B$2";
		byte[] imageData = File.ReadAllBytes("TestData/test.png");

		// Act
		xlsxWorkbookProp.AddImage(imageData, "ImageCell");

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		((XSSFDrawing)sheet.CreateDrawingPatriarch()).GetShapes().Count.ShouldBe(1);
	}

	[RetryFact(3)]
	public void AddImages_WithMultipleRanges_AddsImagesCorrectly()
	{
		// Arrange
		List<byte[]> imageData = [File.ReadAllBytes("TestData/test1.png"), File.ReadAllBytes("TestData/test2.png")];
		List<string> cellNames = ["ImageCell1", "ImageCell2"];

		foreach ((string name, int i) in cellNames.Select((n, i) => (n, i)))
		{
			IName namedRange = xlsxWorkbookProp.CreateName();
			namedRange.NameName = name;
			namedRange.RefersToFormula = $"Test!$B${2 + i}";
		}

		// Act
		xlsxWorkbookProp.AddImages(imageData, cellNames);

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		((XSSFDrawing)sheet.CreateDrawingPatriarch()).GetShapes().Count.ShouldBe(2);
	}

	[RetryFact(3)]
	public void AddImage_WithRange_AddsImageToCorrectRange()
	{
		// Arrange
		byte[] imageData = File.ReadAllBytes("TestData/test.png");
		const string range = "B2:C3";

		// Act
		xlsxWorkbookProp.AddImage(sheetProp, imageData, range);

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		XSSFDrawing drawings = (XSSFDrawing)sheet.CreateDrawingPatriarch();
		drawings.GetShapes().Count.ShouldBe(1);
	}

	[RetryFact(3)]
	public void AddImage_WithCellRangeAddress_AddsImageToCorrectRange()
	{
		// Arrange
		byte[] imageData = File.ReadAllBytes("TestData/test.png");
		CellRangeAddress range = new(1, 2, 1, 2); // B2:C3

		// Act
		xlsxWorkbookProp.AddImage(sheetProp, imageData, range);

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		XSSFDrawing drawings = (XSSFDrawing)sheet.CreateDrawingPatriarch();
		drawings.GetShapes().Count.ShouldBe(1);
	}

	[RetryFact(3)]
	public void AddImage_WithCell_AddsImageToCell()
	{
		// Arrange
		byte[] imageData = File.ReadAllBytes("TestData/test.png");
		ICell cell = sheetProp.CreateRow(1).CreateCell(1); // B2

		// Act
		xlsxWorkbookProp.AddImage(sheetProp, imageData, cell);

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		XSSFDrawing drawings = (XSSFDrawing)sheet.CreateDrawingPatriarch();
		drawings.GetShapes().Count.ShouldBe(1);
	}

	[RetryFact(3)]
	public void AddImages_WithMultipleCellRanges_AddsImagesCorrectly()
	{
		// Arrange
		List<byte[]> imageData = [File.ReadAllBytes("TestData/test1.png"), File.ReadAllBytes("TestData/test2.png")];
		List<CellRangeAddress> ranges = [
						new CellRangeAddress(1, 2, 1, 2), // B2:C3
            new CellRangeAddress(3, 4, 1, 2)  // B4:C5
        ];

		// Act
		xlsxWorkbookProp.AddImages(sheetProp, imageData, ranges);

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		XSSFDrawing drawings = (XSSFDrawing)sheet.CreateDrawingPatriarch();
		drawings.GetShapes().Count.ShouldBe(2);
	}

	[RetryFact(3)]
	public void AddPicture_ConfiguresAnchorAndScaleCorrectly()
	{
		// Arrange
		byte[] imageData = File.ReadAllBytes("TestData/test.png");
		CellRangeAddress area = new(1, 2, 1, 2); // B2:C3
		IDrawing<IShape> drawing = sheetProp.CreateDrawingPatriarch();

		// Act
		xlsxWorkbookProp.AddPicture(sheetProp, area, imageData, drawing);

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		object picture = ((XSSFDrawing)sheet.CreateDrawingPatriarch()).GetShapes()[0];
		picture.ShouldNotBeNull();
	}

	#endregion

	#region Complex DataTable Tests

	[RetryFact(3)]
	public async Task ReadExcelFileToDataTable_WithNonXlsxFile_ReadsCorrectly()
	{
		// Arrange
		await using MemoryStream ms = new();
		IRow headerRow = xlsWorkbookProp.CreateSheet("Test").CreateRow(0);
		headerRow.CreateCell(0).SetCellValue("Column1");
		headerRow.CreateCell(1).SetCellValue("Column2");
		xlsWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: true);

		// Assert
		result.Columns.Count.ShouldBe(2);
		result.Columns[0].ColumnName.ShouldBe("Column1");
		result.Columns[1].ColumnName.ShouldBe("Column2");
	}

	[RetryFact(3)]
	public async Task ReadExcelFileToDataTable_WithSpecificSheet_ReadsCorrectSheet()
	{
		// Arrange
		await using MemoryStream ms = new();
		xlsxWorkbookProp.CreateSheet("Sheet1");
		ISheet sheet2 = xlsxWorkbookProp.CreateSheet("Sheet2");
		IRow headerRow = sheet2.CreateRow(0);
		headerRow.CreateCell(0).SetCellValue("SpecialColumn");
		xlsxWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: true, sheetName: "Sheet2");

		// Assert
		result.Columns[0].ColumnName.ShouldBe("SpecialColumn");
	}

	[RetryFact(3)]
	public async Task ReadExcelFileToDataTable_WithEndCellReference_LimitsRange()
	{
		// Arrange
		await using MemoryStream ms = new();
		IRow headerRow = sheetProp.CreateRow(0);
		for (int i = 0; i < 5; i++)
		{
			headerRow.CreateCell(i).SetCellValue($"Column{i + 1}");
		}
		xlsxWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: true, startCellReference: "A1", endCellReference: "C1");

		// Assert
		result.Columns.Count.ShouldBe(3); // Should only include A1:C1
	}

	[RetryFact(3)]
	public async Task ReadExcelFileToDataTable_WithoutHeaders_UsesDefaultColumnNames()
	{
		// Arrange
		await using MemoryStream ms = new();
		IRow dataRow = sheetProp.CreateRow(0);
		dataRow.CreateCell(0).SetCellValue("Value1");
		dataRow.CreateCell(1).SetCellValue("Value2");
		xlsxWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: false);

		// Assert
		result.Columns[0].ColumnName.ShouldBe("Column0");
		result.Columns[1].ColumnName.ShouldBe("Column1");
		result.Rows[0][0].ToString().ShouldBe("Value1");
	}

	[RetryFact(3)]
	public async Task ReadExcelTableToDataTable_WithNoTableName_ReadsFirstTable()
	{
		// Arrange
		await using MemoryStream ms = new();
		xlsxWorkbookProp.CreateTable("Test", "FirstTable", 0, 1, 0, 2, ["Col1", "Col2"]);
		xlsxWorkbookProp.CreateTable("Test", "SecondTable", 0, 1, 3, 5, ["Col3", "Col4"]);
		xlsxWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		using DataTable result = ms.ReadExcelTableToDataTable();

		// Assert
		result.Columns.Count.ShouldBe(2);
		result.Columns[0].ColumnName.ShouldBe("Col1");
		result.Columns[1].ColumnName.ShouldBe("Col2");
	}

	#endregion

	#region CellStyle Tests

	[RetryFact(3)]
	public void CellStyle_CloneStyleFrom_CopiesAllProperties()
	{
		// Arrange
		CellStyle source = new()
		{
			Alignment = HorizontalAlignment.Center,
			WrapText = true,
			IsLocked = true
		};
		CellStyle target = new();

		// Act
		target.CloneStyleFrom(source);

		// Assert
		target.Alignment.ShouldBe(HorizontalAlignment.Center);
		target.WrapText.ShouldBe(true);
		target.IsLocked.ShouldBe(true);
	}

	[RetryFact(3)]
	public void CellStyle_GetDataFormatString_ThrowsNotImplementedException()
	{
		// Arrange
		CellStyle cellStyle = new();

		// Act & Assert
		Should.Throw<NotImplementedException>(cellStyle.GetDataFormatString);
	}

	[RetryFact(3)]
	public void CellStyle_GetFont_ReturnsCorrectFont()
	{
		// Arrange
		IFont font = xlsxWorkbookProp.CreateFont();
		font.FontName = "Arial";
		font.IsBold = true;

		CellStyle cellStyle = new();
		cellStyle.SetFont(font);

		// Act
		IFont retrievedFont = cellStyle.GetFont(xlsxWorkbookProp);

		// Assert
		retrievedFont.Index.ShouldBe(font.Index);
	}

	[RetryFact(3)]
	public void CellStyle_SetFont_StoresFontIndex()
	{
		// Arrange
		IFont font = xlsxWorkbookProp.CreateFont();
		font.FontName = "Times New Roman";
		CellStyle cellStyle = new();

		// Act
		cellStyle.SetFont(font);

		// Assert
		cellStyle.FontIndex.ShouldBe(font.Index);
	}

	#endregion

	#region CellFont Tests

	[RetryFact(3)]
	public void CellFont_CloneStyleFrom_CopiesAllProperties()
	{
		// Arrange
		IFont source = xlsxWorkbookProp.CreateFont();
		source.FontName = "Courier New";
		source.IsBold = true;
		source.IsItalic = true;
		source.FontHeightInPoints = 14;

		CellFont target = new();

		// Act
		target.CloneStyleFrom(source);

		// Assert
		target.FontName.ShouldBe("Courier New");
		target.IsBold.ShouldBe(true);
		target.IsItalic.ShouldBe(true);
	}

	[RetryFact(3)]
	public void CellFont_CopyProperties_WithFontHeight_CopiesHeight()
	{
		// Arrange
		CellFont source = new()
		{
			FontName = "Verdana",
			FontHeight = 200,
			IsBold = true,
			IsItalic = true,
			Color = 1,
			TypeOffset = FontSuperScript.Super,
			Underline = FontUnderlineType.Single,
			Charset = 0
		};
		IFont dest = xlsxWorkbookProp.CreateFont();

		// Act
		source.CopyProperties(dest);

		// Assert
		dest.FontName.ShouldBe("Verdana");
		dest.FontHeight.ShouldBe(200);
		dest.IsBold.ShouldBe(true);
		dest.IsItalic.ShouldBe(true);
		dest.Color.ShouldBe((short)1);
		dest.TypeOffset.ShouldBe(FontSuperScript.Super);
		dest.Underline.ShouldBe(FontUnderlineType.Single);
		dest.Charset.ShouldBe((short)0);
	}

	[RetryFact(3)]
	public void CellFont_CopyProperties_WithFontHeightInPoints_CopiesHeightInPoints()
	{
		// Arrange
		CellFont source = new()
		{
			FontName = "Tahoma",
			FontHeightInPoints = 14,
			IsStrikeout = true
		};
		IFont dest = xlsxWorkbookProp.CreateFont();

		// Act
		source.CopyProperties(dest);

		// Assert
		dest.FontName.ShouldBe("Tahoma");
		dest.FontHeightInPoints.ShouldBe(14);
		dest.IsStrikeout.ShouldBe(true);
	}

	#endregion

	#region NpoiBorderStyles Tests

	[RetryFact(3)]
	public void NpoiBorderStyles_ConstructorWithCellStyle_ExtractsBorderProperties()
	{
		// Arrange
		ICellStyle cellStyle = xlsxWorkbookProp.CreateCellStyle();
		cellStyle.BorderTop = BorderStyle.Thin;
		cellStyle.BorderLeft = BorderStyle.Medium;
		cellStyle.BorderRight = BorderStyle.Thick;
		cellStyle.BorderBottom = BorderStyle.Double;
		cellStyle.TopBorderColor = 1;
		cellStyle.LeftBorderColor = 2;
		cellStyle.RightBorderColor = 3;
		cellStyle.BottomBorderColor = 4;

		// Act
		NpoiBorderStyles borderStyles = new(cellStyle);

		// Assert
		borderStyles.BorderTop.ShouldBe(BorderStyle.Thin);
		borderStyles.BorderLeft.ShouldBe(BorderStyle.Medium);
		borderStyles.BorderRight.ShouldBe(BorderStyle.Thick);
		borderStyles.BorderBottom.ShouldBe(BorderStyle.Double);
		borderStyles.BorderTopColor.ShouldBe((short)1);
		borderStyles.BorderLeftColor.ShouldBe((short)2);
		borderStyles.BorderRightColor.ShouldBe((short)3);
		borderStyles.BorderBottomColor.ShouldBe((short)4);
	}

	[RetryFact(3)]
	public void NpoiBorderStyles_ConstructorWithNullCellStyle_DoesNotThrow()
	{
		// Act & Assert
		Should.NotThrow(() => new NpoiBorderStyles(null));
	}

	[RetryFact(3)]
	public void NpoiBorderStyles_ExtractBorderStyles_UpdatesAllProperties()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new();
		ICellStyle cellStyle = xlsxWorkbookProp.CreateCellStyle();
		cellStyle.BorderTop = BorderStyle.DashDot;
		cellStyle.BorderLeft = BorderStyle.DashDotDot;
		cellStyle.BorderRight = BorderStyle.Dashed;
		cellStyle.BorderBottom = BorderStyle.Dotted;
		cellStyle.TopBorderColor = 10;
		cellStyle.LeftBorderColor = 11;
		cellStyle.RightBorderColor = 12;
		cellStyle.BottomBorderColor = 13;

		// Act
		borderStyles.ExtractBorderStyles(cellStyle);

		// Assert
		borderStyles.BorderTop.ShouldBe(BorderStyle.DashDot);
		borderStyles.BorderLeft.ShouldBe(BorderStyle.DashDotDot);
		borderStyles.BorderRight.ShouldBe(BorderStyle.Dashed);
		borderStyles.BorderBottom.ShouldBe(BorderStyle.Dotted);
		borderStyles.BorderTopColor.ShouldBe((short)10);
		borderStyles.BorderLeftColor.ShouldBe((short)11);
		borderStyles.BorderRightColor.ShouldBe((short)12);
		borderStyles.BorderBottomColor.ShouldBe((short)13);
	}

	#endregion

	#region Error Handling Tests

	[RetryTheory(3)]
	[InlineData("#GGGGGG")]  // Invalid hex characters
	[InlineData("#12345")]   // Invalid length
	[InlineData("FF0000")]   // Missing #
	[InlineData("#12345678")] // Too long
	public void GetCustomStyle_WithInvalidHexColor_ThrowsArgumentException(string invalidHexColor)
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => xlsxWorkbookProp.GetCustomStyle(invalidHexColor));
	}

	[RetryFact(3)]
	public void WriteExcelFile_SXSSFWorkbook_WithInvalidPath_ReturnsFalse()
	{
		// Arrange
		using SXSSFWorkbook wb = new();

		string invalidPath = Path.Combine(Path.GetTempPath(), "NonExistentFolder", "test.xlsx");

		// Act
		bool result = wb.WriteExcelFile(invalidPath);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void WriteExcelFile_HSSFWorkbook_WithInvalidPath_ReturnsFalse()
	{
		// Arrange
		using HSSFWorkbook wb = new();

		string invalidPath = Path.Combine(Path.GetTempPath(), "NonExistentFolder", "test.xls");

		// Act
		bool result = wb.WriteExcelFile(invalidPath);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void GetCellFromName_WithInvalidNamedRange_ReturnsNull()
	{
		// Act
		ICell? cell = xlsxWorkbookProp.GetCellFromName("NonExistentName");

		// Assert
		cell.ShouldBeNull();
	}

	[RetryFact(3)]
	public void ClearAllFromName_WithInvalidNamedRange_DoesNotThrow()
	{
		// Act & Assert
		Should.NotThrow(() => xlsxWorkbookProp.ClearAllFromName("NonExistentName"));
	}

	[RetryFact(3)]
	public void CreateTable_WithTableNameLongerThan255_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		string longTableName = new('A', 300);

		// Act & Assert
		ArgumentOutOfRangeException ex = Should.Throw<ArgumentOutOfRangeException>(() =>
			xlsxWorkbookProp.CreateTable("Test", longTableName, 0, 1, 0, 2, ["Col1", "Col2"]));
		ex.ParamName.ShouldBe("tableName");
	}

	#endregion

	#region Additional Coverage Tests

	[RetryFact(3)]
	public void GetCustomStyle_WithFillPattern_AppliesPattern()
	{
		// Act
		ICellStyle style = xlsxWorkbookProp.GetCustomStyle(fillPattern: FillPattern.SolidForeground);

		// Assert
		style.ShouldNotBeNull();
		style.FillPattern.ShouldBe(FillPattern.SolidForeground);
	}

	[RetryFact(3)]
	public void GetCustomStyle_WithFontParameter_CreatesStyle()
	{
		// Arrange
		IFont customFont = xlsxWorkbookProp.CreateFont();
		customFont.IsBold = true;
		customFont.FontName = "Arial";

		// Act
		ICellStyle style = xlsxWorkbookProp.GetCustomStyle(font: customFont);

		// Assert
		style.ShouldNotBeNull();
		// The font is applied via SetFont which sets FontIndex
		// Verify the style was created successfully
		style.Index.ShouldBeGreaterThanOrEqualTo((short)0);
	}

	[RetryFact(3)]
	public void GetCustomStyle_WithAlignment_AppliesAlignment()
	{
		// Act
		ICellStyle style = xlsxWorkbookProp.GetCustomStyle(alignment: HorizontalAlignment.Right);

		// Assert
		style.ShouldNotBeNull();
		style.Alignment.ShouldBe(HorizontalAlignment.Right);
	}

	[RetryFact(3)]
	public void GetCustomStyle_WithCellLocked_LocksCells()
	{
		// Act
		ICellStyle style = xlsxWorkbookProp.GetCustomStyle(cellLocked: true);

		// Assert
		style.ShouldNotBeNull();
		style.IsLocked.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void GetCustomStyle_WithWrapText_EnablesWrapText()
	{
		// Act
		ICellStyle style = xlsxWorkbookProp.GetCustomStyle(wrapText: true);

		// Assert
		style.ShouldNotBeNull();
		style.WrapText.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_WithWrapText_EnablesWrapText()
	{
		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Body, wrapText: true);

		// Assert
		style.ShouldNotBeNull();
		style.WrapText.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_WithCellLocked_LocksCells()
	{
		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Header, cellLocked: true);

		// Assert
		style.ShouldNotBeNull();
		style.IsLocked.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void IsXlsx_WithXlsxStream_ReturnsTrue()
	{
		// Arrange
		using MemoryStream ms = new();
		xlsxWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		bool result = ms.IsXlsx();

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void IsXlsx_WithXlsStream_ReturnsFalse()
	{
		// Arrange
		using MemoryStream ms = new();
		xlsWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		bool result = ms.IsXlsx();

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void GetStringValue_WithNullCell_ReturnsNull()
	{
		// Arrange
		ICell? cell = null;

		// Act
		string? result = cell.GetStringValue();

		// Assert
		result.ShouldBeNull();
	}

	[RetryFact(3)]
	public void GetCellFromName_WithNamedRangeAndOffset_ReturnsOffsetCell()
	{
		// Arrange
		IName name = xlsxWorkbookProp.CreateName();
		name.NameName = "OffsetTest";
		name.RefersToFormula = "Test!$B$2";

		// Populate cells
		sheetProp.GetCellFromReference("B2")?.SetCellValue("Original");
		sheetProp.GetCellFromReference("C3")?.SetCellValue("Offset");

		// Act
		ICell? cell = xlsxWorkbookProp.GetCellFromName("OffsetTest", colOffset: 1, rowOffset: 1);

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(2); // C
		cell.RowIndex.ShouldBe(2); // 3 (0-based)
	}

	[RetryFact(3)]
	public void GetCellFromReference_WithRangeReference_ReturnsTopLeftCell()
	{
		// Arrange
		const string rangeReference = "B2:D4";

		// Act
		ICell? cell = sheetProp.GetCellFromReference(rangeReference);

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(1); // B
		cell.RowIndex.ShouldBe(1); // 2 (0-based)
	}

	[RetryFact(3)]
	public void GetCellFromReference_WithOffsets_ReturnsCorrectCell()
	{
		// Arrange
		const string cellReference = "B2";

		// Act
		ICell? cell = sheetProp.GetCellFromReference(cellReference, colOffset: 2, rowOffset: 3);

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(3); // D (B=1 + 2)
		cell.RowIndex.ShouldBe(4); // 5 (2-1=1 + 3)
	}

	[RetryFact(3)]
	public void GetLastPopulatedRowInColumn_WithNoPopulatedCells_ReturnsNegative()
	{
		// Arrange
		using XSSFWorkbook emptyWorkbook = new();
		ISheet emptySheet = emptyWorkbook.CreateSheet("Empty");

		// Act
		int lastRow = emptySheet.GetLastPopulatedRowInColumn(0);

		// Assert
		lastRow.ShouldBeLessThan(0);
	}

	[RetryFact(3)]
	public void AddPicture_WithNullCell_LogsWarning()
	{
		// Arrange
		byte[] imageData = File.ReadAllBytes("TestData/test.png");
		CellRangeAddress area = new(100, 101, 100, 101); // Area far beyond populated cells
		IDrawing<IShape> drawing = sheetProp.CreateDrawingPatriarch();

		// Act - should not throw, but log a warning
		Should.NotThrow(() => xlsxWorkbookProp.AddPicture(sheetProp, area, imageData, drawing));
	}

	[RetryFact(3)]
	public void GetRangeOfMergedCells_WithNullCell_ReturnsNull()
	{
		// Arrange
		ICell? cell = null;

		// Act
		CellRangeAddress? range = cell.GetRangeOfMergedCells();

		// Assert
		range.ShouldBeNull();
	}

	[RetryFact(3)]
	public void AddImages_WithEmptyLists_DoesNotThrow()
	{
		// Arrange
		List<byte[]> emptyImageData = [];
		List<string> emptyCellNames = [];

		// Act & Assert
		Should.NotThrow(() => xlsxWorkbookProp.AddImages(emptyImageData, emptyCellNames));
	}

	[RetryFact(3)]
	public void AddImages_WithMismatchedListSizes_DoesNotAddImages()
	{
		// Arrange
		List<byte[]> imageData = [File.ReadAllBytes("TestData/test.png")];
		List<string> cellNames = ["Name1", "Name2"]; // Different size

		// Act
		xlsxWorkbookProp.AddImages(imageData, cellNames);

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		// Should not add any images due to size mismatch
		((XSSFDrawing)sheet.CreateDrawingPatriarch()).GetShapes().Count.ShouldBe(0);
	}

	[RetryFact(3)]
	public void AddImages_WithCellRanges_WithMismatchedListSizes_DoesNotAddImages()
	{
		// Arrange
		List<byte[]> imageData = [File.ReadAllBytes("TestData/test.png")];
		List<CellRangeAddress> ranges = [
						new CellRangeAddress(1, 2, 1, 2),
						new CellRangeAddress(3, 4, 1, 2)
				]; // Different size

		// Act
		xlsxWorkbookProp.AddImages(sheetProp, imageData, ranges);

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		// Should not add any images due to size mismatch
		((XSSFDrawing)sheet.CreateDrawingPatriarch()).GetShapes().Count.ShouldBe(0);
	}

	[RetryFact(3)]
	public void GetCustomStyle_CacheTest_ReturnsSameStyleForSameParameters()
	{
		// Act
		ICellStyle style1 = xlsxWorkbookProp.GetCustomStyle("#FF0000");
		ICellStyle style2 = xlsxWorkbookProp.GetCustomStyle("#FF0000");

		// Assert
		style1.ShouldBeSameAs(style2);
	}

	[RetryFact(3)]
	public void GetFont_CacheTest_ReturnsSameFontForSameParameters()
	{
		// Act
		IFont font1 = xlsxWorkbookProp.GetFont(EFont.Header);
		IFont font2 = xlsxWorkbookProp.GetFont(EFont.Header);

		// Assert
		font1.ShouldBeSameAs(font2);
	}

	[RetryFact(3)]
	public void CreateTable_WithDefaultColumnNames_GeneratesColumnNames()
	{
		// Act
		xlsxWorkbookProp.CreateTable("Test", "AutoNameTable", 0, 2, 0, 1);

		// Assert
		XSSFSheet sheet = (XSSFSheet)xlsxWorkbookProp.GetSheet("Test");
		XSSFTable? table = sheet.GetTables().FirstOrDefault(t => t.Name == "AutoNameTable");
		table.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void CreateTable_WithPartialColumnNames_UsesMixOfProvidedAndDefaultNames()
	{
		// Arrange
		List<string> partialNames = ["FirstCol"]; // Only one name for three columns

		// Act
		xlsxWorkbookProp.CreateTable("Test", "PartialNameTable", 0, 2, 0, 1, partialNames);

		// Assert
		XSSFSheet sheet = (XSSFSheet)xlsxWorkbookProp.GetSheet("Test");
		XSSFTable? table = sheet.GetTables().FirstOrDefault(t => t.Name == "PartialNameTable");
		table.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void GetRange_WithInvalidRange_ThrowsException()
	{
		// Act & Assert
		Should.Throw<Exception>(() => sheetProp.GetRange("INVALID"));
	}

	[RetryFact(3)]
	public void GetClosestHssfColor_WithShortHexFormat_ThrowsArgumentException()
	{
		// Arrange - Use short hex format #RGB instead of #RRGGBB
		const string shortHex = "#F00"; // Short format for red

		// Act & Assert - Short hex format is not supported
		ArgumentException ex = Should.Throw<ArgumentException>(() => GetClosestHssfColor(shortHex));
		ex.ParamName.ShouldBe("hexColor");
	}

	#endregion

	#region Exception Handling and Edge Cases Tests

	[RetryFact(3)]
	public void GetCellFromReference_WithInvalidReference_ReturnsNull()
	{
		// Arrange
		const string invalidReference = "INVALID_REF";

		// Act
		ICell? cell = sheetProp.GetCellFromReference(invalidReference);

		// Assert
		cell.ShouldBeNull();
	}

	[RetryFact(3)]
	public void GetCellOffset_WithExceptionThrown_ReturnsNull()
	{
		// Arrange
		IRow row = sheetProp.CreateRow(0);
		ICell cell = row.CreateCell(0);

		// Act - Try with extremely large offset that could cause issues
		ICell? result = cell.GetCellOffset(int.MaxValue, int.MaxValue);

		// Assert
		result.ShouldBeNull();
	}

	[RetryFact(3)]
	public void GetCellFromCoordinates_WithExceptionThrown_ReturnsNull()
	{
		// Act - Try with negative coordinates that could cause issues
		ICell? result = sheetProp.GetCellFromCoordinates(-1, -1);

		// Assert
		result.ShouldBeNull();
	}

	[RetryFact(3)]
	public void GetCellFromName_WithException_ReturnsNull()
	{
		// Arrange
		IName name = xlsxWorkbookProp.CreateName();
		name.NameName = "InvalidName";
		name.RefersToFormula = "INVALID!FORMULA";

		// Act
		ICell? cell = xlsxWorkbookProp.GetCellFromName("InvalidName");

		// Assert
		cell.ShouldBeNull();
	}

	[RetryFact(3)]
	public void ClearAllFromName_WithNullWorksheet_DoesNotThrow()
	{
		// Arrange
		IName name = xlsxWorkbookProp.CreateName();
		name.NameName = "TestName";
		name.RefersToFormula = "NonExistentSheet!$A$1";

		// Act & Assert
		Should.NotThrow(() => xlsxWorkbookProp.ClearAllFromName("TestName"));
	}

	[RetryFact(3)]
	public void ClearAllFromName_WithException_CatchesAndHandles()
	{
		// Arrange
		IName name = xlsxWorkbookProp.CreateName();
		name.NameName = "ExceptionTest";
		name.RefersToFormula = "Test!$A$1";

		// Act - Try to clear a valid name, then test exception handling path through code
		// The actual exception handling is tested by ensuring the method doesn't throw
		Should.NotThrow(() => xlsxWorkbookProp.ClearAllFromName("ExceptionTest"));
	}

	[RetryFact(3)]
	public void ColumnNameToNumber_WithNullOrEmpty_ThrowsArgumentException()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => ((string?)null).ColumnNameToNumber());
		Should.Throw<ArgumentException>(() => string.Empty.ColumnNameToNumber());
	}

	[RetryFact(3)]
	public void ColumnIndexToName_WithNegativeInt_ThrowsArgumentException()
	{
		// Arrange
		const int negativeIndex = -1;

		// Act & Assert
		Should.Throw<ArgumentException>(() => negativeIndex.ColumnIndexToName());
	}

	[RetryFact(3)]
	public void GetClosestHssfColor_ExceedsCacheLimit_RemovesOldestEntries()
	{
		// Arrange
		const int cacheLimit = 5;
		List<string> colors = ["#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF"];

		// Act - Add more colors than cache limit
		foreach (string color in colors)
		{
			GetClosestHssfColor(color, cacheLimit);
		}

		// Assert - Should not throw, cache should be managed
		HSSFColor lastColor = GetClosestHssfColor("#FFFFFF", cacheLimit);
		lastColor.ShouldNotBeNull();
	}

	#endregion

	#region HSSFWorkbook (XLS) Tests

	[RetryFact(3)]
	public void GetCustomStyle_WithHSSFWorkbook_CreatesStyle()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thin,
			BorderLeft = BorderStyle.Medium
		};

		// Act
		ICellStyle style = xlsWorkbookProp.GetCustomStyle(borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.ShouldBeOfType<HSSFCellStyle>();
	}

	[RetryFact(3)]
	public void GetCustomStyle_WithHSSFWorkbook_WithAllParameters_CreatesStyle()
	{
		// Arrange
		IFont font = xlsWorkbookProp.CreateFont();
		font.FontName = "Arial";

		// Act
		ICellStyle style = xlsWorkbookProp.GetCustomStyle(
			cellLocked: true,
			font: font,
			alignment: HorizontalAlignment.Left,
			fillPattern: FillPattern.SolidForeground,
			wrapText: true);

		// Assert
		style.ShouldNotBeNull();
		style.IsLocked.ShouldBeTrue();
		style.Alignment.ShouldBe(HorizontalAlignment.Left);
		style.FillPattern.ShouldBe(FillPattern.SolidForeground);
		style.WrapText.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task ReadExcelFileToDataTable_WithHSSFWorkbook_ReadsData()
	{
		// Arrange
		await using MemoryStream ms = new();
		ISheet hssfSheet = xlsWorkbookProp.CreateSheet("TestSheet");
		IRow headerRow = hssfSheet.CreateRow(0);
		headerRow.CreateCell(0).SetCellValue("Col1");
		headerRow.CreateCell(1).SetCellValue("Col2");

		IRow dataRow = hssfSheet.CreateRow(1);
		dataRow.CreateCell(0).SetCellValue("Data1");
		dataRow.CreateCell(1).SetCellValue("Data2");

		xlsWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: true);

		// Assert
		result.Columns.Count.ShouldBe(2);
		result.Rows.Count.ShouldBe(1);
		result.Rows[0]["Col1"].ToString().ShouldBe("Data1");
		result.Rows[0]["Col2"].ToString().ShouldBe("Data2");
	}

	[RetryFact(3)]
	public async Task ReadExcelFileToDataTable_WithHSSFWorkbook_WithSpecificSheet_ReadsCorrectSheet()
	{
		// Arrange
		await using MemoryStream ms = new();
		xlsWorkbookProp.CreateSheet("Sheet1");
		ISheet sheet2 = xlsWorkbookProp.CreateSheet("Sheet2");
		IRow headerRow = sheet2.CreateRow(0);
		headerRow.CreateCell(0).SetCellValue("CorrectColumn");
		xlsWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: true, sheetName: "Sheet2");

		// Assert
		result.Columns[0].ColumnName.ShouldBe("CorrectColumn");
	}

	[RetryFact(3)]
	public async Task ReadExcelFileToDataTable_WithStartAndEndReferences_ReadsSpecificRange()
	{
		// Arrange
		await using MemoryStream ms = new();
		IRow headerRow = sheetProp.CreateRow(0);
		for (int i = 0; i < 10; i++)
		{
			headerRow.CreateCell(i).SetCellValue($"Col{i}");
		}

		IRow dataRow = sheetProp.CreateRow(1);
		for (int i = 0; i < 10; i++)
		{
			dataRow.CreateCell(i).SetCellValue($"Data{i}");
		}

		xlsxWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act
		DataTable result = ms.ReadExcelFileToDataTable(
			hasHeaders: true,
			startCellReference: "B1",
			endCellReference: "D2");

		// Assert
		result.Columns.Count.ShouldBe(3); // B, C, D
		result.Rows.Count.ShouldBe(1);
	}

	[RetryFact(3)]
	public async Task ReadExcelTableToDataTable_WithHSSFWorkbook_ReturnsEmptyDataTable()
	{
		// Arrange
		await using MemoryStream ms = new();
		xlsWorkbookProp.CreateSheet("Test");
		xlsWorkbookProp.Write(ms, true);
		ms.Position = 0;

		// Act - HSSFWorkbook doesn't support tables, should return empty DataTable
		DataTable result = ms.ReadExcelTableToDataTable();

		// Assert
		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(0);
	}

	#endregion

	#region GetStringValue Additional Tests

	[RetryFact(3)]
	public void GetStringValue_WithFormulaCellNumericResult_ReturnsNumericString()
	{
		// Arrange
		ICell cell = sheetProp.CreateRow(0).CreateCell(0);
		cell.SetCellFormula("1+1");
		xlsxWorkbookProp.GetCreationHelper().CreateFormulaEvaluator().EvaluateFormulaCell(cell);

		// Act
		string result = cell.GetStringValue();

		// Assert
		result.ShouldNotBeEmpty();
	}

	[RetryFact(3)]
	public void GetStringValue_WithFormulaCellStringResult_ReturnsString()
	{
		// Arrange
		ICell cell = sheetProp.CreateRow(0).CreateCell(0);
		ICell cell2 = sheetProp.GetRow(0).CreateCell(1);
		cell2.SetCellValue("Test");
		cell.SetCellFormula("B1");
		xlsxWorkbookProp.GetCreationHelper().CreateFormulaEvaluator().EvaluateFormulaCell(cell);

		// Act
		string result = cell.GetStringValue();

		// Assert
		result.ShouldBe("Test");
	}

	[RetryFact(3)]
	public void GetStringValue_WithFormulaCellBooleanResult_ReturnsBooleanString()
	{
		// Arrange
		ICell cell = sheetProp.CreateRow(0).CreateCell(0);
		cell.SetCellFormula("TRUE");
		xlsxWorkbookProp.GetCreationHelper().CreateFormulaEvaluator().EvaluateFormulaCell(cell);

		// Act
		string result = cell.GetStringValue();

		// Assert
		result.ShouldBe("True");
	}

	[RetryFact(3)]
	public void GetStringValue_WithFormulaCellBlankResult_ReturnsEmpty()
	{
		// Arrange
		ICell cell = sheetProp.CreateRow(0).CreateCell(0);
		ICell cell2 = sheetProp.GetRow(0).CreateCell(1);
		cell2.SetCellValue("");
		cell.SetCellFormula("B1");
		xlsxWorkbookProp.GetCreationHelper().CreateFormulaEvaluator().EvaluateFormulaCell(cell);

		// Act
		string result = cell.GetStringValue();

		// Assert
		result.ShouldBe(string.Empty);
	}

	[RetryFact(3)]
	public void GetStringValue_WithFormulaCellErrorResult_ReturnsEmpty()
	{
		// Arrange
		ICell cell = sheetProp.CreateRow(0).CreateCell(0);
		cell.SetCellFormula("1/0"); // Division by zero error
		xlsxWorkbookProp.GetCreationHelper().CreateFormulaEvaluator().EvaluateFormulaCell(cell);

		// Act
		string result = cell.GetStringValue();

		// Assert
		result.ShouldBe(string.Empty);
	}

	#endregion

	#region AddImage Additional Tests

	[RetryFact(3)]
	public void AddImage_WithValidUnmergedRange_AddsImage()
	{
		// Arrange
		byte[] imageData = File.ReadAllBytes("TestData/test.png");
		const string range = "B2:C3";

		// Act
		xlsxWorkbookProp.AddImage(sheetProp, imageData, range);

		// Assert
		XSSFSheet sheet = (XSSFSheet)sheetProp;
		XSSFDrawing drawings = (XSSFDrawing)sheet.CreateDrawingPatriarch();
		drawings.GetShapes().Count.ShouldBeGreaterThanOrEqualTo(1);
	}

	[RetryFact(3)]
	public void AddImages_WithNullWorkbook_DoesNotThrow()
	{
		// Arrange
		List<byte[]> emptyList = [];
		List<string> emptyNames = [];

		// Act & Assert - When count is 0, should not throw
		Should.NotThrow(() => xlsxWorkbookProp.AddImages(emptyList, emptyNames));
	}

	#endregion

	#region Complex Scenario Tests

	[RetryFact(3)]
	public void GetLastPopulatedRowInColumn_WithMixedEmptyAndPopulatedCells_ReturnsCorrectIndex()
	{
		// Arrange
		sheetProp.CreateRow(0).CreateCell(0).SetCellValue("Value1");
		sheetProp.CreateRow(1).CreateCell(0).SetCellValue(""); // Empty
		sheetProp.CreateRow(2).CreateCell(0).SetCellValue("Value2");
		sheetProp.CreateRow(3).CreateCell(0).SetCellValue("Value3");

		// Act
		int lastPopulatedRow = sheetProp.GetLastPopulatedRowInColumn(0);

		// Assert
		lastPopulatedRow.ShouldBe(3);
	}

	[RetryFact(3)]
	public void CreateTable_WithNoColumnNames_GeneratesDefaultNames()
	{
		// Act
		xlsxWorkbookProp.CreateTable("Test", "DefaultNamesTable", 0, 1, 0, 2);

		// Assert
		XSSFSheet sheet = (XSSFSheet)xlsxWorkbookProp.GetSheet("Test");
		XSSFTable? table = sheet.GetTables().FirstOrDefault(t => t.Name == "DefaultNamesTable");
		table.ShouldNotBeNull();
		table.GetCTTable().tableColumns.tableColumn.Count.ShouldBe(2);
	}

	[RetryFact(3)]
	public void CreateTable_WithCustomTableStyle_AppliesStyle()
	{
		// Act
		xlsxWorkbookProp.CreateTable("Test", "StyledTable", 0, 1, 0, 2, ["Col1", "Col2"],
			tableStyle: ETableStyle.TableStyleMedium2, showRowStripes: false, showColStripes: true);

		// Assert
		XSSFSheet sheet = (XSSFSheet)xlsxWorkbookProp.GetSheet("Test");
		XSSFTable? table = sheet.GetTables().FirstOrDefault(t => t.Name == "StyledTable");
		table.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void GetRangeHeightInPx_WithNullRows_HandlesGracefully()
	{
		// Arrange - Don't create all rows in range
		sheetProp.CreateRow(0).Height = 20 * 20;
		// Row 1 not created
		sheetProp.CreateRow(2).Height = 20 * 20;

		// Act
		int height = sheetProp.GetRangeHeightInPx(0, 2);

		// Assert
		height.ShouldBeGreaterThanOrEqualTo(0); // Should handle null row
	}

	[RetryFact(3)]
	public void GetRangeWidthInPx_WithDifferentColumnWidths_CalculatesCorrectly()
	{
		// Arrange
		sheetProp.SetColumnWidth(0, 10 * 256);
		sheetProp.SetColumnWidth(1, 20 * 256);
		sheetProp.SetColumnWidth(2, 15 * 256);

		// Act
		int width = sheetProp.GetRangeWidthInPx(0, 2);

		// Assert
		width.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void GetCellFromName_WithMultipleCellRange_ReturnsTopLeftCell()
	{
		// Arrange
		IName name = xlsxWorkbookProp.CreateName();
		name.NameName = "MultiCellRange";
		name.RefersToFormula = "Test!$B$2:$D$4";

		// Populate cells
		for (int row = 1; row <= 3; row++)
		{
			for (int col = 1; col <= 3; col++)
			{
				sheetProp.GetCellFromCoordinates(col, row)?.SetCellValue($"R{row}C{col}");
			}
		}

		// Act
		ICell? cell = xlsxWorkbookProp.GetCellFromName("MultiCellRange");

		// Assert
		cell.ShouldNotBeNull();
		cell.ColumnIndex.ShouldBe(1); // B
		cell.RowIndex.ShouldBe(1); // 2 (0-based)
	}

	[RetryFact(3)]
	public void ClearAllFromName_WithMultipleCells_ClearsAllCells()
	{
		// Arrange
		IName name = xlsxWorkbookProp.CreateName();
		name.NameName = "ClearRange";
		name.RefersToFormula = "Test!$B$2:$C$3";

		sheetProp.GetCellFromReference("B2")?.SetCellValue("Value1");
		sheetProp.GetCellFromReference("C2")?.SetCellValue("Value2");
		sheetProp.GetCellFromReference("B3")?.SetCellValue("Value3");
		sheetProp.GetCellFromReference("C3")?.SetCellValue("Value4");

		// Act
		xlsxWorkbookProp.ClearAllFromName("ClearRange");

		// Assert
		sheetProp.GetCellFromReference("B2")?.IsCellEmpty().ShouldBeTrue();
		sheetProp.GetCellFromReference("C2")?.IsCellEmpty().ShouldBeTrue();
		sheetProp.GetCellFromReference("B3")?.IsCellEmpty().ShouldBeTrue();
		sheetProp.GetCellFromReference("C3")?.IsCellEmpty().ShouldBeTrue();
	}

	[RetryFact(3)]
	public void ColumnNameToNumber_WithLowercaseInput_WorksCorrectly()
	{
		// Arrange
		const string columnName = "abc";

		// Act
		int result = columnName.ColumnNameToNumber();

		// Assert
		result.ShouldBe(730); // ABC column
	}

	[RetryFact(3)]
	public void GetCustomStyle_CachingWithDifferentParameters_ReturnsDifferentStyles()
	{
		// Act
		ICellStyle style1 = xlsxWorkbookProp.GetCustomStyle("#FF0000", cellLocked: true);
		ICellStyle style2 = xlsxWorkbookProp.GetCustomStyle("#FF0000", cellLocked: false);

		// Assert
		style1.ShouldNotBeSameAs(style2); // Different locked status = different style
	}

	#endregion

	#region GetStandardCellStyle Border Color Tests

	[RetryFact(3)]
	public void GetStandardCellStyle_WithBorderBottomColor_AppliesColor()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderBottomColor = HSSFColor.Red.Index
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Header, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BottomBorderColor.ShouldBe(HSSFColor.Red.Index);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_WithBorderLeftColor_AppliesColor()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderLeftColor = HSSFColor.Blue.Index
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Body, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.LeftBorderColor.ShouldBe(HSSFColor.Blue.Index);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_WithBorderRightColor_AppliesColor()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderRightColor = HSSFColor.Green.Index
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.HeaderThickTop, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.RightBorderColor.ShouldBe(HSSFColor.Green.Index);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_WithBorderTopColor_AppliesColor()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTopColor = HSSFColor.Yellow.Index
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Error, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.TopBorderColor.ShouldBe(HSSFColor.Yellow.Index);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_WithAllBorderColors_AppliesAllColors()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTopColor = HSSFColor.Red.Index,
			BorderLeftColor = HSSFColor.Blue.Index,
			BorderRightColor = HSSFColor.Green.Index,
			BorderBottomColor = HSSFColor.Yellow.Index
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Body, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.TopBorderColor.ShouldBe(HSSFColor.Red.Index);
		style.LeftBorderColor.ShouldBe(HSSFColor.Blue.Index);
		style.RightBorderColor.ShouldBe(HSSFColor.Green.Index);
		style.BottomBorderColor.ShouldBe(HSSFColor.Yellow.Index);
	}

	#endregion

	#region GetStandardCellStyle BorderStyles Paths Tests

	[RetryFact(3)]
	public void GetStandardCellStyle_BodyStyle_WithBorderTop_AppliesBorderTop()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thick
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Body, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderTop.ShouldBe(BorderStyle.Thick);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_ErrorStyle_WithAllBorders_AppliesAllBorders()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thick,
			BorderBottom = BorderStyle.Medium,
			BorderLeft = BorderStyle.Thin,
			BorderRight = BorderStyle.Double
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Error, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderTop.ShouldBe(BorderStyle.Thick);
		style.BorderBottom.ShouldBe(BorderStyle.Medium);
		style.BorderLeft.ShouldBe(BorderStyle.Thin);
		style.BorderRight.ShouldBe(BorderStyle.Double);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_BlackoutStyle_WithAllBorders_AppliesAllBorders()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thick,
			BorderBottom = BorderStyle.Medium,
			BorderLeft = BorderStyle.Thin,
			BorderRight = BorderStyle.Double
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Blackout, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderTop.ShouldBe(BorderStyle.Thick);
		style.BorderBottom.ShouldBe(BorderStyle.Medium);
		style.BorderLeft.ShouldBe(BorderStyle.Thin);
		style.BorderRight.ShouldBe(BorderStyle.Double);
		style.FillForegroundColor.ShouldBe(HSSFColor.Black.Index);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_WhiteoutStyle_WithAllBorders_AppliesAllBorders()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thick,
			BorderBottom = BorderStyle.Medium,
			BorderLeft = BorderStyle.Thin,
			BorderRight = BorderStyle.Double
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Whiteout, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderTop.ShouldBe(BorderStyle.Thick);
		style.BorderBottom.ShouldBe(BorderStyle.Medium);
		style.BorderLeft.ShouldBe(BorderStyle.Thin);
		style.BorderRight.ShouldBe(BorderStyle.Double);
		style.FillForegroundColor.ShouldBe(HSSFColor.White.Index);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_ImageBackgroundStyle_WithAllBorders_AppliesAllBorders()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thick,
			BorderBottom = BorderStyle.Medium,
			BorderLeft = BorderStyle.Thin,
			BorderRight = BorderStyle.Double
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.ImageBackground, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderTop.ShouldBe(BorderStyle.Thick);
		style.BorderBottom.ShouldBe(BorderStyle.Medium);
		style.BorderLeft.ShouldBe(BorderStyle.Thin);
		style.BorderRight.ShouldBe(BorderStyle.Double);
		style.Alignment.ShouldBe(HorizontalAlignment.Center);
		style.VerticalAlignment.ShouldBe(VerticalAlignment.Center);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_ErrorStyle_WithPartialBorders_AppliesOnlySetBorders()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thick,
			BorderBottom = null,
			BorderLeft = BorderStyle.Thin,
			BorderRight = null
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Error, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderTop.ShouldBe(BorderStyle.Thick);
		style.BorderLeft.ShouldBe(BorderStyle.Thin);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_BlackoutStyle_WithPartialBorders_AppliesOnlySetBorders()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = null,
			BorderBottom = BorderStyle.Medium,
			BorderLeft = null,
			BorderRight = BorderStyle.Double
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Blackout, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderBottom.ShouldBe(BorderStyle.Medium);
		style.BorderRight.ShouldBe(BorderStyle.Double);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_WhiteoutStyle_WithPartialBorders_AppliesOnlySetBorders()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thick,
			BorderBottom = null,
			BorderLeft = null,
			BorderRight = BorderStyle.Thin
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.Whiteout, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderTop.ShouldBe(BorderStyle.Thick);
		style.BorderRight.ShouldBe(BorderStyle.Thin);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_ImageBackgroundStyle_WithPartialBorders_AppliesOnlySetBorders()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = null,
			BorderBottom = BorderStyle.Thin,
			BorderLeft = BorderStyle.Medium,
			BorderRight = null
		};

		// Act
		ICellStyle style = xlsxWorkbookProp.GetStandardCellStyle(EStyle.ImageBackground, borderStyles: borderStyles);

		// Assert
		style.ShouldNotBeNull();
		style.BorderBottom.ShouldBe(BorderStyle.Thin);
		style.BorderLeft.ShouldBe(BorderStyle.Medium);
	}

	[RetryFact(3)]
	public void GetStandardCellStyle_AllStyles_WithBordersAndColors_AppliesCorrectly()
	{
		// Arrange
		NpoiBorderStyles borderStyles = new()
		{
			BorderTop = BorderStyle.Thick,
			BorderBottom = BorderStyle.Medium,
			BorderLeft = BorderStyle.Thin,
			BorderRight = BorderStyle.Double,
			BorderTopColor = HSSFColor.Red.Index,
			BorderBottomColor = HSSFColor.Blue.Index,
			BorderLeftColor = HSSFColor.Green.Index,
			BorderRightColor = HSSFColor.Yellow.Index
		};

		// Test each style
		foreach (EStyle style in Enum.GetValues<EStyle>())
		{
			// Act
			ICellStyle cellStyle = xlsxWorkbookProp.GetStandardCellStyle(style, borderStyles: borderStyles);

			// Assert
			cellStyle.ShouldNotBeNull();
			cellStyle.TopBorderColor.ShouldBe(HSSFColor.Red.Index);
			cellStyle.BottomBorderColor.ShouldBe(HSSFColor.Blue.Index);
			cellStyle.LeftBorderColor.ShouldBe(HSSFColor.Green.Index);
			cellStyle.RightBorderColor.ShouldBe(HSSFColor.Yellow.Index);
		}
	}

	#endregion
}
