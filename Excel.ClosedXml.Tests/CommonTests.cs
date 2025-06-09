using ClosedXML.Excel;
using CommonNetFuncs.Excel.ClosedXml;
using CommonNetFuncs.Excel.Common;
using NSubstitute;

namespace Excel.ClosedXml.Tests;

public sealed class CommonTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsCellEmpty_WithEmptyValues_ShouldReturnTrue(string? value)
    {
        // Arrange
        IXLCell cell = Substitute.For<IXLCell>();
        cell.Value.Returns(value);

        // Act
        bool result = cell.IsCellEmpty();

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Test")]
    [InlineData("123")]
    public void IsCellEmpty_WithNonEmptyValues_ShouldReturnFalse(string value)
    {
        // Arrange
        IXLCell cell = Substitute.For<IXLCell>();
        cell.Value.Returns(value);

        // Act
        bool result = cell.IsCellEmpty();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void WriteExcelFile_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        string tempPath = Path.GetTempFileName();
        using XLWorkbook workbook = new();
        workbook.AddWorksheet("TestSheet");
        // Act
        bool result = Common.WriteExcelFile(workbook, tempPath);

        // Assert
        result.ShouldBeTrue();
        File.Exists(tempPath).ShouldBeTrue();

        // Cleanup
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void WriteExcelFile_WhenPathIsInvalid_ShouldReturnFalse()
    {
        // Arrange
        const string invalidPath = "Z:\\invalid\\path\\file.xlsx";
        using XLWorkbook workbook = new();

        // Act
        bool result = Common.WriteExcelFile(workbook, invalidPath);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(EStyle.Header)]
    [InlineData(EStyle.Body)]
    [InlineData(EStyle.Error)]
    public void GetStyle_WithValidStyle_ShouldReturnNonNullStyle(EStyle style)
    {
        // Arrange
        using XLWorkbook workbook = new();

        // Act
        IXLStyle? result = Common.GetStyle(style, workbook);

        // Assert
        result.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("#FF0000")]
    [InlineData("#00FF00")]
    public void GetStyle_WithCustomStyle_ShouldApplyCustomProperties(string htmlColor)
    {
        // Arrange
        using XLWorkbook workbook = new();
        const XLAlignmentHorizontalValues alignment = XLAlignmentHorizontalValues.Center;

        // Act
        IXLStyle? result = Common.GetStyle(EStyle.Custom, workbook, htmlColor: htmlColor, font: Common.GetFont(EFont.Default, workbook), alignment: alignment);

        // Assert
        result.ShouldNotBeNull();
        if (!string.IsNullOrEmpty(htmlColor))
        {
            // Updated to use XLColor.FromHtml for comparison
            result.Fill.BackgroundColor.ShouldBe(XLColor.FromHtml(htmlColor));
        }
        result.Alignment.Horizontal.ShouldBe(alignment);
        result.Font.ShouldBe(Common.GetFont(EFont.Default, workbook));
    }

    [Theory]
    [InlineData(EFont.Default)]
    [InlineData(EFont.Header)]
    public void GetFont_WithValidFont_ShouldReturnConfiguredFont(EFont font)
    {
        // Arrange
        using XLWorkbook workbook = new();

        // Act
        IXLFont result = Common.GetFont(font, workbook);

        // Assert
        result.ShouldNotBeNull();
        result.FontName.ShouldBe("Calibri");
        result.FontSize.ShouldBe(10);

        if (font == EFont.Header)
        {
            result.Bold.ShouldBeTrue();
        }
        else
        {
            result.Bold.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task WriteFileToMemoryStreamAsync_ShouldWriteWorkbookToStream()
    {
        // Arrange
        using XLWorkbook workbook = new();
        await using MemoryStream memoryStream = new();
        workbook.AddWorksheet("Sheet1");

        // Act
        await memoryStream.WriteFileToMemoryStreamAsync(workbook);

        // Assert
        memoryStream.Length.ShouldBeGreaterThan(0);
        memoryStream.Position.ShouldBe(0);
    }

    [Fact]
    public async Task WriteFileToMemoryStreamAsync_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        using XLWorkbook workbook = new();
        await using MemoryStream memoryStream = new();
        using CancellationTokenSource cts = new();
        workbook.AddWorksheet("Sheet1");
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await memoryStream.WriteFileToMemoryStreamAsync(workbook, cts.Token));
    }
}
