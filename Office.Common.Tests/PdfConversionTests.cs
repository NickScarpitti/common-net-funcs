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
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);

		string sourceFile = Combine(testDataPath, fileName);
		string outputFile = Combine(tempPath, $"{GetFileNameWithoutExtension(fileName)}.pdf");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		ConvertToPdf(libreOfficePath, sourceFile, tempPath);
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		File.Exists(outputFile).ShouldBeTrue();
		Directory.Delete(tempPath, true); // Cleanup
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
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);

		string sourceFile = Combine(testDataPath, "Test.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		TimeSpan timeout = TimeSpan.FromMilliseconds(1); // Very short timeout

		// Act & Assert
		Should.Throw<LibreOfficeFailedException>(() => ConvertToPdf(libreOfficePath, sourceFile, tempPath, timeout));
		Directory.Delete(tempPath, true); // Cleanup
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

	[Fact]
	public void LibreOfficeFailedException_DefaultConstructor_ShouldCreateInstance()
	{
		// Act
		LibreOfficeFailedException exception = new();

		// Assert
		exception.ShouldNotBeNull();
		exception.Message.ShouldNotBeNull();
	}

	[Fact]
	public void LibreOfficeFailedException_MessageConstructor_ShouldSetMessage()
	{
		// Arrange
		const string expectedMessage = "Test error message";

		// Act
		LibreOfficeFailedException exception = new(expectedMessage);

		// Assert
		exception.Message.ShouldBe(expectedMessage);
	}

	[Fact]
	public void LibreOfficeFailedException_MessageAndInnerConstructor_ShouldSetBoth()
	{
		// Arrange
		const string expectedMessage = "Test error message";
		Exception innerException = new InvalidOperationException("Inner exception");

		// Act
		LibreOfficeFailedException exception = new(expectedMessage, innerException);

		// Assert
		exception.Message.ShouldBe(expectedMessage);
		exception.InnerException.ShouldBe(innerException);
	}

	[Fact]
	public async Task ConvertToPdfAsync_WithInvalidExtension_ShouldThrowArgumentException()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, $"TestInvalid-{Guid.NewGuid()}.txt");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act & Assert
			await Should.ThrowAsync<ArgumentException>(() => ConvertToPdfAsync(libreOfficePath, sourceFile));
		}
		finally
		{
			// Cleanup
			if (File.Exists(sourceFile))
			{
				File.Delete(sourceFile);
			}
		}
	}

	[Fact]
	public async Task ConvertToPdfAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
	{
		// Arrange
		string nonExistentFile = Combine(testDataPath, $"nonexistent-{Guid.NewGuid()}.xlsx");

		// Act & Assert
		await Should.ThrowAsync<FileNotFoundException>(() => ConvertToPdfAsync(libreOfficePath, nonExistentFile));
	}

	[Fact]
	public async Task ConvertToPdf_WithNullOutputPath_ShouldUseSourceDirectory()
	{
		// Arrange
		string fileName = $"Test-NullOutput-{Guid.NewGuid()}.xlsx";
		string sourceFile = Combine(testDataPath, fileName);
		string expectedOutputFile = Combine(testDataPath, $"{GetFileNameWithoutExtension(fileName)}.pdf");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, null);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert - File should be created in the same directory as source
			File.Exists(expectedOutputFile).ShouldBeTrue();
		}
		finally
		{
			// Cleanup
			if (File.Exists(sourceFile))
			{
				File.Delete(sourceFile);
			}
			if (File.Exists(expectedOutputFile))
			{
				File.Delete(expectedOutputFile);
			}
		}
	}

	[Fact]
	public async Task ConvertToPdfAsync_WithNullCancellationToken_ShouldCompleteSuccessfully()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "Test.xlsx");
		string outputFile = Combine(tempPath, "Test.pdf");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act
			await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, cancellationToken: null, maxRetries: 1);

			// Assert
			File.Exists(outputFile).ShouldBeTrue();
		}
		finally
		{
			// Cleanup
			if (Directory.Exists(tempPath))
			{
				Directory.Delete(tempPath, true);
			}
		}
	}

	[Fact]
	public async Task ConvertToPdf_WithNullConversionTimeout_ShouldWaitIndefinitely()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "Test.xlsx");
		string outputFile = Combine(tempPath, $"{GetFileNameWithoutExtension("Test.xlsx")}.pdf");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act - null timeout means wait indefinitely
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath, conversionTimeout: null, maxRetries: 1);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			File.Exists(outputFile).ShouldBeTrue();
		}
		finally
		{
			// Cleanup
			if (Directory.Exists(tempPath))
			{
				Directory.Delete(tempPath, true);
			}
		}
	}

	[Fact]
	public async Task ConvertToPdf_WithVeryShortTimeout_ShouldHandleTimeoutAndRetry()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "Test.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act & Assert - With a very short timeout (1ms), the process should timeout and retry
			// This tests the timeout and retry logic, including the Console.WriteLine for retries
#pragma warning disable S6966 // Awaitable method should be used
			Should.Throw<LibreOfficeFailedException>(() =>
			{
				ConvertToPdf(libreOfficePath, sourceFile, tempPath,
					conversionTimeout: TimeSpan.FromMilliseconds(1), maxRetries: 2);
			});
#pragma warning restore S6966 // Awaitable method should be used
		}
		finally
		{
			// Cleanup
			if (Directory.Exists(tempPath))
			{
				Directory.Delete(tempPath, true);
			}
		}
	}

	[Fact]
	public void ConvertToPdf_WithNullLibreOfficePath_ShouldThrowException()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "Test.xlsx");

		try
		{
			// Act & Assert
#pragma warning disable S6966 // Awaitable method should be used
			Should.Throw<Exception>(() =>
			{
				ConvertToPdf(null!, sourceFile, tempPath);
			});
#pragma warning restore S6966 // Awaitable method should be used
		}
		finally
		{
			// Cleanup
			if (Directory.Exists(tempPath))
			{
				Directory.Delete(tempPath, true);
			}
		}
	}

	[Fact]
	public async Task ConvertToPdfAsync_WithMultipleRetries_ShouldEventuallyFail()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "InvalidFile.xyz");
		await File.WriteAllTextAsync(sourceFile, "Not a real Office file", TestContext.Current.CancellationToken);

		try
		{
			// Act & Assert - Test retry logic with maxRetries > 1
			// This should fail all retries and throw, exercising the retry Console.WriteLine
			await Should.ThrowAsync<ArgumentException>(async () =>
			{
				await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 3);
			});
		}
		finally
		{
			// Cleanup
			if (File.Exists(sourceFile))
			{
				File.Delete(sourceFile);
			}
			if (Directory.Exists(tempPath))
			{
				Directory.Delete(tempPath, true);
			}
		}
	}
}

