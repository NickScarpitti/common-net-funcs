<<<<<<< HEAD
﻿using System.Runtime.InteropServices;
using static CommonNetFuncs.Office.Common.PdfConversion;
using static System.IO.Path;

namespace Office.Common.Tests;

public sealed class PdfConversionTests //: IDisposable
{
    private readonly string _testDataPath;
    private readonly string _libreOfficePath;

    public PdfConversionTests()
    {
        // Set LibreOffice path based on OS
        _libreOfficePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\Program Files\LibreOffice\program\soffice.com" : "soffice";
        _testDataPath = Combine(GetDirectoryName(typeof(PdfConversionTests).Assembly.Location)!, "TestData");
    }

    [Theory]
    [InlineData("Test.xlsx")]
    [InlineData("Test.xls")]
    [InlineData("Test.csv")]
    [InlineData("Test.docx")]
    [InlineData("Test.doc")]
    [InlineData("Test.pptx")]
    [InlineData("Test.ppt")]
    public void ConvertToPdf_WithValidExtension_ShouldCreatePdfFile(string fileName)
    {
        // Arrange
        string _tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        string sourceFile = Combine(_testDataPath, fileName);
        string outputFile = Combine(_tempPath, $"{GetFileNameWithoutExtension(fileName)}.pdf");
        File.WriteAllText(sourceFile, "Test content");

        // Act
        ConvertToPdf(_libreOfficePath, sourceFile, _tempPath);

        // Assert
        File.Exists(outputFile).ShouldBeTrue();
        Directory.Delete(_tempPath, true); // Cleanup
    }

    [Theory]
    [InlineData("TestText.txt")]
    [InlineData("TestJpg.jpg")]
    [InlineData("TestInvalid.invalid")]
    public void ConvertToPdf_WithInvalidExtension_ShouldThrowArgumentException(string fileName)
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, fileName);
        File.WriteAllText(sourceFile, "Test content");

        // Act & Assert
        Should.Throw<ArgumentException>(() => ConvertToPdf(_libreOfficePath, sourceFile));
    }

    [Fact]
    public void ConvertToPdf_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        string nonExistentFile = Combine(_testDataPath, "nonexistent.xlsx");

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => ConvertToPdf(_libreOfficePath, nonExistentFile));
    }

    [Fact]
    public void ConvertToPdf_WithInvalidLibreOfficePath_ShouldThrowLibreOfficeFailedException()
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, $"TestExcel-{Guid.NewGuid()}.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        const string invalidLibreOfficePath = "invalid/path/to/soffice";

        // Act & Assert
        Should.Throw<LibreOfficeFailedException>(() => ConvertToPdf(invalidLibreOfficePath, sourceFile));
    }

    [Fact]
    public void ConvertToPdf_WithTimeout_ShouldRespectTimeout()
    {
        // Arrange
        string _tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        TimeSpan timeout = TimeSpan.FromMilliseconds(1); // Very short timeout

        // Act & Assert
        Should.Throw<LibreOfficeFailedException>(() => ConvertToPdf(_libreOfficePath, sourceFile, _tempPath, timeout));
        Directory.Delete(_tempPath, true); // Cleanup
    }

    [Fact]
    public void ConvertToPdf_WithDefaultOutput_ShouldCreatePdfInSameDirectory()
    {
        // Arrange
        string fileName = $"Test-{Guid.NewGuid()}.xlsx";
        string sourceFile = Combine(_testDataPath, fileName);
        string expectedOutputFile = Combine(_testDataPath, $"{GetFileNameWithoutExtension(fileName)}.pdf");
        File.WriteAllText(sourceFile, "Test content");

        // Act
        ConvertToPdf(_libreOfficePath, sourceFile);

        // Assert
        File.Exists(expectedOutputFile).ShouldBeTrue();
        File.Delete(expectedOutputFile); // Cleanup
    }

    [Fact]
    public void ConvertToPdf_WithCancellation_ShouldThrowLibreOfficeFailedException()
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        using CancellationTokenSource cts = new();

        // Act & Assert
        cts.Cancel();
        Should.Throw<LibreOfficeFailedException>(() => ConvertToPdfAsync(_libreOfficePath, sourceFile, cancellationToken: cts.Token));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ConvertToPdf_WithRetries_ShouldRetrySpecifiedTimes(int maxRetries)
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        const string invalidLibreOfficePath = "invalid/path/to/soffice";

        // Act & Assert
        LibreOfficeFailedException exception = Should.Throw<LibreOfficeFailedException>(() => ConvertToPdfAsync(invalidLibreOfficePath, sourceFile, maxRetries: maxRetries));

        exception.Message.ShouldContain("Failed to run LibreOffice!");
    }

    [Fact]
    public void ConvertToPdf_WithMaxRetriesZero_ShouldFailImmediately()
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        const string invalidLibreOfficePath = "invalid/path/to/soffice";

        // Act & Assert
        LibreOfficeFailedException exception = Should.Throw<LibreOfficeFailedException>(() => ConvertToPdfAsync(invalidLibreOfficePath, sourceFile, maxRetries: 0));

        exception.Message.ShouldContain("Failed to run LibreOffice!");
    }

    [Fact]
    public async Task ConvertToPdf_WithSuccessfulRetry_ShouldCompleteConversion()
    {
        // Arrange
        string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        string outputFile = Combine(tempPath, "Test.pdf");
        File.WriteAllText(sourceFile, "Test content");

        // Act
        await ConvertToPdfAsync(_libreOfficePath, sourceFile, tempPath, maxRetries: 3);

        // Assert
        File.Exists(outputFile).ShouldBeTrue();
        Directory.Delete(tempPath, true); // Cleanup
    }

    [Fact]
    public void ConvertToPdf_WithCancellationAndRetries_ShouldRespectBoth()
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        using CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        Should.Throw<LibreOfficeFailedException>(() => ConvertToPdfAsync(_libreOfficePath, sourceFile, cancellationToken: cts.Token, maxRetries: 3));
    }
}
=======
﻿using System.Runtime.InteropServices;
using static CommonNetFuncs.Office.Common.PdfConversion;
using static System.IO.Path;

namespace Office.Common.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly

public sealed class PdfConversionTests //: IDisposable
{
    private readonly string _testDataPath;
    private readonly string _libreOfficePath;

    public PdfConversionTests()
    {
        // Set LibreOffice path based on OS
        _libreOfficePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\Program Files\LibreOffice\program\soffice.com" : "soffice";
        _testDataPath = Combine(GetDirectoryName(typeof(PdfConversionTests).Assembly.Location)!, "TestData");
    }

    [Theory]
    [InlineData("Test.xlsx")]
    [InlineData("Test.xls")]
    [InlineData("Test.csv")]
    [InlineData("Test.docx")]
    [InlineData("Test.doc")]
    [InlineData("Test.pptx")]
    [InlineData("Test.ppt")]
    public void ConvertToPdf_WithValidExtension_ShouldCreatePdfFile(string fileName)
    {
        // Arrange
        string _tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        string sourceFile = Combine(_testDataPath, fileName);
        string outputFile = Combine(_tempPath, $"{GetFileNameWithoutExtension(fileName)}.pdf");
        File.WriteAllText(sourceFile, "Test content");

        // Act
        ConvertToPdf(_libreOfficePath, sourceFile, _tempPath);

        // Assert
        File.Exists(outputFile).ShouldBeTrue();
        Directory.Delete(_tempPath, true); // Cleanup
    }

    [Theory]
    [InlineData("TestText.txt")]
    [InlineData("TestJpg.jpg")]
    [InlineData("TestInvalid.invalid")]
    public void ConvertToPdf_WithInvalidExtension_ShouldThrowArgumentException(string fileName)
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, fileName);
        File.WriteAllText(sourceFile, "Test content");

        // Act & Assert
        Should.Throw<ArgumentException>(() => ConvertToPdf(_libreOfficePath, sourceFile));
    }

    [Fact]
    public void ConvertToPdf_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        string nonExistentFile = Combine(_testDataPath, "nonexistent.xlsx");

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => ConvertToPdf(_libreOfficePath, nonExistentFile));
    }

    [Fact]
    public void ConvertToPdf_WithInvalidLibreOfficePath_ShouldThrowLibreOfficeFailedException()
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, $"TestExcel-{Guid.NewGuid()}.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        const string invalidLibreOfficePath = "invalid/path/to/soffice";

        // Act & Assert
        Should.Throw<LibreOfficeFailedException>(() => ConvertToPdf(invalidLibreOfficePath, sourceFile));
    }

    [Fact]
    public void ConvertToPdf_WithTimeout_ShouldRespectTimeout()
    {
        // Arrange
        string _tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        TimeSpan timeout = TimeSpan.FromMilliseconds(1); // Very short timeout

        // Act & Assert
        Should.Throw<LibreOfficeFailedException>(() => ConvertToPdf(_libreOfficePath, sourceFile, _tempPath, timeout));
        Directory.Delete(_tempPath, true); // Cleanup
    }

    [Fact]
    public void ConvertToPdf_WithDefaultOutput_ShouldCreatePdfInSameDirectory()
    {
        // Arrange
        string fileName = $"Test-{Guid.NewGuid()}.xlsx";
        string sourceFile = Combine(_testDataPath, fileName);
        string expectedOutputFile = Combine(_testDataPath, $"{GetFileNameWithoutExtension(fileName)}.pdf");
        File.WriteAllText(sourceFile, "Test content");

        // Act
        ConvertToPdf(_libreOfficePath, sourceFile);

        // Assert
        File.Exists(expectedOutputFile).ShouldBeTrue();
        File.Delete(expectedOutputFile); // Cleanup
    }

    [Fact]
    public void ConvertToPdf_WithCancellation_ShouldThrowLibreOfficeFailedException()
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        using CancellationTokenSource cts = new();

        // Act & Assert
        cts.Cancel();
        Should.Throw<LibreOfficeFailedException>(() => ConvertToPdfAsync(_libreOfficePath, sourceFile, cancellationToken: cts.Token));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ConvertToPdf_WithRetries_ShouldRetrySpecifiedTimes(int maxRetries)
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        const string invalidLibreOfficePath = "invalid/path/to/soffice";

        // Act & Assert
        LibreOfficeFailedException exception = Should.Throw<LibreOfficeFailedException>(() => ConvertToPdfAsync(invalidLibreOfficePath, sourceFile, maxRetries: maxRetries));

        exception.Message.ShouldContain("Failed to run LibreOffice!");
    }

    [Fact]
    public void ConvertToPdf_WithMaxRetriesZero_ShouldFailImmediately()
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        const string invalidLibreOfficePath = "invalid/path/to/soffice";

        // Act & Assert
        LibreOfficeFailedException exception = Should.Throw<LibreOfficeFailedException>(() => ConvertToPdfAsync(invalidLibreOfficePath, sourceFile, maxRetries: 0));

        exception.Message.ShouldContain("Failed to run LibreOffice!");
    }

    [Fact]
    public async Task ConvertToPdf_WithSuccessfulRetry_ShouldCompleteConversion()
    {
        // Arrange
        string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        string outputFile = Combine(tempPath, "Test.pdf");
        File.WriteAllText(sourceFile, "Test content");

        // Act
        await ConvertToPdfAsync(_libreOfficePath, sourceFile, tempPath, maxRetries: 3);

        // Assert
        File.Exists(outputFile).ShouldBeTrue();
        Directory.Delete(tempPath, true); // Cleanup
    }

    [Fact]
    public void ConvertToPdf_WithCancellationAndRetries_ShouldRespectBoth()
    {
        // Arrange
        string sourceFile = Combine(_testDataPath, "Test.xlsx");
        File.WriteAllText(sourceFile, "Test content");
        using CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        Should.Throw<LibreOfficeFailedException>(() => ConvertToPdfAsync(_libreOfficePath, sourceFile, cancellationToken: cts.Token, maxRetries: 3));
    }
}

#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
