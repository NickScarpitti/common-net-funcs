using System.Data;
using CommonNetFuncs.Excel.Common;
using CommonNetFuncs.Excel.Npoi;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using static CommonNetFuncs.Excel.Npoi.Common;

namespace Excel.Npoi.Tests;

public sealed class CommonTests : IDisposable
{
  private readonly XSSFWorkbook _xlsxWorkbook;
  private readonly HSSFWorkbook _xlsWorkbook;
  private readonly ISheet _sheet;

  public CommonTests()
  {
    _xlsxWorkbook = new XSSFWorkbook();
    _xlsWorkbook = new HSSFWorkbook();
    _sheet = _xlsxWorkbook.CreateSheet("Test");
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
        _xlsxWorkbook?.Dispose();
        _xlsWorkbook?.Dispose();
      }
      disposed = true;
    }
  }

  ~CommonTests()
  {
    Dispose(false);
  }

  #region Cell Manipulation Tests

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public void IsCellEmpty_WithEmptyValues_ReturnsTrue(string? value)
  {
    // Arrange
    IRow row = _sheet.CreateRow(0);
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

  [Theory]
  [InlineData("Test")]
  [InlineData("123")]
  public void IsCellEmpty_WithNonEmptyValues_ReturnsFalse(string value)
  {
    // Arrange
    IRow row = _sheet.CreateRow(0);
    ICell cell = row.CreateCell(0);
    cell.SetCellValue(value);

    // Act
    bool result = cell.IsCellEmpty();

    // Assert
    result.ShouldBeFalse();
  }

  [Fact]
  public void GetCellFromReference_WithValidReference_ReturnsCellAtLocation()
  {
    // Arrange
    const string cellReference = "B2";

    // Act
    ICell? cell = _sheet.GetCellFromReference(cellReference);

    // Assert
    cell.ShouldNotBeNull();
    cell.ColumnIndex.ShouldBe(1); // B = 1 (0-based)
    cell.RowIndex.ShouldBe(1); // 2-1 = 1 (0-based)
  }

  [Theory]
  [InlineData(1, 0)]  // Right
  [InlineData(-1, 0)] // Left
  [InlineData(0, 1)]  // Down
  [InlineData(0, -1)] // Up
  public void GetCellOffset_WithValidOffsets_ReturnsCorrectCell(int colOffset, int rowOffset)
  {
    // Arrange
    IRow row = _sheet.CreateRow(1);
    ICell startCell = row.CreateCell(1); // B2

    // Act
    ICell? offsetCell = startCell.GetCellOffset(colOffset, rowOffset);

    // Assert
    offsetCell.ShouldNotBeNull();
    offsetCell.ColumnIndex.ShouldBe(startCell.ColumnIndex + colOffset);
    offsetCell.RowIndex.ShouldBe(startCell.RowIndex + rowOffset);
  }

  [Theory]
  [InlineData(0, 0)]
  [InlineData(1, 1)]
  [InlineData(2, 2)]
  public void GetCellFromCoordinates_ReturnsCorrectCell(int colIndex, int rowIndex)
  {
    // Act
    ICell? cell = _sheet.GetCellFromCoordinates(colIndex, rowIndex);

    // Assert
    cell.ShouldNotBeNull();
    cell.ColumnIndex.ShouldBe(colIndex);
    cell.RowIndex.ShouldBe(rowIndex);
  }

  [Fact]
  public void GetLastPopulatedRowInColumn_WithPopulatedCells_ReturnsCorrectIndex()
  {
    // Arrange
    for (int i = 0; i < 3; i++)
    {
      IRow row = _sheet.CreateRow(i);
      ICell cell = row.CreateCell(0);
      cell.SetCellValue($"Value {i}");
    }

    // Add an empty cell
    IRow emptyRow = _sheet.CreateRow(3);
    emptyRow.CreateCell(0);

    // Act
    int lastPopulatedRow = _sheet.GetLastPopulatedRowInColumn(0);

    // Assert
    lastPopulatedRow.ShouldBe(2); // Zero-based index of last populated row
  }

  [Theory]
  [InlineData("A")]
  [InlineData("B")]
  [InlineData("C")]
  public void GetLastPopulatedRowInColumn_WithColumnName_ReturnsCorrectIndex(string columnName)
  {
    // Arrange
    int colIndex = columnName.ColumnNameToNumber();
    for (int i = 0; i < 3; i++)
    {
      IRow row = _sheet.CreateRow(i);
      ICell cell = row.CreateCell(colIndex);
      cell.SetCellValue($"Value {i}");
    }

    // Act
    int lastPopulatedRow = _sheet.GetLastPopulatedRowInColumn(columnName);

    // Assert
    lastPopulatedRow.ShouldBe(2);
  }

  [Fact]
  public void GetCellFromName_WithValidName_ReturnsCellFromNamedRange()
  {
    // Arrange
    IName name = _xlsxWorkbook.CreateName();
    name.NameName = "TestRange";
    name.RefersToFormula = "Test!$B$2";

    // Act
    ICell? cell = _xlsxWorkbook.GetCellFromName("TestRange");

    // Assert
    cell.ShouldNotBeNull();
    cell.ColumnIndex.ShouldBe(1); // B = 1 (0-based)
    cell.RowIndex.ShouldBe(1); // 2-1 = 1 (0-based)
  }

  #endregion

  #region CreateCell Tests

  [Theory]
  [InlineData(0)]  // First column
  [InlineData(1)]  // Second column
  [InlineData(10)] // Arbitrary column
  public void CreateCell_WithValidColumnIndex_ReturnsNewCell(int columnIndex)
  {
    // Arrange
    IRow row = _sheet.CreateRow(0);

    // Act
    ICell cell = row.CreateCell(columnIndex);

    // Assert
    cell.ShouldNotBeNull();
    cell.ColumnIndex.ShouldBe(columnIndex);
    cell.RowIndex.ShouldBe(0);
  }

  [Fact]
  public void CreateCell_WithExistingCell_OverwritesCell()
  {
    // Arrange
    IRow row = _sheet.CreateRow(0);
    ICell existingCell = row.CreateCell(0);
    existingCell.SetCellValue("Original");

    // Act
    ICell newCell = row.CreateCell(0);
    newCell.SetCellValue("New");

    // Assert
    newCell.ShouldNotBeNull();
    newCell.StringCellValue.ShouldBe("New");
    row.GetCell(0).ShouldBeSameAs(newCell);
  }

  [Fact]
  public void CreateCell_MultipleCellsInRow_MaintainsCorrectIndices()
  {
    // Arrange
    IRow row = _sheet.CreateRow(0);

    // Act
    ICell cell1 = row.CreateCell(0);
    ICell cell2 = row.CreateCell(1);
    ICell cell3 = row.CreateCell(2);

    // Assert
    cell1.ColumnIndex.ShouldBe(0);
    cell2.ColumnIndex.ShouldBe(1);
    cell3.ColumnIndex.ShouldBe(2);
    row.LastCellNum.ShouldBe((short)3);
  }

  [Fact]
  public void CreateCell_Extension_CreatesCellAtSpecifiedIndex()
  {
    // Arrange
    IRow row = _sheet.CreateRow(0);
    const int columnIndex = 2;

    // Act
    ICell cell = row.CreateCell(columnIndex);

    // Assert
    cell.ShouldNotBeNull();
    cell.ColumnIndex.ShouldBe(columnIndex);
    cell.RowIndex.ShouldBe(0);
    row.GetCell(columnIndex).ShouldBeSameAs(cell);
  }

  #endregion

  #region DataTables and Validation Tests

  [Fact]
  public async Task ReadExcelFileToDataTable_WithHeaders_ReturnsCorrectDataTable()
  {
    // Arrange
    await using MemoryStream ms = new();
    IRow headerRow = _sheet.CreateRow(0);
    headerRow.CreateCell(0).SetCellValue("Column1");
    headerRow.CreateCell(1).SetCellValue("Column2");

    IRow dataRow = _sheet.CreateRow(1);
    dataRow.CreateCell(0).SetCellValue("Value1");
    dataRow.CreateCell(1).SetCellValue("Value2");

    _xlsxWorkbook.Write(ms, true);
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

  [Fact]
  public void AddDataValidation_CreatesValidDropdown()
  {
    // Arrange
    List<string> options = [ "Option1", "Option2", "Option3" ];
    CellRangeAddressList addressList = new(0, 1, 0, 0); // A1:A2

    // Act
    _sheet.AddDataValidation(addressList, options);

    // Assert
    IDataValidation? validation = _sheet.GetDataValidations()[0];
    validation.ShouldNotBeNull();
    validation.ShowErrorBox.ShouldBeTrue();
    validation.ErrorStyle.ShouldBe(0);
  }

  [Fact]
  public async Task WriteFileToMemoryStreamAsync_WritesWorkbookCorrectly()
  {
    // Arrange
    IRow row = _sheet.CreateRow(0);
    row.CreateCell(0).SetCellValue("Test");
    await using MemoryStream ms = new();

    // Act
    await ms.WriteFileToMemoryStreamAsync(_xlsxWorkbook);

    // Assert
    ms.Length.ShouldBeGreaterThan(0);
    ms.Position.ShouldBe(0);
  }

  #endregion

  #region Workbook and Style Tests

  [Theory]
  [InlineData(EStyle.Header)]
  [InlineData(EStyle.Body)]
  [InlineData(EStyle.Error)]
  public void GetStandardCellStyle_ReturnsCorrectStyle(EStyle style)
  {
    // Act
    ICellStyle cellStyle = _xlsxWorkbook.GetStandardCellStyle(style);

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

  [Theory]
  [InlineData(EFont.Default)]
  [InlineData(EFont.Header)]
  [InlineData(EFont.Whiteout)]
  public void GetFont_ReturnsCorrectFont(EFont font)
  {
    // Act
    IFont cellFont = _xlsxWorkbook.GetFont(font);

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

  [Fact]
  public void GetCustomStyle_WithHexColor_CreatesCorrectStyle()
  {
    // Arrange
    const string hexColor = "#FF0000"; // Red

    // Act
    ICellStyle style = _xlsxWorkbook.GetCustomStyle(hexColor);

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

  [Theory]
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

  [Theory]
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

  [Fact]
  public void IsXlsx_WithXlsxWorkbook_ReturnsTrue()
  {
    // Act
    bool result = _xlsxWorkbook.IsXlsx();

    // Assert
    result.ShouldBeTrue();
  }

  [Fact]
  public void IsXlsx_WithXlsWorkbook_ReturnsFalse()
  {
    // Act
    bool result = _xlsWorkbook.IsXlsx();

    // Assert
    result.ShouldBeFalse();
  }

  #endregion

  #region Additional Tests

  [Fact]
  public void ClearAllFromName_WithValidName_ClearsCells()
  {
    // Arrange
    IName name = _xlsxWorkbook.CreateName();
    name.NameName = "TestRange";
    name.RefersToFormula = "Test!$B$2:$B$3";

    ICell? cell1 = _sheet.GetCellFromReference("B2");
    ICell? cell2 = _sheet.GetCellFromReference("B3");
    cell1?.SetCellValue("Test1");
    cell2?.SetCellValue("Test2");

    // Act
    _xlsxWorkbook.ClearAllFromName("TestRange");

    // Assert
    _sheet.GetRow(1).GetCell(1).ShouldBeNull();
    _sheet.GetRow(2).GetCell(1).ShouldBeNull();
  }

  [Fact]
  public void CreateTable_CreatesValidTable()
  {
    // Arrange
    List<string> columnNames = [ "Col1", "Col2" ];

    // Act
    _xlsxWorkbook.CreateTable("Test", "TestTable", 0, 1, 0, 2, columnNames);

    // Assert
    XSSFSheet sheet = (XSSFSheet)_xlsxWorkbook.GetSheet("Test");
    XSSFTable? table = sheet.GetTables().FirstOrDefault();
    table.ShouldNotBeNull();
    table.Name.ShouldBe("TestTable");
    table.DisplayName.ShouldBe("TestTable");
    table.StartColIndex.ShouldBe(0);
    table.EndColIndex.ShouldBe(1);
    table.StartRowIndex.ShouldBe(0);
    table.EndRowIndex.ShouldBe(2);
  }

  [Fact]
  public void GetRange_ReturnsCorrectCellArray()
  {
    // Arrange
    ICell? cell1 = _sheet.GetCellFromReference("A1");
    ICell? cell2 = _sheet.GetCellFromReference("B1");
    ICell? cell3 = _sheet.GetCellFromReference("A2");
    ICell? cell4 = _sheet.GetCellFromReference("B2");

    cell1?.SetCellValue("1");
    cell2?.SetCellValue("2");
    cell3?.SetCellValue("3");
    cell4?.SetCellValue("4");

    // Act
    ICell[,] range = _sheet.GetRange("A1:B2");

    // Assert
    range.GetLength(0).ShouldBe(2); // Rows
    range.GetLength(1).ShouldBe(2); // Columns
    range[0, 0].GetStringValue().ShouldBe("1");
    range[0, 1].GetStringValue().ShouldBe("2");
    range[1, 0].GetStringValue().ShouldBe("3");
    range[1, 1].GetStringValue().ShouldBe("4");
  }

  [Fact]
  public void GetRangeOfMergedCells_WithMergedCell_ReturnsCorrectRange()
  {
    // Arrange
    CellRangeAddress mergedRange = new(0, 1, 0, 1); // A1:B2
    _sheet.AddMergedRegion(mergedRange);
    ICell? cell = _sheet.GetCellFromReference("A1");

    // Act
    CellRangeAddress? result = cell.GetRangeOfMergedCells();

    // Assert
    result.ShouldNotBeNull();
    result.FirstRow.ShouldBe(0);
    result.LastRow.ShouldBe(1);
    result.FirstColumn.ShouldBe(0);
    result.LastColumn.ShouldBe(1);
  }

  [Fact]
  public void GetRangeWidthInPx_ReturnsCorrectWidth()
  {
    // Arrange
    _sheet.SetColumnWidth(0, 20 * 256); // 20 characters width
    _sheet.SetColumnWidth(1, 15 * 256); // 15 characters width

    // Act
    int width = _sheet.GetRangeWidthInPx(0, 1);

    // Assert
    width.ShouldBeGreaterThan(0);
  }

  [Fact]
  public void GetRangeHeightInPx_ReturnsCorrectHeight()
  {
    // Arrange
    IRow row1 = _sheet.CreateRow(0);
    IRow row2 = _sheet.CreateRow(1);
    row1.Height = 20 * 20; // 20 points
    row2.Height = 15 * 20; // 15 points

    // Act
    int height = _sheet.GetRangeHeightInPx(0, 1);

    // Assert
    height.ShouldBeGreaterThan(0);
  }

  [Theory]
  [InlineData(CellType.Numeric, 123.45, "123.45")]
  [InlineData(CellType.String, "Test", "Test")]
  [InlineData(CellType.Boolean, true, "True")]
  [InlineData(CellType.Blank, null, "")]
  public void GetStringValue_ReturnsCorrectString(CellType cellType, object? value, string expected)
  {
    // Arrange
    ICell cell = _sheet.CreateRow(0).CreateCell(0);
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

  [Fact]
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

  [Theory]
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

  [Fact]
  public async Task ReadExcelTableToDataTable_WithValidTable_ReturnsPopulatedDataTable()
  {
    // Arrange
    List<string> columnNames = [ "Col1", "Col2" ];
    _xlsxWorkbook.CreateTable("Test", "TestTable", 0, 1, 0, 2, columnNames);

    ICell? cell1 = _sheet.GetCellFromReference("A2");
    ICell? cell2 = _sheet.GetCellFromReference("B2");
    cell1?.SetCellValue("Value1");
    cell2?.SetCellValue("Value2");

    await using MemoryStream ms = new();
    _xlsxWorkbook.Write(ms, true);
    ms.Position = 0;

    // Act
    DataTable result = ms.ReadExcelTableToDataTable("TestTable");

    // Assert
    result.Columns.Count.ShouldBe(2);
    result.Rows.Count.ShouldBe(2);
    result.Rows[0][0].ToString().ShouldBe("Value1");
    result.Rows[0][1].ToString().ShouldBe("Value2");
  }

  [Theory]
  [InlineData(0)]    // Valid index
  [InlineData(null)] // null
  [InlineData(-1)]   // negative
  public void ColumnIndexToName_WithNullableInt_WithInvalidValues_ThrowsArgumentException(int? value)
  {
    // Act & Assert
    if (value == null || value < 0)
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

  [Theory]
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

  [Fact]
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

  [Fact]
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

  [Fact]
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
    ICellStyle style = _xlsxWorkbook.GetCustomStyle(borderStyles: borderStyles);

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

  [Fact]
  public void GetCustomStyle_WithHexColor_OnNonXlsxWorkbook_UsesHssfColor()
  {
    // Arrange
    const string hexColor = "#FF0000";

    // Act
    ICellStyle style = _xlsWorkbook.GetCustomStyle(hexColor);

    // Assert
    style.ShouldNotBeNull();
    style.ShouldBeOfType<HSSFCellStyle>();
        ((HSSFCellStyle)style).FillForegroundColor.ShouldNotBe((short)0);
  }

  [Fact]
  public void GetCustomStyle_WithHssfColor_AppliesCorrectColor()
  {
    // Arrange
    const short colorIndex = HSSFColor.Red.Index;

    // Act
    ICellStyle style = _xlsxWorkbook.GetCustomStyle(hssfColor: colorIndex);

    // Assert
    style.ShouldNotBeNull();
    style.FillForegroundColor.ShouldBe(colorIndex);
  }

  [Theory]
  [InlineData(EStyle.HeaderThickTop)]
  [InlineData(EStyle.Blackout)]
  [InlineData(EStyle.Whiteout)]
  [InlineData(EStyle.ImageBackground)]
  public void GetStandardCellStyle_WithSpecialStyles_ReturnsCorrectStyle(EStyle style)
  {
    // Act
    ICellStyle cellStyle = _xlsxWorkbook.GetStandardCellStyle(style);

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

  [Fact]
  public void GetFont_WithImageBackground_ReturnsCorrectFont()
  {
    // Act
    IFont font = _xlsxWorkbook.GetFont(EFont.ImageBackground);

    // Assert
    font.ShouldNotBeNull();
    font.FontName.ShouldBe("Calibri");
    font.FontHeightInPoints.ShouldBe((short)11);
  }

  [Theory]
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

  [Fact]
  public void GetRangeHeightInPx_WithStartRowGreaterThanEndRow_SwapsAndCalculatesCorrectly()
  {
    // Arrange
    const int startRow = 5;
    const int endRow = 2;

    for (int i = Math.Min(startRow, endRow); i <= Math.Max(startRow, endRow); i++)
    {
      IRow row = _sheet.CreateRow(i);
      row.Height = 20 * 20; // 20 points
    }

    // Act
    int height = _sheet.GetRangeHeightInPx(startRow, endRow);

    // Assert
    height.ShouldBeGreaterThan(0);
  }

  [Fact]
  public void GetRangeWidthInPx_WithStartColGreaterThanEndCol_SwapsAndCalculatesCorrectly()
  {
    // Arrange
    const int startCol = 5;
    const int endCol = 2;

    for (int i = Math.Min(startCol, endCol); i <= Math.Max(startCol, endCol); i++)
    {
      _sheet.SetColumnWidth(i, .0001); // Set to effectively 0 to test warning path (actual 0 does not work)
    }

    // Act
    int width = _sheet.GetRangeWidthInPx(startCol, endCol);

    // Assert
    width.ShouldBe(0);
  }

  [Fact]
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

  [Fact]
  public void GetRangeOfMergedCells_WithNonMergedCell_ReturnsSingleCellRange()
  {
    // Arrange
    ICell cell = _sheet.CreateRow(0).CreateCell(0);
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

  [Fact]
  public void AddImage_WithNamedRange_AddsImageCorrectly()
  {
    // Arrange
    IName name = _xlsxWorkbook.CreateName();
    name.NameName = "ImageCell";
    name.RefersToFormula = "Test!$B$2";
    byte[] imageData = File.ReadAllBytes("TestData/test.png");

    // Act
    _xlsxWorkbook.AddImage(imageData, "ImageCell");

    // Assert
    XSSFSheet sheet = (XSSFSheet)_sheet;
        ((XSSFDrawing)sheet.CreateDrawingPatriarch()).GetShapes().Count.ShouldBe(1);
  }

  [Fact]
  public void AddImages_WithMultipleRanges_AddsImagesCorrectly()
  {
    // Arrange
    List<byte[]> imageData = [ File.ReadAllBytes("TestData/test1.png"), File.ReadAllBytes("TestData/test2.png") ];
    List<string> cellNames = [ "ImageCell1", "ImageCell2" ];

    foreach ((string name, int i) in cellNames.Select((n, i) => (n, i)))
        {
      IName namedRange = _xlsxWorkbook.CreateName();
      namedRange.NameName = name;
      namedRange.RefersToFormula = $"Test!$B${2 + i}";
        }

    // Act
    _xlsxWorkbook.AddImages(imageData, cellNames);

    // Assert
    XSSFSheet sheet = (XSSFSheet)_sheet;
        ((XSSFDrawing)sheet.CreateDrawingPatriarch()).GetShapes().Count.ShouldBe(2);
  }

  [Fact]
  public void AddImage_WithRange_AddsImageToCorrectRange()
  {
    // Arrange
    byte[] imageData = File.ReadAllBytes("TestData/test.png");
    const string range = "B2:C3";

    // Act
    _xlsxWorkbook.AddImage(_sheet, imageData, range);

    // Assert
    XSSFSheet sheet = (XSSFSheet)_sheet;
    XSSFDrawing drawings = (XSSFDrawing)sheet.CreateDrawingPatriarch();
    drawings.GetShapes().Count.ShouldBe(1);
  }

  [Fact]
  public void AddImage_WithCellRangeAddress_AddsImageToCorrectRange()
  {
    // Arrange
    byte[] imageData = File.ReadAllBytes("TestData/test.png");
    CellRangeAddress range = new(1, 2, 1, 2); // B2:C3

    // Act
    _xlsxWorkbook.AddImage(_sheet, imageData, range);

    // Assert
    XSSFSheet sheet = (XSSFSheet)_sheet;
    XSSFDrawing drawings = (XSSFDrawing)sheet.CreateDrawingPatriarch();
    drawings.GetShapes().Count.ShouldBe(1);
  }

  [Fact]
  public void AddImage_WithCell_AddsImageToCell()
  {
    // Arrange
    byte[] imageData = File.ReadAllBytes("TestData/test.png");
    ICell cell = _sheet.CreateRow(1).CreateCell(1); // B2

    // Act
    _xlsxWorkbook.AddImage(_sheet, imageData, cell);

    // Assert
    XSSFSheet sheet = (XSSFSheet)_sheet;
    XSSFDrawing drawings = (XSSFDrawing)sheet.CreateDrawingPatriarch();
    drawings.GetShapes().Count.ShouldBe(1);
  }

  [Fact]
  public void AddImages_WithMultipleCellRanges_AddsImagesCorrectly()
  {
    // Arrange
    List<byte[]> imageData = [ File.ReadAllBytes("TestData/test1.png"), File.ReadAllBytes("TestData/test2.png") ];
    List<CellRangeAddress> ranges = [
            new CellRangeAddress(1, 2, 1, 2), // B2:C3
            new CellRangeAddress(3, 4, 1, 2)  // B4:C5
        ];

    // Act
    _xlsxWorkbook.AddImages(_sheet, imageData, ranges);

    // Assert
    XSSFSheet sheet = (XSSFSheet)_sheet;
    XSSFDrawing drawings = (XSSFDrawing)sheet.CreateDrawingPatriarch();
    drawings.GetShapes().Count.ShouldBe(2);
  }

  [Fact]
  public void AddPicture_ConfiguresAnchorAndScaleCorrectly()
  {
    // Arrange
    byte[] imageData = File.ReadAllBytes("TestData/test.png");
    CellRangeAddress area = new(1, 2, 1, 2); // B2:C3
    IDrawing<IShape> drawing = _sheet.CreateDrawingPatriarch();

    // Act
    _xlsxWorkbook.AddPicture(_sheet, area, imageData, drawing);

    // Assert
    XSSFSheet sheet = (XSSFSheet)_sheet;
    object picture = ((XSSFDrawing)sheet.CreateDrawingPatriarch()).GetShapes()[0];
    picture.ShouldNotBeNull();
  }

  #endregion

  #region Complex DataTable Tests

  [Fact]
  public async Task ReadExcelFileToDataTable_WithNonXlsxFile_ReadsCorrectly()
  {
    // Arrange
    await using MemoryStream ms = new();
    IRow headerRow = _xlsWorkbook.CreateSheet("Test").CreateRow(0);
    headerRow.CreateCell(0).SetCellValue("Column1");
    headerRow.CreateCell(1).SetCellValue("Column2");
    _xlsWorkbook.Write(ms, true);
    ms.Position = 0;

    // Act
    DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: true);

    // Assert
    result.Columns.Count.ShouldBe(2);
    result.Columns[0].ColumnName.ShouldBe("Column1");
    result.Columns[1].ColumnName.ShouldBe("Column2");
  }

  [Fact]
  public async Task ReadExcelFileToDataTable_WithSpecificSheet_ReadsCorrectSheet()
  {
    // Arrange
    await using MemoryStream ms = new();
    _xlsxWorkbook.CreateSheet("Sheet1");
    ISheet sheet2 = _xlsxWorkbook.CreateSheet("Sheet2");
    IRow headerRow = sheet2.CreateRow(0);
    headerRow.CreateCell(0).SetCellValue("SpecialColumn");
    _xlsxWorkbook.Write(ms, true);
    ms.Position = 0;

    // Act
    DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: true, sheetName: "Sheet2");

    // Assert
    result.Columns[0].ColumnName.ShouldBe("SpecialColumn");
  }

  [Fact]
  public async Task ReadExcelFileToDataTable_WithEndCellReference_LimitsRange()
  {
    // Arrange
    await using MemoryStream ms = new();
    IRow headerRow = _sheet.CreateRow(0);
    for (int i = 0; i < 5; i++)
    {
      headerRow.CreateCell(i).SetCellValue($"Column{i + 1}");
    }
    _xlsxWorkbook.Write(ms, true);
    ms.Position = 0;

    // Act
    DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: true, startCellReference: "A1", endCellReference: "C1");

    // Assert
    result.Columns.Count.ShouldBe(3); // Should only include A1:C1
  }

  [Fact]
  public async Task ReadExcelFileToDataTable_WithoutHeaders_UsesDefaultColumnNames()
  {
    // Arrange
    await using MemoryStream ms = new();
    IRow dataRow = _sheet.CreateRow(0);
    dataRow.CreateCell(0).SetCellValue("Value1");
    dataRow.CreateCell(1).SetCellValue("Value2");
    _xlsxWorkbook.Write(ms, true);
    ms.Position = 0;

    // Act
    DataTable result = ms.ReadExcelFileToDataTable(hasHeaders: false);

    // Assert
    result.Columns[0].ColumnName.ShouldBe("Column0");
    result.Columns[1].ColumnName.ShouldBe("Column1");
    result.Rows[0][0].ToString().ShouldBe("Value1");
  }

  [Fact]
  public async Task ReadExcelTableToDataTable_WithNoTableName_ReadsFirstTable()
  {
    // Arrange
    await using MemoryStream ms = new();
    _xlsxWorkbook.CreateTable("Test", "FirstTable", 0, 1, 0, 2, [ "Col1", "Col2" ]);
    _xlsxWorkbook.CreateTable("Test", "SecondTable", 0, 1, 3, 5, [ "Col3", "Col4" ]);
    _xlsxWorkbook.Write(ms, true);
    ms.Position = 0;

    // Act
    using DataTable result = ms.ReadExcelTableToDataTable();

    // Assert
    result.Columns.Count.ShouldBe(2);
    result.Columns[0].ColumnName.ShouldBe("Col1");
    result.Columns[1].ColumnName.ShouldBe("Col2");
  }

  #endregion
}