using CommonNetFuncs.Images;
using SixLabors.ImageSharp;
using xRetry.v3;

namespace Images.Tests;

public sealed class Base64Tests : IDisposable
{
	private readonly string testImagePath;
	private readonly string tempSavePath;
	private const string TestBase64String = "iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAFUlEQVR42mNk+M9Qz0AEYBxVSF+FAAhKDveksOjmAAAAAElFTkSuQmCC";

	public Base64Tests()
	{
		testImagePath = Path.Combine("TestData", "test.png");
		tempSavePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
	}

	private bool disposed;

	public void Dispose()
	{
		File.Delete(tempSavePath);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				File.Delete(tempSavePath);
			}
			disposed = true;
		}
	}

	~Base64Tests()
	{
		Dispose(false);
	}

	[RetryFact(3)]
	public void ConvertImageFileToBase64_WithValidPath_ReturnsBase64String()
	{
		// Act
		string? result = testImagePath.ConvertImageFileToBase64();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeEmpty();
		// Verify it's valid base64
		Span<byte> buffer = new byte[result.Length];
		Convert.TryFromBase64String(result, buffer, out _).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("")]
	[InlineData("nonexistent.png")]
	public void ConvertImageFileToBase64_WithInvalidPath_ReturnsNull(string invalidPath)
	{
		// Act & Assert
		Should.Throw<FileNotFoundException>(invalidPath.ConvertImageFileToBase64);
	}

	[RetryFact(3)]
	public async Task ConvertImageFileToBase64_WithCorruptedImage_ReturnsNull()
	{
		// Arrange
		string corruptedPath = Path.GetTempFileName();
		await File.WriteAllTextAsync(corruptedPath, "Not an image");

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

	[RetryFact(3)]
	public void ConvertImageFileToBase64_WithMemoryStream_ReturnsBase64String()
	{
		// Arrange
		using MemoryStream ms = new(File.ReadAllBytes(testImagePath));

		// Act
		string? result = ms.ConvertImageFileToBase64();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeEmpty();
		// Verify it's valid base64
		Span<byte> buffer = new byte[result.Length];
		Convert.TryFromBase64String(result, buffer, out _).ShouldBeTrue();
	}

	[RetryFact(3)]
	public void ConvertImageFileToBase64_WithEmptyMemoryStream_ReturnsNull()
	{
		// Arrange
		using MemoryStream ms = new();

		// Act
		string? result = ms.ConvertImageFileToBase64();

		// Assert
		result.ShouldBeNull();
	}

	[RetryTheory(3)]
	[InlineData($"data:image/png;base64,{TestBase64String}", TestBase64String)]
	[InlineData($"base64{TestBase64String}", TestBase64String)]
	[InlineData(TestBase64String, TestBase64String)]
	public void ExtractBase64_WithValidInput_ReturnsCleanedValue(string input, string expected)
	{
		// Act
		string? result = input.ExtractBase64();

		// Assert
		result.ShouldBe(expected);
	}

	//[RetryFact(3)]
	//[InlineData($"data:image/png;base64,{TestBase64String}", TestBase64String)]
	//[InlineData($"base64{TestBase64String}", TestBase64String)]
	//[InlineData(TestBase64String, TestBase64String)]
	//public void CleanImageValue_WithValidInput_ReturnsCleanedValue(string input, string expected)
	//{
	//    // Act
	//    #pragma warning disable CS0618 // Type or member is obsolete
	//    string? result = input.CleanImageValue();
	//    #pragma warning restore CS0618 // Type or member is obsolete

	//    // Assert
	//    result.ShouldBe(expected);
	//}

	[RetryTheory(3)]
	[InlineData("https://example.com/image.png?v=123", "https://example.com/image.png")]
	[InlineData("https://example.com/image.png?V=456", "https://example.com/image.png")]
	[InlineData("https://example.com/image.png?v=abc&other=1", "https://example.com/image.png")]
	[InlineData("https://example.com/image.png", "https://example.com/image.png")]
	[InlineData("https://example.com/image.png?x=1", "https://example.com/image.png?x=1")]
	[InlineData("?v=123", "")]
	[InlineData("?V=123", "")]
	[InlineData("", "")]
	[InlineData("   ", "   ")]
	[InlineData(null, null)]
	public void RemoveImageVersionQuery_Should_Remove_Version_Parameter_Correctly(string? input, string? expected)
	{
		// Act
#pragma warning disable RCS1249 // Unnecessary null-forgiving operator
		string result = Base64.RemoveImageVersionQuery(input!);
#pragma warning restore RCS1249 // Unnecessary null-forgiving operator

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("invalid")]
	[InlineData("base64")]
	public void ExtractBase64_WithInvalidInput_ReturnsNull(string? input)
	{
		// Act
		string? result = input.ExtractBase64();

		// Assert
		result.ShouldBeNull();
	}

	//	[RetryTheory(3)]
	//	[InlineData(null)]
	//	[InlineData("")]
	//	[InlineData(" ")]
	//	[InlineData("invalid")]
	//	[InlineData("base64")]
	//	public void CleanImageValue_WithInvalidInput_ReturnsNull(string? input)
	//	{
	//		// Act
	//#pragma warning disable CS0618 // Type or member is obsolete
	//		string? result = input.CleanImageValue();
	//#pragma warning restore CS0618 // Type or member is obsolete

	//	// Assert
	//	result.ShouldBeNull();
	//    }

	[RetryFact(3)]
	public async Task ImageSaveToFile_WithValidBase64_SavesFileSuccessfully()
	{
		// Arrange
		string base64Image = testImagePath.ConvertImageFileToBase64()!;

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		bool result = base64Image.ImageSaveToFile(tempSavePath);
#pragma warning restore S6966 // Awaitable method should be used

		File.Exists(tempSavePath).ShouldBeTrue();
		result.ShouldBeTrue();
		File.Exists(tempSavePath).ShouldBeTrue();
		using Image image = await Image.LoadAsync(tempSavePath);

		image.Height.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ImageSaveToFile_WithInvalidBase64_ReturnsFalse()
	{
		// Arrange
		const string invalidBase64 = "invalid base64 string";

		// Act
		bool result = invalidBase64.ImageSaveToFile(tempSavePath);

		// Assert
		result.ShouldBeFalse();
		File.Exists(tempSavePath).ShouldBeFalse();
	}
}
