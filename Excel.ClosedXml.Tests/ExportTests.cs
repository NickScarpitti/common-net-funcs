using System.Data;
using ClosedXML.Excel;
using static CommonNetFuncs.Excel.ClosedXml.Export;

namespace Excel.ClosedXml.Tests;

public sealed class ExportTests : IDisposable
{
    private readonly IFixture _fixture;
    private readonly XLWorkbook _workbook;

    public ExportTests()
    {
        _fixture = new Fixture();
        _workbook = new XLWorkbook();
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
                _workbook?.Dispose();
            }
            disposed = true;
        }
    }

    ~ExportTests()
    {
        Dispose(false);
    }

    public sealed class TestData
    {
        public string? StringProperty { get; set; }

        public int IntProperty { get; set; }

        public DateTime DateProperty { get; set; }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExportFromTable_Generic_WithValidData_ShouldExportSuccessfully(bool createTable)
    {
        // Arrange
        IXLWorksheet worksheet = _workbook.AddWorksheet("Test");
        List<TestData> testData = _fixture.CreateMany<TestData>(3).ToList();

        // Act
        bool result = ExportFromTable(_workbook, worksheet, testData, createTable);

        // Assert
        result.ShouldBeTrue();
        worksheet.Cell(1, 1).Value.ToString().ShouldBe("StringProperty");
        worksheet.Cell(1, 2).Value.ToString().ShouldBe("IntProperty");
        worksheet.Cell(1, 3).Value.ToString().ShouldBe("DateProperty");

        // Verify data rows
        worksheet.Cell(2, 1).Value.ToString().ShouldNotBeEmpty();
        worksheet.Cell(2, 2).Value.ToString().ShouldNotBeEmpty();
        worksheet.Cell(2, 3).Value.ToString().ShouldNotBeEmpty();

        if (createTable)
        {
            worksheet.Tables.Count().ShouldBe(1);
            worksheet.Tables.First().ShowAutoFilter.ShouldBeTrue();
        }
        else
        {
            worksheet.Tables.Count().ShouldBe(0);
            worksheet.AutoFilter.IsEnabled.ShouldBeTrue();
        }
    }

    [Fact]
    public void ExportFromTable_Generic_WithNullData_ShouldReturnTrue()
    {
        // Arrange
        IXLWorksheet worksheet = _workbook.AddWorksheet("Test");
        IEnumerable<TestData>? testData = null;

        // Act
        bool result = ExportFromTable(_workbook, worksheet, testData);

        // Assert
        result.ShouldBeTrue();
        worksheet.CellsUsed().Count().ShouldBe(0);
    }

    [Fact]
    public void ExportFromTable_Generic_WithEmptyData_ShouldReturnTrue()
    {
        // Arrange
        IXLWorksheet worksheet = _workbook.AddWorksheet("Test");
        TestData[] testData = Array.Empty<TestData>();

        // Act
        bool result = ExportFromTable(_workbook, worksheet, testData);

        // Assert
        result.ShouldBeTrue();
        worksheet.CellsUsed().Count().ShouldBe(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExportFromTable_DataTable_WithValidData_ShouldExportSuccessfully(bool createTable)
    {
        // Arrange
        IXLWorksheet worksheet = _workbook.AddWorksheet("Test");
        DataTable dataTable = new();
        dataTable.Columns.Add("Column1", typeof(string));
        dataTable.Columns.Add("Column2", typeof(int));
        dataTable.Columns.Add("Column3", typeof(DateTime));

        for (int i = 0; i < 3; i++)
        {
            dataTable.Rows.Add(
                _fixture.Create<string>(),
                _fixture.Create<int>(),
                _fixture.Create<DateTime>());
        }

        // Act
        bool result = ExportFromTable(_workbook, worksheet, dataTable, createTable);

        // Assert
        result.ShouldBeTrue();
        worksheet.Cell(1, 1).Value.ToString().ShouldBe("Column1");
        worksheet.Cell(1, 2).Value.ToString().ShouldBe("Column2");
        worksheet.Cell(1, 3).Value.ToString().ShouldBe("Column3");

        // Verify data rows
        worksheet.Cell(2, 1).Value.ToString().ShouldNotBeEmpty();
        worksheet.Cell(2, 2).Value.ToString().ShouldNotBeEmpty();
        worksheet.Cell(2, 3).Value.ToString().ShouldNotBeEmpty();

        if (createTable)
        {
            worksheet.Tables.Count().ShouldBe(1);
            worksheet.Tables.First().ShowAutoFilter.ShouldBeTrue();
        }
        else
        {
            worksheet.Tables.Count().ShouldBe(0);
            worksheet.AutoFilter.IsEnabled.ShouldBeTrue();
        }
    }

    [Fact]
    public void ExportFromTable_DataTable_WithNullData_ShouldReturnTrue()
    {
        // Arrange
        IXLWorksheet worksheet = _workbook.AddWorksheet("Test");
        using DataTable? dataTable = null;

        // Act
        bool result = ExportFromTable(_workbook, worksheet, dataTable);

        // Assert
        result.ShouldBeTrue();
        worksheet.CellsUsed().Count().ShouldBe(0);
    }

    [Fact]
    public void ExportFromTable_DataTable_WithEmptyData_ShouldReturnTrue()
    {
        // Arrange
        IXLWorksheet worksheet = _workbook.AddWorksheet("Test");
        DataTable dataTable = new();
        dataTable.Columns.Add("Column1");

        // Act
        bool result = ExportFromTable(_workbook, worksheet, dataTable);

        // Assert
        result.ShouldBeTrue();
        worksheet.CellsUsed().Count().ShouldBe(0);
    }

    [Fact]
    public void ExportFromTable_Generic_ShouldRespectCancellationToken()
    {
        // Arrange
        IXLWorksheet worksheet = _workbook.AddWorksheet("Test");
        List<TestData> testData = _fixture.CreateMany<TestData>(1000).ToList();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        bool result = ExportFromTable(_workbook, worksheet, testData, false, cts.Token);

        // Assert
        result.ShouldBe(false);
    }

    [Fact]
    public void ExportFromTable_DataTable_ShouldRespectCancellationToken()
    {
        // Arrange
        IXLWorksheet worksheet = _workbook.AddWorksheet("Test");
        DataTable dataTable = new();
        dataTable.Columns.Add("Column1");
        for (int i = 0; i < 1000; i++)
        {
            dataTable.Rows.Add(_fixture.Create<string>());
        }

        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        bool result = ExportFromTable(_workbook, worksheet, dataTable, false, cts.Token);

        // Assert
        result.ShouldBe(false);
    }
}
