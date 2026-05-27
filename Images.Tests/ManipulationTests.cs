using CommonNetFuncs.Images;
using SkiaSharp;
using xRetry.v3;

namespace Images.Tests;

public sealed class ManipulationTests : IDisposable
{
	private bool disposed;

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				Task.Delay(2000).Wait();
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

	private static SKBitmap InvertBitmap(SKBitmap source)
	{
		SKBitmap result = source.Copy();
		SKColor[] pixels = result.Pixels;
		for (int i = 0; i < pixels.Length; i++)
		{
			SKColor c = pixels[i];
			pixels[i] = new SKColor((byte)(255 - c.Red), (byte)(255 - c.Green), (byte)(255 - c.Blue), c.Alpha);
		}
		result.Pixels = pixels;
		return result;
	}

	private static readonly Func<SKBitmap, SKBitmap> InvertMutate = InvertBitmap;

	private static bool IsInvertedVersion(SKBitmap original, SKBitmap inverted)
	{
		int checkW = Math.Min(original.Width, 10);
		int checkH = Math.Min(original.Height, 10);
		if (checkW == 0 || checkH == 0) return false;

		for (int x = 0; x < checkW; x++)
		{
			for (int y = 0; y < checkH; y++)
			{
				int invX = x * inverted.Width / original.Width;
				int invY = y * inverted.Height / original.Height;
				SKColor origPixel = original.GetPixel(x, y);
				SKColor invPixel = inverted.GetPixel(invX, invY);
				if (Math.Abs(255 - origPixel.Red - invPixel.Red) > 10) return false;
				if (Math.Abs(255 - origPixel.Green - invPixel.Green) > 10) return false;
				if (Math.Abs(255 - origPixel.Blue - invPixel.Blue) > 10) return false;
			}
		}
		return true;
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	public void ResizeImage_FilePath_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			// Act
			bool result = Manipulation.ResizeImage(inputPath, outputPath, width, height);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKBitmap img = SKBitmap.Decode(outputPath);
			img.ShouldNotBeNull();
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
	[InlineData("test.bmp", 10, 10)]
	public void ResizeImage_Stream_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ResizeImage(input, output, width, height, SKEncodedImageFormat.Jpeg);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.bmp", 10, 10)]
	public void ResizeImage_Span_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ResizeImage(bytes, output, width, height, SKEncodedImageFormat.Jpeg);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.bmp", 10)]
	public void ReduceImageQuality_FilePath_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKCodec? codec = SKCodec.Create(outputPath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Jpeg);
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
	[InlineData("test.bmp", 10)]
	public void ReduceImageQuality_Stream_Succeeds(string fileName, int quality)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(input, output, quality, null);

		// Assert
		result.ShouldBeTrue();
		byte[] outputBytes = output.ToArray();
		using SKData skData = SKData.CreateCopy(outputBytes);
		using SKCodec? codec = SKCodec.Create(skData);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Jpeg);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.bmp", 10)]
	public void ReduceImageQuality_Span_Succeeds(string fileName, int quality)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(bytes, output, quality, null);

		// Assert
		result.ShouldBeTrue();
		byte[] outputBytes = output.ToArray();
		using SKData skData = SKData.CreateCopy(outputBytes);
		using SKCodec? codec = SKCodec.Create(skData);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Jpeg);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	public void ConvertImageFormat_FilePath_Succeeds(string fileName, string outExt)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(outExt);

		try
		{
			SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

			// Act
			bool result = Manipulation.ConvertImageFormat(inputPath, outputPath, format);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKCodec? codec = SKCodec.Create(outputPath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(format);
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
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	public void ConvertImageFormat_Stream_Succeeds(string fileName, string outExt)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();
		SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

		// Act
		bool result = Manipulation.ConvertImageFormat(input, output, format);

		// Assert
		result.ShouldBeTrue();
		byte[] outputBytes = output.ToArray();
		using SKData skData = SKData.CreateCopy(outputBytes);
		using SKCodec? codec = SKCodec.Create(skData);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(format);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	public void ConvertImageFormat_Span_Succeeds(string fileName, string outExt)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		using MemoryStream output = new();
		SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

		// Act
		bool result = Manipulation.ConvertImageFormat(bytes, output, format);

		// Assert
		result.ShouldBeTrue();
		byte[] outputBytes = output.ToArray();
		using SKData skData = SKData.CreateCopy(outputBytes);
		using SKCodec? codec = SKCodec.Create(skData);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(format);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	public void TryDetectImageType_FilePath_Works(string fileName)
	{
		// Arrange
		string path = GetTestImagePath(fileName);

		// Act
		bool result = Manipulation.TryDetectImageType(path, out SKEncodedImageFormat? format);

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
	public void TryDetectImageType_Stream_Works(string fileName)
	{
		// Arrange
		using MemoryStream stream = GetTestImageStream(fileName);

		// Act
		bool result = Manipulation.TryDetectImageType(stream, out SKEncodedImageFormat? format);

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
	public void TryDetectImageType_Span_Works(string fileName)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);

		// Act
		bool result = Manipulation.TryDetectImageType(bytes, out SKEncodedImageFormat? format);

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
	public void TryGetMetadata_FilePath_Works(string fileName)
	{
		// Arrange
		string path = GetTestImagePath(fileName);

		// Act
		bool result = Manipulation.TryGetMetadata(path, out ImageInfo metadata);

		// Assert
		result.ShouldBeTrue();
		metadata.Width.ShouldBeGreaterThan(0);
		metadata.Height.ShouldBeGreaterThan(0);
		metadata.HorizontalResolution.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	public void TryGetMetadata_Stream_Works(string fileName)
	{
		// Arrange
		using MemoryStream stream = GetTestImageStream(fileName);

		// Act
		bool result = Manipulation.TryGetMetadata(stream, out ImageInfo metadata);

		// Assert
		result.ShouldBeTrue();
		metadata.Width.ShouldBeGreaterThan(0);
		metadata.Height.ShouldBeGreaterThan(0);
		metadata.HorizontalResolution.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	public void TryGetMetadata_Span_Works(string fileName)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);

		// Act
		bool result = Manipulation.TryGetMetadata(bytes, out ImageInfo metadata);

		// Assert
		result.ShouldBeTrue();
		metadata.Width.ShouldBeGreaterThan(0);
		metadata.Height.ShouldBeGreaterThan(0);
		metadata.HorizontalResolution.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	public async Task TryDetectImageTypeAsync_FilePath_Works(string fileName)
	{
		// Arrange
		string path = GetTestImagePath(fileName);

		// Act
		SKEncodedImageFormat? format = await Manipulation.TryDetectImageTypeAsync(path);

		// Assert
		format.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	public async Task TryDetectImageTypeAsync_Stream_Works(string fileName)
	{
		// Arrange
		await using MemoryStream stream = GetTestImageStream(fileName);

		// Act
		SKEncodedImageFormat? format = await Manipulation.TryDetectImageTypeAsync(stream);

		// Assert
		format.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData("test.bmp")]
	[InlineData("test.gif")]
	[InlineData("test.jpeg")]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	public async Task TryGetMetadataAsync_FilePath_Works(string fileName)
	{
		// Arrange
		string path = GetTestImagePath(fileName);

		// Act
		ImageInfo? metadata = await Manipulation.TryGetMetadataAsync(path);

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
	public async Task TryGetMetadataAsync_Stream_Works(string fileName)
	{
		// Arrange
		using MemoryStream stream = GetTestImageStream(fileName);

		// Act
		ImageInfo? metadata = await Manipulation.TryGetMetadataAsync(stream);

		// Assert
		metadata.ShouldNotBeNull();
		metadata!.HorizontalResolution.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	public async Task ConvertImageFormatAsync_FilePath_Succeeds(string fileName, string outExt)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(outExt);

		try
		{
			SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

			// Act
			bool result = await Manipulation.ConvertImageFormatAsync(inputPath, outputPath, format);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKCodec? codec = SKCodec.Create(outputPath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(format);
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
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	public async Task ConvertImageFormatAsync_Stream_Succeeds(string fileName, string outExt)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();
		SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

		// Act
		bool result = await Manipulation.ConvertImageFormatAsync(input, output, format);

		// Assert
		result.ShouldBeTrue();
		byte[] outputBytes = output.ToArray();
		using SKData skData = SKData.CreateCopy(outputBytes);
		using SKCodec? codec = SKCodec.Create(skData);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(format);
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
	[InlineData("test.gif", -100)]
	public void ReduceImageQuality_InvalidQuality_Throws(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");

		// Act & Assert
		Should.Throw<ArgumentException>(() => Manipulation.ReduceImageQualityBase(inputPath, outputPath, quality, null, null, null, null, null, false, null));
	}

	[RetryFact(3)]
	public void TryDetectImageType_FilePath_TooShort_ReturnsFalse()
	{
		// Act
		bool result = Manipulation.TryDetectImageType("a", out SKEncodedImageFormat? format);

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
		bool result = Manipulation.TryDetectImageType(stream, out SKEncodedImageFormat? format);

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
		bool result = Manipulation.TryDetectImageType(data, out SKEncodedImageFormat? format);

		// Assert
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_FilePath_TooShort_ReturnsFalse()
	{
		// Act
		bool result = Manipulation.TryGetMetadata("a", out ImageInfo metadata);

		// Assert
		result.ShouldBeFalse();
		metadata.Width.ShouldBe(0);
	}

	[RetryFact(3)]
	public void TryGetMetadata_Stream_TooShort_ReturnsFalse()
	{
		// Arrange
		using MemoryStream stream = new(new byte[2]);

		// Act
		bool result = Manipulation.TryGetMetadata(stream, out ImageInfo metadata);

		// Assert
		result.ShouldBeFalse();
		metadata.Width.ShouldBe(0);
	}

	[RetryFact(3)]
	public void TryGetMetadata_Span_TooShort_ReturnsFalse()
	{
		// Arrange
		byte[] data = new byte[2];

		// Act
		bool result = Manipulation.TryGetMetadata(data, out ImageInfo metadata);

		// Assert
		result.ShouldBeFalse();
		metadata.Width.ShouldBe(0);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 120, 80)]
	[InlineData("test.jpeg", 75, 40)]
	[InlineData("test.png", 50, 25)]
	public void ResizeImage_FilePath_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(Path.GetExtension(fileName));
		ResizeOptions options = new()
		{
			Size = new ImageSize(width, height),
			Mode = ResizeMode.Max
		};

		try
		{
			// Act
			bool result = Manipulation.ResizeImage(inputPath, outputPath, options);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKBitmap img = SKBitmap.Decode(outputPath);
			img.ShouldNotBeNull();
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
	[InlineData("test.bmp", 10, 5)]
	public void ResizeImage_Stream_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();
		ResizeOptions options = new()
		{
			Size = new ImageSize(width, height),
			Mode = ResizeMode.Max
		};

		// Act
		bool result = Manipulation.ResizeImage(input, output, options, SKEncodedImageFormat.Jpeg);

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();
		img.Width.ShouldBeLessThanOrEqualTo(width);
		img.Height.ShouldBeLessThanOrEqualTo(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 120, 80)]
	[InlineData("test.jpeg", 75, 40)]
	[InlineData("test.png", 50, 25)]
	[InlineData("test.gif", 25, 13)]
	[InlineData("test.bmp", 10, 5)]
	public void ResizeImage_Span_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		using MemoryStream output = new();
		ResizeOptions options = new()
		{
			Size = new ImageSize(width, height),
			Mode = ResizeMode.Max
		};

		// Act
		bool result = Manipulation.ResizeImage(bytes, output, options, SKEncodedImageFormat.Jpeg);

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();
		img.Width.ShouldBeLessThanOrEqualTo(width);
		img.Height.ShouldBeLessThanOrEqualTo(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.bmp", 10)]
	public void ReduceImageQuality_FilePath_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".png");

		try
		{
			// Act
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, SKEncodedImageFormat.Png, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKCodec? codec = SKCodec.Create(outputPath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
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
	[InlineData("test.bmp", 10)]
	public void ReduceImageQuality_Stream_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(input, output, SKEncodedImageFormat.Png, quality, null);

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		byte[] outputBytes = output.ToArray();
		using SKData skData = SKData.CreateCopy(outputBytes);
		using SKCodec? codec = SKCodec.Create(skData);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.bmp", 10)]
	public void ReduceImageQuality_Span_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(bytes, output, SKEncodedImageFormat.Png, quality, null);

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		byte[] outputBytes = output.ToArray();
		using SKData skData = SKData.CreateCopy(outputBytes);
		using SKCodec? codec = SKCodec.Create(skData);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQualityAsync_FilePath_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".png");

		try
		{
			// Act
			bool result = await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, SKEncodedImageFormat.Png, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKCodec? codec = SKCodec.Create(outputPath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
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
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQualityAsync_Stream_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();

		// Act
		bool result = await Manipulation.ReduceImageQualityAsync(input, output, SKEncodedImageFormat.Png, quality, null);

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		byte[] outputBytes = output.ToArray();
		using SKData skData = SKData.CreateCopy(outputBytes);
		using SKCodec? codec = SKCodec.Create(skData);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 120, 80)]
	[InlineData("test.jpeg", 75, 40)]
	[InlineData("test.png", 50, 25)]
	public async Task ResizeImageAsync_FilePath_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(Path.GetExtension(fileName));
		ResizeOptions options = new()
		{
			Size = new ImageSize(width, height),
			Mode = ResizeMode.Max
		};

		try
		{
			// Act
			bool result = await Manipulation.ResizeImageAsync(inputPath, outputPath, options);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKBitmap img = SKBitmap.Decode(outputPath);
			img.ShouldNotBeNull();
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
	[InlineData("test.bmp", 10, 5)]
	public async Task ResizeImageAsync_Stream_WithResizeOptions_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();
		ResizeOptions options = new()
		{
			Size = new ImageSize(width, height),
			Mode = ResizeMode.Max
		};

		// Act
		bool result = await Manipulation.ResizeImageAsync(input, output, options, SKEncodedImageFormat.Jpeg);

		// Assert
		result.ShouldBeTrue();
		output.Position.ShouldBe(0);
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();
		img.Width.ShouldBeLessThanOrEqualTo(width);
		img.Height.ShouldBeLessThanOrEqualTo(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.bmp", 10, 10)]
	public void ResizeImage_FilePath_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");
		string refOutputPath = GetTempFilePath(".jpg");

		try
		{
			// Act - with mutate (invert)
			bool result = Manipulation.ResizeImage(inputPath, outputPath, width, height, mutate: InvertMutate);
			// Act - without mutate (reference)
			Manipulation.ResizeImage(inputPath, refOutputPath, width, height);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKBitmap img = SKBitmap.Decode(outputPath);
			img.ShouldNotBeNull();
			img.Width.ShouldBe(width);
			img.Height.ShouldBe(height);

			using SKBitmap orig = SKBitmap.Decode(refOutputPath);
			orig.ShouldNotBeNull();
			IsInvertedVersion(orig, img).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
			if (File.Exists(refOutputPath)) File.Delete(refOutputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.bmp", 10, 10)]
	public void ResizeImage_Stream_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();
		using MemoryStream nonInvertedOutput = new();

		// Act
		bool result = Manipulation.ResizeImage(input, output, width, height, SKEncodedImageFormat.Jpeg, mutate: InvertMutate);
		Manipulation.ResizeImage(input, nonInvertedOutput, width, height, SKEncodedImageFormat.Jpeg);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);

		using SKBitmap orig = SKBitmap.Decode(nonInvertedOutput);
		orig.ShouldNotBeNull();
		IsInvertedVersion(orig, img).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.bmp", 10, 10)]
	public void ResizeImage_Span_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		using MemoryStream output = new();
		using MemoryStream nonInvertedOutput = new();

		// Act
		bool result = Manipulation.ResizeImage(bytes, output, width, height, SKEncodedImageFormat.Jpeg, mutate: InvertMutate);
		Manipulation.ResizeImage(bytes, nonInvertedOutput, width, height, SKEncodedImageFormat.Jpeg);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);

		using SKBitmap orig = SKBitmap.Decode(nonInvertedOutput);
		orig.ShouldNotBeNull();
		IsInvertedVersion(orig, img).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.bmp", 10)]
	public void ReduceImageQuality_FilePath_Mutate_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");

		try
		{
			// Act
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, quality, null, mutate: InvertMutate);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKBitmap img = SKBitmap.Decode(outputPath);
			img.ShouldNotBeNull();

			using SKBitmap orig = SKBitmap.Decode(inputPath);
			orig.ShouldNotBeNull();
			IsInvertedVersion(orig, img).ShouldBeTrue();
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
	[InlineData("test.bmp", 10)]
	public void ReduceImageQuality_Stream_Mutate_Succeeds(string fileName, int quality)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(input, output, quality, null, mutate: InvertMutate);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();

		using SKBitmap orig = SKBitmap.Decode(input);
		orig.ShouldNotBeNull();
		IsInvertedVersion(orig, img).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.bmp", 10)]
	public void ReduceImageQuality_Span_Mutate_Succeeds(string fileName, int quality)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		using MemoryStream output = new();

		// Act
		bool result = Manipulation.ReduceImageQuality(bytes, output, quality, null, mutate: InvertMutate);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();

		using SKBitmap orig = SKBitmap.Decode(bytes);
		orig.ShouldNotBeNull();
		IsInvertedVersion(orig, img).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	public async Task ConvertImageFormatAsync_FilePath_Mutate_Succeeds(string fileName, string outExt)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(outExt);
		string invertedOutputPath = GetTempFilePath(outExt);

		try
		{
			SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

			// Act - with invert mutate
			bool result = await Manipulation.ConvertImageFormatAsync(inputPath, invertedOutputPath, format, mutate: InvertMutate);
			// Act - without mutate (reference)
			await Manipulation.ConvertImageFormatAsync(inputPath, outputPath, format);

			// Assert
			result.ShouldBeTrue();
			File.Exists(invertedOutputPath).ShouldBeTrue();
			using SKBitmap img = SKBitmap.Decode(invertedOutputPath);
			img.ShouldNotBeNull();

using SKCodec? codec = SKCodec.Create(invertedOutputPath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(format);

			using SKBitmap orig = SKBitmap.Decode(outputPath);
			orig.ShouldNotBeNull();
			IsInvertedVersion(orig, img).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
			if (File.Exists(invertedOutputPath)) File.Delete(invertedOutputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.bmp", ".jpeg")]
	[InlineData("test.bmp", ".jpg")]
	[InlineData("test.bmp", ".png")]
	[InlineData("test.gif", ".jpeg")]
	[InlineData("test.gif", ".jpg")]
	[InlineData("test.gif", ".png")]
	[InlineData("test.jpeg", ".jpeg")]
	[InlineData("test.jpeg", ".jpg")]
	[InlineData("test.jpeg", ".png")]
	[InlineData("test.jpg", ".jpeg")]
	[InlineData("test.jpg", ".jpg")]
	[InlineData("test.jpg", ".png")]
	[InlineData("test.png", ".jpeg")]
	[InlineData("test.png", ".jpg")]
	[InlineData("test.png", ".png")]
	public async Task ConvertImageFormatAsync_Stream_Mutate_Succeeds(string fileName, string outExt)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();
		SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

		// Act
		bool result = await Manipulation.ConvertImageFormatAsync(input, output, format, mutate: InvertMutate);

		// Assert
		result.ShouldBeTrue();
		byte[] outputBytes = output.ToArray();
		using SKData skData = SKData.CreateCopy(outputBytes);
using SKCodec? codec = SKCodec.Create(skData);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(format);

		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();

		using SKBitmap orig = SKBitmap.Decode(input);
		orig.ShouldNotBeNull();
		IsInvertedVersion(orig, img).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImageAsync_FilePath_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");
		string refOutputPath = GetTempFilePath(".jpg");

		try
		{
			// Act - with mutate (invert)
			bool result = await Manipulation.ResizeImageAsync(inputPath, outputPath, width, height, mutate: InvertMutate);
			// Act - without mutate (reference)
			Manipulation.ResizeImage(inputPath, refOutputPath, width, height);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKBitmap img = SKBitmap.Decode(outputPath);
			img.ShouldNotBeNull();
			img.Width.ShouldBe(width);
			img.Height.ShouldBe(height);

			using SKBitmap orig = SKBitmap.Decode(refOutputPath);
			orig.ShouldNotBeNull();
			IsInvertedVersion(orig, img).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
			if (File.Exists(refOutputPath)) File.Delete(refOutputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100, 100)]
	[InlineData("test.jpeg", 75, 75)]
	[InlineData("test.png", 50, 50)]
	[InlineData("test.gif", 25, 25)]
	[InlineData("test.bmp", 10, 10)]
	public async Task ResizeImageAsync_Stream_Mutate_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();
		using MemoryStream nonInvertedOutput = new();

		// Act
		bool result = await Manipulation.ResizeImageAsync(input, output, width, height, SKEncodedImageFormat.Jpeg, mutate: InvertMutate);
		Manipulation.ResizeImage(input, nonInvertedOutput, width, height, SKEncodedImageFormat.Jpeg);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();
		img.Width.ShouldBe(width);
		img.Height.ShouldBe(height);

		using SKBitmap orig = SKBitmap.Decode(nonInvertedOutput);
		orig.ShouldNotBeNull();
		IsInvertedVersion(orig, img).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
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
			using SKBitmap img = SKBitmap.Decode(outputPath);
			img.ShouldNotBeNull();

			using SKBitmap orig = SKBitmap.Decode(inputPath);
			orig.ShouldNotBeNull();
			IsInvertedVersion(orig, img).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 100)]
	[InlineData("test.jpeg", 75)]
	[InlineData("test.png", 50)]
	[InlineData("test.gif", 25)]
	[InlineData("test.bmp", 10)]
	public async Task ReduceImageQualityAsync_Stream_Mutate_Succeeds(string fileName, int quality)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();

		// Act
		bool result = await Manipulation.ReduceImageQualityAsync(input, output, quality, null, mutate: InvertMutate);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();

		using SKBitmap orig = SKBitmap.Decode(input);
		orig.ShouldNotBeNull();
		IsInvertedVersion(orig, img).ShouldBeTrue();
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
	public void ResizeImage_FilePath_UseDimsAsMax_Works(string fileName, int width, int height, bool useDimsAsMax, bool useResizeOptions)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jpg");

		try
		{
			if (width < 0 && height < 0)
			{
				using SKBitmap originalImg = SKBitmap.Decode(inputPath);
				originalImg.ShouldNotBeNull();
				width = originalImg.Width;
				height = originalImg.Height;
			}

			// Act
			bool result = !useResizeOptions
				? Manipulation.ResizeImage(inputPath, outputPath, width, height, useDimsAsMax: useDimsAsMax)
				: Manipulation.ResizeImage(inputPath, outputPath, new ResizeOptions { Size = new ImageSize(width, height) }, useDimsAsMax: useDimsAsMax);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKBitmap img = SKBitmap.Decode(outputPath);
			img.ShouldNotBeNull();

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
			if (File.Exists(outputPath)) File.Delete(outputPath);
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
	public void ResizeImage_Stream_UseDimsAsMax_Works(string fileName, int width, int height, bool useDimsAsMax, bool useResizeOptions)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();

		if (width < 0 && height < 0)
		{
			using SKBitmap originalImg = SKBitmap.Decode(input.ToArray());
			originalImg.ShouldNotBeNull();
			width = originalImg.Width;
			height = originalImg.Height;
			input.Position = 0;
		}

		// Act
		bool result = !useResizeOptions
			? Manipulation.ResizeImage(input, output, width, height, SKEncodedImageFormat.Jpeg, null, useDimsAsMax)
			: Manipulation.ResizeImage(input, output, new ResizeOptions { Size = new ImageSize(width, height) }, SKEncodedImageFormat.Jpeg, useDimsAsMax);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();

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
	public void ResizeImage_Span_UseDimsAsMax_Works(string fileName, int width, int height, bool useDimsAsMax, bool useResizeOptions)
	{
		// Arrange
		byte[] bytes = GetTestImageBytes(fileName);
		using MemoryStream output = new();

		if (width < 0 && height < 0)
		{
			using SKBitmap originalImg = SKBitmap.Decode(bytes);
			originalImg.ShouldNotBeNull();
			width = originalImg.Width;
			height = originalImg.Height;
		}

		// Act
		bool result = !useResizeOptions
			? Manipulation.ResizeImage(bytes, output, width, height, SKEncodedImageFormat.Jpeg, null, useDimsAsMax)
			: Manipulation.ResizeImage(bytes, output, new ResizeOptions { Size = new ImageSize(width, height) }, SKEncodedImageFormat.Jpeg, useDimsAsMax);

		// Assert
		result.ShouldBeTrue();
		using SKBitmap img = SKBitmap.Decode(output);
		img.ShouldNotBeNull();

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
				using SKBitmap originalImg = SKBitmap.Decode(inputPath);
				originalImg.ShouldNotBeNull();
				width = originalImg.Width;
				height = originalImg.Height;
			}

			// Act
			bool result = !useResizeOptions
				? await Manipulation.ResizeImageAsync(inputPath, outputPath, width, height, useDimsAsMax: useDimsAsMax)
				: await Manipulation.ResizeImageAsync(inputPath, outputPath, new ResizeOptions { Size = new ImageSize(width, height) }, useDimsAsMax: useDimsAsMax);

			// Assert
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
			using SKBitmap img = SKBitmap.Decode(outputPath);
			img.ShouldNotBeNull();

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
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 75)]
	[InlineData("test.jpeg", 50)]
	[InlineData("test.png", 60)]
	[InlineData("test.gif", 80)]
	[InlineData("test.bmp", 65)]
	public void ReduceImageQuality_SameFilePath_Jpeg_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string testFilePath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			File.Copy(inputPath, testFilePath, true);

			using SKBitmap originalImage = SKBitmap.Decode(testFilePath);
			originalImage.ShouldNotBeNull();
			int originalWidth = originalImage.Width;
			int originalHeight = originalImage.Height;

			// Act - Use same path for input and output
			bool result = Manipulation.ReduceImageQuality(testFilePath, testFilePath, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(testFilePath).ShouldBeTrue();
			using SKCodec? codec = SKCodec.Create(testFilePath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Jpeg);
			using SKBitmap img = SKBitmap.Decode(testFilePath);
			img.ShouldNotBeNull();
			img.Width.ShouldBe(originalWidth);
			img.Height.ShouldBe(originalHeight);
		}
		finally
		{
			if (File.Exists(testFilePath)) File.Delete(testFilePath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 75)]
	[InlineData("test.jpeg", 50)]
	[InlineData("test.png", 60)]
	[InlineData("test.gif", 80)]
	[InlineData("test.bmp", 65)]
	public void ReduceImageQuality_SameFilePath_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string testFilePath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			File.Copy(inputPath, testFilePath, true);

			using SKBitmap originalImage = SKBitmap.Decode(testFilePath);
			originalImage.ShouldNotBeNull();
			int originalWidth = originalImage.Width;
			int originalHeight = originalImage.Height;

			// Act - Use same path for input and output, converting to PNG
			bool result = Manipulation.ReduceImageQuality(testFilePath, testFilePath, SKEncodedImageFormat.Png, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(testFilePath).ShouldBeTrue();
			using SKCodec? codec = SKCodec.Create(testFilePath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
			using SKBitmap img = SKBitmap.Decode(testFilePath);
			img.ShouldNotBeNull();
			img.Width.ShouldBe(originalWidth);
			img.Height.ShouldBe(originalHeight);
		}
		finally
		{
			if (File.Exists(testFilePath)) File.Delete(testFilePath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 75)]
	[InlineData("test.jpeg", 50)]
	[InlineData("test.png", 60)]
	[InlineData("test.gif", 80)]
	[InlineData("test.bmp", 65)]
	public async Task ReduceImageQualityAsync_SameFilePath_Jpeg_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string testFilePath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			File.Copy(inputPath, testFilePath, true);

			using SKBitmap originalImage = SKBitmap.Decode(testFilePath);
			originalImage.ShouldNotBeNull();
			int originalWidth = originalImage.Width;
			int originalHeight = originalImage.Height;

			// Act - Use same path for input and output
			bool result = await Manipulation.ReduceImageQualityAsync(testFilePath, testFilePath, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(testFilePath).ShouldBeTrue();
			using SKCodec? codec = SKCodec.Create(testFilePath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Jpeg);
			using SKBitmap img = SKBitmap.Decode(testFilePath);
			img.ShouldNotBeNull();
			img.Width.ShouldBe(originalWidth);
			img.Height.ShouldBe(originalHeight);
		}
		finally
		{
			if (File.Exists(testFilePath)) File.Delete(testFilePath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 75)]
	[InlineData("test.jpeg", 50)]
	[InlineData("test.png", 60)]
	[InlineData("test.gif", 80)]
	[InlineData("test.bmp", 65)]
	public async Task ReduceImageQualityAsync_SameFilePath_ToPng_Succeeds(string fileName, int quality)
	{
		// Arrange
		string inputPath = GetTestImagePath(fileName);
		string testFilePath = GetTempFilePath(Path.GetExtension(fileName));

		try
		{
			File.Copy(inputPath, testFilePath, true);

			using SKBitmap originalImage = SKBitmap.Decode(testFilePath);
			originalImage.ShouldNotBeNull();
			int originalWidth = originalImage.Width;
			int originalHeight = originalImage.Height;

			// Act - Use same path for input and output, converting to PNG
			bool result = await Manipulation.ReduceImageQualityAsync(testFilePath, testFilePath, SKEncodedImageFormat.Png, quality, null);

			// Assert
			result.ShouldBeTrue();
			File.Exists(testFilePath).ShouldBeTrue();
using SKCodec? codec = SKCodec.Create(testFilePath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
			using SKBitmap img = SKBitmap.Decode(testFilePath);
			img.ShouldNotBeNull();
			img.Width.ShouldBe(originalWidth);
			img.Height.ShouldBe(originalHeight);
		}
		finally
		{
			if (File.Exists(testFilePath)) File.Delete(testFilePath);
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
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".jpg");
		try
		{
			Should.Throw<ArgumentException>(() => Manipulation.ReduceImageQuality(inputPath, outputPath, invalidQuality, resizeOptions: null));
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(101)]
	[InlineData(200)]
	public void ReduceImageQuality_Stream_WithInvalidQuality_ThrowsArgumentException(int invalidQuality)
	{
		string inputPath = GetTestImagePath("test.png");
		using MemoryStream inputStream = new(File.ReadAllBytes(inputPath));
		using MemoryStream outputStream = new();
		Should.Throw<ArgumentException>(() => Manipulation.ReduceImageQuality(inputStream, outputStream, invalidQuality, resizeOptions: null));
	}

	[RetryTheory(3)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(101)]
	[InlineData(200)]
	public void ReduceImageQuality_Span_WithInvalidQuality_ThrowsArgumentException(int invalidQuality)
	{
		string inputPath = GetTestImagePath("test.png");
		byte[] inputBytes = File.ReadAllBytes(inputPath);
		using MemoryStream outputStream = new();
		Should.Throw<ArgumentException>(() => Manipulation.ReduceImageQuality(new ReadOnlySpan<byte>(inputBytes), outputStream, invalidQuality, resizeOptions: null));
	}

	[RetryTheory(3)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(101)]
	[InlineData(200)]
	public async Task ReduceImageQualityAsync_WithInvalidQuality_ThrowsArgumentException(int invalidQuality)
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".jpg");
		try
		{
			await Should.ThrowAsync<ArgumentException>(async () =>
				await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, invalidQuality, resizeOptions: null));
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(101)]
	[InlineData(200)]
	public async Task ReduceImageQualityAsync_Stream_WithInvalidQuality_ThrowsArgumentException(int invalidQuality)
	{
		string inputPath = GetTestImagePath("test.png");
		using MemoryStream inputStream = new(await File.ReadAllBytesAsync(inputPath));
		using MemoryStream outputStream = new();
		await Should.ThrowAsync<ArgumentException>(async () =>
			await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, invalidQuality, resizeOptions: null));
	}

	[RetryFact(3)]
	public void ResizeImage_Stream_WithValidFormat_Succeeds()
	{
		string inputPath = GetTestImagePath("test.png");
		using MemoryStream inputStream = new(File.ReadAllBytes(inputPath));
		using MemoryStream outputStream = new();
		bool result = Manipulation.ResizeImageBase(inputStream, outputStream, null, 100, 100, null, SKEncodedImageFormat.Png, false, null);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_Span_WithValidFormat_Succeeds()
	{
		string inputPath = GetTestImagePath("test.png");
		byte[] inputBytes = File.ReadAllBytes(inputPath);
		using MemoryStream outputStream = new();
		bool result = Manipulation.ResizeImageBase(new ReadOnlySpan<byte>(inputBytes), outputStream, null, 100, 100, null, SKEncodedImageFormat.Png, false, null);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_Stream_WithValidFormat_Succeeds()
	{
		string inputPath = GetTestImagePath("test.png");
		using MemoryStream inputStream = new(await File.ReadAllBytesAsync(inputPath));
		using MemoryStream outputStream = new();
		bool result = await Manipulation.ResizeImageBaseAsync(inputStream, outputStream, null, 100, 100, null, SKEncodedImageFormat.Png, false, null);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_WithInvalidFilePath_ReturnsFalse()
	{
		string invalidPath = GetTempFilePath() + "_nonexistent_file.png";
		string outputPath = GetTempFilePath(".png");
		try
		{
			bool result = Manipulation.ResizeImage(invalidPath, outputPath, 100, 100);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_WithInvalidFilePath_ReturnsFalse()
	{
		string invalidPath = GetTempFilePath() + "_nonexistent_file.png";
		string outputPath = GetTempFilePath(".png");
		try
		{
			bool result = await Manipulation.ResizeImageAsync(invalidPath, outputPath, 100, 100);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ReduceImageQuality_WithInvalidFilePath_ReturnsFalse()
	{
		string invalidPath = GetTempFilePath() + "_nonexistent_file.png";
		string outputPath = GetTempFilePath(".jpg");
		try
		{
			bool result = Manipulation.ReduceImageQuality(invalidPath, outputPath, 75, resizeOptions: null);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_WithInvalidFilePath_ReturnsFalse()
	{
		string invalidPath = GetTempFilePath() + "_nonexistent_file.png";
		string outputPath = GetTempFilePath(".jpg");
		try
		{
			bool result = await Manipulation.ReduceImageQualityAsync(invalidPath, outputPath, 75, resizeOptions: null);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ResizeImage_Stream_WithCorruptedData_ReturnsFalse()
	{
		using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("This is not an image"));
		using MemoryStream outputStream = new();
		bool result = Manipulation.ResizeImage(inputStream, outputStream, 100, 100, SKEncodedImageFormat.Png);
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_Stream_WithCorruptedData_ReturnsFalse()
	{
		using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("This is not an image"));
		using MemoryStream outputStream = new();
		bool result = await Manipulation.ResizeImageAsync(inputStream, outputStream, 100, 100, SKEncodedImageFormat.Png);
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Stream_WithCorruptedData_ReturnsFalse()
	{
		using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("This is not an image"));
		using MemoryStream outputStream = new();
		bool result = Manipulation.ReduceImageQuality(inputStream, outputStream, 75, resizeOptions: null);
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_Stream_WithCorruptedData_ReturnsFalse()
	{
		using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("This is not an image"));
		using MemoryStream outputStream = new();
		bool result = await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, 75, resizeOptions: null);
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ResizeImage_Span_WithCorruptedData_ReturnsFalse()
	{
		ReadOnlySpan<byte> inputSpan = System.Text.Encoding.UTF8.GetBytes("This is not an image");
		using MemoryStream outputStream = new();
		bool result = Manipulation.ResizeImage(inputSpan, outputStream, 100, 100, SKEncodedImageFormat.Png);
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Span_WithCorruptedData_ReturnsFalse()
	{
		byte[] corruptedData = System.Text.Encoding.UTF8.GetBytes("This is not an image");
		using MemoryStream outputStream = new();
		bool result = Manipulation.ReduceImageQuality(new ReadOnlySpan<byte>(corruptedData), outputStream, 75, resizeOptions: null);
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ReduceImageQuality_SameFilePath_WithInvalidInput_ReturnsFalse()
	{
		string filePath = GetTempFilePath(".jpg");
		try
		{
			File.WriteAllBytes(filePath, [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10]);
			bool result = Manipulation.ReduceImageQuality(filePath, filePath, 80, resizeOptions: null);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(filePath)) { try { File.Delete(filePath); } catch { /* Ignore */ } }
		}
	}

	#region ConvertImageFormat Tests

	[RetryFact(3)]
	public void ConvertImageFormat_String_ToDefaultFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
		try
		{
			bool result = Manipulation.ConvertImageFormat(inputPath, outputPath);
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ConvertImageFormat_String_WithSpecificFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
		try
		{
			bool result = Manipulation.ConvertImageFormat(inputPath, outputPath, SKEncodedImageFormat.Jpeg);
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ConvertImageFormat_String_WithInvalidInput_ReturnsFalse()
	{
		string invalidInput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
		try
		{
			File.WriteAllText(invalidInput, "Not an image");
			bool result = Manipulation.ConvertImageFormat(invalidInput, outputPath, SKEncodedImageFormat.Png);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(invalidInput)) File.Delete(invalidInput);
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ConvertImageFormat_Stream_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();
		bool result = Manipulation.ConvertImageFormat(inputStream, outputStream, SKEncodedImageFormat.Jpeg);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
		inputStream.Position.ShouldBe(0);
		outputStream.Position.ShouldBe(0);
	}

	[RetryFact(3)]
	public void ConvertImageFormat_Stream_WithInvalidData_ReturnsFalse()
	{
		using MemoryStream inputStream = new(System.Text.Encoding.UTF8.GetBytes("Not an image"));
		using MemoryStream outputStream = new();
		bool result = Manipulation.ConvertImageFormat(inputStream, outputStream, SKEncodedImageFormat.Png);
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ConvertImageFormat_Span_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		byte[] imageBytes = File.ReadAllBytes(inputPath);
		ReadOnlySpan<byte> inputSpan = imageBytes;
		using MemoryStream outputStream = new();
		bool result = Manipulation.ConvertImageFormat(inputSpan, outputStream, SKEncodedImageFormat.Png);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
		outputStream.Position.ShouldBe(0);
	}

	[RetryFact(3)]
	public void ConvertImageFormat_Span_WithInvalidData_ReturnsFalse()
	{
		ReadOnlySpan<byte> inputSpan = System.Text.Encoding.UTF8.GetBytes("Not an image");
		using MemoryStream outputStream = new();
		bool result = Manipulation.ConvertImageFormat(inputSpan, outputStream, SKEncodedImageFormat.Png);
		result.ShouldBeFalse();
	}

	#endregion

	#region Async Tests

	[RetryFact(3)]
	public async Task ResizeImageAsync_Stream_WithIImageFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();
		bool result = await Manipulation.ResizeImageAsync(inputStream, outputStream, 100, 100, SKEncodedImageFormat.Png);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
		inputStream.Position.ShouldBe(0);
		outputStream.Position.ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_Stream_WithResizeOptionsAndFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();
		ResizeOptions options = new() { Size = new ImageSize(150, 150), Mode = ResizeMode.Crop };
		bool result = await Manipulation.ResizeImageAsync(inputStream, outputStream, options, SKEncodedImageFormat.Jpeg);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_String_WithOutputFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bmp");
		try
		{
			bool result = await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, SKEncodedImageFormat.Jpeg, 80, resizeOptions: null);
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_String_WithResizeOptions_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
		ResizeOptions options = new() { Size = new ImageSize(200, 200) };
		try
		{
			bool result = await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, 85, options);
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_Stream_WithOutputFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();
		bool result = await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, SKEncodedImageFormat.Png, 90, resizeOptions: null);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	#endregion

	#region Public API Coverage Tests

	[RetryFact(3)]
	public void ResizeImage_Stream_WithIImageFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();
		bool result = Manipulation.ResizeImage(inputStream, outputStream, 100, 100, SKEncodedImageFormat.Png);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_Stream_WithResizeOptionsAndFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();
		ResizeOptions options = new() { Size = new ImageSize(75, 75) };
		bool result = Manipulation.ResizeImage(inputStream, outputStream, options, SKEncodedImageFormat.Jpeg);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_Span_WithIImageFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		byte[] imageBytes = File.ReadAllBytes(inputPath);
		ReadOnlySpan<byte> inputSpan = imageBytes;
		using MemoryStream outputStream = new();
		bool result = Manipulation.ResizeImage(inputSpan, outputStream, 120, 120, SKEncodedImageFormat.Jpeg);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ResizeImage_Span_WithResizeOptionsAndFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		byte[] imageBytes = File.ReadAllBytes(inputPath);
		ReadOnlySpan<byte> inputSpan = imageBytes;
		using MemoryStream outputStream = new();
		ResizeOptions options = new() { Size = new ImageSize(90, 90) };
		bool result = Manipulation.ResizeImage(inputSpan, outputStream, options, SKEncodedImageFormat.Png);
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ReduceImageQuality_String_WithFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".bmp");
		try
		{
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, SKEncodedImageFormat.Png, 80,
				new ResizeOptions { Size = new ImageSize(150, 150) });
			result.ShouldBeTrue();
			File.Exists(outputPath).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Stream_WithFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();
		bool result = Manipulation.ReduceImageQuality(inputStream, outputStream, SKEncodedImageFormat.Png, 85,
			new ResizeOptions { Size = new ImageSize(200, 200) });
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Span_WithFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		byte[] imageBytes = File.ReadAllBytes(inputPath);
		ReadOnlySpan<byte> inputSpan = imageBytes;
		using MemoryStream outputStream = new();
		bool result = Manipulation.ReduceImageQuality(inputSpan, outputStream, SKEncodedImageFormat.Png, 70,
			new ResizeOptions { Size = new ImageSize(180, 180) });
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 82, 64, 48)]
	[InlineData("test.png", 76, 72, 54)]
	public void ReduceImageQuality_String_WithWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");
		try
		{
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, quality, width, height);
			result.ShouldBeTrue();
			using SKBitmap output = SKBitmap.Decode(outputPath);
			output.ShouldNotBeNull();
			output.Width.ShouldBe(width);
			output.Height.ShouldBe(height);
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 82, 64, 48)]
	[InlineData("test.png", 76, 72, 54)]
	public void ReduceImageQuality_String_WithFormatWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".png");
		try
		{
			bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, SKEncodedImageFormat.Png, quality, width, height);
			result.ShouldBeTrue();
			using SKCodec? codec = SKCodec.Create(outputPath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
			using SKBitmap output = SKBitmap.Decode(outputPath);
			output.ShouldNotBeNull();
			output.Width.ShouldBe(width);
			output.Height.ShouldBe(height);
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 84, 60, 46)]
	[InlineData("test.png", 79, 68, 50)]
	public void ReduceImageQuality_Stream_WithWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		using MemoryStream inputStream = GetTestImageStream(fileName);
		using MemoryStream outputStream = new();

		bool result = Manipulation.ReduceImageQuality(inputStream, outputStream, quality, width, height);
		result.ShouldBeTrue();
		outputStream.Position.ShouldBe(0);
		using SKBitmap output = SKBitmap.Decode(outputStream);
		output.ShouldNotBeNull();
		output.Width.ShouldBe(width);
		output.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 84, 60, 46)]
	[InlineData("test.png", 79, 68, 50)]
	public void ReduceImageQuality_Stream_WithFormatWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		using MemoryStream inputStream = GetTestImageStream(fileName);
		using MemoryStream outputStream = new();

		bool result = Manipulation.ReduceImageQuality(inputStream, outputStream, SKEncodedImageFormat.Png, quality, width, height);
		result.ShouldBeTrue();
		outputStream.Position.ShouldBe(0);
		using SKData data = SKData.CreateCopy(outputStream.ToArray());
		using SKCodec? codec = SKCodec.Create(data);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
		outputStream.Position = 0;
		using SKBitmap output = SKBitmap.Decode(outputStream);
		output.ShouldNotBeNull();
		output.Width.ShouldBe(width);
		output.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 81, 62, 44)]
	[InlineData("test.png", 77, 66, 52)]
	public void ReduceImageQuality_Span_WithWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		byte[] imageBytes = GetTestImageBytes(fileName);
		using MemoryStream outputStream = new();

		bool result = Manipulation.ReduceImageQuality(imageBytes, outputStream, quality, width, height);
		result.ShouldBeTrue();
		outputStream.Position.ShouldBe(0);
		using SKBitmap output = SKBitmap.Decode(outputStream);
		output.ShouldNotBeNull();
		output.Width.ShouldBe(width);
		output.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 81, 62, 44)]
	[InlineData("test.png", 77, 66, 52)]
	public void ReduceImageQuality_Span_WithFormatWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		byte[] imageBytes = GetTestImageBytes(fileName);
		using MemoryStream outputStream = new();

		bool result = Manipulation.ReduceImageQuality(imageBytes, outputStream, SKEncodedImageFormat.Png, quality, width, height);
		result.ShouldBeTrue();
		outputStream.Position.ShouldBe(0);
		using SKData data = SKData.CreateCopy(outputStream.ToArray());
		using SKCodec? codec = SKCodec.Create(data);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
		outputStream.Position = 0;
		using SKBitmap output = SKBitmap.Decode(outputStream);
		output.ShouldNotBeNull();
		output.Width.ShouldBe(width);
		output.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 83, 58, 42)]
	[InlineData("test.png", 78, 70, 48)]
	public async Task ReduceImageQualityAsync_String_WithWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".jpg");
		try
		{
			bool result = await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, quality, width, height);
			result.ShouldBeTrue();
			using SKBitmap output = SKBitmap.Decode(outputPath);
			output.ShouldNotBeNull();
			output.Width.ShouldBe(width);
			output.Height.ShouldBe(height);
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 83, 58, 42)]
	[InlineData("test.png", 78, 70, 48)]
	public async Task ReduceImageQualityAsync_String_WithFormatWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		string inputPath = GetTestImagePath(fileName);
		string outputPath = GetTempFilePath(".png");
		try
		{
			bool result = await Manipulation.ReduceImageQualityAsync(inputPath, outputPath, SKEncodedImageFormat.Png, quality, width, height);
			result.ShouldBeTrue();
using SKCodec? codec = SKCodec.Create(outputPath);
			codec.ShouldNotBeNull();
			codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
			using SKBitmap output = SKBitmap.Decode(outputPath);
			output.ShouldNotBeNull();
			output.Width.ShouldBe(width);
			output.Height.ShouldBe(height);
		}
		finally
		{
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 86, 56, 40)]
	[InlineData("test.png", 74, 64, 46)]
	public async Task ReduceImageQualityAsync_Stream_WithWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		using MemoryStream inputStream = GetTestImageStream(fileName);
		using MemoryStream outputStream = new();

		bool result = await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, quality, width, height);
		result.ShouldBeTrue();
		outputStream.Position.ShouldBe(0);
		using SKBitmap output = SKBitmap.Decode(outputStream);
		output.ShouldNotBeNull();
		output.Width.ShouldBe(width);
		output.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 86, 56, 40)]
	[InlineData("test.png", 74, 64, 46)]
	public async Task ReduceImageQualityAsync_Stream_WithFormatWidthHeightOverload_Success(string fileName, int quality, int width, int height)
	{
		using MemoryStream inputStream = GetTestImageStream(fileName);
		using MemoryStream outputStream = new();

		bool result = await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, SKEncodedImageFormat.Png, quality, width, height);
		result.ShouldBeTrue();
		outputStream.Position.ShouldBe(0);
		using SKData data = SKData.CreateCopy(outputStream.ToArray());
using SKCodec? codec = SKCodec.Create(data);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
		outputStream.Position = 0;
		using SKBitmap output = SKBitmap.Decode(outputStream);
		output.ShouldNotBeNull();
		output.Width.ShouldBe(width);
		output.Height.ShouldBe(height);
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_Stream_WithFormat_Success()
	{
		string inputPath = GetTestImagePath("test.png");
		using FileStream inputStream = File.OpenRead(inputPath);
		using MemoryStream outputStream = new();
		bool result = await Manipulation.ReduceImageQualityAsync(inputStream, outputStream, SKEncodedImageFormat.Png, 95,
			new ResizeOptions { Size = new ImageSize(160, 160) });
		result.ShouldBeTrue();
		outputStream.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Stream_SimpleOverload_Succeeds()
	{
		string inputPath = GetTestImagePath("test.png");
		using FileStream input = File.OpenRead(inputPath);
		using MemoryStream output = new();
		bool result = Manipulation.ReduceImageQuality(input, output, 80, new ResizeOptions { Size = new ImageSize(100, 100) });
		result.ShouldBeTrue();
		output.Length.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public void ReduceImageQuality_Span_SimpleOverload_Succeeds()
	{
		byte[] imageData = GetTestImageBytes("test.png");
		ReadOnlySpan<byte> inputSpan = imageData;
		using MemoryStream output = new();
		bool result = Manipulation.ReduceImageQuality(inputSpan, output, 80, new ResizeOptions { Size = new ImageSize(100, 100) });
		result.ShouldBeTrue();
		output.Length.ShouldBeGreaterThan(0);
	}

	#endregion

	#region Exception Path Coverage Tests

	[RetryFact(3)]
	public void ResizeImage_String_WithUnwritableOutput_ReturnsFalse()
	{
		string inputPath = GetTestImagePath("test.png");
		string invalidOutputPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "/", "Windows", "System32", "test_should_fail.png");
		bool result = Manipulation.ResizeImage(inputPath, invalidOutputPath, 100, 100);
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ResizeImage_String_WithEncoder_InvalidInput_ReturnsFalse()
	{
		string invalidPath = GetTempFilePath(".txt");
		string outputPath = GetTempFilePath(".png");
		try
		{
			File.WriteAllText(invalidPath, "Not a valid image file content here");
			bool result = Manipulation.ResizeImage(invalidPath, outputPath, 100, 100, SKEncodedImageFormat.Png);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(invalidPath)) File.Delete(invalidPath);
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ResizeImage_WithCorruptedJpeg_ReturnsFalse()
	{
		string corruptedPath = GetTempFilePath(".jpg");
		string outputPath = GetTempFilePath(".jpg");
		try
		{
			byte[] corruptedJpeg = new byte[100];
			corruptedJpeg[0] = 0xFF; corruptedJpeg[1] = 0xD8;
			corruptedJpeg[2] = 0xFF; corruptedJpeg[3] = 0xE0;
			File.WriteAllBytes(corruptedPath, corruptedJpeg);
			bool result = Manipulation.ResizeImage(corruptedPath, outputPath, 50, 50);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(corruptedPath)) File.Delete(corruptedPath);
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ResizeImage_WithCorruptedPng_ReturnsFalse()
	{
		string corruptedPath = GetTempFilePath(".png");
		string outputPath = GetTempFilePath(".png");
		try
		{
			byte[] corruptedPng = new byte[100];
			corruptedPng[0] = 0x89; corruptedPng[1] = 0x50; corruptedPng[2] = 0x4E; corruptedPng[3] = 0x47;
			corruptedPng[4] = 0x0D; corruptedPng[5] = 0x0A; corruptedPng[6] = 0x1A; corruptedPng[7] = 0x0A;
			File.WriteAllBytes(corruptedPath, corruptedPng);
			bool result = Manipulation.ResizeImage(corruptedPath, outputPath, 75, 75, SKEncodedImageFormat.Jpeg);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(corruptedPath)) File.Delete(corruptedPath);
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_WithInvalidFile_ReturnsFalse()
	{
		string invalidPath = GetTempFilePath(".bin");
		string outputPath = GetTempFilePath(".png");
		try
		{
			await File.WriteAllBytesAsync(invalidPath, new byte[] { 0x00, 0x01, 0x02, 0x03 });
			bool result = await Manipulation.ResizeImageAsync(invalidPath, outputPath, 100, 100);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(invalidPath)) File.Delete(invalidPath);
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void ReduceImageQuality_WithOutputFormat_InvalidInput_ReturnsFalse()
	{
		string invalidPath = GetTempFilePath(".dat");
		string outputPath = GetTempFilePath(".bmp");
		try
		{
			File.WriteAllBytes(invalidPath, System.Text.Encoding.UTF8.GetBytes("Definitely not an image"));
			bool result = Manipulation.ReduceImageQuality(invalidPath, outputPath, SKEncodedImageFormat.Bmp, 70, resizeOptions: null);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(invalidPath)) File.Delete(invalidPath);
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public async Task ReduceImageQualityAsync_SameFile_WithOutputFormat_InvalidInput_ReturnsFalse()
	{
		string filePath = GetTempFilePath(".png");
		try
		{
			await File.WriteAllBytesAsync(filePath, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
			bool result = await Manipulation.ReduceImageQualityAsync(filePath, filePath, SKEncodedImageFormat.Bmp, 75, resizeOptions: null);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(filePath)) { try { File.Delete(filePath); } catch { /* Ignore */ } }
		}
	}

	[RetryFact(3)]
	public void ConvertImageFormat_WithCorruptedFile_ReturnsFalse()
	{
		string corruptedPath = GetTempFilePath(".jpg");
		string outputPath = GetTempFilePath(".png");
		try
		{
			File.WriteAllBytes(corruptedPath, new byte[] { 0xFF, 0xD8, 0x00, 0x00, 0x00 });
			bool result = Manipulation.ConvertImageFormat(corruptedPath, outputPath, SKEncodedImageFormat.Png);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(corruptedPath)) File.Delete(corruptedPath);
			if (File.Exists(outputPath)) File.Delete(outputPath);
		}
	}

	[RetryFact(3)]
	public void TryDetectImageType_String_WithIOError_ReturnsFalse()
	{
		string nonExistentPath = GetTempFilePath() + "_does_not_exist.png";
		bool result = Manipulation.TryDetectImageType(nonExistentPath, out SKEncodedImageFormat? format);
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_String_WithCorruptedFile_ReturnsFalse()
	{
		string corruptedPath = GetTempFilePath(".gif");
		try
		{
			File.WriteAllBytes(corruptedPath, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x00 });
			bool result = Manipulation.TryGetMetadata(corruptedPath, out ImageInfo _);
			result.ShouldBeFalse();
		}
		finally
		{
			if (File.Exists(corruptedPath)) File.Delete(corruptedPath);
		}
	}

	[RetryFact(3)]
	public void TryDetectImageType_Stream_WithCorruptedData_ReturnsFalse()
	{
		using MemoryStream stream = new(new byte[] { 0x00, 0x01, 0x02, 0x03 });
		bool result = Manipulation.TryDetectImageType(stream, out SKEncodedImageFormat? format);
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_Stream_WithCorruptedData_ReturnsFalse()
	{
		using MemoryStream stream = new(new byte[] { 0xFF, 0xD8, 0x00 });
		bool result = Manipulation.TryGetMetadata(stream, out ImageInfo _);
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void TryDetectImageType_Span_WithCorruptedData_ReturnsFalse()
	{
		ReadOnlySpan<byte> span = new byte[] { 0x89, 0x50, 0x4E, 0x00 };
		bool result = Manipulation.TryDetectImageType(span, out SKEncodedImageFormat? format);
		result.ShouldBeFalse();
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public void TryGetMetadata_Span_WithTooShortData_ReturnsFalse()
	{
		ReadOnlySpan<byte> span = new byte[] { 0x00, 0x01, 0x02 };
		bool result = Manipulation.TryGetMetadata(span, out ImageInfo _);
		result.ShouldBeFalse();
	}

	#endregion

	#region GetImageFormatByExtension Tests

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithNull_ThrowsArgumentException()
	{
		Should.Throw<ArgumentException>(() => Manipulation.GetImageFormatByExtension(null!));
	}

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithEmptyString_ThrowsArgumentException()
	{
		Should.Throw<ArgumentException>(() => Manipulation.GetImageFormatByExtension(""));
	}

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithSingleChar_ThrowsArgumentException()
	{
		Should.Throw<ArgumentException>(() => Manipulation.GetImageFormatByExtension("x"));
	}

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithUnsupportedFormat_ThrowsNotSupportedException()
	{
		Should.Throw<NotSupportedException>(() => Manipulation.GetImageFormatByExtension(".xyz"));
	}

	[RetryFact(3)]
	public void GetImageFormatByExtension_WithUnsupportedFormatNoDot_ThrowsNotSupportedException()
	{
		Should.Throw<NotSupportedException>(() => Manipulation.GetImageFormatByExtension("abc"));
	}

	[RetryTheory(3)]
	[InlineData("bmp")]
	[InlineData(".bmp")]
	[InlineData("BMP")]
	[InlineData(".BMP")]
	public void GetImageFormatByExtension_WithBmp_ReturnsBmpFormat(string extension)
	{
		SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(extension);
		format.ShouldBe(SKEncodedImageFormat.Bmp);
	}

	[RetryTheory(3)]
	[InlineData("gif")]
	[InlineData(".gif")]
	public void GetImageFormatByExtension_WithGif_ThrowsNotSupportedException(string extension)
	{
		Should.Throw<NotSupportedException>(() => Manipulation.GetImageFormatByExtension(extension));
	}

	[RetryTheory(3)]
	[InlineData("jpeg")]
	[InlineData(".jpeg")]
	[InlineData("jpg")]
	[InlineData(".jpg")]
	public void GetImageFormatByExtension_WithJpeg_ReturnsJpegFormat(string extension)
	{
		SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(extension);
		format.ShouldBe(SKEncodedImageFormat.Jpeg);
	}

	[RetryTheory(3)]
	[InlineData("png")]
	[InlineData(".png")]
	public void GetImageFormatByExtension_WithPng_ReturnsPngFormat(string extension)
	{
		SKEncodedImageFormat format = Manipulation.GetImageFormatByExtension(extension);
		format.ShouldBe(SKEncodedImageFormat.Png);
	}

	[RetryTheory(3)]
	[InlineData("tiff")]
	[InlineData(".tiff")]
	public void GetImageFormatByExtension_WithTiff_ThrowsNotSupportedException(string extension)
	{
		Should.Throw<NotSupportedException>(() => Manipulation.GetImageFormatByExtension(extension));
	}

	#endregion

	#region ConvertImageFormatAsync Exception Tests

	[RetryFact(3)]
	public async Task ConvertImageFormatAsync_FilePath_WithInvalidInputPath_ReturnsFalse()
	{
		string invalidInputPath = GetTestImagePath("nonexistent_file_12345.png");
		string outputPath = GetTempFilePath(".jpg");
		bool result = await Manipulation.ConvertImageFormatAsync(invalidInputPath, outputPath, SKEncodedImageFormat.Jpeg);
		result.ShouldBeFalse();
		if (File.Exists(outputPath)) File.Delete(outputPath);
	}

	[RetryFact(3)]
	public async Task ConvertImageFormatAsync_Stream_WithInvalidData_ReturnsFalse()
	{
		byte[] invalidData = [0x00, 0x01, 0x02, 0x03, 0x04];
		using MemoryStream inputStream = new(invalidData);
		using MemoryStream outputStream = new();
		bool result = await Manipulation.ConvertImageFormatAsync(inputStream, outputStream, SKEncodedImageFormat.Png);
		result.ShouldBeFalse();
	}

	#endregion

	#region ResizeImage Encoder Coverage Tests

	[RetryFact(3)]
	public void ResizeImage_WithoutEncoder_Succeeds()
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".png");
		bool result = Manipulation.ResizeImage(inputPath, outputPath, 100, 100, null);
		result.ShouldBeTrue();
		File.Exists(outputPath).ShouldBeTrue();
		if (File.Exists(outputPath)) File.Delete(outputPath);
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_WithoutEncoder_Succeeds()
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".png");
		bool result = await Manipulation.ResizeImageAsync(inputPath, outputPath, 100, 100, null);
		result.ShouldBeTrue();
		File.Exists(outputPath).ShouldBeTrue();
		if (File.Exists(outputPath)) File.Delete(outputPath);
	}

	[RetryFact(3)]
	public void ResizeImage_WithExplicitEncoder_Succeeds()
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".png");
		bool result = Manipulation.ResizeImage(inputPath, outputPath, 100, 100, SKEncodedImageFormat.Png);
		result.ShouldBeTrue();
		File.Exists(outputPath).ShouldBeTrue();
		if (File.Exists(outputPath)) File.Delete(outputPath);
	}

	[RetryFact(3)]
	public async Task ResizeImageAsync_WithExplicitEncoder_Succeeds()
	{
		string inputPath = GetTestImagePath("test.png");
		string outputPath = GetTempFilePath(".jpg");
		bool result = await Manipulation.ResizeImageAsync(inputPath, outputPath, 100, 100, SKEncodedImageFormat.Jpeg);
		result.ShouldBeTrue();
		File.Exists(outputPath).ShouldBeTrue();
		if (File.Exists(outputPath)) File.Delete(outputPath);
	}

	#endregion

	#region Async Methods Short Data Coverage Tests

	[RetryFact(3)]
	public async Task TryDetectImageTypeAsync_String_WithShortPath_ReturnsNull()
	{
		const string shortPath = "ab";
		SKEncodedImageFormat? format = await Manipulation.TryDetectImageTypeAsync(shortPath);
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public async Task TryGetMetadataAsync_String_WithShortPath_ReturnsNull()
	{
		const string shortPath = "xyz";
		ImageInfo? metadata = await Manipulation.TryGetMetadataAsync(shortPath);
		metadata.ShouldBeNull();
	}

	[RetryFact(3)]
	public async Task TryDetectImageTypeAsync_Stream_WithShortStream_ReturnsNull()
	{
		byte[] shortData = [0x01, 0x02];
		using MemoryStream stream = new(shortData);
		SKEncodedImageFormat? format = await Manipulation.TryDetectImageTypeAsync(stream);
		format.ShouldBeNull();
	}

	[RetryFact(3)]
	public async Task TryGetMetadataAsync_Stream_WithShortStream_ReturnsNull()
	{
		byte[] shortData = [0xFF, 0xFE, 0xFD];
		using MemoryStream stream = new(shortData);
		ImageInfo? metadata = await Manipulation.TryGetMetadataAsync(stream);
		metadata.ShouldBeNull();
	}

	#endregion

	#region ResizeTo

	[RetryTheory(3)]
	[InlineData("test.jpg", 50, 50)]
	[InlineData("test.png", 100, 75)]
	[InlineData("test.bmp", 32, 32)]
	[InlineData("test.gif", 16, 16)]
	public void ResizeTo_Stream_ReturnsBitmapWithCorrectDimensions(string fileName, int width, int height)
	{
		// Arrange
		using MemoryStream stream = GetTestImageStream(fileName);

		// Act
		using SKBitmap result = stream.ResizeTo(width, height);

		// Assert
		result.ShouldNotBeNull();
		result.Width.ShouldBe(width);
		result.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 50, 50)]
	[InlineData("test.png", 100, 75)]
	[InlineData("test.bmp", 32, 32)]
	public void ResizeTo_Stream_WithCustomResampler_ReturnsCorrectDimensions(string fileName, int width, int height)
	{
		// Arrange
		using MemoryStream stream = GetTestImageStream(fileName);

		// Act
		using SKBitmap result = stream.ResizeTo(width, height, SKCubicResampler.CatmullRom);

		// Assert
		result.Width.ShouldBe(width);
		result.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 50, 50)]
	[InlineData("test.png", 100, 75)]
	[InlineData("test.bmp", 32, 32)]
	[InlineData("test.gif", 16, 16)]
	public void ResizeTo_StreamToOutputStream_ReturnsBitmapAndWritesToStream(string fileName, int width, int height)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();

		// Act
		using SKBitmap result = input.ResizeTo(output, width, height, SKEncodedImageFormat.Jpeg, 90);

		// Assert
		result.ShouldNotBeNull();
		result.Width.ShouldBe(width);
		result.Height.ShouldBe(height);

		// Output stream should contain valid JPEG data
		output.Position.ShouldBe(0);
		output.Length.ShouldBeGreaterThan(0);
using SKCodec? codec = SKCodec.Create(output);
		codec.ShouldNotBeNull();
		codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Jpeg);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 50, 50)]
	[InlineData("test.png", 100, 75)]
	[InlineData("test.bmp", 32, 32)]
	public void ResizeTo_StreamToOutputStream_WithCustomResampler_Succeeds(string fileName, int width, int height)
	{
		// Arrange
		using MemoryStream input = GetTestImageStream(fileName);
		using MemoryStream output = new();

		// Act
		using SKBitmap result = input.ResizeTo(output, width, height, SKEncodedImageFormat.Png, 85, SKCubicResampler.CatmullRom);

		// Assert
		result.Width.ShouldBe(width);
		result.Height.ShouldBe(height);
		output.Position.ShouldBe(0);
		output.Length.ShouldBeGreaterThan(0);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 50, 50)]
	[InlineData("test.png", 100, 75)]
	[InlineData("test.bmp", 32, 32)]
	[InlineData("test.gif", 16, 16)]
	public void ResizeTo_SKBitmap_ReturnsBitmapWithCorrectDimensions(string fileName, int width, int height)
	{
		// Arrange
		using SKBitmap source = SKBitmap.Decode(GetTestImagePath(fileName));

		// Act
		using SKBitmap result = source.ResizeTo(width, height);

		// Assert
		result.ShouldNotBeNull();
		result.Width.ShouldBe(width);
		result.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 50, 50)]
	[InlineData("test.png", 100, 75)]
	public void ResizeTo_SKBitmap_WithCustomResampler_ReturnsCorrectDimensions(string fileName, int width, int height)
	{
		// Arrange
		using SKBitmap source = SKBitmap.Decode(GetTestImagePath(fileName));

		// Act
		using SKBitmap result = source.ResizeTo(width, height, SKCubicResampler.CatmullRom);

		// Assert
		result.Width.ShouldBe(width);
		result.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 50, 50)]
	[InlineData("test.png", 100, 75)]
	[InlineData("test.bmp", 32, 32)]
	[InlineData("test.gif", 16, 16)]
	public void ResizeTo_SKImage_ReturnsBitmapWithCorrectDimensions(string fileName, int width, int height)
	{
		// Arrange
		using SKBitmap bmp = SKBitmap.Decode(GetTestImagePath(fileName));
		using SKImage source = SKImage.FromBitmap(bmp);

		// Act
		using SKBitmap result = source.ResizeTo(width, height);

		// Assert
		result.ShouldNotBeNull();
		result.Width.ShouldBe(width);
		result.Height.ShouldBe(height);
	}

	[RetryTheory(3)]
	[InlineData("test.jpg", 50, 50)]
	[InlineData("test.png", 100, 75)]
	public void ResizeTo_SKImage_WithCustomResampler_ReturnsCorrectDimensions(string fileName, int width, int height)
	{
		// Arrange
		using SKBitmap bmp = SKBitmap.Decode(GetTestImagePath(fileName));
		using SKImage source = SKImage.FromBitmap(bmp);

		// Act
		using SKBitmap result = source.ResizeTo(width, height, SKCubicResampler.CatmullRom);

		// Assert
		result.Width.ShouldBe(width);
		result.Height.ShouldBe(height);
	}

	[RetryFact(3)]
	public void ResizeTo_Stream_DefaultResampler_IsMitchell()
	{
		// Verify that not providing a resampler (null) uses the default Mitchell resampler and still works
		using MemoryStream stream = GetTestImageStream("test.jpg");
		using SKBitmap result = stream.ResizeTo(40, 40, null);
		result.Width.ShouldBe(40);
		result.Height.ShouldBe(40);
	}

	[RetryFact(3)]
	public void ResizeTo_SKBitmap_DefaultResampler_IsMitchell()
	{
		using SKBitmap source = SKBitmap.Decode(GetTestImagePath("test.png"));
		using SKBitmap result = source.ResizeTo(40, 40, null);
		result.Width.ShouldBe(40);
		result.Height.ShouldBe(40);
	}

	[RetryFact(3)]
	public void ResizeTo_SKImage_DefaultResampler_IsMitchell()
	{
		using SKBitmap bmp = SKBitmap.Decode(GetTestImagePath("test.png"));
		using SKImage source = SKImage.FromBitmap(bmp);
		using SKBitmap result = source.ResizeTo(40, 40, null);
		result.Width.ShouldBe(40);
		result.Height.ShouldBe(40);
	}

	[RetryFact(3)]
	public void ResizeTo_StreamToOutputStream_SeekableOutput_ResetToZero()
	{
		// Arrange
		using MemoryStream input = GetTestImageStream("test.jpg");
		using MemoryStream output = new();

		// Act
		using SKBitmap result = input.ResizeTo(output, 30, 30, SKEncodedImageFormat.Jpeg, 80);

		// Assert — seekable output stream should be reset to position 0
		output.Position.ShouldBe(0);
	}

	#endregion
}
