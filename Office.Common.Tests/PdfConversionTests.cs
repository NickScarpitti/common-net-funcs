using System.Runtime.InteropServices;
using static System.IO.Path;
using static CommonNetFuncs.Office.Common.PdfConversion;

namespace Office.Common.Tests;

public sealed class PdfConversionTests //: IDisposable
{
	private readonly string testDataPath;
	private readonly string libreOfficePath;

	public PdfConversionTests()
	{
		// Set LibreOffice path based on OS
		libreOfficePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\Program Files\LibreOffice\program\soffice.com" : "soffice";
		testDataPath = Combine(GetDirectoryName(typeof(PdfConversionTests).Assembly.Location)!, "TestData");
	}

	[Theory]
	[InlineData("Test.xlsx")]
	[InlineData("Test.xls")]
	[InlineData("Test.csv")]
	[InlineData("Test.docx")]
	[InlineData("Test.doc")]
	[InlineData("Test.pptx")]
	[InlineData("Test.ppt")]
	public async Task ConvertToPdf_WithValidExtension_ShouldCreatePdfFile(string fileName)
	{
		// Arrange
		string _tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(_tempPath);

		string sourceFile = Combine(testDataPath, fileName);
		string outputFile = Combine(_tempPath, $"{GetFileNameWithoutExtension(fileName)}.pdf");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		ConvertToPdf(libreOfficePath, sourceFile, _tempPath);
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		File.Exists(outputFile).ShouldBeTrue();
		Directory.Delete(_tempPath, true); // Cleanup
	}

	[Theory]
	[InlineData("TestText.txt")]
	[InlineData("TestJpg.jpg")]
	[InlineData("TestInvalid.invalid")]
	public async Task ConvertToPdf_WithInvalidExtension_ShouldThrowArgumentException(string fileName)
	{
		// Arrange
		string sourceFile = Combine(testDataPath, fileName);
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		// Act & Assert
		Should.Throw<ArgumentException>(() => ConvertToPdf(libreOfficePath, sourceFile));
	}

	[Fact]
	public void ConvertToPdf_WithNonExistentFile_ShouldThrowFileNotFoundException()
	{
		// Arrange
		string nonExistentFile = Combine(testDataPath, "nonexistent.xlsx");

		// Act & Assert
		Should.Throw<FileNotFoundException>(() => ConvertToPdf(libreOfficePath, nonExistentFile));
	}

	[Fact]
	public async Task ConvertToPdf_WithInvalidLibreOfficePath_ShouldThrowLibreOfficeFailedException()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, $"TestExcel-{Guid.NewGuid()}.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		const string invalidLibreOfficePath = "invalid/path/to/soffice";

		// Act & Assert
		Should.Throw<LibreOfficeFailedException>(() => ConvertToPdf(invalidLibreOfficePath, sourceFile));
	}

	[Fact]
	public async Task ConvertToPdf_WithTimeout_ShouldRespectTimeout()
	{
		// Arrange
		string _tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(_tempPath);

		string sourceFile = Combine(testDataPath, "Test.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		TimeSpan timeout = TimeSpan.FromMilliseconds(1); // Very short timeout

		// Act & Assert
		Should.Throw<LibreOfficeFailedException>(() => ConvertToPdf(libreOfficePath, sourceFile, _tempPath, timeout));
		Directory.Delete(_tempPath, true); // Cleanup
	}

	[Fact]
	public async Task ConvertToPdf_WithDefaultOutput_ShouldCreatePdfInSameDirectory()
	{
		// Arrange
		string fileName = $"Test-{Guid.NewGuid()}.xlsx";
		string sourceFile = Combine(testDataPath, fileName);
		string expectedOutputFile = Combine(testDataPath, $"{GetFileNameWithoutExtension(fileName)}.pdf");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		ConvertToPdf(libreOfficePath, sourceFile);
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		File.Exists(expectedOutputFile).ShouldBeTrue();
		File.Delete(expectedOutputFile); // Cleanup
	}

	[Fact]
	public async Task ConvertToPdf_WithCancellation_ShouldThrowLibreOfficeFailedException()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, "Test.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		using CancellationTokenSource cts = new();

		// Act & Assert
		await cts.CancelAsync();
		await Should.ThrowAsync<LibreOfficeFailedException>(() => ConvertToPdfAsync(libreOfficePath, sourceFile, cancellationToken: cts.Token));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public async Task ConvertToPdf_WithRetries_ShouldRetrySpecifiedTimes(int maxRetries)
	{
		// Arrange
		string sourceFile = Combine(testDataPath, "Test.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		const string invalidLibreOfficePath = "invalid/path/to/soffice";

		// Act & Assert
		LibreOfficeFailedException exception = await Should.ThrowAsync<LibreOfficeFailedException>(() => ConvertToPdfAsync(invalidLibreOfficePath, sourceFile, maxRetries: maxRetries));

		exception.Message.ShouldContain("Failed to run LibreOffice!");
	}

	[Fact]
	public async Task ConvertToPdf_WithMaxRetriesZero_ShouldFailImmediately()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, "Test.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		const string invalidLibreOfficePath = "invalid/path/to/soffice";

		// Act & Assert
		LibreOfficeFailedException exception = await Should.ThrowAsync<LibreOfficeFailedException>(() => ConvertToPdfAsync(invalidLibreOfficePath, sourceFile, maxRetries: 0));

		exception.Message.ShouldContain("Failed to run LibreOffice!");
	}

	[Fact]
	public async Task ConvertToPdf_WithSuccessfulRetry_ShouldCompleteConversion()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "Test.xlsx");
		string outputFile = Combine(tempPath, "Test.pdf");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		// Act
		await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 3);

		// Assert
		File.Exists(outputFile).ShouldBeTrue();
		Directory.Delete(tempPath, true); // Cleanup
	}

	[Fact]
	public async Task ConvertToPdf_WithCancellationAndRetries_ShouldRespectBoth()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, "Test.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		using CancellationTokenSource cts = new();
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));

		// Act & Assert
		await Should.ThrowAsync<LibreOfficeFailedException>(() => ConvertToPdfAsync(libreOfficePath, sourceFile, cancellationToken: cts.Token, maxRetries: 3));
	}
}
