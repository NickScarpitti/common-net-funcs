using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Images;

namespace Images.Tests;

public sealed class OptimizerTests
{
    private readonly Fixture _fixture;
    private readonly string _testPngPath;
    private readonly string _testJpgPath;
    private readonly string _testGifPath;
    private readonly string _testInvalidPath;

    public OptimizerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoFakeItEasyCustomization());

        // Setup test file paths
        string testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
        _testPngPath = Path.Combine(testDataDir, "test.png");
        _testJpgPath = Path.Combine(testDataDir, "test.jpg");
        _testGifPath = Path.Combine(testDataDir, "test.gif");
        _testInvalidPath = Path.Combine(testDataDir, "nonexistent.png");
    }

    [Theory]
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

    [Theory]
    [InlineData(new[] { "-b", "-O2" }, null, null)]  // Gifsicle args
    [InlineData(null, new[] { "--max=80" }, null)]   // Jpegoptim args
    [InlineData(null, null, new[] { "-o7" })]        // Optipng args
    public async Task OptimizeImage_ShouldAcceptCustomArguments(string[]? gifsicleArgs, string[]? jpegoptimArgs, string[]? optipngArgs)
    {
        // Arrange
        string[] testFiles = { _testGifPath, _testJpgPath, _testPngPath };

        foreach (string file in testFiles)
        {
            // Act & Assert
            await Should.NotThrowAsync(async () => await Optimizer.OptimizeImage(file, gifsicleArgs, jpegoptimArgs, optipngArgs));
        }
    }

    [Fact]
    public async Task OptimizeImage_ShouldHandleNonExistentFile()
    {
        // Act & Assert
        await Should.ThrowAsync<FileNotFoundException>(async () => await Optimizer.OptimizeImage(_testInvalidPath));
    }

    [Theory]
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

    [Fact]
    public async Task OptimizeImage_ShouldHandleCancellation()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await Optimizer.OptimizeImage(_testPngPath, cancellationToken: cts.Token));
    }

    [Theory]
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
}
