using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using CommonNetFuncs.Excel.ClosedXml;
using CommonNetFuncs.Excel.Common;
using NSubstitute;
using System.Data;

namespace Excel.ClosedXml.Tests;

public sealed class CommonTests : IDisposable
{
	private readonly XLWorkbook workbook;
	private readonly IXLWorksheet sheet;

	public CommonTests()
	{
		workbook = new XLWorkbook();
		sheet = workbook.AddWorksheet("Test");
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
				workbook?.Dispose();
			}
			disposed = true;
		}
	}

	~CommonTests()
	{
		Dispose(false);
	}

	#region ClosedXmlBorderStyles Tests

	[Fact]
	public void ClosedXmlBorderStyles_FromNullStyle_HasNullProperties()
	{
		ClosedXmlBorderStyles bs = new(cellStyle: null);
		bs.BorderTop.ShouldBeNull();
		bs.BorderLeft.ShouldBeNull();
		bs.BorderRight.ShouldBeNull();
		bs.BorderBottom.ShouldBeNull();
		bs.BorderTopColor.ShouldBeNull();
		bs.BorderLeftColor.ShouldBeNull();
		bs.BorderRightColor.ShouldBeNull();
		bs.BorderBottomColor.ShouldBeNull();
	}

	[Fact]
	public void ClosedXmlBorderStyles_FromCellStyle_ExtractsBorders()
	{
		// Arrange
		sheet.Cell(1, 1).Style.Border.TopBorder = XLBorderStyleValues.Thick;
		sheet.Cell(1, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
		sheet.Cell(1, 1).Style.Border.LeftBorder = XLBorderStyleValues.Medium;
		sheet.Cell(1, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;

		// Act
		ClosedXmlBorderStyles bs = new(sheet.Cell(1, 1).Style);

		// Assert
		bs.BorderTop.ShouldBe(XLBorderStyleValues.Thick);
		bs.BorderBottom.ShouldBe(XLBorderStyleValues.Thin);
		bs.BorderLeft.ShouldBe(XLBorderStyleValues.Medium);
		bs.BorderRight.ShouldBe(XLBorderStyleValues.Thin);
	}

	[Fact]
	public void ClosedXmlBorderStyles_ExplicitConstructor_SetsProperties()
	{
		ClosedXmlBorderStyles bs = new(
			borderTop: XLBorderStyleValues.Thick,
			borderLeft: XLBorderStyleValues.Medium,
			borderRight: XLBorderStyleValues.Thin,
			borderBottom: XLBorderStyleValues.Double,
			borderTopColor: XLColor.Red,
			borderLeftColor: XLColor.Blue,
			borderRightColor: XLColor.Green,
			borderBottomColor: XLColor.Black);

		bs.BorderTop.ShouldBe(XLBorderStyleValues.Thick);
		bs.BorderLeft.ShouldBe(XLBorderStyleValues.Medium);
		bs.BorderRight.ShouldBe(XLBorderStyleValues.Thin);
		bs.BorderBottom.ShouldBe(XLBorderStyleValues.Double);
		bs.BorderTopColor.ShouldBe(XLColor.Red);
		bs.BorderLeftColor.ShouldBe(XLColor.Blue);
		bs.BorderRightColor.ShouldBe(XLColor.Green);
		bs.BorderBottomColor.ShouldBe(XLColor.Black);
	}

	[Fact]
	public void ClosedXmlBorderStyles_ExtractBorderStyles_UpdatesFromStyle()
	{
		ClosedXmlBorderStyles bs = new(borderTop: XLBorderStyleValues.Thick);
		sheet.Cell(2, 2).Style.Border.TopBorder = XLBorderStyleValues.Thin;
		bs.ExtractBorderStyles(sheet.Cell(2, 2).Style);
		bs.BorderTop.ShouldBe(XLBorderStyleValues.Thin);
	}

	#endregion

	#region IsCellEmpty Tests

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	public void IsCellEmpty_WithEmptyValues_ShouldReturnTrue(string? value)
	{
		IXLCell cell = Substitute.For<IXLCell>();
		cell.Value.Returns(value);
		cell.IsCellEmpty().ShouldBeTrue();
	}

	[Theory]
	[InlineData("Test")]
	[InlineData("123")]
	[InlineData("0")]
	public void IsCellEmpty_WithNonEmptyValues_ShouldReturnFalse(string value)
	{
		IXLCell cell = Substitute.For<IXLCell>();
		cell.Value.Returns(value);
		cell.IsCellEmpty().ShouldBeFalse();
	}

	#endregion

	#region GetStringValue Tests

	[Fact]
	public void GetStringValue_WithNullCell_ReturnsNull()
	{
		IXLCell? cell = null;
		cell.GetStringValue().ShouldBeNull();
	}

	[Fact]
	public void GetStringValue_WithStringCell_ReturnsValue()
	{
		sheet.Cell(1, 1).Value = "Hello";
		sheet.Cell(1, 1).GetStringValue().ShouldBe("Hello");
	}

	[Fact]
	public void GetStringValue_WithNumericCell_ReturnsStringRepresentation()
	{
		sheet.Cell(1, 1).Value = 42.5;
		string result = sheet.Cell(1, 1).GetStringValue();
		result.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void GetStringValue_WithBoolCell_ReturnsStringRepresentation()
	{
		sheet.Cell(1, 1).Value = true;
		string result = sheet.Cell(1, 1).GetStringValue();
		result.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void GetStringValue_WithBlankCell_ReturnsEmpty()
	{
		IXLCell cell = sheet.Cell(5, 5); // fresh cell, blank
		cell.DataType.ShouldBe(XLDataType.Blank);
		cell.GetStringValue().ShouldBe(string.Empty);
	}

	#endregion

	#region GetCellFromReference Tests

	[Fact]
	public void GetCellFromReference_WithB2_ReturnsCellAtB2()
	{
		IXLCell? cell = sheet.GetCellFromReference("B2");
		cell.ShouldNotBeNull();
		cell!.Address.RowNumber.ShouldBe(2);
		cell.Address.ColumnNumber.ShouldBe(2);
	}

	[Fact]
	public void GetCellFromReference_WithRange_ReturnsTopLeftCell()
	{
		IXLCell? cell = sheet.GetCellFromReference("B2:D5");
		cell.ShouldNotBeNull();
		cell!.Address.RowNumber.ShouldBe(2);
		cell.Address.ColumnNumber.ShouldBe(2);
	}

	[Theory]
	[InlineData(1, 0)]   // right
	[InlineData(-1, 0)]  // left
	[InlineData(0, 1)]   // down
	[InlineData(0, -1)]  // up
	[InlineData(2, 3)]   // diagonal
	public void GetCellFromReference_WithOffset_ReturnsOffsetCell(int colOffset, int rowOffset)
	{
		IXLCell? cell = sheet.GetCellFromReference("B2", colOffset, rowOffset);
		cell.ShouldNotBeNull();
		cell!.Address.RowNumber.ShouldBe(2 + rowOffset);
		cell.Address.ColumnNumber.ShouldBe(2 + colOffset);
	}

	#endregion

	#region GetCellOffset Tests

	[Theory]
	[InlineData(1, 0)]
	[InlineData(-1, 0)]
	[InlineData(0, 1)]
	[InlineData(0, -1)]
	public void GetCellOffset_WithValidOffsets_ReturnsCorrectCell(int colOffset, int rowOffset)
	{
		IXLCell start = sheet.Cell(3, 3); // C3
		IXLCell? result = start.GetCellOffset(colOffset, rowOffset);
		result.ShouldNotBeNull();
		result!.Address.ColumnNumber.ShouldBe(3 + colOffset);
		result.Address.RowNumber.ShouldBe(3 + rowOffset);
	}

	[Fact]
	public void GetCellOffset_WithZeroOffset_ReturnsSamePosition()
	{
		IXLCell start = sheet.Cell(2, 2);
		IXLCell? result = start.GetCellOffset(0, 0);
		result.ShouldNotBeNull();
		result!.Address.ColumnNumber.ShouldBe(2);
		result.Address.RowNumber.ShouldBe(2);
	}

	[Fact]
	public void GetCellOffset_WithOutOfRangeNegativeOffset_ReturnsNull()
	{
		// ClosedXML throws for row/col < 1; catch block should return null
		IXLCell start = sheet.Cell(1, 1);
		IXLCell? result = start.GetCellOffset(rowOffset: -1); // row 0 is invalid
		result.ShouldBeNull();
	}

	#endregion

	#region GetCellFromCoordinates Tests

	[Theory]
	[InlineData(1, 1)]
	[InlineData(3, 5)]
	[InlineData(10, 10)]
	public void GetCellFromCoordinates_ReturnsCorrectCell(int colIndex, int rowIndex)
	{
		IXLCell? cell = sheet.GetCellFromCoordinates(colIndex, rowIndex);
		cell.ShouldNotBeNull();
		cell!.Address.ColumnNumber.ShouldBe(colIndex);
		cell.Address.RowNumber.ShouldBe(rowIndex);
	}

	[Theory]
	[InlineData(1, 1, 1, 1)]
	[InlineData(2, 2, 2, 3)]
	public void GetCellFromCoordinates_WithOffset_ReturnsOffsetCell(int col, int row, int colOff, int rowOff)
	{
		IXLCell? cell = sheet.GetCellFromCoordinates(col, row, colOff, rowOff);
		cell.ShouldNotBeNull();
		cell!.Address.ColumnNumber.ShouldBe(col + colOff);
		cell.Address.RowNumber.ShouldBe(row + rowOff);
	}

	[Fact]
	public void GetCellFromCoordinates_WithNegativeIndices_ReturnsNull()
	{
		// ClosedXML throws for row/col < 1; catch block should return null
		IXLCell? cell = sheet.GetCellFromCoordinates(1, -1); // row -1 is invalid
		cell.ShouldBeNull();
	}

	#endregion

	#region GetLastPopulatedRowInColumn Tests

	[Fact]
	public void GetLastPopulatedRowInColumn_WithPopulatedCells_ReturnsLastRow()
	{
		sheet.Cell(1, 1).Value = "Row1";
		sheet.Cell(2, 1).Value = "Row2";
		sheet.Cell(3, 1).Value = "Row3";

		int last = sheet.GetLastPopulatedRowInColumn(1);
		last.ShouldBe(3);
	}

	[Fact]
	public void GetLastPopulatedRowInColumn_WithEmptyColumn_ReturnsZero()
	{
		using XLWorkbook wb2 = new();
		IXLWorksheet ws2 = wb2.AddWorksheet("Empty");
		ws2.GetLastPopulatedRowInColumn(1).ShouldBe(0);
	}

	[Fact]
	public void GetLastPopulatedRowInColumn_WithGapRows_IgnoresTrailingEmpty()
	{
		sheet.Cell(1, 2).Value = "A";
		sheet.Cell(2, 2).Value = "B";
		// row 3 col 2 is empty

		int last = sheet.GetLastPopulatedRowInColumn(2);
		last.ShouldBe(2);
	}

	[Theory]
	[InlineData("A")]
	[InlineData("B")]
	[InlineData("C")]
	public void GetLastPopulatedRowInColumn_ByColumnName_ReturnsLastRow(string colName)
	{
		int colNum = XLHelper.GetColumnNumberFromLetter(colName);
		sheet.Cell(1, colNum).Value = "Val1";
		sheet.Cell(2, colNum).Value = "Val2";
		sheet.Cell(3, colNum).Value = "Val3";

		int last = sheet.GetLastPopulatedRowInColumn(colName);
		last.ShouldBe(3);
	}

	[Fact]
	public void GetLastPopulatedRowInColumn_WithGapAboveDataColumn_ExercisesEmptyCellBranch()
	{
		// col B row 5 sets lastRow used = 5; col A only has data at row 2
		// scanning col A from row 5→2 exercises the empty-cell false branch
		using XLWorkbook wb2 = new();
		IXLWorksheet ws2 = wb2.AddWorksheet("G");
		ws2.Cell(5, 2).Value = "FarDown"; // makes lastRow = 5
		ws2.Cell(2, 1).Value = "Data";    // data only at row 2, col A

		int result = ws2.GetLastPopulatedRowInColumn(1);
		result.ShouldBe(2);
	}

	#endregion

	#region GetCellFromName Tests

	[Fact]
	public void GetCellFromName_WithValidName_ReturnsCellAtNamedRange()
	{
		workbook.DefinedNames.Add("MyCell", "Test!$B$3");

		IXLCell? cell = workbook.GetCellFromName("MyCell");
		cell.ShouldNotBeNull();
		cell!.Address.ColumnNumber.ShouldBe(2);
		cell.Address.RowNumber.ShouldBe(3);
	}

	[Fact]
	public void GetCellFromName_WithOffset_ReturnsOffsetCell()
	{
		workbook.DefinedNames.Add("OffsetCell", "Test!$B$3");

		IXLCell? cell = workbook.GetCellFromName("OffsetCell", 1, 1);
		cell.ShouldNotBeNull();
		cell!.Address.ColumnNumber.ShouldBe(3);
		cell.Address.RowNumber.ShouldBe(4);
	}

	[Fact]
	public void GetCellFromName_WithNonExistentName_ReturnsNull()
	{
		IXLCell? cell = workbook.GetCellFromName("DoesNotExist");
		cell.ShouldBeNull();
	}

	[Fact]
	public void GetCellFromName_WithMultipleRanges_ReturnsTopLeftMostCell()
	{
		// Define a name with two ranges; second range should be chosen as top-left
		using XLWorkbook wb2 = new();
		IXLWorksheet _ = wb2.AddWorksheet("MR");
		wb2.DefinedNames.Add("MultiRange", "'MR'!$C$5,'MR'!$A$1");

		IXLCell? cell = wb2.GetCellFromName("MultiRange");
		cell.ShouldNotBeNull();
		// A1 (row 1, col 1) is higher/left than C5 (row 5, col 3)
		cell!.Address.RowNumber.ShouldBe(1);
		cell.Address.ColumnNumber.ShouldBe(1);
	}

	[Fact]
	public void GetCellFromName_WithDeadSheetReference_ReturnsNull()
	{
		// A defined name pointing to a non-existent sheet has no resolvable ranges;
		// topLeftCell stays null after the foreach loop → returns null
		using XLWorkbook wb2 = new();
		wb2.DefinedNames.Add("DeadRef", "NoSuchSheet!$A$1");

		IXLCell? cell = wb2.GetCellFromName("DeadRef");
		cell.ShouldBeNull();
	}

	#endregion

	#region ClearAllFromName Tests

	[Fact]
	public void ClearAllFromName_WithValidRange_ClearsCellContents()
	{
		sheet.Cell(1, 1).Value = "ClearMe";
		sheet.Cell(2, 1).Value = "ClearMeToo";
		workbook.DefinedNames.Add("ClearRange", "Test!$A$1:$A$2");

		workbook.ClearAllFromName("ClearRange");

		sheet.Cell(1, 1).IsEmpty().ShouldBeTrue();
		sheet.Cell(2, 1).IsEmpty().ShouldBeTrue();
	}

	[Fact]
	public void ClearAllFromName_WithNonExistentName_DoesNotThrow()
	{
		Should.NotThrow(() => workbook.ClearAllFromName("NonExistent"));
	}

	#endregion

	#region GetRangeOfMergedCells Tests

	[Fact]
	public void GetRangeOfMergedCells_WithNullCell_ReturnsNull()
	{
		IXLCell? cell = null;
		cell.GetRangeOfMergedCells().ShouldBeNull();
	}

	[Fact]
	public void GetRangeOfMergedCells_WithUnmergedCell_ReturnsSingleCellRange()
	{
		IXLRange? range = sheet.Cell(1, 1).GetRangeOfMergedCells();
		range.ShouldNotBeNull();
		range!.RowCount().ShouldBe(1);
		range.ColumnCount().ShouldBe(1);
	}

	[Fact]
	public void GetRangeOfMergedCells_WithMergedCell_ReturnsMergedRange()
	{
		sheet.Range("A1:C3").Merge();
		IXLRange? range = sheet.Cell(1, 1).GetRangeOfMergedCells();
		range.ShouldNotBeNull();
		range!.RowCount().ShouldBe(3);
		range.ColumnCount().ShouldBe(3);
	}

	#endregion

	#region GetRange Tests

	[Fact]
	public void GetRange_WithValidRange_ReturnsCorrectDimensions()
	{
		IXLCell[,] cells = sheet.GetRange("A1:C3");
		cells.GetLength(0).ShouldBe(3); // 3 rows
		cells.GetLength(1).ShouldBe(3); // 3 cols
	}

	[Fact]
	public void GetRange_ContainsCellValues()
	{
		sheet.Cell(1, 1).Value = "R1C1";
		sheet.Cell(1, 2).Value = "R1C2";
		sheet.Cell(2, 1).Value = "R2C1";
		sheet.Cell(2, 2).Value = "R2C2";

		IXLCell[,] cells = sheet.GetRange("A1:B2");
		cells[0, 0].GetStringValue().ShouldBe("R1C1");
		cells[0, 1].GetStringValue().ShouldBe("R1C2");
		cells[1, 0].GetStringValue().ShouldBe("R2C1");
		cells[1, 1].GetStringValue().ShouldBe("R2C2");
	}

	[Fact]
	public void GetRange_SingleCell_ReturnsSingleElementArray()
	{
		IXLCell[,] cells = sheet.GetRange("B2:B2");
		cells.GetLength(0).ShouldBe(1);
		cells.GetLength(1).ShouldBe(1);
	}

	#endregion

	#region AddDataValidation Tests

	[Fact]
	public void AddDataValidation_WithStringRange_CreatesValidation()
	{
		List<string> options = ["Red", "Green", "Blue"];
		sheet.AddDataValidation("A1:A5", options);

		IXLDataValidation? validation = sheet.DataValidations.FirstOrDefault();
		validation.ShouldNotBeNull();
		validation!.ShowErrorMessage.ShouldBeTrue();
		validation.ErrorStyle.ShouldBe(XLErrorStyle.Stop);
	}

	[Fact]
	public void AddDataValidation_WithIXLRange_CreatesValidation()
	{
		List<string> options = ["Option1", "Option2"];
		IXLRange range = sheet.Range("B1:B10");
		sheet.AddDataValidation(range, options);

		sheet.DataValidations.Count().ShouldBe(1);
	}

	[Fact]
	public void AddDataValidation_ErrorMessageIsSet()
	{
		sheet.AddDataValidation("C1:C5", ["A", "B"]);
		IXLDataValidation v = sheet.DataValidations.First();
		v.ErrorTitle.ShouldBe("InvalidValue");
		v.ErrorMessage.ShouldBe("Selected value must be in list");
		v.ShowInputMessage.ShouldBeFalse();
	}

	#endregion

	#region ReadExcelFileToDataTable Tests

	[Fact]
	public void ReadExcelFileToDataTable_WithHeaders_ReturnsCorrectDataTable()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Sheet1");
		ws.Cell(1, 1).Value = "Name";
		ws.Cell(1, 2).Value = "Age";
		ws.Cell(2, 1).Value = "Alice";
		ws.Cell(2, 2).Value = "30";
		ws.Cell(3, 1).Value = "Bob";
		ws.Cell(3, 2).Value = "25";

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		DataTable dt = ms.ReadExcelFileToDataTable(hasHeaders: true, cancellationToken: TestContext.Current.CancellationToken);
		dt.Columns.Count.ShouldBe(2);
		dt.Columns[0].ColumnName.ShouldBe("Name");
		dt.Columns[1].ColumnName.ShouldBe("Age");
		dt.Rows.Count.ShouldBe(2);
		dt.Rows[0]["Name"].ToString().ShouldBe("Alice");
		dt.Rows[1]["Name"].ToString().ShouldBe("Bob");
	}

	[Fact]
	public void ReadExcelFileToDataTable_WithoutHeaders_UseColumnIndexNames()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Sheet1");
		ws.Cell(1, 1).Value = "DataA";
		ws.Cell(1, 2).Value = "DataB";
		ws.Cell(2, 1).Value = "DataC";
		ws.Cell(2, 2).Value = "DataD";

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		DataTable dt = ms.ReadExcelFileToDataTable(hasHeaders: false, cancellationToken: TestContext.Current.CancellationToken);
		dt.Columns[0].ColumnName.ShouldBe("Column0");
		dt.Columns[1].ColumnName.ShouldBe("Column1");
		dt.Rows.Count.ShouldBe(2);
	}

	[Fact]
	public void ReadExcelFileToDataTable_WithSheetName_ReadsCorrectSheet()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws1 = wb.AddWorksheet("First");
		ws1.Cell(1, 1).Value = "H1";
		ws1.Cell(2, 1).Value = "D1";
		IXLWorksheet ws2 = wb.AddWorksheet("Second");
		ws2.Cell(1, 1).Value = "H2";
		ws2.Cell(2, 1).Value = "D2";

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		DataTable dt = ms.ReadExcelFileToDataTable(hasHeaders: true, sheetName: "Second", cancellationToken: TestContext.Current.CancellationToken);
		dt.Columns[0].ColumnName.ShouldBe("H2");
		dt.Rows[0][0].ToString().ShouldBe("D2");
	}

	[Fact]
	public void ReadExcelFileToDataTable_WithStartAndEndRef_ReadsSubRange()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Sheet1");
		ws.Cell(2, 2).Value = "ColB";
		ws.Cell(2, 3).Value = "ColC";
		ws.Cell(3, 2).Value = "Val1";
		ws.Cell(3, 3).Value = "Val2";
		ws.Cell(4, 2).Value = "Val3";
		ws.Cell(4, 3).Value = "Val4";

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		DataTable dt = ms.ReadExcelFileToDataTable(hasHeaders: true, startCellReference: "B2", endCellReference: "C4", cancellationToken: TestContext.Current.CancellationToken);
		dt.Columns.Count.ShouldBe(2);
		dt.Columns[0].ColumnName.ShouldBe("ColB");
		dt.Rows.Count.ShouldBe(2);
	}

	[Fact]
	public async Task ReadExcelFileToDataTable_WithCancellation_RespectsToken()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Sheet1");
		ws.Cell(1, 1).Value = "H";
		for (int i = 2; i <= 100; i++)
		{
			ws.Cell(i, 1).Value = $"Row{i}";
		}

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		// Should not throw - cancellation returns partial/empty results
		DataTable dt = ms.ReadExcelFileToDataTable(hasHeaders: true, cancellationToken: cts.Token);
		dt.ShouldNotBeNull();
	}

	[Fact]
	public void ReadExcelFileToDataTable_WithWhitespaceOnlyDataRow_StopsBeforeIt()
	{
		// Row 3 has whitespace in the scan range but real data outside it,
		// so row.IsEmpty() is false but rowHasData stays false → break
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("WS");
		ws.Cell(1, 1).Value = "Name";
		ws.Cell(1, 2).Value = "Age";
		ws.Cell(2, 1).Value = "Alice";
		ws.Cell(2, 2).Value = "30";
		ws.Cell(3, 1).Value = " "; // whitespace in scan range
		ws.Cell(3, 2).Value = " "; // whitespace in scan range
		ws.Cell(3, 3).Value = "OutOfRange"; // real data outside range makes IsEmpty() false

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		DataTable dt = ms.ReadExcelFileToDataTable(hasHeaders: true, cancellationToken: TestContext.Current.CancellationToken);
		dt.Columns.Count.ShouldBe(2);
		dt.Rows.Count.ShouldBe(1); // only Alice, whitespace row triggers break
	}

	[Fact]
	public void ReadExcelFileToDataTable_WithEndRefAndCancelledToken_ReturnsEmpty()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("CT");
		ws.Cell(1, 1).Value = "H1";
		for (int i = 2; i <= 10; i++)
		{
			ws.Cell(i, 1).Value = $"D{i}";
		}

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		using CancellationTokenSource cts = new();
		cts.Cancel();

		// cancelled token with startRef+endRef path
		DataTable dt = ms.ReadExcelFileToDataTable(hasHeaders: true, startCellReference: "A1", endCellReference: "A10", cancellationToken: cts.Token);
		dt.ShouldNotBeNull();
	}

	#endregion

	#region ReadExcelTableToDataTable Tests

	[Fact]
	public void ReadExcelTableToDataTable_WithNamedTable_ReturnsTableData()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Sheet1");
		ws.Cell(1, 1).Value = "Col1";
		ws.Cell(1, 2).Value = "Col2";
		ws.Cell(2, 1).Value = "A";
		ws.Cell(2, 2).Value = "B";
		ws.Cell(3, 1).Value = "C";
		ws.Cell(3, 2).Value = "D";
		IXLTable table = ws.Range("A1:B3").CreateTable("MyTable");
		table.Name = "MyTable";

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		DataTable dt = ms.ReadExcelTableToDataTable("MyTable", cancellationToken: TestContext.Current.CancellationToken);
		dt.Columns.Count.ShouldBe(2);
		dt.Columns[0].ColumnName.ShouldBe("Col1");
		dt.Rows.Count.ShouldBe(2);
		dt.Rows[0][0].ToString().ShouldBe("A");
		dt.Rows[1][0].ToString().ShouldBe("C");
	}

	[Fact]
	public void ReadExcelTableToDataTable_WithNoTableName_ReturnsFirstTable()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Sheet1");
		ws.Cell(1, 1).Value = "Header";
		ws.Cell(2, 1).Value = "Value";
		ws.Range("A1:A2").CreateTable("FirstTable");

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		DataTable dt = ms.ReadExcelTableToDataTable(cancellationToken: TestContext.Current.CancellationToken);
		dt.Columns.Count.ShouldBe(1);
		dt.Rows.Count.ShouldBe(1);
	}

	[Fact]
	public void ReadExcelTableToDataTable_WithNonExistentTableName_ReadsFirstTable()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Sheet1");
		ws.Cell(1, 1).Value = "H";
		ws.Cell(2, 1).Value = "V";
		ws.Range("A1:A2").CreateTable("ExistingTable");

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		DataTable dt = ms.ReadExcelTableToDataTable("NoSuchTable", cancellationToken: TestContext.Current.CancellationToken);
		dt.ShouldNotBeNull();
		dt.Rows.Count.ShouldBe(1); // falls back to first table
	}

	[Fact]
	public async Task ReadExcelTableToDataTable_WithCancellation_RespectsToken()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Sheet1");
		ws.Cell(1, 1).Value = "Col";
		for (int i = 2; i <= 50; i++)
		{
			ws.Cell(i, 1).Value = $"R{i}";
		}
		ws.Range("A1:A50").CreateTable("BigTable");

		using MemoryStream ms = new();
		wb.SaveAs(ms);
		ms.Position = 0;

		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		DataTable dt = ms.ReadExcelTableToDataTable(cancellationToken: cts.Token);
		dt.ShouldNotBeNull();
	}

	#endregion

	#region ColumnNameToNumber / ColumnIndexToName Tests

	[Theory]
	[InlineData("A", 1)]
	[InlineData("B", 2)]
	[InlineData("Z", 26)]
	[InlineData("AA", 27)]
	[InlineData("AZ", 52)]
	[InlineData("BA", 53)]
	public void ColumnNameToNumber_ReturnsCorrect1BasedIndex(string name, int expected)
	{
		name.ColumnNameToNumber().ShouldBe(expected);
	}

	[Fact]
	public void ColumnNameToNumber_WithNullOrEmpty_Throws()
	{
		Should.Throw<ArgumentException>(() => Common.ColumnNameToNumber(null));
		Should.Throw<ArgumentException>(() => Common.ColumnNameToNumber(string.Empty));
	}

	[Theory]
	[InlineData(1, "A")]
	[InlineData(2, "B")]
	[InlineData(26, "Z")]
	[InlineData(27, "AA")]
	[InlineData(52, "AZ")]
	[InlineData(702, "ZZ")]
	public void ColumnIndexToName_Int_ReturnsCorrectName(int col, string expected)
	{
		col.ColumnIndexToName().ShouldBe(expected);
	}

	[Theory]
	[InlineData(1, "A")]
	[InlineData(26, "Z")]
	[InlineData(27, "AA")]
	public void ColumnIndexToName_NullableInt_ReturnsCorrectName(int col, string expected)
	{
		int? nullable = col;
		nullable.ColumnIndexToName().ShouldBe(expected);
	}

	[Fact]
	public void ColumnIndexToName_WithZeroOrNegative_Throws()
	{
		Should.Throw<ArgumentException>(() => 0.ColumnIndexToName());
		Should.Throw<ArgumentException>(() => (-1).ColumnIndexToName());
	}

	[Fact]
	public void ColumnIndexToName_NullableInt_WithNullOrBelowOne_Throws()
	{
		int? nullVal = null;
		Should.Throw<ArgumentException>(() => nullVal.ColumnIndexToName());
		int? zero = 0;
		Should.Throw<ArgumentException>(() => zero.ColumnIndexToName());
	}

	#endregion

	#region WriteExcelFile Tests

	[Fact]
	public void WriteExcelFile_WhenSuccessful_ShouldReturnTrueAndCreateFile()
	{
		string tempPath = Path.GetTempFileName();
		try
		{
			using XLWorkbook wb = new();
			wb.AddWorksheet("TestSheet");
			Common.WriteExcelFile(wb, tempPath).ShouldBeTrue();
			File.Exists(tempPath).ShouldBeTrue();
			new FileInfo(tempPath).Length.ShouldBeGreaterThan(0);
		}
		finally
		{
			if (File.Exists(tempPath)) File.Delete(tempPath);
		}
	}

	[Fact]
	public void WriteExcelFile_WhenPathIsInvalid_ShouldReturnFalse()
	{
		using XLWorkbook wb = new();
		Common.WriteExcelFile(wb, "Z:\\invalid\\path\\file.xlsx").ShouldBeFalse();
	}

	#endregion

	#region GetStandardCellStyle Tests

	[Theory]
	[InlineData(EStyle.Header)]
	[InlineData(EStyle.HeaderThickTop)]
	[InlineData(EStyle.Body)]
	[InlineData(EStyle.Error)]
	[InlineData(EStyle.Blackout)]
	[InlineData(EStyle.Whiteout)]
	[InlineData(EStyle.ImageBackground)]
	public void GetStandardCellStyle_WithAllStyles_ReturnsNonNull(EStyle style)
	{
		IXLStyle? result = workbook.GetStandardCellStyle(style);
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetStandardCellStyle_Header_HasCenteredAlignmentAndGrayFill()
	{
		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.Header);
		style.ShouldNotBeNull();
		style!.Alignment.Horizontal.ShouldBe(XLAlignmentHorizontalValues.Center);
		style.Fill.BackgroundColor.ShouldBe(XLColor.LightGray);
		style.Border.BottomBorder.ShouldBe(XLBorderStyleValues.Thin);
	}

	[Fact]
	public void GetStandardCellStyle_HeaderThickTop_HasMediumTopBorder()
	{
		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.HeaderThickTop);
		style.ShouldNotBeNull();
		style!.Border.TopBorder.ShouldBe(XLBorderStyleValues.Medium);
		style.Fill.BackgroundColor.ShouldBe(XLColor.LightGray);
	}

	[Fact]
	public void GetStandardCellStyle_Error_HasRedFill()
	{
		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.Error);
		style.ShouldNotBeNull();
		style!.Fill.BackgroundColor.ShouldBe(XLColor.Red);
		style.Fill.PatternType.ShouldBe(XLFillPatternValues.Solid);
	}

	[Fact]
	public void GetStandardCellStyle_Blackout_HasBlackFont_And_BlackFill()
	{
		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.Blackout);
		style.ShouldNotBeNull();
		style!.Font.FontColor.ShouldBe(XLColor.Black);
		style.Fill.BackgroundColor.ShouldBe(XLColor.Black);
	}

	[Fact]
	public void GetStandardCellStyle_Whiteout_HasWhiteFont_And_WhiteFill()
	{
		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.Whiteout);
		style.ShouldNotBeNull();
		style!.Font.FontColor.ShouldBe(XLColor.White);
		style.Fill.BackgroundColor.ShouldBe(XLColor.White);
	}

	[Fact]
	public void GetStandardCellStyle_ImageBackground_HasCenterAlignmentAndDefaultFill()
	{
		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.ImageBackground);
		style.ShouldNotBeNull();
		style!.Alignment.Horizontal.ShouldBe(XLAlignmentHorizontalValues.Center);
		style.Alignment.Vertical.ShouldBe(XLAlignmentVerticalValues.Center);
	}

	[Fact]
	public void GetStandardCellStyle_WithCellLocked_SetsLockedProtection()
	{
		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.Header, cellLocked: true);
		style.ShouldNotBeNull();
		style!.Protection.Locked.ShouldBeTrue();
	}

	[Fact]
	public void GetStandardCellStyle_WithWrapText_SetsWrapText()
	{
		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.Body, wrapText: true);
		style.ShouldNotBeNull();
		style!.Alignment.WrapText.ShouldBeTrue();
	}

	[Fact]
	public void GetStandardCellStyle_WithBorderStyles_OverridesBorders()
	{
		ClosedXmlBorderStyles bs = new(
			borderTop: XLBorderStyleValues.Thick,
			borderLeft: XLBorderStyleValues.Double,
			borderRight: XLBorderStyleValues.Medium,
			borderBottom: XLBorderStyleValues.Thin);

		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.Error, borderStyles: bs);
		style.ShouldNotBeNull();
		style!.Border.TopBorder.ShouldBe(XLBorderStyleValues.Thick);
		style.Border.LeftBorder.ShouldBe(XLBorderStyleValues.Double);
		style.Border.RightBorder.ShouldBe(XLBorderStyleValues.Medium);
		style.Border.BottomBorder.ShouldBe(XLBorderStyleValues.Thin);
	}

	[Fact]
	public void GetStandardCellStyle_WithBorderColors_AppliesColors()
	{
		ClosedXmlBorderStyles bs = new(
			borderTopColor: XLColor.Red,
			borderBottomColor: XLColor.Blue,
			borderLeftColor: XLColor.Green,
			borderRightColor: XLColor.Black);

		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.Header, borderStyles: bs);
		style.ShouldNotBeNull();
		style!.Border.TopBorderColor.ShouldBe(XLColor.Red);
		style.Border.BottomBorderColor.ShouldBe(XLColor.Blue);
		style.Border.LeftBorderColor.ShouldBe(XLColor.Green);
		style.Border.RightBorderColor.ShouldBe(XLColor.Black);
	}

	[Fact]
	public void GetStandardCellStyle_Body_WithTopBorderOverride_AppliesTopBorder()
	{
		ClosedXmlBorderStyles bs = new(borderTop: XLBorderStyleValues.Thick);
		IXLStyle? style = workbook.GetStandardCellStyle(EStyle.Body, borderStyles: bs);
		style.ShouldNotBeNull();
		style!.Border.TopBorder.ShouldBe(XLBorderStyleValues.Thick);
	}

	#endregion

	#region GetStyle Tests

	[Theory]
	[InlineData(EStyle.Header)]
	[InlineData(EStyle.HeaderThickTop)]
	[InlineData(EStyle.Body)]
	[InlineData(EStyle.Error)]
	[InlineData(EStyle.Blackout)]
	[InlineData(EStyle.Whiteout)]
	[InlineData(EStyle.ImageBackground)]
	public void GetStyle_WithValidStyle_ReturnsNonNull(EStyle style)
	{
		Common.GetStyle(style, workbook).ShouldNotBeNull();
	}

	[Fact]
	public void GetStyle_Custom_WithHtmlColor_AppliesColor()
	{
		IXLStyle? style = Common.GetStyle(EStyle.Custom, workbook, htmlColor: "#FF0000");
		style.ShouldNotBeNull();
		style!.Fill.BackgroundColor.ShouldBe(XLColor.FromHtml("#FF0000"));
	}

	[Fact]
	public void GetStyle_Custom_WithAlignment_AppliesAlignment()
	{
		IXLStyle? style = Common.GetStyle(EStyle.Custom, workbook, alignment: XLAlignmentHorizontalValues.Left);
		style.ShouldNotBeNull();
		style!.Alignment.Horizontal.ShouldBe(XLAlignmentHorizontalValues.Left);
	}

	[Fact]
	public void GetStyle_Custom_WithFont_AppliesFont()
	{
		IXLFont font = Common.GetFont(EFont.Header, workbook);
		IXLStyle? style = Common.GetStyle(EStyle.Custom, workbook, font: font);
		style.ShouldNotBeNull();
		style!.Font.Bold.ShouldBeTrue();
	}

	[Fact]
	public void GetStyle_WithCellLocked_SetsLocked()
	{
		IXLStyle? style = Common.GetStyle(EStyle.Header, workbook, cellLocked: true);
		style!.Protection.Locked.ShouldBeTrue();
	}

	[Fact]
	public void GetStyle_WithWrapText_SetsWrap()
	{
		IXLStyle? style = Common.GetStyle(EStyle.Body, workbook, wrapText: true);
		style!.Alignment.WrapText.ShouldBeTrue();
	}

	#endregion

	#region GetFont Tests

	[Theory]
	[InlineData(EFont.Default, false, 10.0, "Calibri")]
	[InlineData(EFont.Header, true, 10.0, "Calibri")]
	[InlineData(EFont.Whiteout, false, 10.0, "Calibri")]
	[InlineData(EFont.ImageBackground, false, 11.0, "Calibri")]
	public void GetFont_WithVariousFontTypes_ReturnsCorrectProperties(EFont font, bool bold, double size, string name)
	{
		IXLFont result = Common.GetFont(font, workbook);
		result.ShouldNotBeNull();
		result.Bold.ShouldBe(bold);
		result.FontSize.ShouldBe(size);
		result.FontName.ShouldBe(name);
	}

	#endregion

	#region WriteFileToMemoryStreamAsync Tests

	[Fact]
	public async Task WriteFileToMemoryStreamAsync_WritesNonEmptyStream()
	{
		await using MemoryStream ms = new();
		using XLWorkbook wb = new();
		wb.AddWorksheet("Sheet1").Cell(1, 1).Value = "Test";

		await ms.WriteFileToMemoryStreamAsync(wb, TestContext.Current.CancellationToken);

		ms.Length.ShouldBeGreaterThan(0);
		ms.Position.ShouldBe(0);
	}

	[Fact]
	public async Task WriteFileToMemoryStreamAsync_ProducesValidXlsxContent()
	{
		await using MemoryStream ms = new();
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Data");
		ws.Cell(1, 1).Value = "Hello";
		await ms.WriteFileToMemoryStreamAsync(wb, TestContext.Current.CancellationToken);

		// Re-read and verify
		using XLWorkbook wb2 = new(ms);
		wb2.Worksheet("Data").Cell(1, 1).Value.ToString().ShouldBe("Hello");
	}

	[Fact]
	public async Task WriteFileToMemoryStreamAsync_WithCancellation_Throws()
	{
		using XLWorkbook wb = new();
		wb.AddWorksheet("Sheet1");
		await using MemoryStream ms = new();
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		await Should.ThrowAsync<OperationCanceledException>(() => ms.WriteFileToMemoryStreamAsync(wb, cts.Token));
	}

	#endregion

	#region GetRangeWidthInPx Tests

	[Fact]
	public void GetRangeWidthInPx_WithSetColumnWidths_ReturnsPositiveValue()
	{
		sheet.Column(1).Width = 20; // 20 character units
		sheet.Column(2).Width = 15;
		int width = sheet.GetRangeWidthInPx(1, 2);
		width.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void GetRangeWidthInPx_SwappedStartEnd_GivesSameResultAsOrdered()
	{
		sheet.Column(3).Width = 10;
		sheet.Column(4).Width = 12;
		int w1 = sheet.GetRangeWidthInPx(3, 4);
		int w2 = sheet.GetRangeWidthInPx(4, 3); // swapped
		w1.ShouldBe(w2);
	}

	[Fact]
	public void GetRangeWidthInPx_SingleColumn_ReturnsPositive()
	{
		sheet.Column(5).Width = 8;
		sheet.GetRangeWidthInPx(5, 5).ShouldBeGreaterThan(0);
	}

	[Fact]
	public void GetRangeWidthInPx_WithZeroWidthColumn_ReturnsZeroAndDoesNotThrow()
	{
		// Setting Width = 0 exercises the zero-width warning branch
		using XLWorkbook wb2 = new();
		IXLWorksheet ws2 = wb2.AddWorksheet("ZW");
		ws2.Column(1).Width = 0;
		int width = ws2.GetRangeWidthInPx(1, 1);
		width.ShouldBe(0);
	}

	#endregion

	#region GetRangeHeightInPx Tests

	[Fact]
	public void GetRangeHeightInPx_WithSetRowHeights_ReturnsPositiveValue()
	{
		sheet.Row(1).Height = 20; // 20 points
		sheet.Row(2).Height = 15;
		int height = sheet.GetRangeHeightInPx(1, 2);
		height.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void GetRangeHeightInPx_SwappedStartEnd_GivesSameResultAsOrdered()
	{
		sheet.Row(3).Height = 12;
		sheet.Row(4).Height = 18;
		int h1 = sheet.GetRangeHeightInPx(3, 4);
		int h2 = sheet.GetRangeHeightInPx(4, 3);
		h1.ShouldBe(h2);
	}

	[Fact]
	public void GetRangeHeightInPx_SingleRow_ReturnsPositive()
	{
		sheet.Row(6).Height = 15;
		sheet.GetRangeHeightInPx(6, 6).ShouldBeGreaterThan(0);
	}

	#endregion

	#region CreateTable Tests

	[Fact]
	public void CreateTable_CreatesTableWithCorrectName()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Data");
		ws.Cell(1, 1).Value = "Col1";
		ws.Cell(1, 2).Value = "Col2";
		ws.Cell(2, 1).Value = "A";
		ws.Cell(2, 2).Value = "B";

		wb.CreateTable("Data", "MyTable", 1, 2, 1, 2);

		ws.Tables.Count().ShouldBe(1);
		ws.Tables.First().Name.ShouldBe("MyTable");
	}

	[Fact]
	public void CreateTable_WithColumnNames_SetsHeaderValues()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Sheet1");
		ws.Cell(2, 1).Value = "R1";
		ws.Cell(3, 1).Value = "R2";

		wb.CreateTable("Sheet1", "T1", 1, 1, 1, 3, ["MyHeader"]);

		ws.Cell(1, 1).Value.ToString().ShouldBe("MyHeader");
	}

	[Fact]
	public void CreateTable_WithStyleOption_AppliesTheme()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("S");
		ws.Cell(1, 1).Value = "H";
		ws.Cell(2, 1).Value = "V";

		wb.CreateTable("S", "T2", 1, 1, 1, 2, tableStyle: ETableStyle.TableStyleLight1);

		IXLTable tbl = ws.Tables.First();
		tbl.Theme.ShouldNotBeNull();
	}

	[Fact]
	public void CreateTable_WithFewerColumnNamesThanColumns_UsesFallbackNames()
	{
		// Provide only 1 column name for 3 columns → col 2+ should get "Column2", "Column3"
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("FN");
		ws.Cell(2, 1).Value = "R1";
		ws.Cell(2, 2).Value = "R2";
		ws.Cell(2, 3).Value = "R3";

		wb.CreateTable("FN", "TFN", 1, 3, 1, 2, columnNames: ["OnlyFirst"]);

		ws.Cell(1, 1).Value.ToString().ShouldBe("OnlyFirst");
		ws.Cell(1, 2).Value.ToString().ShouldBe("Column2");
		ws.Cell(1, 3).Value.ToString().ShouldBe("Column3");
	}

	[Fact]
	public void CreateTable_WithTableNameTooLong_Throws()
	{
		using XLWorkbook wb = new();
		wb.AddWorksheet("S").Cell(1, 1).Value = "H";
		string longName = new('X', 256);
		Should.Throw<ArgumentOutOfRangeException>(() => wb.CreateTable("S", longName, 1, 1, 1, 1));
	}

	#endregion

	#region AddImage / AddImages / AddPicture Tests

	private static byte[] GetTestImageBytes()
	{
		string path = Path.Combine("TestData", "test.png");
		return File.ReadAllBytes(path);
	}

	[Fact]
	public void AddImage_ByNamedRange_InsertsImage()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Img");
		ws.Row(1).Height = 60;
		ws.Column(1).Width = 30;
		wb.DefinedNames.Add("ImgCell", "Img!$A$1");

		byte[] img = GetTestImageBytes();
		Should.NotThrow(() => wb.AddImage(img, "ImgCell"));

		ws.Pictures.Count.ShouldBe(1);
	}

	[Fact]
	public void AddImage_ByRangeString_InsertsImage()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("I");
		ws.Row(1).Height = 60;
		ws.Column(1).Width = 30;

		byte[] img = GetTestImageBytes();
		Should.NotThrow(() => wb.AddImage(ws, img, "A1:A1"));

		ws.Pictures.Count.ShouldBe(1);
	}

	[Fact]
	public void AddImage_ByIXLRange_InsertsImage()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("I2");
		ws.Row(1).Height = 60;
		ws.Column(1).Width = 30;

		IXLRange range = ws.Range("A1:A1");
		byte[] img = GetTestImageBytes();
		Should.NotThrow(() => wb.AddImage(ws, img, range));

		ws.Pictures.Count.ShouldBe(1);
	}

	[Fact]
	public void AddImage_ByIXLCell_InsertsImage()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("I3");
		ws.Row(1).Height = 60;
		ws.Column(1).Width = 30;

		IXLCell cell = ws.Cell(1, 1);
		byte[] img = GetTestImageBytes();
		Should.NotThrow(() => wb.AddImage(ws, img, cell));

		ws.Pictures.Count.ShouldBe(1);
	}

	[Fact]
	public void AddImages_ByNamedRangeList_InsertsMultipleImages()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("MI");
		ws.Row(1).Height = 60;
		ws.Row(2).Height = 60;
		ws.Column(1).Width = 30;
		wb.DefinedNames.Add("IC1", "MI!$A$1");
		wb.DefinedNames.Add("IC2", "MI!$A$2");

		byte[] img = GetTestImageBytes();
		Should.NotThrow(() => wb.AddImages([img, img], ["IC1", "IC2"]));

		ws.Pictures.Count.ShouldBe(2);
	}

	[Fact]
	public void AddImages_ByRangeList_InsertsMultipleImages()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("MR");
		ws.Row(1).Height = 60;
		ws.Row(2).Height = 60;
		ws.Column(1).Width = 30;

		byte[] img = GetTestImageBytes();
		List<IXLRange> ranges = [ws.Range("A1:A1"), ws.Range("A2:A2")];
		Should.NotThrow(() => wb.AddImages(ws, [img, img], ranges));

		ws.Pictures.Count.ShouldBe(2);
	}

	[Fact]
	public void AddImages_WithMismatchedCounts_DoesNotInsert()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("MM");
		byte[] img = GetTestImageBytes();
		wb.AddImages([img], ["N1", "N2"]); // mismatched
		ws.Pictures.Count.ShouldBe(0);
	}

	[Fact]
	public void AddImages_WithEmptyImageList_DoesNotInsert()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("EL");
		wb.AddImages([], []);
		ws.Pictures.Count.ShouldBe(0);
	}

	[Fact]
	public void AddPicture_InsertsPictureWithCorrectPlacement()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("AP");
		ws.Row(1).Height = 80;
		ws.Column(1).Width = 40;

		IXLRange area = ws.Range("A1:A1");
		byte[] img = GetTestImageBytes();
		wb.AddPicture(ws, area, img, XLPicturePlacement.MoveAndSize);

		ws.Pictures.Count.ShouldBe(1);
		ws.Pictures.First().Placement.ShouldBe(XLPicturePlacement.MoveAndSize);
	}

	[Fact]
	public void AddPicture_WithFreeFloatingPlacement_InsertsCorrectly()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("FF");
		ws.Row(1).Height = 60;
		ws.Column(1).Width = 30;

		IXLRange area = ws.Range("A1:A1");
		byte[] img = GetTestImageBytes();
		wb.AddPicture(ws, area, img, XLPicturePlacement.FreeFloating);

		ws.Pictures.Count.ShouldBe(1);
	}

	[Fact]
	public void AddPicture_WiderThanTall_ScalesToFitWidth()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("W");
		ws.Row(1).Height = 60;
		ws.Column(1).Width = 120; // much wider than tall

		IXLRange area = ws.Range("A1:A1");
		byte[] img = GetTestImageBytes(); // 760x760 square image
		wb.AddPicture(ws, area, img);

		IXLPicture pic = ws.Pictures.First();
		pic.Width.ShouldBeGreaterThan(0);
		pic.Height.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void AddPicture_TallerThanWide_ScalesToFitHeight()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("T");
		ws.Row(1).Height = 120; // much taller than wide
		ws.Column(1).Width = 30;

		IXLRange area = ws.Range("A1:A1");
		byte[] img = GetTestImageBytes();
		wb.AddPicture(ws, area, img);

		IXLPicture pic = ws.Pictures.First();
		pic.Width.ShouldBeGreaterThan(0);
		pic.Height.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void AddImage_ByRangeString_WithInvalidRange_Throws()
	{
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("Err");
		byte[] img = GetTestImageBytes();
		Should.Throw<ArgumentException>(() => wb.AddImage(ws, img, "INVALIDREF"));
	}

	[Fact]
	public void AddImages_ByNamedRangeList_WithEmptyImageBytes_SkipsThatImage()
	{
		// imageData[0].Length == 0 means the inner if is false → image skipped
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("EI");
		ws.Row(1).Height = 60;
		ws.Column(1).Width = 30;
		wb.DefinedNames.Add("EICell", "EI!$A$1");

		wb.AddImages([Array.Empty<byte>()], ["EICell"]);

		ws.Pictures.Count.ShouldBe(0); // no picture inserted
	}

	[Fact]
	public void AddImages_ByNamedRangeList_WithNonExistentCellName_SkipsImage()
	{
		// cell is null (not found) → ws/area are null → inner ws-null check is false → skip
		using XLWorkbook wb = new();
		IXLWorksheet ws = wb.AddWorksheet("NE");
		byte[] img = GetTestImageBytes();

		wb.AddImages([img], ["NoSuchNamedRange"]);

		ws.Pictures.Count.ShouldBe(0);
	}

	#endregion
}

