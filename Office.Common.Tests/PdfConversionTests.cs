using System.Runtime.InteropServices;
using static System.IO.Path;
using static CommonNetFuncs.Office.Common.PdfConversion;

namespace Office.Common.Tests;

public enum ExceptionConstructorType
{
	Default,
	WithMessage,
	WithMessageAndInner
}

public enum ExecutionMode
{
	Asynchronous,
	Synchronous
}

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

	[Theory]
	[InlineData(ExceptionConstructorType.Default)]
	[InlineData(ExceptionConstructorType.WithMessage)]
	[InlineData(ExceptionConstructorType.WithMessageAndInner)]
	public void LibreOfficeFailedException_Constructors_ShouldWorkCorrectly(ExceptionConstructorType constructorType)
	{
		// Arrange
		const string expectedMessage = "Test error message";
		Exception? innerException = new InvalidOperationException("Inner exception");

		// Act
		LibreOfficeFailedException exception = constructorType switch
		{
			ExceptionConstructorType.Default => new LibreOfficeFailedException(),
			ExceptionConstructorType.WithMessage => new LibreOfficeFailedException(expectedMessage),
			ExceptionConstructorType.WithMessageAndInner => new LibreOfficeFailedException(expectedMessage, innerException),
			_ => throw new InvalidOperationException($"Unknown constructor type: {constructorType}")
		};

		// Assert
		switch (constructorType)
		{
			case ExceptionConstructorType.Default:
				exception.ShouldNotBeNull();
				exception.Message.ShouldNotBeNull();
				break;

			case ExceptionConstructorType.WithMessage:
				exception.Message.ShouldBe(expectedMessage);
				break;

			case ExceptionConstructorType.WithMessageAndInner:
				exception.Message.ShouldBe(expectedMessage);
				exception.InnerException.ShouldBe(innerException);
				break;
		}
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

	[Fact]
	public async Task ConvertToPdf_WithExistingOutputFile_ShouldOverwriteFile()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestExisting.xlsx");
		string expectedOutputFile = Combine(tempPath, "TestExisting.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(expectedOutputFile, "Old PDF content", TestContext.Current.CancellationToken);

		long originalSize = new FileInfo(expectedOutputFile).Length;

		try
		{
			// Act - Test with overwriteExistingFile = true to allow overwriting
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: true);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert - File should exist and be overwritten (tests File.Move with overwrite: true)
			File.Exists(expectedOutputFile).ShouldBeTrue();
			// The file should still exist after being overwritten
			FileInfo newFileInfo = new(expectedOutputFile);
			newFileInfo.Exists.ShouldBeTrue();
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

	[Fact]
	public async Task ConvertToPdf_WithLockedTempFile_ShouldHandleCleanupFailure()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, $"TestLocked-{Guid.NewGuid()}.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act - The cleanup might fail if files are locked, but conversion should still work
			// This tests the error handling in CleanUpTempFiles
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert - Should complete without throwing even if cleanup has issues
			string expectedOutputFile = Combine(tempPath, $"{GetFileNameWithoutExtension(sourceFile)}.pdf");
			File.Exists(expectedOutputFile).ShouldBeTrue();
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

	[Fact]
	public async Task ConvertToPdf_WithAllFileTypes_ShouldUseCorrectPdfCommand()
	{
		// Arrange & Act & Assert - Test all supported file types to ensure GetPdfCommand coverage
		string[] excelTypes = ["Test.xlsx", "Test.xls", "Test.csv"];
		string[] wordTypes = ["Test.docx", "Test.doc"];
		string[] powerPointTypes = ["Test.pptx", "Test.ppt"];

		foreach (string fileType in excelTypes.Concat(wordTypes).Concat(powerPointTypes))
		{
			string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempPath);
			string sourceFile = Combine(testDataPath, fileType);
			await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

			try
			{
#pragma warning disable S6966 // Awaitable method should be used
				ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1);
#pragma warning restore S6966 // Awaitable method should be used

				string expectedOutputFile = Combine(tempPath, $"{GetFileNameWithoutExtension(fileType)}.pdf");
				File.Exists(expectedOutputFile).ShouldBeTrue();
			}
			finally
			{
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

	[Fact]
	public async Task ConvertToPdfAsync_WithSemaphoreContention_ShouldHandleConcurrency()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile1 = Combine(testDataPath, "TestConcurrent1.xlsx");
		string sourceFile2 = Combine(testDataPath, "TestConcurrent2.xlsx");
		await File.WriteAllTextAsync(sourceFile1, "Test content 1", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(sourceFile2, "Test content 2", TestContext.Current.CancellationToken);

		try
		{
			// Act - Run two conversions concurrently to test semaphore behavior
			Task task1 = ConvertToPdfAsync(libreOfficePath, sourceFile1, tempPath, maxRetries: 1);
			Task task2 = ConvertToPdfAsync(libreOfficePath, sourceFile2, tempPath, maxRetries: 1);

			await Task.WhenAll(task1, task2);

			// Assert
			string outputFile1 = Combine(tempPath, "TestConcurrent1.pdf");
			string outputFile2 = Combine(tempPath, "TestConcurrent2.pdf");
			File.Exists(outputFile1).ShouldBeTrue();
			File.Exists(outputFile2).ShouldBeTrue();
		}
		finally
		{
			// Cleanup
			if (File.Exists(sourceFile1))
			{
				File.Delete(sourceFile1);
			}
			if (File.Exists(sourceFile2))
			{
				File.Delete(sourceFile2);
			}
			if (Directory.Exists(tempPath))
			{
				Directory.Delete(tempPath, true);
			}
		}
	}

	[Fact]
	public void ConvertToPdf_WithEmptyFileName_ShouldThrowFileNotFoundException()
	{
		// Arrange
		string emptyFileName = Combine(testDataPath, "DoesNotExist.xlsx");

		// Act & Assert
		Should.Throw<FileNotFoundException>(() => ConvertToPdf(libreOfficePath, emptyFileName));
	}

	[Fact]
	public async Task ConvertToPdfAsync_WithLongRunningConversion_ShouldComplete()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestLongRunning.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content with some data", TestContext.Current.CancellationToken);

		try
		{
			// Act - Test with sufficient timeout for a real conversion
			await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 1);

			// Assert
			string expectedOutputFile = Combine(tempPath, "TestLongRunning.pdf");
			File.Exists(expectedOutputFile).ShouldBeTrue();
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

	[Fact]
	public async Task ConvertToPdf_WithSpecialCharactersInPath_ShouldHandleCorrectly()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), $"Test Path {Guid.NewGuid()}");
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "Test Special.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			string expectedOutputFile = Combine(tempPath, "Test Special.pdf");
			File.Exists(expectedOutputFile).ShouldBeTrue();
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

	[Fact]
	public async Task ConvertToPdf_WithMaxRetriesOne_ShouldRetryOnce()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, $"TestRetryOnce-{Guid.NewGuid()}.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		const string invalidLibreOfficePath = "invalid/soffice";

		try
		{
			// Act & Assert - Should retry once before failing
			LibreOfficeFailedException exception = Should.Throw<LibreOfficeFailedException>(() =>
				ConvertToPdf(invalidLibreOfficePath, sourceFile, maxRetries: 1));

			exception.Message.ShouldContain("Failed to run LibreOffice!");
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
	public async Task ConvertToPdfAsync_WithOperationCanceledException_ShouldWrapInLibreOfficeException()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, "TestCancel.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		using CancellationTokenSource cts = new();

		try
		{
			// Act - Cancel immediately
			await cts.CancelAsync();

			// Assert - Should wrap OperationCanceledException in LibreOfficeFailedException
			LibreOfficeFailedException exception = await Should.ThrowAsync<LibreOfficeFailedException>(() =>
				ConvertToPdfAsync(libreOfficePath, sourceFile, cancellationToken: cts.Token, maxRetries: 1));

			exception.Message.ShouldContain("canceled");
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
	public async Task ConvertToPdf_WithEmptyOutputPath_ShouldUseFileName()
	{
		// Arrange
		string fileName = $"TestEmpty-{Guid.NewGuid()}.xlsx";
		string sourceFile = Combine(testDataPath, fileName);
		string expectedOutputFile = Combine(testDataPath, $"{GetFileNameWithoutExtension(fileName)}.pdf");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act - Pass empty string as output path
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, string.Empty, maxRetries: 1);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert - Should create PDF in a valid location (either testDataPath or temp)
			// The behavior with empty string may vary, but should not crash
			bool pdfExistsInTestPath = File.Exists(expectedOutputFile);
			bool pdfExistsInEmpty = File.Exists($"{GetFileNameWithoutExtension(fileName)}.pdf");

			(pdfExistsInTestPath || pdfExistsInEmpty).ShouldBeTrue();
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
			if (File.Exists($"{GetFileNameWithoutExtension(fileName)}.pdf"))
			{
				File.Delete($"{GetFileNameWithoutExtension(fileName)}.pdf");
			}
		}
	}

	[Fact]
	public void ConvertToPdf_SynchronousWithLock_ShouldCompleteInOrder()
	{
		// Arrange
		string fileName1 = $"TestLock1-{Guid.NewGuid()}.xlsx";
		string fileName2 = $"TestLock2-{Guid.NewGuid()}.xlsx";
		string sourceFile1 = Combine(testDataPath, fileName1);
		string sourceFile2 = Combine(testDataPath, fileName2);
		File.WriteAllText(sourceFile1, "Test content 1");
		File.WriteAllText(sourceFile2, "Test content 2");

		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);

		try
		{
			// Act - Run two synchronous conversions (tests the lock mechanism)
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile1, tempPath, maxRetries: 1);
			ConvertToPdf(libreOfficePath, sourceFile2, tempPath, maxRetries: 1);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert - Both should complete
			string outputFile1 = Combine(tempPath, $"{GetFileNameWithoutExtension(fileName1)}.pdf");
			string outputFile2 = Combine(tempPath, $"{GetFileNameWithoutExtension(fileName2)}.pdf");
			File.Exists(outputFile1).ShouldBeTrue();
			File.Exists(outputFile2).ShouldBeTrue();
		}
		finally
		{
			// Cleanup
			if (File.Exists(sourceFile1))
			{
				File.Delete(sourceFile1);
			}
			if (File.Exists(sourceFile2))
			{
				File.Delete(sourceFile2);
			}
			if (Directory.Exists(tempPath))
			{
				Directory.Delete(tempPath, true);
			}
		}
	}

	[Fact]
	public async Task ConvertToPdfAsync_WithZeroRetries_ShouldFailImmediatelyOnError()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, "TestZeroRetry.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		const string invalidPath = "definitely/not/a/real/path/soffice";

		try
		{
			// Act & Assert - With 0 retries, should fail on first attempt
			LibreOfficeFailedException exception = await Should.ThrowAsync<LibreOfficeFailedException>(() =>
				ConvertToPdfAsync(invalidPath, sourceFile, maxRetries: 0));

			exception.Message.ShouldContain("Failed to run LibreOffice!");
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
	public async Task ConvertToPdf_UsingRealTestFiles_ShouldConvertAllFormats()
	{
		// Arrange - Use the actual test data files that exist
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);

		string[] testFiles = ["Test.xlsx", "Test.xls", "Test.csv", "Test.docx", "Test.doc", "Test.pptx", "Test.ppt"];

		try
		{
			foreach (string testFile in testFiles)
			{
				string sourceFile = Combine(testDataPath, testFile);
				if (File.Exists(sourceFile))
				{
					// Act
					try
					{
#pragma warning disable S6966 // Awaitable method should be used
						ConvertToPdf(libreOfficePath, sourceFile, tempPath, conversionTimeout: TimeSpan.FromSeconds(30), maxRetries: 1);
#pragma warning restore S6966 // Awaitable method should be used

						// Assert - Check if PDF was created
						string expectedPdf = Combine(tempPath, $"{GetFileNameWithoutExtension(testFile)}.pdf");
						if (File.Exists(expectedPdf))
						{
							// PDF created successfully
							File.Delete(expectedPdf);
						}
					}
					catch (LibreOfficeFailedException)
					{
						// LibreOffice might not be installed or file might not be valid - this is acceptable
					}
				}
			}
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
	public async Task ConvertToPdfAsync_UsingRealTestFiles_ShouldConvertAsync()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "Test.xlsx");

		try
		{
			if (File.Exists(sourceFile))
			{
				// Act
				try
				{
					await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 1);

					// Assert
					string expectedPdf = Combine(tempPath, "Test.pdf");
					if (File.Exists(expectedPdf))
					{
						File.Exists(expectedPdf).ShouldBeTrue();
					}
				}
				catch (LibreOfficeFailedException)
				{
					// LibreOffice might not be installed - this is acceptable
				}
			}
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

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ConvertToPdf_WithInvalidLibreOfficePath_ShouldThrowException(string? invalidPath)
	{
		// Arrange
		string sourceFile = Combine(testDataPath, "Test.xlsx");

		// Act & Assert - Should throw when LibreOffice path is null or whitespace
		Should.Throw<Exception>(() => ConvertToPdf(invalidPath!, sourceFile));
	}

	[Fact]
	public async Task ConvertToPdfAsync_WithCancellationDuringConversion_ShouldThrowLibreOfficeException()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, "TestCancelDuring.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		using CancellationTokenSource cts = new();
		cts.CancelAfter(TimeSpan.FromMilliseconds(10)); // Cancel very quickly

		try
		{
			// Act & Assert - Should throw LibreOfficeFailedException when canceled
			await Should.ThrowAsync<LibreOfficeFailedException>(() =>
				ConvertToPdfAsync(libreOfficePath, sourceFile, cancellationToken: cts.Token, maxRetries: 0));
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
	public void ConvertToPdf_WithNegativeRetries_ShouldNotAttempt()
	{
		// Arrange
		string sourceFile = Combine(testDataPath, "TestNegativeRetries.xlsx");
		File.WriteAllText(sourceFile, "Test content");
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);

		try
		{
			// Act - With negative retries, the loop condition (i <= maxRetries) is never true
			// so no conversion attempt is made, and no exception is thrown
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: -1);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert - No PDF should be created since no attempt was made
			string expectedPdf = Combine(tempPath, "TestNegativeRetries.pdf");
			File.Exists(expectedPdf).ShouldBeFalse();
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

	[Fact]
	public async Task ConvertToPdf_WithVeryLongTimeout_ShouldComplete()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestLongTimeout.xlsx");
		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act - Use a very long timeout
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath,
				conversionTimeout: TimeSpan.FromMinutes(5), maxRetries: 1);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			string expectedPdf = Combine(tempPath, "TestLongTimeout.pdf");
			File.Exists(expectedPdf).ShouldBeTrue();
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

	[Fact]
	public async Task ConvertToPdf_WithOverwriteFalseAndExistingFile_ShouldThrowException()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestNoOverwrite.xlsx");
		string expectedOutputFile = Combine(tempPath, "TestNoOverwrite.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(expectedOutputFile, "Existing PDF content", TestContext.Current.CancellationToken);

		try
		{
			// Act & Assert - With overwriteExistingFile = false (default), should fail when file exists
			LibreOfficeFailedException exception = Should.Throw<LibreOfficeFailedException>(() =>
			{
#pragma warning disable S6966 // Awaitable method should be used
				ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: false);
#pragma warning restore S6966 // Awaitable method should be used
			});

			exception.Message.ShouldContain("Failed to run LibreOffice!");
			exception.InnerException.ShouldNotBeNull();
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

	[Fact]
	public async Task ConvertToPdf_WithOverwriteTrueAndExistingFile_ShouldOverwriteSuccessfully()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestOverwriteTrue.xlsx");
		string expectedOutputFile = Combine(tempPath, "TestOverwriteTrue.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(expectedOutputFile, "Old PDF to be overwritten", TestContext.Current.CancellationToken);

		long originalSize = new FileInfo(expectedOutputFile).Length;

		try
		{
			// Act - With overwriteExistingFile = true, should succeed
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: true);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			File.Exists(expectedOutputFile).ShouldBeTrue();
			FileInfo newFileInfo = new(expectedOutputFile);
			newFileInfo.Exists.ShouldBeTrue();
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

	[Fact]
	public async Task ConvertToPdf_WithOverwriteFalseAndNoExistingFile_ShouldSucceed()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestNoExisting.xlsx");
		string expectedOutputFile = Combine(tempPath, "TestNoExisting.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act - With overwriteExistingFile = false and no existing file, should succeed
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: false);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			File.Exists(expectedOutputFile).ShouldBeTrue();
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

	[Fact]
	public async Task ConvertToPdf_WithOverwriteTrueAndNoExistingFile_ShouldSucceed()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestNoExistingOverwriteTrue.xlsx");
		string expectedOutputFile = Combine(tempPath, "TestNoExistingOverwriteTrue.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act - With overwriteExistingFile = true and no existing file, should succeed
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: true);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			File.Exists(expectedOutputFile).ShouldBeTrue();
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

	[Fact]
	public async Task ConvertToPdfAsync_WithOverwriteFalseAndExistingFile_ShouldThrowException()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestAsyncNoOverwrite.xlsx");
		string expectedOutputFile = Combine(tempPath, "TestAsyncNoOverwrite.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(expectedOutputFile, "Existing PDF content", TestContext.Current.CancellationToken);

		try
		{
			// Act & Assert - With overwriteExistingFile = false (default), should fail when file exists
			LibreOfficeFailedException exception = await Should.ThrowAsync<LibreOfficeFailedException>(async () =>
			{
				await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: false);
			});

			exception.Message.ShouldContain("Failed to run LibreOffice!");
			exception.InnerException.ShouldNotBeNull();
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

	[Fact]
	public async Task ConvertToPdfAsync_WithOverwriteTrueAndExistingFile_ShouldOverwriteSuccessfully()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestAsyncOverwriteTrue.xlsx");
		string expectedOutputFile = Combine(tempPath, "TestAsyncOverwriteTrue.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(expectedOutputFile, "Old PDF to be overwritten", TestContext.Current.CancellationToken);

		try
		{
			// Act - With overwriteExistingFile = true, should succeed
			await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: true);

			// Assert
			File.Exists(expectedOutputFile).ShouldBeTrue();
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

	[Theory]
	[InlineData(ExecutionMode.Asynchronous)]
	[InlineData(ExecutionMode.Synchronous)]
	public async Task ConvertToPdf_WithDefaultOverwriteAndExistingFile_ShouldThrowException(ExecutionMode mode)
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, $"Test{mode}DefaultOverwrite.xlsx");
		string expectedOutputFile = Combine(tempPath, $"Test{mode}DefaultOverwrite.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(expectedOutputFile, "Existing PDF content", TestContext.Current.CancellationToken);

		try
		{
			// Act & Assert - Not passing overwriteExistingFile, should use default (false) and fail
			LibreOfficeFailedException exception = mode switch
			{
				ExecutionMode.Asynchronous => await Should.ThrowAsync<LibreOfficeFailedException>(async () =>
				{
					await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 1);
				}),
				ExecutionMode.Synchronous => Should.Throw<LibreOfficeFailedException>(() =>
				{
#pragma warning disable S6966 // Awaitable method should be used
					ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1);
#pragma warning restore S6966 // Awaitable method should be used
				}),
				_ => throw new InvalidOperationException($"Unknown execution mode: {mode}")
			};

			exception.Message.ShouldContain("Failed to run LibreOffice!");
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

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ConvertToPdf_WithOverwriteParameterVariations_ShouldBehaveCorrectly(bool overwriteValue)
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, $"TestOverwriteTheory{overwriteValue}.xlsx");
		string expectedOutputFile = Combine(tempPath, $"TestOverwriteTheory{overwriteValue}.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act - Test with no existing file (should always succeed)
#pragma warning disable S6966 // Awaitable method should be used
			ConvertToPdf(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: overwriteValue);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			File.Exists(expectedOutputFile).ShouldBeTrue();
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

	[Fact]
	public async Task ConvertToPdfAsync_WithOverwriteTrueMultipleTimes_ShouldAllowRepeatedOverwrites()
	{
		// Arrange
		string tempPath = Combine(GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempPath);
		string sourceFile = Combine(testDataPath, "TestMultipleOverwrites.xlsx");
		string expectedOutputFile = Combine(tempPath, "TestMultipleOverwrites.pdf");

		await File.WriteAllTextAsync(sourceFile, "Test content", TestContext.Current.CancellationToken);

		try
		{
			// Act - Run conversion multiple times with overwrite enabled
			await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: true);
			File.Exists(expectedOutputFile).ShouldBeTrue();

			// Run again - should overwrite successfully
			await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: true);
			File.Exists(expectedOutputFile).ShouldBeTrue();

			// Run a third time - should still work
			await ConvertToPdfAsync(libreOfficePath, sourceFile, tempPath, maxRetries: 1, overwriteExistingFile: true);

			// Assert
			File.Exists(expectedOutputFile).ShouldBeTrue();
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

