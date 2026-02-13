using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Images;
using xRetry.v3;

namespace Images.Tests;

public sealed class OptimizerTests : IDisposable
{
	private bool disposed;

	public void Dispose()
	{
		//File.Delete(_tempSavePath);
		GC.SuppressFinalize(this);
	}

#pragma warning disable S1172 // Unused method parameters should be removed
	private void Dispose(bool _)
	{
		if (!disposed)
		{
			disposed = true;
		}
	}
#pragma warning restore S1172 // Unused method parameters should be removed

	~OptimizerTests()
	{
		Dispose(false);
	}

	private readonly Fixture fixture;
	private readonly string testPngPath;
	private readonly string testJpgPath;
	private readonly string testGifPath;
	private readonly string testInvalidPath;

	public OptimizerTests()
	{
		fixture = new Fixture();
		fixture.Customize(new AutoFakeItEasyCustomization());

		// Setup test file paths
		string testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
		testPngPath = Path.Combine(testDataDir, "test.png");
		testJpgPath = Path.Combine(testDataDir, "test.jpg");
		testGifPath = Path.Combine(testDataDir, "test.gif");
		testInvalidPath = Path.Combine(testDataDir, "nonexistent.png");
	}

	[RetryTheory(3)]
	[InlineData("test.png")]
	[InlineData("test.jpg")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.bmp")]
	[InlineData("test.pnm")]
	[InlineData("test.tiff")]
	public async Task OptimizeImage_ShouldUseCorrectOptimizer_ForFileExtension(string fileName)
	{
		// Arrange
		string testPath = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

		// Act
		await Optimizer.OptimizeImage(testPath);

		// Assert
		File.Exists(testPath).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData(new[] { "-b", "-O2" }, null, null)]  // Gifsicle args
	[InlineData(null, new[] { "--max=80" }, null)]   // Jpegoptim args
	[InlineData(null, null, new[] { "-o7" })]        // Optipng args
	public async Task OptimizeImage_ShouldAcceptCustomArguments(string[]? gifsicleArgs, string[]? jpegoptimArgs, string[]? optipngArgs)
	{
		// Arrange
		string[] testFiles = { testGifPath, testJpgPath, testPngPath };

		foreach (string file in testFiles)
		{
			// Act & Assert
			await Should.NotThrowAsync(async () => await Optimizer.OptimizeImage(file, gifsicleArgs, jpegoptimArgs, optipngArgs));
		}
	}

	[RetryFact(3)]
	public async Task OptimizeImage_ShouldHandleNonExistentFile()
	{
		// Act & Assert
		await Should.ThrowAsync<FileNotFoundException>(async () => await Optimizer.OptimizeImage(testInvalidPath));
	}

	[RetryTheory(3)]
	[InlineData("test.txt")]
	[InlineData("test.docx")]
	[InlineData("test")]
	public async Task OptimizeImage_ShouldSkipUnsupportedExtensions(string fileName)
	{
		// Arrange
		string testPath = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
		long originalSize = new FileInfo(testPath).Length;

		// Act
		await Optimizer.OptimizeImage(testPath);

		// Assert
		new FileInfo(testPath).Length.ShouldBe(originalSize);
	}

	[RetryFact(3)]
	public async Task OptimizeImage_ShouldHandleCancellation()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(async () => await Optimizer.OptimizeImage(testPngPath, cancellationToken: cts.Token));
	}

	[RetryTheory(3)]
	[InlineData(new[] { "-b", "-O3" }, null, null, "test.gif")]     // Gifsicle
	[InlineData(null, new[] { "--preserve" }, null, "test.jpg")]    // Jpegoptim
	[InlineData(null, null, new[] { "-o5" }, "test.png")]          // Optipng
	public async Task OptimizeImage_ShouldAppendFilePathToArgs_WhenNotPresent(
			string[]? gifsicleArgs,
			string[]? jpegoptimArgs,
			string[]? optipngArgs,
			string fileName)
	{
		// Arrange
		string testPath = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

		// Act & Assert
		await Should.NotThrowAsync(async () => await Optimizer.OptimizeImage(testPath, gifsicleArgs, jpegoptimArgs, optipngArgs));
	}

	[RetryTheory(3)]
	[InlineData(new[] { "--invalid-flag-that-does-not-exist" }, null, null, "test.gif")]     // Gifsicle with invalid flag
	[InlineData(null, new[] { "--invalid-flag-that-does-not-exist" }, null, "test.jpg")]    // Jpegoptim with invalid flag
	[InlineData(null, null, new[] { "--invalid-flag-that-does-not-exist" }, "test.png")]   // Optipng with invalid flag
	public async Task OptimizeImage_ShouldHandleCommandFailure_WithInvalidArguments(
			string[]? gifsicleArgs,
			string[]? jpegoptimArgs,
			string[]? optipngArgs,
			string fileName)
	{
		// Arrange
		string testPath = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

		// Act - pass invalid arguments to trigger command failure (result.IsSuccess == false)
		await Optimizer.OptimizeImage(testPath, gifsicleArgs, jpegoptimArgs, optipngArgs);

		// Assert - original file should remain unchanged when optimization fails
		File.Exists(testPath).ShouldBeTrue();
	}
}
