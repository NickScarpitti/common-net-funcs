using CommonNetFuncs.Images;
using Shouldly;
using SixLabors.ImageSharp;

namespace ImagesTests;

public sealed class Base64Tests : IDisposable
{
    private readonly string _testImagePath;
    private readonly string _tempSavePath;
    private const string TestBase64String = "iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAFUlEQVR42mNk+M9Qz0AEYBxVSF+FAAhKDveksOjmAAAAAElFTkSuQmCC";

    public Base64Tests()
    {
        _testImagePath = Path.Combine("TestData", "test.png");
        _tempSavePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
    }

    public void Dispose()
    {
        File.Delete(_tempSavePath);
    }

    [Fact]
    public void ConvertImageFileToBase64_WithValidPath_ReturnsBase64String()
    {
        // Act
        string? result = _testImagePath.ConvertImageFileToBase64();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        // Verify it's valid base64
        Span<byte> buffer = new byte[result.Length];
        Convert.TryFromBase64String(result, buffer, out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("nonexistent.png")]
    public void ConvertImageFileToBase64_WithInvalidPath_ReturnsNull(string invalidPath)
    {
        // Act
        string? result = invalidPath.ConvertImageFileToBase64();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertImageFileToBase64_WithCorruptedImage_ReturnsNull()
    {
        // Arrange
        string corruptedPath = Path.GetTempFileName();
        File.WriteAllText(corruptedPath, "Not an image");

        try
        {
            // Act
            string? result = corruptedPath.ConvertImageFileToBase64();

            // Assert
            result.ShouldBeNull();
        }
        finally
        {
            File.Delete(corruptedPath);
        }
    }

    [Fact]
    public void ConvertImageFileToBase64_WithMemoryStream_ReturnsBase64String()
    {
        // Arrange
        using MemoryStream ms = new MemoryStream(File.ReadAllBytes(_testImagePath));

        // Act
        string? result = ms.ConvertImageFileToBase64();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        // Verify it's valid base64
        Span<byte> buffer = new byte[result.Length];
        Convert.TryFromBase64String(result, buffer, out _).ShouldBeTrue();
    }

    [Fact]
    public void ConvertImageFileToBase64_WithEmptyMemoryStream_ReturnsNull()
    {
        // Arrange
        using MemoryStream ms = new MemoryStream();

        // Act
        string? result = ms.ConvertImageFileToBase64();

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData($"data:image/png;base64,{TestBase64String}", TestBase64String)]
    [InlineData($"base64{TestBase64String}", TestBase64String)]
    [InlineData(TestBase64String, TestBase64String)]
    public void CleanImageValue_WithValidInput_ReturnsCleanedValue(string input, string expected)
    {
        // Act
        string? result = input.CleanImageValue();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid")]
    [InlineData("base64")]
    public void CleanImageValue_WithInvalidInput_ReturnsNull(string? input)
    {
        // Act
        string? result = input.CleanImageValue();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ImageSaveToFile_WithValidBase64_SavesFileSuccessfully()
    {
        // Arrange
        string base64Image = _testImagePath.ConvertImageFileToBase64()!;

        // Act
        bool result = base64Image.ImageSaveToFile(_tempSavePath);

        // Assert
        result.ShouldBeTrue();
        File.Exists(_tempSavePath).ShouldBeTrue();
        using Image image = Image.Load(_tempSavePath);
        image.Width.ShouldBeGreaterThan(0);
        image.Height.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ImageSaveToFile_WithInvalidBase64_ReturnsFalse()
    {
        // Arrange
        string invalidBase64 = "invalid base64 string";

        // Act
        bool result = invalidBase64.ImageSaveToFile(_tempSavePath);

        // Assert
        result.ShouldBeFalse();
        File.Exists(_tempSavePath).ShouldBeFalse();
    }
}
