using System.Numerics;
using CommonNetFuncs.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using xRetry.v3;

namespace Images.Tests;

public sealed class ManipulationTests : IDisposable
{
	private bool disposed;

	public void Dispose()
	{
		//File.Delete(_tempSavePath);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				Thread.Sleep(2000);
			}
			disposed = true;
		}
	}

	~ManipulationTests()
	{
		Dispose(false);
	}

	private static readonly string TestDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");

	private static string GetTestImagePath(string fileName)
	{
		return Path.Combine(TestDataDir, fileName);
	}

	private static string GetTempFilePath(string extension = ".tmp")
	{
		return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
	}

	private static byte[] GetTestImageBytes(string fileName)
	{
		string path = GetTestImagePath(fileName);
		return File.ReadAllBytes(path);
	}

	private static MemoryStream GetTestImageStream(string fileName)
	{
		string path = GetTestImagePath(fileName);
		return new MemoryStream(File.ReadAllBytes(path));
	}

	private static readonly Action<IImageProcessingContext> InvertMutate = ctx => ctx.Invert();

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.tiff", 33, 33)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImage_FilePath_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			// Act
#pragma warning disable S6966 // Awaitable method should be used
			bool result = Manipulation.ResizeImage(inputPath, outputPath, width, height);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Width.ShouldBe(width);
			img.Height.ShouldBe(height);
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.tiff", 33, 33)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImage_Stream_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		await using Stream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		bool result = Manipulation.ResizeImage(input, output, width, height, new JpegEncoder());
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.tiff", 33, 33)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImage_Span_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		await using MemoryStream output = new();

		// Act
		bool result = Manipulation.ResizeImage(bytes, output, width, height, new JpegEncoder());

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQuality_FilePath_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act
#pragma warning disable S6966 // Awaitable method should be used
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, quality, null);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQuality_Stream_Succeeds(string fileName, int quality)
	{
		// Arrange
		await using Stream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		bool result = Manipulation.ReduceImageQuality(input, output, quality, null);
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQuality_Span_Succeeds(string fileName, int quality)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		await using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(bytes, output, quality, null);

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".bmp")]
	[InlineData("test.bmp", ".gif")]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.bmp", ".tiff")]
	[InlineData("test.gif", ".bmp")]
	[InlineData("test.gif", ".gif")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.gif", ".tiff")]
	[InlineData("test.jpeg", ".bmp")]
	[InlineData("test.jpeg", ".gif")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpeg", ".tiff")]
	[InlineData("test.jpg", ".bmp")]
	[InlineData("test.jpg", ".gif")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.jpg", ".tiff")]
	[InlineData("test.png", ".bmp")]
	[InlineData("test.png", ".gif")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	[InlineData("test.png", ".tiff")]
	[InlineData("test.tiff", ".bmp")]
	[InlineData("test.tiff", ".gif")]
	[InlineData("test.tiff", ".jpeg")]
	[InlineData("test.tiff", ".jpg")]
	[InlineData("test.tiff", ".png")]
	[InlineData("test.tiff", ".tiff")]
	public async Task ConvertImageFormat_FilePath_Succeeds(string fileName, string outExt)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(outExt);

		try
		{
			IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

			// Act
#pragma warning disable S6966 // Awaitable method should be used
			bool result = Manipulation.ConvertImageFormat(inputPath, outputPath, format);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Metadata.DecodedImageFormat.ShouldBe(format);
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".bmp")]
	[InlineData("test.bmp", ".gif")]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.bmp", ".tiff")]
	[InlineData("test.gif", ".bmp")]
	[InlineData("test.gif", ".gif")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.gif", ".tiff")]
	[InlineData("test.jpeg", ".bmp")]
	[InlineData("test.jpeg", ".gif")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpeg", ".tiff")]
	[InlineData("test.jpg", ".bmp")]
	[InlineData("test.jpg", ".gif")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.jpg", ".tiff")]
	[InlineData("test.png", ".bmp")]
	[InlineData("test.png", ".gif")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	[InlineData("test.png", ".tiff")]
	[InlineData("test.tiff", ".bmp")]
	[InlineData("test.tiff", ".gif")]
	[InlineData("test.tiff", ".jpeg")]
	[InlineData("test.tiff", ".jpg")]
	[InlineData("test.tiff", ".png")]
	[InlineData("test.tiff", ".tiff")]
	public async Task ConvertImageFormat_Stream_Succeeds(string fileName, string outExt)
	{
		// Arrange
		await using Stream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();
		IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		bool result = Manipulation.ConvertImageFormat(input, output, format);
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(format);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".bmp")]
	[InlineData("test.bmp", ".gif")]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.bmp", ".tiff")]
	[InlineData("test.gif", ".bmp")]
	[InlineData("test.gif", ".gif")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.gif", ".tiff")]
	[InlineData("test.jpeg", ".bmp")]
	[InlineData("test.jpeg", ".gif")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpeg", ".tiff")]
	[InlineData("test.jpg", ".bmp")]
	[InlineData("test.jpg", ".gif")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.jpg", ".tiff")]
	[InlineData("test.png", ".bmp")]
	[InlineData("test.png", ".gif")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	[InlineData("test.png", ".tiff")]
	[InlineData("test.tiff", ".bmp")]
	[InlineData("test.tiff", ".gif")]
	[InlineData("test.tiff", ".jpeg")]
	[InlineData("test.tiff", ".jpg")]
	[InlineData("test.tiff", ".png")]
	[InlineData("test.tiff", ".tiff")]
	public async Task ConvertImageFormat_Span_Succeeds(string fileName, string outExt)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		await using MemoryStream output = new();
		IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

		// Act
		bool result = Manipulation.ConvertImageFormat(bytes, output, format);

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(format);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	[InlineData("test.tiff")]
	public void TryDetectImageType_FilePath_Works(string fileName)
	{
		// Arrange
		string path = GetTestImagePath(fileName);

		// Act
		bool result = Manipulation.TryDetectImageType(path, out IImageFormat? format);

		// Assert
		result.ShouldBeTrue();
		format.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	[InlineData("test.tiff")]
	public void TryDetectImageType_Stream_Works(string fileName)
	{
		// Arrange
		using Stream stream = GetTestImageStream(fileName);

		// Act
		bool result = Manipulation.TryDetectImageType(stream, out IImageFormat? format);

		// Assert
		result.ShouldBeTrue();
		format.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	[InlineData("test.tiff")]
	public void TryDetectImageType_Span_Works(string fileName)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);

		// Act
		bool result = Manipulation.TryDetectImageType(bytes, out IImageFormat? format);

		// Assert
		result.ShouldBeTrue();
		format.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	[InlineData("test.tiff")]
	public void TryGetMetadata_FilePath_Works(string fileName)
	{
		// Arrange
		string path = GetTestImagePath(fileName);

		// Act
		bool result = Manipulation.TryGetMetadata(path, out ImageMetadata? metadata);

		// Assert
		result.ShouldBeTrue();
		metadata.ShouldNotBeNull();
		metadata!.HorizontalResolution.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	[InlineData("test.tiff")]
	public void TryGetMetadata_Stream_Works(string fileName)
	{
		// Arrange
		using Stream stream = GetTestImageStream(fileName);

		// Act
		bool result = Manipulation.TryGetMetadata(stream, out ImageMetadata? metadata);

		// Assert
		result.ShouldBeTrue();
		metadata.ShouldNotBeNull();
		metadata!.HorizontalResolution.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	[InlineData("test.tiff")]
	public void TryGetMetadata_Span_Works(string fileName)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);

		// Act
		bool result = Manipulation.TryGetMetadata(bytes, out ImageMetadata? metadata);

		// Assert
		result.ShouldBeTrue();
		metadata.ShouldNotBeNull();
		metadata!.HorizontalResolution.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", "BMP")]
	[InlineData("test.gif", "GIF")]
	[InlineData("test.jpeg", "JPEG")]
	[InlineData("test.jpg", "JPEG")]
	[InlineData("test.png", "PNG")]
	[InlineData("test.tiff", "TIFF")]
	public async Task TryDetectImageTypeAsync_FilePath_Works(string fileName, string expectedFormat)
	{
		// Arrange
		string path = GetTestImagePath(fileName);

		// Act
		IImageFormat? format = await Manipulation.TryDetectImageTypeAsync(path);

		// Assert
		format.ShouldNotBeNull();
		format.Name.ShouldBe(expectedFormat);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", "BMP")]
	[InlineData("test.gif", "GIF")]
	[InlineData("test.jpeg", "JPEG")]
	[InlineData("test.jpg", "JPEG")]
	[InlineData("test.png", "PNG")]
	[InlineData("test.tiff", "TIFF")]
	public async Task TryDetectImageTypeAsync_Stream_Works(string fileName, string expectedFormat)
	{
		// Arrange
		await using Stream stream = GetTestImageStream(fileName);

		// Act
		IImageFormat? format = await Manipulation.TryDetectImageTypeAsync(stream);

		// Assert
		format.ShouldNotBeNull();
		format.Name.ShouldBe(expectedFormat);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	[InlineData("test.tiff")]
	public async Task TryGetMetadataAsync_FilePath_Works(string fileName)
	{
		// Arrange
		string path = GetTestImagePath(fileName);

		// Act
		ImageMetadata? metadata = await Manipulation.TryGetMetadataAsync(path);

		// Assert
		metadata.ShouldNotBeNull();
		metadata!.HorizontalResolution.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	[InlineData("test.tiff")]
	public async Task TryGetMetadataAsync_Stream_Works(string fileName)
	{
		// Arrange
		await using Stream stream = GetTestImageStream(fileName);

		// Act
		ImageMetadata? metadata = await Manipulation.TryGetMetadataAsync(stream);

		// Assert
		metadata.ShouldNotBeNull();
		metadata!.HorizontalResolution.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".bmp")]
	[InlineData("test.bmp", ".gif")]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.bmp", ".tiff")]
	[InlineData("test.gif", ".bmp")]
	[InlineData("test.gif", ".gif")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.gif", ".tiff")]
	[InlineData("test.jpeg", ".bmp")]
	[InlineData("test.jpeg", ".gif")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpeg", ".tiff")]
	[InlineData("test.jpg", ".bmp")]
	[InlineData("test.jpg", ".gif")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.jpg", ".tiff")]
	[InlineData("test.png", ".bmp")]
	[InlineData("test.png", ".gif")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	[InlineData("test.png", ".tiff")]
	[InlineData("test.tiff", ".bmp")]
	[InlineData("test.tiff", ".gif")]
	[InlineData("test.tiff", ".jpeg")]
	[InlineData("test.tiff", ".jpg")]
	[InlineData("test.tiff", ".png")]
	[InlineData("test.tiff", ".tiff")]
	public async Task ConvertImageFormatAsync_FilePath_Succeeds(string fileName, string outExt)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(outExt);

		try
		{
			IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

			// Act
			bool result = await Manipulation.ConvertImageFormatAsync(inputPath, outputPath, format);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Metadata.DecodedImageFormat.ShouldBe(format);
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".bmp")]
	[InlineData("test.bmp", ".gif")]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.bmp", ".tiff")]
	[InlineData("test.gif", ".bmp")]
	[InlineData("test.gif", ".gif")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.gif", ".tiff")]
	[InlineData("test.jpeg", ".bmp")]
	[InlineData("test.jpeg", ".gif")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpeg", ".tiff")]
	[InlineData("test.jpg", ".bmp")]
	[InlineData("test.jpg", ".gif")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.jpg", ".tiff")]
	[InlineData("test.png", ".bmp")]
	[InlineData("test.png", ".gif")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	[InlineData("test.png", ".tiff")]
	[InlineData("test.tiff", ".bmp")]
	[InlineData("test.tiff", ".gif")]
	[InlineData("test.tiff", ".jpeg")]
	[InlineData("test.tiff", ".jpg")]
	[InlineData("test.tiff", ".png")]
	[InlineData("test.tiff", ".tiff")]
	public async Task ConvertImageFormatAsync_Stream_Succeeds(string fileName, string outExt)
	{
		// Arrange
		await using Stream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();
		IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

		// Act
		bool result = await Manipulation.ConvertImageFormatAsync(input, output, format);

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(format);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 0, 0)]
	[InlineData("test.png", -1, -1)]
	[InlineData("test.gif", -100, -100)]
	public void ResizeImage_InvalidParams_Throws(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");

		// Act
		bool result = Manipulation.ResizeImageBase(inputPath, outputPath, null, width, height, null, null, false, null);

		//Assert
		result.ShouldBeFalse();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 0)]
	[InlineData("test.png", 101)]
	[InlineData("test.tiff", -1)]
	[InlineData("test.gif", -100)]
	public void ReduceImageQuality_InvalidQuality_Throws(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");

		// Act & Assert
		Should.Throw<ArgumentException>(() => Manipulation.ReduceImageQualityBase(inputPath, outputPath, quality, null, null, null, null, null, null, false, null));
	}

	[RetryFact(3)]
	public void TryDetectImageType_FilePath_TooShort_ReturnsFalse()
	{
		// Act
		bool result = Manipulation.TryDetectImageType("a", out IImageFormat? format);

		// Assert
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryDetectImageType_Stream_TooShort_ReturnsFalse()
	{
		// Arrange
		using MemoryStream stream = new(new byte[2]);

		// Act
		bool result = Manipulation.TryDetectImageType(stream, out IImageFormat? format);

		// Assert
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryDetectImageType_Span_TooShort_ReturnsFalse()
	{
		// Arrange
		byte[] data = new byte[2];

		// Act
		bool result = Manipulation.TryDetectImageType(data, out IImageFormat? format);

		// Assert
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_FilePath_TooShort_ReturnsFalse()
	{
		// Act
		bool result = Manipulation.TryGetMetadata("a", out ImageMetadata? metadata);

		// Assert
		result.ShouldBeFalse();
		metadata.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_Stream_TooShort_ReturnsFalse()
	{
		// Arrange
		using MemoryStream stream = new(new byte[2]);

		// Act
		bool result = Manipulation.TryGetMetadata(stream, out ImageMetadata? metadata);

		// Assert
		result.ShouldBeFalse();
		metadata.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_Span_TooShort_ReturnsFalse()
	{
		// Arrange
		byte[] data = new byte[2];

		// Act
		bool result = Manipulation.TryGetMetadata(data, out ImageMetadata? metadata);

		// Assert
		result.ShouldBeFalse();
		metadata.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 120, 80)]
	[InlineData("test.jpeg", 75, 40)]
	[InlineData("test.png", 50, 25)]
	[InlineData("test.gif", 25, 13)]
	[InlineData("test.tiff", 33, 15)]
	[InlineData("test.bmp", 10, 5)]
	public async Task ResizeImage_FilePath_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(Path.GetExtension(fileName));
		ResizeOptions options = new()
		{
			Size = new Size(width, height),
			Mode = ResizeMode.Max
		};

		try
		{
			// Act
#pragma warning disable S6966 // Awaitable method should be used
			bool result = Manipulation.ResizeImage(inputPath, outputPath, options);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Width.ShouldBeLessThanOrEqualTo(width);
			img.Height.ShouldBeLessThanOrEqualTo(height);
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 120, 80)]
	[InlineData("test.jpeg", 75, 40)]
	[InlineData("test.png", 50, 25)]
	[InlineData("test.gif", 25, 13)]
	[InlineData("test.tiff", 33, 15)]
	[InlineData("test.bmp", 10, 5)]
	public async Task ResizeImage_Stream_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		await using Stream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();
		ResizeOptions options = new()
		{
			Size = new Size(width, height),
			Mode = ResizeMode.Max
		};

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		bool result = Manipulation.ResizeImage(input, output, options, new JpegEncoder());
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		using Image img = await Image.LoadAsync(output);
		img.Width.ShouldBeLessThanOrEqualTo(width);
		img.Height.ShouldBeLessThanOrEqualTo(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 120, 80)]
	[InlineData("test.jpeg", 75, 40)]
	[InlineData("test.png", 50, 25)]
	[InlineData("test.gif", 25, 13)]
	[InlineData("test.tiff", 33, 15)]
	[InlineData("test.bmp", 10, 5)]
	public async Task ResizeImage_Span_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		await using MemoryStream output = new();
		ResizeOptions options = new()
		{
			Size = new Size(width, height),
			Mode = ResizeMode.Max
		};

		// Act
		bool result = Manipulation.ResizeImage(bytes, output, options, new JpegEncoder());

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		using Image img = await Image.LoadAsync(output);
		img.Width.ShouldBeLessThanOrEqualTo(width);
		img.Height.ShouldBeLessThanOrEqualTo(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQuality_FilePath_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".png");

		try
		{
			// Act
#pragma warning disable S6966 // Awaitable method should be used
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, PngFormat.Instance, quality, null);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQuality_Stream_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		await using Stream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		bool result = Manipulation.ReduceImageQuality(input, output, PngFormat.Instance, quality, null);
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQuality_Span_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		await using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(bytes, output, PngFormat.Instance, quality, null);

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQualityAsync_FilePath_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".png");

		try
		{
			// Act
			bool result = await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, PngFormat.Instance, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQualityAsync_Stream_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		await using Stream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();

		// Act
		bool result = await Manipulation.ReduceImageQualityAsync(input, output, PngFormat.Instance, quality, null, null);

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 120, 80)]
	[InlineData("test.jpeg", 75, 40)]
	[InlineData("test.png", 50, 25)]
	[InlineData("test.gif", 25, 13)]
	[InlineData("test.tiff", 33, 15)]
	[InlineData("test.bmp", 10, 5)]
	public async Task ResizeImageAsync_FilePath_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(Path.GetExtension(fileName));

		ResizeOptions options = new()
		{
			Size = new Size(width, height),
			Mode = ResizeMode.Max
		};

		try
		{
			// Act
			bool result = await Manipulation.ResizeImageAsync(inputPath, outputPath, options);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Width.ShouldBeLessThanOrEqualTo(width);
			img.Height.ShouldBeLessThanOrEqualTo(height);
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 120, 80)]
	[InlineData("test.jpeg", 75, 40)]
	[InlineData("test.png", 50, 25)]
	[InlineData("test.gif", 25, 13)]
	[InlineData("test.tiff", 33, 15)]
	[InlineData("test.bmp", 10, 5)]
	public async Task ResizeImageAsync_Stream_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		await using Stream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();
		ResizeOptions options = new()
		{
			Size = new Size(width, height),
			Mode = ResizeMode.Max
		};

		// Act
		bool result = await Manipulation.ResizeImageAsync(input, output, options, new JpegEncoder());

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		using Image img = await Image.LoadAsync(output);
		img.Width.ShouldBeLessThanOrEqualTo(width);
		img.Height.ShouldBeLessThanOrEqualTo(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.tiff", 33, 33)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImage_FilePath_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act
#pragma warning disable S6966 // Awaitable method should be used
			bool result = Manipulation.ResizeImage(inputPath, outputPath, width, height, mutate: InvertMutate);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Width.ShouldBe(width);
			img.Height.ShouldBe(height);

			File.Delete(outputPath);

			// Now check if the image is inverted
#pragma warning disable S6966 // Awaitable method should be used
			Manipulation.ResizeImage(inputPath, outputPath, width, height);
#pragma warning restore S6966 // Awaitable method should be used

			using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();
			// Spot check: pixel [0,0] should be inverted from original
			using Image<Rgb24> orig = await Image.LoadAsync<Rgb24>(outputPath);
			bool isInverted = IsInvertedVersion(orig, imgClone);

			isInverted.ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.tiff", 33, 33)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImage_Stream_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		await using MemoryStream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();
		await using MemoryStream nonInvertedOutput = new();

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		bool result = Manipulation.ResizeImage(input, output, width, height, new JpegEncoder(), mutate: InvertMutate);
		Manipulation.ResizeImage(input, nonInvertedOutput, width, height, new JpegEncoder());
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);

		using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();
		using Image<Rgb24> orig = await Image.LoadAsync<Rgb24>(nonInvertedOutput);
		bool isInverted = IsInvertedVersion(orig, imgClone);

		isInverted.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.tiff", 33, 33)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImage_Span_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		await using MemoryStream output = new();
		await using MemoryStream nonInvertedOutput = new();

		// Act
		bool result = Manipulation.ResizeImage(bytes, output, width, height, new JpegEncoder(), mutate: InvertMutate);
		Manipulation.ResizeImage(bytes, nonInvertedOutput, width, height, new JpegEncoder());

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);

		using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();
		using Image<Rgb24> orig = await Image.LoadAsync<Rgb24>(nonInvertedOutput);
		bool isInverted = IsInvertedVersion(orig, imgClone);

		isInverted.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQuality_FilePath_Mutate_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act
#pragma warning disable S6966 // Awaitable method should be used
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, quality, null, mutate: InvertMutate);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

			using Image orig = await Image.LoadAsync(inputPath);
			using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();
			using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
			bool isInverted = IsInvertedVersion(origClone, imgClone);
			isInverted.ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQuality_Stream_Mutate_Succeeds(string fileName, int quality)
	{
		// Arrange
		await using MemoryStream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		bool result = Manipulation.ReduceImageQuality(input, output, quality, null, mutate: InvertMutate);
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

		using Image orig = await Image.LoadAsync(input);
		using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();
		using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
		bool isInverted = IsInvertedVersion(origClone, imgClone);
		isInverted.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQuality_Span_Mutate_Succeeds(string fileName, int quality)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		await using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(bytes, output, quality, null, mutate: InvertMutate);

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

		using Image orig = Image.Load(bytes);
		using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();
		using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
		bool isInverted = IsInvertedVersion(origClone, imgClone);
		isInverted.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".bmp")]
	[InlineData("test.bmp", ".gif")]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.bmp", ".tiff")]
	[InlineData("test.gif", ".bmp")]
	[InlineData("test.gif", ".gif")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.gif", ".tiff")]
	[InlineData("test.jpeg", ".bmp")]
	[InlineData("test.jpeg", ".gif")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpeg", ".tiff")]
	[InlineData("test.jpg", ".bmp")]
	[InlineData("test.jpg", ".gif")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.jpg", ".tiff")]
	[InlineData("test.png", ".bmp")]
	[InlineData("test.png", ".gif")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	[InlineData("test.png", ".tiff")]
	[InlineData("test.tiff", ".bmp")]
	[InlineData("test.tiff", ".gif")]
	[InlineData("test.tiff", ".jpeg")]
	[InlineData("test.tiff", ".jpg")]
	[InlineData("test.tiff", ".png")]
	[InlineData("test.tiff", ".tiff")]
	public async Task ConvertImageFormatAsync_FilePath_Mutate_Succeeds(string fileName, string outExt)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(outExt);
		string invertedOutputPath = GetTempFilePath(outExt);

		try
		{
			IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

			// Act
			bool result = await Manipulation.ConvertImageFormatAsync(inputPath, invertedOutputPath, format, mutate: InvertMutate);

			// Assert
			result.ShouldBeTrue();
			File.Exists(invertedOutputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(invertedOutputPath);
			img.Metadata.DecodedImageFormat.ShouldBe(format);

			File.Delete(outputPath);

			// Now check if the image is inverted
			await Manipulation.ConvertImageFormatAsync(inputPath, outputPath, format);

			using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();
			// Spot check: pixel [0,0] should be inverted from original
			using Image<Rgb24> orig = await Image.LoadAsync<Rgb24>(outputPath);
			bool isInverted = IsInvertedVersion(orig, imgClone);

			isInverted.ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".bmp")]
	[InlineData("test.bmp", ".gif")]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.bmp", ".tiff")]
	[InlineData("test.gif", ".bmp")]
	[InlineData("test.gif", ".gif")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.gif", ".tiff")]
	[InlineData("test.jpeg", ".bmp")]
	[InlineData("test.jpeg", ".gif")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpeg", ".tiff")]
	[InlineData("test.jpg", ".bmp")]
	[InlineData("test.jpg", ".gif")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.jpg", ".tiff")]
	[InlineData("test.png", ".bmp")]
	[InlineData("test.png", ".gif")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	[InlineData("test.png", ".tiff")]
	[InlineData("test.tiff", ".bmp")]
	[InlineData("test.tiff", ".gif")]
	[InlineData("test.tiff", ".jpeg")]
	[InlineData("test.tiff", ".jpg")]
	[InlineData("test.tiff", ".png")]
	[InlineData("test.tiff", ".tiff")]
	public async Task ConvertImageFormatAsync_Stream_Mutate_Succeeds(string fileName, string outExt)
	{
		// Arrange
		await using MemoryStream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();
		IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

		// Act
		bool result = await Manipulation.ConvertImageFormatAsync(input, output, format, mutate: InvertMutate);

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(format);

		using Image orig = await Image.LoadAsync(input);
		using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();
		using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
		bool isInverted = IsInvertedVersion(origClone, imgClone);
		isInverted.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.tiff", 33, 33)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImageAsync_FilePath_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act
			bool result = await Manipulation.ResizeImageAsync(inputPath, outputPath, width, height, mutate: InvertMutate);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Width.ShouldBe(width);
			img.Height.ShouldBe(height);

			File.Delete(outputPath);

			// Now check if the image is inverted
#pragma warning disable S6966 // Awaitable method should be used
			Manipulation.ResizeImage(inputPath, outputPath, width, height);
#pragma warning restore S6966 // Awaitable method should be used

			using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();
			// Spot check: pixel [0,0] should be inverted from original
			using Image<Rgb24> orig = await Image.LoadAsync<Rgb24>(outputPath);
			bool isInverted = IsInvertedVersion(orig, imgClone);

			isInverted.ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.tiff", 33, 33)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImageAsync_Stream_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		await using MemoryStream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();
		await using MemoryStream nonInvertedOutput = new();

		// Act
		bool result = await Manipulation.ResizeImageAsync(input, output, width, height, new JpegEncoder(), mutate: InvertMutate);
#pragma warning disable S6966 // Awaitable method should be used
		Manipulation.ResizeImage(input, nonInvertedOutput, width, height, new JpegEncoder());
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);

		using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();
		using Image<Rgb24> orig = await Image.LoadAsync<Rgb24>(nonInvertedOutput);
		bool isInverted = IsInvertedVersion(orig, imgClone);

		isInverted.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQualityAsync_FilePath_Mutate_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");
		try
		{
			// Act
			bool result = await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, quality, null, mutate: InvertMutate);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);
			img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

			using Image orig = await Image.LoadAsync(inputPath);
			using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();
			using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
			bool isInverted = IsInvertedVersion(origClone, imgClone);
			isInverted.ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.tiff", 33)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQualityAsync_Stream_Mutate_Succeeds(string fileName, int quality)
	{
		// Arrange
		await using MemoryStream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();

		// Act
		bool result = await Manipulation.ReduceImageQualityAsync(input, output, quality, null, mutate: InvertMutate);

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);
		img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

		using Image orig = await Image.LoadAsync(input);
		using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();
		using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
		bool isInverted = IsInvertedVersion(origClone, imgClone);
		isInverted.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100, false, false)]
	[InlineData("test.jpg", 100, 100, true, false)]
	[InlineData("test.png", 50, 25, false, false)]
	[InlineData("test.png", 50, 25, true, false)]
	[InlineData("test.png", -1, -1, true, false)]
	[InlineData("test.jpg", 100, 100, false, true)]
	[InlineData("test.jpg", 100, 100, true, true)]
	[InlineData("test.png", 50, 25, false, true)]
	[InlineData("test.png", 50, 25, true, true)]
	[InlineData("test.png", -1, -1, true, true)]
	public async Task ResizeImage_FilePath_UseDimsAsMax_Works(string fileName, int width, int height, bool useDimsAsMax, bool useResizeOptions)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jpg");

		try
		{
			if (width < 0 && height < 0)
			{
				using Image originalImg = await Image.LoadAsync(inputPath);
				width = originalImg.Width;
				height = originalImg.Height;
				originalImg.Dispose();
			}

			// Act
#pragma warning disable S6966 // Awaitable method should be used
			bool result = !useResizeOptions ? Manipulation.ResizeImage(inputPath, outputPath, width, height, useDimsAsMax: useDimsAsMax)
					: Manipulation.ResizeImage(inputPath, outputPath, new() { Size = new(width, height) }, useDimsAsMax: useDimsAsMax);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);

			if (useDimsAsMax)
			{
				img.Width.ShouldBeLessThanOrEqualTo(width);
				img.Height.ShouldBeLessThanOrEqualTo(height);
			}
			else
			{
				img.Width.ShouldBe(width);
				img.Height.ShouldBe(height);
			}
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100, false, false)]
	[InlineData("test.jpg", 100, 100, true, false)]
	[InlineData("test.png", 50, 25, false, false)]
	[InlineData("test.png", 50, 25, true, false)]
	[InlineData("test.png", -1, -1, true, false)]
	[InlineData("test.jpg", 100, 100, false, true)]
	[InlineData("test.jpg", 100, 100, true, true)]
	[InlineData("test.png", 50, 25, false, true)]
	[InlineData("test.png", 50, 25, true, true)]
	[InlineData("test.png", -1, -1, true, true)]
	public async Task ResizeImage_Stream_UseDimsAsMax_Works(string fileName, int width, int height, bool useDimsAsMax, bool useResizeOptions)
	{
		// Arrange
		await using MemoryStream input = GetTestImageStream(fileName);
		await using MemoryStream output = new();

		if (width < 0 && height < 0)
		{
			using Image originalImg = await Image.LoadAsync(input);
			width = originalImg.Width;
			height = originalImg.Height;
			originalImg.Dispose();
			input.Position = 0; // Reset stream position after loading
		}

		// Act
#pragma warning disable S6966 // Awaitable method should be used
		bool result = !useResizeOptions ? Manipulation.ResizeImage(input, output, width, height, new JpegEncoder(), useDimsAsMax: useDimsAsMax) :
				Manipulation.ResizeImage(input, output, new() { Size = new(width, height) }, new JpegEncoder(), useDimsAsMax: useDimsAsMax);
#pragma warning restore S6966 // Awaitable method should be used

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);

		if (useDimsAsMax)
		{
			img.Width.ShouldBeLessThanOrEqualTo(width);
			img.Height.ShouldBeLessThanOrEqualTo(height);
		}
		else
		{
			img.Width.ShouldBe(width);
			img.Height.ShouldBe(height);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100, false, false)]
	[InlineData("test.jpg", 100, 100, true, false)]
	[InlineData("test.png", 50, 25, false, false)]
	[InlineData("test.png", 50, 25, true, false)]
	[InlineData("test.png", -1, -1, true, false)]
	[InlineData("test.jpg", 100, 100, false, true)]
	[InlineData("test.jpg", 100, 100, true, true)]
	[InlineData("test.png", 50, 25, false, true)]
	[InlineData("test.png", 50, 25, true, true)]
	[InlineData("test.png", -1, -1, true, true)]
	public async Task ResizeImage_Span_UseDimsAsMax_Works(string fileName, int width, int height, bool useDimsAsMax, bool useResizeOptions)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		await using MemoryStream output = new();

		if (width < 0 && height < 0)
		{
			using Image originalImg = Image.Load(bytes);
			width = originalImg.Width;
			height = originalImg.Height;
			originalImg.Dispose();
		}

		// Act
		bool result = !useResizeOptions ? Manipulation.ResizeImage(bytes, output, width, height, new JpegEncoder(), useDimsAsMax: useDimsAsMax) :
				Manipulation.ResizeImage(bytes, output, new() { Size = new(width, height) }, new JpegEncoder(), useDimsAsMax: useDimsAsMax);

		// Assert
		result.ShouldBeTrue();
		output.Position = 0;
		using Image img = await Image.LoadAsync(output);

		if (useDimsAsMax)
		{
			img.Width.ShouldBeLessThanOrEqualTo(width);
			img.Height.ShouldBeLessThanOrEqualTo(height);
		}
		else
		{
			img.Width.ShouldBe(width);
			img.Height.ShouldBe(height);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100, false, false)]
	[InlineData("test.jpg", 100, 100, true, false)]
	[InlineData("test.png", 50, 25, false, false)]
	[InlineData("test.png", 50, 25, true, false)]
	[InlineData("test.png", -1, -1, true, false)]
	[InlineData("test.jpg", 100, 100, false, true)]
	[InlineData("test.jpg", 100, 100, true, true)]
	[InlineData("test.png", 50, 25, false, true)]
	[InlineData("test.png", 50, 25, true, true)]
	[InlineData("test.png", -1, -1, true, true)]
	public async Task ResizeImageAsync_FilePath_UseDimsAsMax_Works(string fileName, int width, int height, bool useDimsAsMax, bool useResizeOptions)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jpg");

		try
		{
			if (width < 0 && height < 0)
			{
				using Image originalImg = await Image.LoadAsync(inputPath);
				width = originalImg.Width;
				height = originalImg.Height;
				originalImg.Dispose();
			}

			// Act
			bool result = !useResizeOptions ? await Manipulation.ResizeImageAsync(inputPath, outputPath, width, height, useDimsAsMax: useDimsAsMax) :
					await Manipulation.ResizeImageAsync(inputPath, outputPath, new() { Size = new(width, height) }, useDimsAsMax: useDimsAsMax);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using Image img = await Image.LoadAsync(outputPath);

			if (useDimsAsMax)
			{
				img.Width.ShouldBeLessThanOrEqualTo(width);
				img.Height.ShouldBeLessThanOrEqualTo(height);
			}
			else
			{
				img.Width.ShouldBe(width);
				img.Height.ShouldBe(height);
			}
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	public static bool IsInvertedVersion<TPixel>(Image<TPixel> original, Image<TPixel> potentialInverted, float tolerance = 1f) where TPixel : unmanaged, IPixel<TPixel>
	{
		// Check dimensions first
		if (original.Width != potentialInverted.Width || original.Height != potentialInverted.Height)
		{
			return false;
		}

		bool isInverted = true;
		object lockObj = new();

		// Process both images simultaneously
		original.ProcessPixelRows(potentialInverted, (originalAccessor, invertedAccessor) =>
		{
			for (int y = 0; y < originalAccessor.Height && isInverted; y++)
			{
				// Get spans for the current row in both images
				Span<TPixel> originalRow = originalAccessor.GetRowSpan(y);
				Span<TPixel> invertedRow = invertedAccessor.GetRowSpan(y);

				for (int x = 0; x < originalRow.Length && isInverted; x++)
				{
					// Get color values as vectors
					Vector4 originalVector = originalRow[x].ToVector4();
					Vector4 invertedVector = invertedRow[x].ToVector4();

					// Check if RGB components are inverted (within tolerance)
					// Note: Alpha channel should remain unchanged in inversion
					if (Math.Abs(1 - originalVector.X - invertedVector.X) > tolerance ||
									Math.Abs(1 - originalVector.Y - invertedVector.Y) > tolerance ||
									Math.Abs(1 - originalVector.Z - invertedVector.Z) > tolerance)
					{
						lock (lockObj)
						{
							isInverted = false;
						}
						break;
					}

					// Optionally check if alpha is preserved
					if (Math.Abs(originalVector.W - invertedVector.W) > tolerance)
					{
						lock (lockObj)
						{
							isInverted = false;
						}
						break;
					}
				}
			}
		});

		return isInverted;
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 75)]
	[InlineData("test.jpeg", 50)]
	[InlineData("test.png", 60)]
	[InlineData("test.gif", 80)]
	[InlineData("test.tiff", 70)]
	[InlineData("test.bmp", 65)]
	public async Task ReduceImageQuality_SameFilePath_Jpeg_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string testFilePath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			// Copy the test file to a temporary location since we'll be modifying it
			File.Copy(inputPath, testFilePath, true);

			// Get original file info
			using Image originalImage = await Image.LoadAsync(testFilePath);
			int originalWidth = originalImage.Width;
			int originalHeight = originalImage.Height;

			// Act - Use same path for input and output
#pragma warning disable S6966 // Awaitable method should be used
			bool result = Manipulation.ReduceImageQuality(testFilePath, testFilePath, quality, null);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(testFilePath).ShouldBeTrue();

			// Verify the file was modified and is still a valid JPEG
			using Image img = await Image.LoadAsync(testFilePath);
			img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);
			img.Width.ShouldBe(originalWidth);
			img.Height.ShouldBe(originalHeight);

			// File should exist and typically be smaller (though not guaranteed with high quality)
			File.Exists(testFilePath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(testFilePath))
			{
				File.Delete(testFilePath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 75)]
	[InlineData("test.jpeg", 50)]
	[InlineData("test.png", 60)]
	[InlineData("test.gif", 80)]
	[InlineData("test.tiff", 70)]
	[InlineData("test.bmp", 65)]
	public async Task ReduceImageQuality_SameFilePath_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string testFilePath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			// Copy the test file to a temporary location since we'll be modifying it
			File.Copy(inputPath, testFilePath, true);

			// Get original file info
			using Image originalImage = await Image.LoadAsync(testFilePath);
			int originalWidth = originalImage.Width;
			int originalHeight = originalImage.Height;

			// Act - Use same path for input and output, converting to PNG
#pragma warning disable S6966 // Awaitable method should be used
			bool result = Manipulation.ReduceImageQuality(testFilePath, testFilePath, PngFormat.Instance, quality, null);
#pragma warning restore S6966 // Awaitable method should be used

			// Assert
			result.ShouldBeTrue();
			File.Exists(testFilePath).ShouldBeTrue();

			// Verify the file was converted to PNG
			using Image img = await Image.LoadAsync(testFilePath);
			img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
			img.Width.ShouldBe(originalWidth);
			img.Height.ShouldBe(originalHeight);
		}
		finally
		{
			if (File.Exists(testFilePath))
			{
				File.Delete(testFilePath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 75)]
	[InlineData("test.jpeg", 50)]
	[InlineData("test.png", 60)]
	[InlineData("test.gif", 80)]
	[InlineData("test.tiff", 70)]
	[InlineData("test.bmp", 65)]
	public async Task ReduceImageQualityAsync_SameFilePath_Jpeg_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string testFilePath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			// Copy the test file to a temporary location since we'll be modifying it
			File.Copy(inputPath, testFilePath, true);

			// Get original file info
			using Image originalImage = await Image.LoadAsync(testFilePath);
			int originalWidth = originalImage.Width;
			int originalHeight = originalImage.Height;

			// Act - Use same path for input and output
			bool result = await Manipulation.ReduceImageQualityAsync(testFilePath, testFilePath, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(testFilePath).ShouldBeTrue();

			// Verify the file was modified and is still a valid JPEG
			using Image img = await Image.LoadAsync(testFilePath);
			img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);
			img.Width.ShouldBe(originalWidth);
			img.Height.ShouldBe(originalHeight);

			// File should exist and typically be smaller (though not guaranteed with high quality)
			File.Exists(testFilePath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(testFilePath))
			{
				File.Delete(testFilePath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 75)]
	[InlineData("test.jpeg", 50)]
	[InlineData("test.png", 60)]
	[InlineData("test.gif", 80)]
	[InlineData("test.tiff", 70)]
	[InlineData("test.bmp", 65)]
	public async Task ReduceImageQualityAsync_SameFilePath_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string testFilePath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			// Copy the test file to a temporary location since we'll be modifying it
			File.Copy(inputPath, testFilePath, true);

			// Get original file info
			using Image originalImage = await Image.LoadAsync(testFilePath);
			int originalWidth = originalImage.Width;
			int originalHeight = originalImage.Height;

			// Act - Use same path for input and output, converting to PNG
			bool result = await Manipulation.ReduceImageQualityAsync(testFilePath, testFilePath, PngFormat.Instance, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(testFilePath).ShouldBeTrue();

			// Verify the file was converted to PNG
			using Image img = await Image.LoadAsync(testFilePath);
			img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
			img.Width.ShouldBe(originalWidth);
			img.Height.ShouldBe(originalHeight);
		}
		finally
		{
			if (File.Exists(testFilePath))
			{
				File.Delete(testFilePath);
			}
		}
	}

	// ===== New Tests for Coverage Gaps =====

	[RetryTheory(3)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(101)]
	[InlineData(200)]
	public void ReduceImageQuality_WithInvalidQuality_ThrowsArgumentException(int invalidQuality)
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act & Assert
			Should.Throw<ArgumentException>(() =>
				Manipulation.ReduceImageQuality(inputPath, outputPath, invalidQuality, -1, -1));
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(101)]
	[InlineData(200)]
	public void ReduceImageQuality_Stream_WithInvalidQuality_ThrowsArgumentException(int invalidQuality)
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		using MemoryStream inputStream = new(File.ReadAllBytes(inputPath));
		using MemoryStream outputStream = new();

		// Act & Assert
		Should.Throw<ArgumentException>(() =>
			Manipulation.ReduceImageQuality(inputStream, outputStream, invalidQuality, -1, -1));
	}

	[RetryTheory(3)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(101)]
	[InlineData(200)]
	public void ReduceImageQuality_Span_WithInvalidQuality_ThrowsArgumentException(int invalidQuality)
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		byte[] inputBytes = File.ReadAllBytes(inputPath);
		using MemoryStream outputStream = new();

		// Act & Assert
		Should.Throw<ArgumentException>(() =>
			Manipulation.ReduceImageQuality(new ReadOnlySpan<byte>(inputBytes), outputStream, invalidQuality, -1, -1));
	}

	[RetryTheory(3)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(101)]
	[InlineData(200)]
	public async Task ReduceImageQualityAsync_WithInvalidQuality_ThrowsArgumentException(int invalidQuality)
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act & Assert
			await Should.ThrowAsync<ArgumentException>(async () =>
				await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, invalidQuality, -1, -1));
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryTheory(3)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(101)]
	[InlineData(200)]
	public async Task ReduceImageQualityAsync_Stream_WithInvalidQuality_ThrowsArgumentException(int invalidQuality)
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		await using MemoryStream inputStream = new(await File.ReadAllBytesAsync(inputPath));
		await using MemoryStream outputStream = new();

		// Act & Assert
		await Should.ThrowAsync<ArgumentException>(async () =>
			await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, invalidQuality, -1, -1));
	}

	[RetryFact(3)]
	public void ResizeImage_Stream_WithoutEncoderOrFormat_ReturnsFalse()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		using MemoryStream inputStream = new(File.ReadAllBytes(inputPath));
		using MemoryStream outputStream = new();

		// Act - This returns false because neither encoder nor format is provided
		bool result = Manipulation.ResizeImageBase(inputStream, outputStream, null, 100, 100, null, null, null, false, null);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ResizeImage_Span_WithoutEncoderOrFormat_ReturnsFalse()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		byte[] inputBytes = File.ReadAllBytes(inputPath);
		using MemoryStream outputStream = new();

		// Act - This returns false because neither encoder nor format is provided
		bool result = Manipulation.ResizeImageBase(new ReadOnlySpan<byte>(inputBytes), outputStream, null, 100, 100, null, null, null, false, null);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_Stream_WithoutEncoderOrFormat_ReturnsFalse()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		await using MemoryStream inputStream = new(await File.ReadAllBytesAsync(inputPath));
		await using MemoryStream outputStream = new();

		// Act - This returns false because neither encoder nor format is provided
		bool result = await Manipulation.ResizeImageBaseAsync(inputStream, outputStream, null, 100, 100, null, null, null, false, null);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ResizeImage_Stream_WithNullEncoderButValidFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		using MemoryStream inputStream = new(File.ReadAllBytes(inputPath));
		using MemoryStream outputStream = new();

		// Act - This should use imageFormat path (encoder is null, format is not null)
		bool result = Manipulation.ResizeImageBase(inputStream, outputStream, null, 100, 100, null, null, PngFormat.Instance, false, null);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_Span_WithNullEncoderButValidFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		byte[] inputBytes = File.ReadAllBytes(inputPath);
		using MemoryStream outputStream = new();

		// Act - This should use imageFormat path (encoder is null, format is not null)
		bool result = Manipulation.ResizeImageBase(new ReadOnlySpan<byte>(inputBytes), outputStream, null, 100, 100, null, null, PngFormat.Instance, false, null);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_Stream_WithNullEncoderButValidFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		await using MemoryStream inputStream = new(await File.ReadAllBytesAsync(inputPath));
		await using MemoryStream outputStream = new();

		// Act - This should use imageFormat path (encoder is null, format is not null)
		bool result = await Manipulation.ResizeImageBaseAsync(inputStream, outputStream, null, 100, 100, null, null, PngFormat.Instance, false, null);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_WithInvalidFilePath_ReturnsFalse()
	{
		// Arrange
		string invalidPath = GetTempFilePath() + "_nonexistent_file.png";
		string outputPath = GetTempFilePath(".png");

		try
		{
			// Act
			bool result = Manipulation.ResizeImage(invalidPath, outputPath, 100, 100);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_WithInvalidFilePath_ReturnsFalse()
	{
		// Arrange
		string invalidPath = GetTempFilePath() + "_nonexistent_file.png";
		string outputPath = GetTempFilePath(".png");

		try
		{
			// Act
			bool result = await Manipulation.ResizeImageAsync(invalidPath, outputPath, 100, 100);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryFact(3)]
	public void ReduceImageQuality_WithInvalidFilePath_ReturnsFalse()
	{
		// Arrange
		string invalidPath = GetTempFilePath() + "_nonexistent_file.png";
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act
			bool result = Manipulation.ReduceImageQuality(invalidPath, outputPath, 75, null, null, false, null);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_WithInvalidFilePath_ReturnsFalse()
	{
		// Arrange
		string invalidPath = GetTempFilePath() + "_nonexistent_file.png";
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act
			bool result = await Manipulation.ReduceImageQualityAsync(invalidPath, outputPath, 75, null, null, false, null);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryFact(3)]
	public void ResizeImage_Stream_WithCorruptedData_ReturnsFalse()
	{
		// Arrange - create a stream with non-image data
		using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("This is not an image"));
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ResizeImage(inputStream, outputStream, 100, 100, new PngEncoder());

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_Stream_WithCorruptedData_ReturnsFalse()
	{
		// Arrange - create a stream with non-image data
		await using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("This is not an image"));
		await using MemoryStream outputStream = new();

		// Act
		bool result = await Manipulation.ResizeImageAsync(inputStream, outputStream, 100, 100, new PngEncoder());

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Stream_WithCorruptedData_ReturnsFalse()
	{
		// Arrange - create a stream with non-image data
		using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("This is not an image"));
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(inputStream, outputStream, 75, null, null, false, null);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_Stream_WithCorruptedData_ReturnsFalse()
	{
		// Arrange - create a stream with non-image data
		await using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("This is not an image"));
		await using MemoryStream outputStream = new();

		// Act
		bool result = await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, 75, null, null, false, null);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ResizeImage_Span_WithCorruptedData_ReturnsFalse()
	{
		// Arrange - create a span with non-image data
		ReadOnlySpan<byte> inputSpan = System.Text.Encoding.UTF8.GetBytes("This is not an image");
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ResizeImage(inputSpan, outputStream, 100, 100, new PngEncoder());

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Span_WithCorruptedData_ReturnsFalse()
	{
		// Arrange - create a span with non-image data
		byte[] corruptedData = System.Text.Encoding.UTF8.GetBytes("This is not an image");
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(new ReadOnlySpan<byte>(corruptedData), outputStream, 75, null, null, false, null);

		// Assert
		result.ShouldBeFalse();
	}

	#region ConvertImageFormat Tests

	[RetryFact(3)]
	public void ConvertImageFormat_String_ToDefaultFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

		try
		{
			// Act
			bool result = Manipulation.ConvertImageFormat(inputPath, outputPath);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ConvertImageFormat_String_WithSpecificFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

		try
		{
			// Act
			bool result = Manipulation.ConvertImageFormat(inputPath, outputPath, JpegFormat.Instance);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ConvertImageFormat_String_WithInvalidInput_ReturnsFalse()
	{
		// Arrange
		string invalidInput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

		try
		{
			File.WriteAllText(invalidInput, "Not an image");

			// Act
			bool result = Manipulation.ConvertImageFormat(invalidInput, outputPath, PngFormat.Instance);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(invalidInput))
				File.Delete(invalidInput);
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ConvertImageFormat_Stream_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ConvertImageFormat(inputStream, outputStream, JpegFormat.Instance);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
		inputStream.Position.ShouldBe(0);
		outputStream.Position.ShouldBe(0);
	}

	[RetryFact(3)]
	public void ConvertImageFormat_Stream_WithInvalidData_ReturnsFalse()
	{
		// Arrange
		using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("Not an image"));
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ConvertImageFormat(inputStream, outputStream, PngFormat.Instance);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ConvertImageFormat_Span_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		byte[] imageBytes = File.ReadAllBytes(inputPath);
		ReadOnlySpan<byte> inputSpan = imageBytes;
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ConvertImageFormat(inputSpan, outputStream, BmpFormat.Instance);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
		outputStream.Position.ShouldBe(0);
	}

	[RetryFact(3)]
	public void ConvertImageFormat_Span_WithInvalidData_ReturnsFalse()
	{
		// Arrange
		ReadOnlySpan<byte> inputSpan = System.Text.Encoding.UTF8.GetBytes("Not an image");
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ConvertImageFormat(inputSpan, outputStream, PngFormat.Instance);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region Async Tests for IImageFormat

	[RetryFact(3)]
	public async Task ResizeImageAsync_Stream_WithIImageFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		await using FileStream inputStream = File.OpenRead(inputPath);
		await using MemoryStream outputStream = new();

		// Act
		bool result = await Manipulation.ResizeImageAsync(inputStream, outputStream, 100, 100, PngFormat.Instance);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
		inputStream.Position.ShouldBe(0);
		outputStream.Position.ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_Stream_WithResizeOptionsAndFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		await using FileStream inputStream = File.OpenRead(inputPath);
		await using MemoryStream outputStream = new();
		ResizeOptions options = new() { Size = new Size(150, 150), Mode = ResizeMode.Crop };

		// Act
		bool result = await Manipulation.ResizeImageAsync(inputStream, outputStream, options, JpegFormat.Instance);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	#endregion

	#region Additional Async Coverage

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_String_WithOutputFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bmp");

		try
		{
			// Act
			bool result = await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, BmpFormat.Instance, 80, -1, -1);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_String_WithResizeOptions_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
		ResizeOptions options = new() { Size = new Size(200, 200) };

		try
		{
			// Act
			bool result = await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, 85, options);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_Stream_WithOutputFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		await using FileStream inputStream = File.OpenRead(inputPath);
		await using MemoryStream outputStream = new();

		// Act
		bool result = await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, PngFormat.Instance, 90, null, null);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	#endregion

	#region Public API Coverage Tests

	[RetryFact(3)]
	public void ResizeImage_Stream_WithIImageFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ResizeImage(inputStream, outputStream, 100, 100, PngFormat.Instance);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_Stream_WithResizeOptionsAndFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();
		ResizeOptions options = new() { Size = new Size(75, 75) };

		// Act
		bool result = Manipulation.ResizeImage(inputStream, outputStream, options, JpegFormat.Instance);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_Span_WithIImageFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		byte[] imageBytes = File.ReadAllBytes(inputPath);
		ReadOnlySpan<byte> inputSpan = imageBytes;
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ResizeImage(inputSpan, outputStream, 120, 120, BmpFormat.Instance);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_Span_WithResizeOptionsAndFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		byte[] imageBytes = File.ReadAllBytes(inputPath);
		ReadOnlySpan<byte> inputSpan = imageBytes;
		using MemoryStream outputStream = new();
		ResizeOptions options = new() { Size = new Size(90, 90) };

		// Act
		bool result = Manipulation.ResizeImage(inputSpan, outputStream, options, PngFormat.Instance);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ReduceImageQuality_String_WithFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".bmp");

		try
		{
			// Act
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, BmpFormat.Instance, 80, 150, 150);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Stream_WithFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(inputStream, outputStream, PngFormat.Instance, 85, 200, 200);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Span_WithFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		byte[] imageBytes = File.ReadAllBytes(inputPath);
		ReadOnlySpan<byte> inputSpan = imageBytes;
		using MemoryStream outputStream = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(inputSpan, outputStream, BmpFormat.Instance, 70, 180, 180);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_Stream_WithFormat_Success()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		await using FileStream inputStream = File.OpenRead(inputPath);
		await using MemoryStream outputStream = new();

		// Act
		bool result = await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, PngFormat.Instance, 95, 160, 160);

		// Assert
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	#endregion

	#region Exception Path Coverage Tests

	[RetryFact(3)]
	public void ResizeImage_String_WithUnwritableOutput_ReturnsFalse()
	{
		// Arrange - try to write to a path that should fail
		string inputPath = GetTestImagePath("test.png");
		string invalidOutputPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\", "Windows", "System32", "test_should_fail.png");

		// Act
		bool result = Manipulation.ResizeImage(inputPath, invalidOutputPath, 100, 100);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ResizeImage_String_WithEncoder_InvalidInput_ReturnsFalse()
	{
		// Arrange
		string invalidPath = GetTempFilePath(".txt");
		string outputPath = GetTempFilePath(".png");

		try
		{
			File.WriteAllText(invalidPath, "Not a valid image file content here");

			// Act - this should trigger the exception with imageEncoder specified
			bool result = Manipulation.ResizeImage(invalidPath, outputPath, 100, 100, new PngEncoder());

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(invalidPath))
			{
				File.Delete(invalidPath);
			}
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	[RetryFact(3)]
	public void ReduceImageQuality_SameFilePath_WithInvalidInput_ReturnsFalse()
	{
		// Arrange - use same input and output to trigger temp file path
		string filePath = GetTempFilePath(".jpg");

		try
		{
			// Create an invalid image file
			File.WriteAllBytes(filePath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }); // Invalid JPEG header

			// Act - this should fail and potentially test the cleanup path
			bool result = Manipulation.ReduceImageQuality(filePath, filePath, 80, -1, -1, null);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(filePath))
			{
				try { File.Delete(filePath); } catch { /* Ignore */ }
			}
		}
	}

	[RetryFact(3)]
	public void ResizeImage_WithCorruptedJpeg_ReturnsFalse()
	{
		// Arrange
		string corruptedPath = GetTempFilePath(".jpg");
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Create a file with JPEG header but corrupted data
			byte[] corruptedJpeg = new byte[100];
			corruptedJpeg[0] = 0xFF;
			corruptedJpeg[1] = 0xD8; // JPEG SOI marker
			corruptedJpeg[2] = 0xFF;
			corruptedJpeg[3] = 0xE0; // JFIF marker
															 // Rest is zeros/garbage
			File.WriteAllBytes(corruptedPath, corruptedJpeg);

			// Act
			bool result = Manipulation.ResizeImage(corruptedPath, outputPath, 50, 50);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(corruptedPath))
				File.Delete(corruptedPath);
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ResizeImage_WithCorruptedPng_ReturnsFalse()
	{
		// Arrange
		string corruptedPath = GetTempFilePath(".png");
		string outputPath = GetTempFilePath(".png");

		try
		{
			// Create a file with PNG header but corrupted data
			byte[] corruptedPng = new byte[100];
			corruptedPng[0] = 0x89;
			corruptedPng[1] = 0x50;
			corruptedPng[2] = 0x4E;
			corruptedPng[3] = 0x47; // PNG signature
			corruptedPng[4] = 0x0D;
			corruptedPng[5] = 0x0A;
			corruptedPng[6] = 0x1A;
			corruptedPng[7] = 0x0A;
			// Rest is garbage
			File.WriteAllBytes(corruptedPath, corruptedPng);

			// Act
			bool result = Manipulation.ResizeImage(corruptedPath, outputPath, 75, 75, new JpegEncoder());

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(corruptedPath))
				File.Delete(corruptedPath);
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_WithInvalidFile_ReturnsFalse()
	{
		// Arrange
		string invalidPath = GetTempFilePath(".bin");
		string outputPath = GetTempFilePath(".png");

		try
		{
			await File.WriteAllBytesAsync(invalidPath, new byte[] { 0x00, 0x01, 0x02, 0x03 });

			// Act
			bool result = await Manipulation.ResizeImageAsync(invalidPath, outputPath, 100, 100);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(invalidPath))
				File.Delete(invalidPath);
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ReduceImageQuality_WithOutputFormat_InvalidInput_ReturnsFalse()
	{
		// Arrange
		string invalidPath = GetTempFilePath(".dat");
		string outputPath = GetTempFilePath(".bmp");

		try
		{
			File.WriteAllBytes(invalidPath, System.Text.Encoding.UTF8.GetBytes("Definitely not an image"));

			// Act - test with output format specified to cover that code path
			bool result = Manipulation.ReduceImageQuality(invalidPath, outputPath, BmpFormat.Instance, 70, -1, -1, null);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(invalidPath))
				File.Delete(invalidPath);
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_SameFile_WithOutputFormat_InvalidInput_ReturnsFalse()
	{
		// Arrange
		string filePath = GetTempFilePath(".png");

		try
		{
			// Write invalid data
			await File.WriteAllBytesAsync(filePath, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

			// Act - same input/output triggers temp file, with output format for more coverage
			bool result = await Manipulation.ReduceImageQualityAsync(filePath, filePath, BmpFormat.Instance, 75, -1, -1, null);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(filePath))
			{
				try { File.Delete(filePath); } catch { /* Ignore */ }
			}
		}
	}

	[RetryFact(3)]
	public void ConvertImageFormat_WithCorruptedFile_ReturnsFalse()
	{
		// Arrange
		string corruptedPath = GetTempFilePath(".jpg");
		string outputPath = GetTempFilePath(".png");

		try
		{
			// Write corrupted data
			File.WriteAllBytes(corruptedPath, new byte[] { 0xFF, 0xD8, 0x00, 0x00, 0x00 }); // Incomplete JPEG

			// Act
			bool result = Manipulation.ConvertImageFormat(corruptedPath, outputPath, PngFormat.Instance);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(corruptedPath))
				File.Delete(corruptedPath);
			if (File.Exists(outputPath))
				File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void TryDetectImageType_String_WithIOError_ReturnsFalse()
	{
		// Arrange
		string nonExistentPath = GetTempFilePath() + "_does_not_exist.png";

		// Act
		bool result = Manipulation.TryDetectImageType(nonExistentPath, out IImageFormat? format);

		// Assert
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_String_WithCorruptedFile_ReturnsFalse()
	{
		// Arrange
		string corruptedPath = GetTempFilePath(".gif");

		try
		{
			File.WriteAllBytes(corruptedPath, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x00 }); // Incomplete GIF

			// Act
			bool result = Manipulation.TryGetMetadata(corruptedPath, out ImageMetadata _);

			// Assert
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(corruptedPath))
			{
				File.Delete(corruptedPath);
			}
		}
	}

	[RetryFact(3)]
	public void TryDetectImageType_Stream_WithCorruptedData_ReturnsFalse()
	{
		// Arrange
		using MemoryStream stream = new(new byte[] { 0x00, 0x01, 0x02, 0x03 });

		// Act
		bool result = Manipulation.TryDetectImageType(stream, out IImageFormat? format);

		// Assert
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_Stream_WithCorruptedData_ReturnsFalse()
	{
		// Arrange
		using MemoryStream stream = new(new byte[] { 0xFF, 0xD8, 0x00 }); // Incomplete JPEG

		// Act
		bool result = Manipulation.TryGetMetadata(stream, out ImageMetadata _);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void TryDetectImageType_Span_WithCorruptedData_ReturnsFalse()
	{
		// Arrange
		ReadOnlySpan<byte> span = new byte[] { 0x89, 0x50, 0x4E, 0x00 }; // Incomplete PNG

		// Act
		bool result = Manipulation.TryDetectImageType(span, out IImageFormat? format);

		// Assert
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_Span_WithTooShortData_ReturnsFalse()
	{
		// Arrange - less than 4 bytes to trigger early return
		ReadOnlySpan<byte> span = new byte[] { 0x00, 0x01, 0x02 };

		// Act
		bool result = Manipulation.TryGetMetadata(span, out ImageMetadata _);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region GetImageFormatByExtension Tests

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithNull_ThrowsArgumentException()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => Manipulation.GetImageFormatByExtension(null!));
	}

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithEmptyString_ThrowsArgumentException()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => Manipulation.GetImageFormatByExtension(""));
	}

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithSingleChar_ThrowsArgumentException()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => Manipulation.GetImageFormatByExtension("x"));
	}

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithUnsupportedFormat_ThrowsNotSupportedException()
	{
		// Act & Assert
		Should.Throw<NotSupportedException>(() => Manipulation.GetImageFormatByExtension(".xyz"));
	}

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithUnsupportedFormatNoDot_ThrowsNotSupportedException()
	{
		// Act & Assert
		Should.Throw<NotSupportedException>(() => Manipulation.GetImageFormatByExtension("abc"));
	}

	[RetryTheory(3)]
	[InlineData("bmp")]
	[InlineData(".bmp")]
	[InlineData("BMP")]
	[InlineData(".BMP")]
	public void GetImageFormatByExtension_WithBmp_ReturnsBmpFormat(string extension)
	{
		// Act
		IImageFormat format = Manipulation.GetImageFormatByExtension(extension);

		// Assert
		format.ShouldBe(BmpFormat.Instance);
	}

	[RetryTheory(3)]
	[InlineData("gif")]
	[InlineData(".gif")]
	public void GetImageFormatByExtension_WithGif_ReturnsGifFormat(string extension)
	{
		// Act
		IImageFormat format = Manipulation.GetImageFormatByExtension(extension);

		// Assert
		format.ShouldBe(GifFormat.Instance);
	}

	[RetryTheory(3)]
	[InlineData("jpeg")]
	[InlineData(".jpeg")]
	[InlineData("jpg")]
	[InlineData(".jpg")]
	public void GetImageFormatByExtension_WithJpeg_ReturnsJpegFormat(string extension)
	{
		// Act
		IImageFormat format = Manipulation.GetImageFormatByExtension(extension);

		// Assert
		format.ShouldBe(JpegFormat.Instance);
	}

	[RetryTheory(3)]
	[InlineData("png")]
	[InlineData(".png")]
	public void GetImageFormatByExtension_WithPng_ReturnsPngFormat(string extension)
	{
		// Act
		IImageFormat format = Manipulation.GetImageFormatByExtension(extension);

		// Assert
		format.ShouldBe(PngFormat.Instance);
	}

	[RetryTheory(3)]
	[InlineData("tiff")]
	[InlineData(".tiff")]
	public void GetImageFormatByExtension_WithTiff_ReturnsTiffFormat(string extension)
	{
		// Act
		IImageFormat format = Manipulation.GetImageFormatByExtension(extension);

		// Assert
		format.ShouldBe(TiffFormat.Instance);
	}

	#endregion

	#region ConvertImageFormatAsync Exception Tests

	[RetryFact(3)]
	public async Task ConvertImageFormatAsync_FilePath_WithInvalidInputPath_ReturnsFalse()
	{
		// Arrange
		string invalidInputPath = GetTestImagePath("nonexistent_file_12345.png");
		string outputPath = GetTempFilePath(".jpg");

		// Act
		bool result = await Manipulation.ConvertImageFormatAsync(invalidInputPath, outputPath, JpegFormat.Instance);

		// Assert
		result.ShouldBeFalse();

		// Cleanup
		if (File.Exists(outputPath))
		{
			File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ConvertImageFormatAsync_Stream_WithInvalidData_ReturnsFalse()
	{
		// Arrange - create a stream with invalid image data
		byte[] invalidData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };
		await using MemoryStream inputStream = new(invalidData);
		await using MemoryStream outputStream = new();

		// Act
		bool result = await Manipulation.ConvertImageFormatAsync(inputStream, outputStream, PngFormat.Instance);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region ReduceImageQuality Wrapper Coverage Tests

	[RetryFact(3)]
	public void ReduceImageQuality_Stream_SimpleOverload_Succeeds()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		using FileStream input = File.OpenRead(inputPath);
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(input, output, 80, 100, 100);

		// Assert
		result.ShouldBeTrue();
		output.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Span_SimpleOverload_Succeeds()
	{
		// Arrange
		byte[] imageData = GetTestImageBytes("test.png");
		ReadOnlySpan<byte> inputSpan = imageData;
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(inputSpan, output, 80, 100, 100);

		// Assert
		result.ShouldBeTrue();
		output.Length.ShouldBeGreaterThan(0);
	}

	#endregion

	#region ResizeImage Without Encoder Coverage Tests

	[RetryFact(3)]
	public void ResizeImage_WithoutEncoder_Succeeds()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".png");

		// Act - passing null encoder to hit the imageEncoder == null path
		bool result = Manipulation.ResizeImage(inputPath, outputPath, 100, 100, null);

		// Assert
		result.ShouldBeTrue();
		File.Exists(outputPath).ShouldBeTrue();

		// Cleanup
		if (File.Exists(outputPath))
		{
			File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_WithoutEncoder_Succeeds()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".png");

		// Act - passing null encoder to hit the imageEncoder == null path
		bool result = await Manipulation.ResizeImageAsync(inputPath, outputPath, 100, 100, null);

		// Assert
		result.ShouldBeTrue();
		File.Exists(outputPath).ShouldBeTrue();

		// Cleanup
		if (File.Exists(outputPath))
		{
			File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ResizeImage_WithExplicitEncoder_Succeeds()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".png");

		// Act - passing explicit encoder to hit the imageEncoder != null path
		bool result = Manipulation.ResizeImage(inputPath, outputPath, 100, 100, new PngEncoder());

		// Assert
		result.ShouldBeTrue();
		File.Exists(outputPath).ShouldBeTrue();

		// Cleanup
		if (File.Exists(outputPath))
		{
			File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_WithExplicitEncoder_Succeeds()
	{
		// Arrange
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".jpg");

		// Act - passing explicit encoder to hit the imageEncoder != null path
		bool result = await Manipulation.ResizeImageAsync(inputPath, outputPath, 100, 100, new JpegEncoder());

		// Assert
		result.ShouldBeTrue();
		File.Exists(outputPath).ShouldBeTrue();

		// Cleanup
		if (File.Exists(outputPath))
		{
			File.Delete(outputPath);
		}
	}

	#endregion

	#region Async Methods Short Data Coverage Tests

	[RetryFact(3)]
	public async Task TryDetectImageTypeAsync_String_WithShortPath_ReturnsNull()
	{
		// Arrange - path less than 4 characters
		string shortPath = "ab";

		// Act
		IImageFormat? format = await Manipulation.TryDetectImageTypeAsync(shortPath);

		// Assert
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public async Task TryGetMetadataAsync_String_WithShortPath_ReturnsNull()
	{
		// Arrange - path less than 4 characters
		string shortPath = "xyz";

		// Act
		ImageMetadata? metadata = await Manipulation.TryGetMetadataAsync(shortPath);

		// Assert
		metadata.ShouldBeNull();
	}

	[RetryFact(3)]
	public async Task TryDetectImageTypeAsync_Stream_WithShortStream_ReturnsNull()
	{
		// Arrange - stream with less than 4 bytes
		byte[] shortData = new byte[] { 0x01, 0x02 };
		await using MemoryStream stream = new(shortData);

		// Act
		IImageFormat? format = await Manipulation.TryDetectImageTypeAsync(stream);

		// Assert
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public async Task TryGetMetadataAsync_Stream_WithShortStream_ReturnsNull()
	{
		// Arrange - stream with less than 4 bytes
		byte[] shortData = new byte[] { 0xFF, 0xFE, 0xFD };
		await using MemoryStream stream = new(shortData);

		// Act
		ImageMetadata? metadata = await Manipulation.TryGetMetadataAsync(stream);

		// Assert
		metadata.ShouldBeNull();
	}

	#endregion
}
