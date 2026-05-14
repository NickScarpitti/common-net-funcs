using SkiaSharp;
using static CommonNetFuncs.Core.DimensionScale;

namespace CommonNetFuncs.Images;

/// <summary>
/// Represents image dimensions used in resize operations.
/// </summary>
public readonly struct ImageSize(int width, int height)
{
	public int Width { get; } = width;
	public int Height { get; } = height;
}

/// <summary>
/// Controls how an image is scaled to fit within the target dimensions.
/// </summary>
public enum ResizeMode
{
	/// <summary>Scale to exactly the specified dimensions (may distort).</summary>
	Stretch = 0,
	/// <summary>Scale to fit within the specified dimensions while preserving aspect ratio. Does not upscale.</summary>
	Max = 1,
	/// <summary>Scale to fill the target dimensions while preserving aspect ratio, then crop the excess.</summary>
	Crop = 2,
}

/// <summary>
/// Options controlling an image resize operation.
/// </summary>
public class ResizeOptions
{
	public ImageSize Size { get; set; }
	public ResizeMode Mode { get; set; } = ResizeMode.Stretch;
}

/// <summary>
/// Basic metadata read from an image.
/// </summary>
public class ImageInfo
{
	public int Width { get; init; }
	public int Height { get; init; }
	public SKEncodedImageFormat? EncodedFormat { get; init; }
	public double HorizontalResolution { get; init; }
	public double VerticalResolution { get; init; }
}

/// <summary>
/// Wrapper for SkiaSharp image manipulation operations.
/// </summary>
public static class Manipulation
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	private const string ReduceQualityErrorMessage = "Error reducing image quality to {Quality} with width {Width} and height {Height}";
	private const int DefaultResizeQuality = 90;
	private static readonly SKSamplingOptions DefaultSampling = new(SKCubicResampler.Mitchell);

	// Returns a newly allocated resized (and optionally cropped) bitmap, or null if no resize is required.
	private static SKBitmap? ResizeCore(SKBitmap source, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? sampling, bool useDimsAsMax, bool resizeRequired)
	{
		int targetW, targetH;
		bool effectiveUseDimsAsMax = useDimsAsMax;
		bool cropMode = false;

		if (resizeOptions != null)
		{
			targetW = resizeOptions.Size.Width;
			targetH = resizeOptions.Size.Height;
			if (resizeOptions.Mode == ResizeMode.Max)
			{
				effectiveUseDimsAsMax = true;
			}
			else if (resizeOptions.Mode == ResizeMode.Crop)
			{
				cropMode = true;
			}
		}
		else if (width.HasValue && height.HasValue && (width.Value > 0 || height.Value > 0))
		{
			targetW = width.Value;
			targetH = height.Value;
		}
		else if (resizeRequired)
		{
			throw new ArgumentException("Either resizeOptions or width and height must be provided for resizing the image.");
		}
		else
		{
			return null;
		}

		if (cropMode)
		{
			return ResizeCrop(source, targetW, targetH, sampling);
		}

		if (effectiveUseDimsAsMax)
		{
			(targetW, targetH) = ScaleDimensionsToConstraint(source.Width, source.Height, targetW, targetH, scaleUpToFit: false);
		}

		if (targetW == source.Width && targetH == source.Height)
		{
			return null; // No resize needed
		}

		SKImageInfo info = new(targetW, targetH, source.ColorType, source.AlphaType);
		return source.Resize(info, sampling ?? DefaultSampling);
	}

	// Scale-to-fill then center-crop to the target dimensions.
	private static SKBitmap ResizeCrop(SKBitmap source, int targetW, int targetH, SKSamplingOptions? sampling)
	{
		// Compute scale factor to fill the target box (scale up so neither dimension is smaller than target)
		double scaleX = (double)targetW / source.Width;
		double scaleY = (double)targetH / source.Height;
		double scale = Math.Max(scaleX, scaleY);

		int scaledW = (int)Math.Ceiling(source.Width * scale);
		int scaledH = (int)Math.Ceiling(source.Height * scale);

		// Resize to scaled dimensions
		SKImageInfo scaledInfo = new(scaledW, scaledH, source.ColorType, source.AlphaType);
		using SKBitmap scaled = source.Resize(scaledInfo, sampling ?? DefaultSampling)
			?? throw new InvalidOperationException("Failed to resize image for crop operation.");

		// Center-crop to target dimensions
		int cropX = (scaledW - targetW) / 2;
		int cropY = (scaledH - targetH) / 2;

		SKBitmap cropped = new(targetW, targetH, source.ColorType, source.AlphaType);
		scaled.ExtractSubset(cropped, new SKRectI(cropX, cropY, cropX + targetW, cropY + targetH));
		return cropped;
	}

	// Saves a bitmap to a file stream in the given format.
	private static void SaveToFile(SKBitmap bitmap, string outputFilePath, SKEncodedImageFormat format, int quality)
	{
		using FileStream fs = new(outputFilePath, FileMode.Create, FileAccess.Write);
		if (!bitmap.Encode(fs, format, quality))
		{
			throw new InvalidOperationException($"Failed to encode image as {format}. The format may not be supported for encoding.");
		}
	}

	// Saves a bitmap to an output stream in the given format, then resets both streams to position 0 if seekable.
	private static void SaveToStream(SKBitmap bitmap, Stream outputStream, SKEncodedImageFormat format, int quality, Stream? inputStreamToReset = null)
	{
		if (!bitmap.Encode(outputStream, format, quality))
		{
			throw new InvalidOperationException($"Failed to encode image as {format}. The format may not be supported for encoding.");
		}

		if (inputStreamToReset?.CanSeek == true)
		{
			inputStreamToReset.Position = 0;
		}

		if (outputStream.CanSeek)
		{
			outputStream.Position = 0;
		}
	}

	internal static bool ResizeImageBase(string inputFilePath, string outputFilePath, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat? outputFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		SKBitmap? original = null;
		SKBitmap? resized = null;
		SKBitmap? mutated = null;
		try
		{
			original = SKBitmap.Decode(inputFilePath) ?? throw new InvalidOperationException($"Failed to load image from {inputFilePath}");
			resized = ResizeCore(original, resizeOptions, width, height, samplingOptions, useDimsAsMax, true);
			SKBitmap current = resized ?? original;

			if (mutate != null)
			{
				mutated = mutate(current);
				current = mutated;
			}

			SKEncodedImageFormat format = outputFormat ?? GetImageFormatByExtension(Path.GetExtension(outputFilePath));
			SaveToFile(current, outputFilePath, format, DefaultResizeQuality);
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error resizing image from {InputFilePath} to {OutputFilePath} with width {Width} and height {Height}",
				inputFilePath, outputFilePath, resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
		}
		finally
		{
			mutated?.Dispose();
			resized?.Dispose();
			original?.Dispose();
		}

		return false;
	}

	internal static bool ResizeImageBase(Stream inputStream, Stream outputStream, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat outputFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		SKBitmap? original = null;
		SKBitmap? resized = null;
		SKBitmap? mutated = null;
		try
		{
			original = SKBitmap.Decode(inputStream) ?? throw new InvalidOperationException("Failed to load image from stream");
			resized = ResizeCore(original, resizeOptions, width, height, samplingOptions, useDimsAsMax, true);
			SKBitmap current = resized ?? original;

			if (mutate != null)
			{
				mutated = mutate(current);
				current = mutated;
			}

			SaveToStream(current, outputStream, outputFormat, DefaultResizeQuality, inputStream);
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error resizing image with width {Width} and height {Height}",
				resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
		}
		finally
		{
			mutated?.Dispose();
			resized?.Dispose();
			original?.Dispose();
		}

		return false;
	}

	internal static bool ResizeImageBase(ReadOnlySpan<byte> inputSpan, Stream outputStream, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat outputFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		SKBitmap? original = null;
		SKBitmap? resized = null;
		SKBitmap? mutated = null;
		try
		{
			original = SKBitmap.Decode(inputSpan.ToArray()) ?? throw new InvalidOperationException("Failed to load image from span");
			resized = ResizeCore(original, resizeOptions, width, height, samplingOptions, useDimsAsMax, true);
			SKBitmap current = resized ?? original;

			if (mutate != null)
			{
				mutated = mutate(current);
				current = mutated;
			}

			SaveToStream(current, outputStream, outputFormat, DefaultResizeQuality);
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error resizing image with width {Width} and height {Height}",
				resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
		}
		finally
		{
			mutated?.Dispose();
			resized?.Dispose();
			original?.Dispose();
		}

		return false;
	}

	internal static bool ReduceImageQualityBase(string inputFilePath, string outputFilePath, int quality, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat? outputImageFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		if (quality is < 1 or > 100)
		{
			throw new ArgumentException($"{nameof(quality)} must be between 1 and 100 (inclusive)", nameof(quality));
		}

		// If input and output paths are the same, use a temporary file to avoid corruption
		bool isSameFile = string.Equals(Path.GetFullPath(inputFilePath), Path.GetFullPath(outputFilePath), StringComparison.OrdinalIgnoreCase);
		string tempFilePath = isSameFile ? Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp") : outputFilePath;

		SKBitmap? original = null;
		SKBitmap? resized = null;
		SKBitmap? mutated = null;
		try
		{
			original = SKBitmap.Decode(inputFilePath) ?? throw new InvalidOperationException($"Failed to load image from {inputFilePath}");
			resized = ResizeCore(original, resizeOptions, width, height, samplingOptions, useDimsAsMax, false);
			SKBitmap current = resized ?? original;

			if (mutate != null)
			{
				mutated = mutate(current);
				current = mutated;
			}

			SKEncodedImageFormat format = outputImageFormat ?? SKEncodedImageFormat.Jpeg;
			SaveToFile(current, tempFilePath, format, quality);

			if (isSameFile)
			{
				File.Move(tempFilePath, outputFilePath, true);
			}

			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error reducing image quality from {InputFilePath} to {OutputFilePath} with quality {Quality}", inputFilePath, outputFilePath, quality);

			if (isSameFile && File.Exists(tempFilePath))
			{
				try { File.Delete(tempFilePath); } catch { /* Ignore cleanup errors */ }
			}
		}
		finally
		{
			mutated?.Dispose();
			resized?.Dispose();
			original?.Dispose();
		}

		return false;
	}

	internal static bool ReduceImageQualityBase(Stream inputStream, Stream outputStream, int quality, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat? outputImageFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		if (quality is < 1 or > 100)
		{
			throw new ArgumentException($"{nameof(quality)} must be between 1 and 100 (inclusive)", nameof(quality));
		}

		SKBitmap? original = null;
		SKBitmap? resized = null;
		SKBitmap? mutated = null;
		try
		{
			original = SKBitmap.Decode(inputStream) ?? throw new InvalidOperationException("Failed to load image from stream");
			resized = ResizeCore(original, resizeOptions, width, height, samplingOptions, useDimsAsMax, false);
			SKBitmap current = resized ?? original;

			if (mutate != null)
			{
				mutated = mutate(current);
				current = mutated;
			}

			SKEncodedImageFormat format = outputImageFormat ?? SKEncodedImageFormat.Jpeg;
			SaveToStream(current, outputStream, format, quality, inputStream);
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, ReduceQualityErrorMessage, quality, resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
		}
		finally
		{
			mutated?.Dispose();
			resized?.Dispose();
			original?.Dispose();
		}

		return false;
	}

	internal static bool ReduceImageQualityBase(ReadOnlySpan<byte> inputSpan, Stream outputStream, int quality, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat? outputImageFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		if (quality is < 1 or > 100)
		{
			throw new ArgumentException($"{nameof(quality)} must be between 1 and 100 (inclusive)", nameof(quality));
		}

		SKBitmap? original = null;
		SKBitmap? resized = null;
		SKBitmap? mutated = null;
		try
		{
			original = SKBitmap.Decode(inputSpan.ToArray()) ?? throw new InvalidOperationException("Failed to load image from span");
			resized = ResizeCore(original, resizeOptions, width, height, samplingOptions, useDimsAsMax, false);
			SKBitmap current = resized ?? original;

			if (mutate != null)
			{
				mutated = mutate(current);
				current = mutated;
			}

			SKEncodedImageFormat format = outputImageFormat ?? SKEncodedImageFormat.Jpeg;
			SaveToStream(current, outputStream, format, quality);
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, ReduceQualityErrorMessage, quality, resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
		}
		finally
		{
			mutated?.Dispose();
			resized?.Dispose();
			original?.Dispose();
		}

		return false;
	}

	internal static Task<bool> ResizeImageBaseAsync(string inputFilePath, string outputFilePath, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat? outputFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		return Task.Run(() => ResizeImageBase(inputFilePath, outputFilePath, resizeOptions, width, height, samplingOptions, outputFormat, useDimsAsMax, mutate));
	}

	internal static Task<bool> ResizeImageBaseAsync(Stream inputStream, Stream outputStream, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat outputFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		return Task.Run(() => ResizeImageBase(inputStream, outputStream, resizeOptions, width, height, samplingOptions, outputFormat, useDimsAsMax, mutate));
	}

	internal static Task<bool> ReduceImageQualityBaseAsync(string inputFilePath, string outputFilePath, int quality, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat? outputImageFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		return Task.Run(() => ReduceImageQualityBase(inputFilePath, outputFilePath, quality, resizeOptions, width, height, samplingOptions, outputImageFormat, useDimsAsMax, mutate));
	}

	internal static Task<bool> ReduceImageQualityBaseAsync(Stream inputStream, Stream outputStream, int quality, ResizeOptions? resizeOptions, int? width, int? height,
		SKSamplingOptions? samplingOptions, SKEncodedImageFormat? outputImageFormat, bool useDimsAsMax, Func<SKBitmap, SKBitmap>? mutate)
	{
		return Task.Run(() => ReduceImageQualityBase(inputStream, outputStream, quality, resizeOptions, width, height, samplingOptions, outputImageFormat, useDimsAsMax, mutate));
	}

	/// <summary>
	/// Resizes an image to the specified width and height.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to resize.</param>
	/// <param name="outputFilePath">Path to output resized image file to.</param>
	/// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
	/// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
	/// <param name="outputFormat">Optional: Output format. If null, inferred from the output file extension.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ResizeImage(string inputFilePath, string outputFilePath, int width, int height, SKEncodedImageFormat? outputFormat = null,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBase(inputFilePath, outputFilePath, null, width, height, samplingOptions, outputFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Resizes an image to the specified width and height.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to resize.</param>
	/// <param name="outputFilePath">Path to output resized image file to.</param>
	/// <param name="resizeOptions">Settings for the resize operation.</param>
	/// <param name="outputFormat">Optional: Output format. If null, inferred from the output file extension.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ResizeImage(string inputFilePath, string outputFilePath, ResizeOptions resizeOptions, SKEncodedImageFormat? outputFormat = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBase(inputFilePath, outputFilePath, resizeOptions, null, null, null, outputFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Resizes an image to the specified width and height.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to resize.</param>
	/// <param name="outputStream">Stream to output resized image stream to.</param>
	/// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
	/// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
	/// <param name="outputFormat">Output format for the resized image.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ResizeImage(Stream inputStream, Stream outputStream, int width, int height, SKEncodedImageFormat outputFormat,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBase(inputStream, outputStream, null, width, height, samplingOptions, outputFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Resizes an image to the specified width and height.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to resize.</param>
	/// <param name="outputStream">Stream to output resized image stream to.</param>
	/// <param name="resizeOptions">Settings for the resize operation.</param>
	/// <param name="outputFormat">Output format for the resized image.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ResizeImage(Stream inputStream, Stream outputStream, ResizeOptions resizeOptions, SKEncodedImageFormat outputFormat,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBase(inputStream, outputStream, resizeOptions, null, null, null, outputFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Resizes an image to the specified width and height.
	/// </summary>
	/// <param name="inputSpan">Span filled with image file to resize.</param>
	/// <param name="outputStream">Stream to output resized image stream to.</param>
	/// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
	/// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
	/// <param name="outputFormat">Output format for the resized image.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ResizeImage(ReadOnlySpan<byte> inputSpan, Stream outputStream, int width, int height, SKEncodedImageFormat outputFormat,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBase(inputSpan, outputStream, null, width, height, samplingOptions, outputFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Resizes an image to the specified width and height.
	/// </summary>
	/// <param name="inputSpan">Span filled with image file to resize.</param>
	/// <param name="outputStream">Stream to output resized image stream to.</param>
	/// <param name="resizeOptions">Settings for the resize operation.</param>
	/// <param name="outputFormat">Output format for the resized image.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ResizeImage(ReadOnlySpan<byte> inputSpan, Stream outputStream, ResizeOptions resizeOptions, SKEncodedImageFormat outputFormat,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBase(inputSpan, outputStream, resizeOptions, null, null, null, outputFormat, useDimsAsMax, mutate);
	}


	/// <summary>
	/// Reduces the quality of an image to the specified quality level.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to reduce quality of.</param>
	/// <param name="outputFilePath">Path to output reduced quality image file to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(string inputFilePath, string outputFilePath, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputFilePath, outputFilePath, quality, null, width, height, samplingOptions, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to reduce quality of.</param>
	/// <param name="outputFilePath">Path to output reduced quality image file to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(string inputFilePath, string outputFilePath, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputFilePath, outputFilePath, quality, resizeOptions, null, null, null, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to reduce quality of.</param>
	/// <param name="outputFilePath">Path to output reduced quality image file to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(string inputFilePath, string outputFilePath, SKEncodedImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputFilePath, outputFilePath, quality, null, width, height, samplingOptions, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to reduce quality of.</param>
	/// <param name="outputFilePath">Path to output reduced quality image file to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(string inputFilePath, string outputFilePath, SKEncodedImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputFilePath, outputFilePath, quality, resizeOptions, null, null, null, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(Stream inputStream, Stream outputStream, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputStream, outputStream, quality, null, width, height, samplingOptions, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(Stream inputStream, Stream outputStream, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputStream, outputStream, quality, resizeOptions, null, null, null, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(Stream inputStream, Stream outputStream, SKEncodedImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputStream, outputStream, quality, null, width, height, samplingOptions, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(Stream inputStream, Stream outputStream, SKEncodedImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputStream, outputStream, quality, resizeOptions, null, null, null, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level.
	/// </summary>
	/// <param name="inputSpan">Span filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(ReadOnlySpan<byte> inputSpan, Stream outputStream, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputSpan, outputStream, quality, null, width, height, samplingOptions, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level.
	/// </summary>
	/// <param name="inputSpan">Span filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(ReadOnlySpan<byte> inputSpan, Stream outputStream, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputSpan, outputStream, quality, resizeOptions, null, null, null, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
	/// </summary>
	/// <param name="inputSpan">Span filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(ReadOnlySpan<byte> inputSpan, Stream outputStream, SKEncodedImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputSpan, outputStream, quality, null, width, height, samplingOptions, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
	/// </summary>
	/// <param name="inputSpan">Span filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ReduceImageQuality(ReadOnlySpan<byte> inputSpan, Stream outputStream, SKEncodedImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBase(inputSpan, outputStream, quality, resizeOptions, null, null, null, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Attempt to detect the image type from the file path.
	/// </summary>
	/// <param name="imagePath">Path to the image to detect image type of.</param>
	/// <param name="format">The format of the image if detected, otherwise null.</param>
	/// <returns><see langword="true"/> if the image format was successfully read.</returns>
	public static bool TryDetectImageType(string imagePath, out SKEncodedImageFormat? format)
	{
		format = null;
		SKCodec? codec = null;
		try
		{
			if (imagePath.Length < 4)
			{
				return false;
			}
			codec = SKCodec.Create(imagePath);
			format = codec?.EncodedFormat;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error detecting image type for file {ImagePath}", imagePath);
		}
		finally
		{
			codec?.Dispose();
		}

		return format != null;
	}

	/// <summary>
	/// Attempt to detect the image type from a stream of the image data.
	/// </summary>
	/// <param name="imageStream">Stream containing the image data to detect image type of.</param>
	/// <param name="format">The format of the image if detected, otherwise null.</param>
	/// <returns><see langword="true"/> if the image format was successfully read.</returns>
	public static bool TryDetectImageType(Stream imageStream, out SKEncodedImageFormat? format)
	{
		format = null;
		try
		{
			long? startPosition = imageStream.CanSeek ? imageStream.Position : null;

			if (imageStream.CanSeek && (imageStream.Length - startPosition!.Value) < 4)
			{
				return false;
			}

			// Read just the header bytes — enough to identify any image format magic bytes
			byte[] header = new byte[64];
			int bytesRead = imageStream.Read(header, 0, header.Length);

			if (imageStream.CanSeek)
			{
				imageStream.Position = startPosition!.Value;
			}

			if (bytesRead < 4)
			{
				return false;
			}

			using SKData data = SKData.CreateCopy(header, (uint)bytesRead);
			using SKCodec? codec = SKCodec.Create(data);
			format = codec?.EncodedFormat;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error detecting image type for stream");
		}

		return format != null;
	}

	/// <summary>
	/// Attempt to detect the image type from image data.
	/// </summary>
	/// <param name="imageData">Span containing the image data to detect image type of.</param>
	/// <param name="format">The format of the image if detected, otherwise null.</param>
	/// <returns><see langword="true"/> if the image format was successfully read.</returns>
	public static bool TryDetectImageType(ReadOnlySpan<byte> imageData, out SKEncodedImageFormat? format)
	{
		format = null;
		try
		{
			if (imageData.Length < 4)
			{
				return false;
			}

			using SKData data = SKData.CreateCopy(imageData.ToArray());
using SKCodec? codec = SKCodec.Create(data);
			format = codec?.EncodedFormat;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error trying to detect image type");
		}

		return format != null;
	}

	/// <summary>
	/// Attempts to read metadata from an image file.
	/// </summary>
	/// <param name="imagePath">Image path for the file to get metadata from.</param>
	/// <param name="metadata">Metadata read from the image.</param>
	/// <returns><see langword="true"/> if the metadata was successfully read.</returns>
	public static bool TryGetMetadata(string imagePath, out ImageInfo metadata)
	{
		metadata = new ImageInfo();
		SKBitmap? bitmap = null;
		SKCodec? codec = null;
		try
		{
			if (imagePath.Length < 4)
			{
				return false;
			}
			bitmap = SKBitmap.Decode(imagePath);
			codec = SKCodec.Create(imagePath);
			if (bitmap != null)
			{
				metadata = new ImageInfo
				{
					Width = bitmap.Width,
					Height = bitmap.Height,
					EncodedFormat = codec?.EncodedFormat,
					HorizontalResolution = 96.0,
					VerticalResolution = 96.0,
				};
				return true;
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error reading metadata from image file {ImagePath}", imagePath);
		}
		finally
		{
			bitmap?.Dispose();
			codec?.Dispose();
		}

		return false;
	}

	/// <summary>
	/// Attempts to read metadata from an image stream.
	/// </summary>
	/// <param name="imageStream">Stream containing the image data to get metadata from.</param>
	/// <param name="metadata">Metadata read from the image.</param>
	/// <returns><see langword="true"/> if the metadata was successfully read.</returns>
	public static bool TryGetMetadata(Stream imageStream, out ImageInfo metadata)
	{
		metadata = new ImageInfo();
		SKData? data = null;
		SKCodec? codec = null;
		SKBitmap? bitmap = null;
		try
		{
			if (imageStream.CanSeek && imageStream.Length < 4)
			{
				return false;
			}

			// Buffer the stream so we can both detect format and decode the bitmap
			using MemoryStream ms = new();
			imageStream.CopyTo(ms);
			byte[] bytes = ms.ToArray();

			if (imageStream.CanSeek)
			{
				imageStream.Position = 0;
			}

			if (bytes.Length < 4)
			{
				return false;
			}

			data = SKData.CreateCopy(bytes);
			codec = SKCodec.Create(data);
			bitmap = SKBitmap.Decode(bytes);

			if (bitmap != null)
			{
				metadata = new ImageInfo
				{
					Width = bitmap.Width,
					Height = bitmap.Height,
					EncodedFormat = codec?.EncodedFormat,
					HorizontalResolution = 96.0,
					VerticalResolution = 96.0,
				};
				return true;
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error reading metadata from image stream");
		}
		finally
		{
			bitmap?.Dispose();
			codec?.Dispose();
			data?.Dispose();
		}

		return false;
	}

	/// <summary>
	/// Attempts to read metadata from image data.
	/// </summary>
	/// <param name="imageData">Span containing the image data to get metadata from.</param>
	/// <param name="metadata">Metadata read from the image.</param>
	/// <returns><see langword="true"/> if the metadata was successfully read.</returns>
	public static bool TryGetMetadata(ReadOnlySpan<byte> imageData, out ImageInfo metadata)
	{
		metadata = new ImageInfo();
		SKData? data = null;
		SKCodec? codec = null;
		SKBitmap? bitmap = null;
		try
		{
			if (imageData.Length < 4)
			{
				return false;
			}

			data = SKData.CreateCopy(imageData.ToArray());
			codec = SKCodec.Create(data);
			bitmap = SKBitmap.Decode(imageData.ToArray());

			if (bitmap != null)
			{
				metadata = new ImageInfo
				{
					Width = bitmap.Width,
					Height = bitmap.Height,
					EncodedFormat = codec?.EncodedFormat,
					HorizontalResolution = 96.0,
					VerticalResolution = 96.0,
				};
				return true;
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error reading metadata from image data");
		}
		finally
		{
			bitmap?.Dispose();
			codec?.Dispose();
			data?.Dispose();
		}

		return false;
	}

	/// <summary>
	/// Converts an image from one format to another, inferring the output format from the output file extension.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to re-format.</param>
	/// <param name="outputFilePath">Path to output re-formatted image file to.</param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ConvertImageFormat(string inputFilePath, string outputFilePath, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ConvertImageFormat(inputFilePath, outputFilePath, GetImageFormatByExtension(Path.GetExtension(outputFilePath)), mutate);
	}

	/// <summary>
	/// Converts an image from one format to another.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to re-format.</param>
	/// <param name="outputFilePath">Path to output re-formatted image file to.</param>
	/// <param name="outputFormat">Image format to convert to.</param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ConvertImageFormat(string inputFilePath, string outputFilePath, SKEncodedImageFormat outputFormat, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		SKBitmap? bitmap = null;
		SKBitmap? mutated = null;
		try
		{
			bitmap = SKBitmap.Decode(inputFilePath) ?? throw new InvalidOperationException($"Failed to load image from {inputFilePath}");
			SKBitmap current = bitmap;
			if (mutate != null)
			{
				mutated = mutate(current);
				current = mutated;
			}
			SaveToFile(current, outputFilePath, outputFormat, 100);
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error converting image format from {InputFilePath} to {OutputFilePath} with output format {OutputFormat}",
				inputFilePath, outputFilePath, outputFormat);
		}
		finally
		{
			mutated?.Dispose();
			bitmap?.Dispose();
		}

		return false;
	}

	/// <summary>
	/// Converts an image from one format to another.
	/// </summary>
	/// <param name="inputStream">Stream containing the image data to re-format.</param>
	/// <param name="outputStream">Stream to output re-formatted image to.</param>
	/// <param name="outputFormat">Image format to convert to.</param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ConvertImageFormat(Stream inputStream, Stream outputStream, SKEncodedImageFormat outputFormat, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		SKBitmap? bitmap = null;
		SKBitmap? mutated = null;
		try
		{
			bitmap = SKBitmap.Decode(inputStream) ?? throw new InvalidOperationException("Failed to load image from stream");
			SKBitmap current = bitmap;
			if (mutate != null)
			{
				mutated = mutate(current);
				current = mutated;
			}
			SaveToStream(current, outputStream, outputFormat, 100, inputStream);
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error converting image format from stream with output format {OutputFormat}", outputFormat);
		}
		finally
		{
			mutated?.Dispose();
			bitmap?.Dispose();
		}

		return false;
	}

	/// <summary>
	/// Converts an image from one format to another.
	/// </summary>
	/// <param name="inputData">Span containing the image data to re-format.</param>
	/// <param name="outputStream">Stream to output re-formatted image to.</param>
	/// <param name="outputFormat">Image format to convert to.</param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static bool ConvertImageFormat(ReadOnlySpan<byte> inputData, Stream outputStream, SKEncodedImageFormat outputFormat, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		SKBitmap? bitmap = null;
		SKBitmap? mutated = null;
		try
		{
			bitmap = SKBitmap.Decode(inputData.ToArray()) ?? throw new InvalidOperationException("Failed to load image from span");
			SKBitmap current = bitmap;
			if (mutate != null)
			{
				mutated = mutate(current);
				current = mutated;
			}
			SaveToStream(current, outputStream, outputFormat, 100);
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Error converting image format from span with output format {OutputFormat}", outputFormat);
		}
		finally
		{
			mutated?.Dispose();
			bitmap?.Dispose();
		}

		return false;
	}

	#region Async

	/// <summary>
	/// Resizes an image to the specified width and height asynchronously.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to resize.</param>
	/// <param name="outputFilePath">Path to output resized image file to.</param>
	/// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
	/// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
	/// <param name="outputFormat">Optional: Output format. If null, inferred from the output file extension.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ResizeImageAsync(string inputFilePath, string outputFilePath, int width, int height, SKEncodedImageFormat? outputFormat = null,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBaseAsync(inputFilePath, outputFilePath, null, width, height, samplingOptions, outputFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Resizes an image to the specified width and height asynchronously.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to resize.</param>
	/// <param name="outputFilePath">Path to output resized image file to.</param>
	/// <param name="resizeOptions">Settings for the resize operation.</param>
	/// <param name="outputFormat">Optional: Output format. If null, inferred from the output file extension.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ResizeImageAsync(string inputFilePath, string outputFilePath, ResizeOptions resizeOptions, SKEncodedImageFormat? outputFormat = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBaseAsync(inputFilePath, outputFilePath, resizeOptions, null, null, null, outputFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Resizes an image to the specified width and height asynchronously.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to resize.</param>
	/// <param name="outputStream">Stream to output resized image stream to.</param>
	/// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
	/// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
	/// <param name="outputFormat">Output format for the resized image.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ResizeImageAsync(Stream inputStream, Stream outputStream, int width, int height, SKEncodedImageFormat outputFormat,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBaseAsync(inputStream, outputStream, null, width, height, samplingOptions, outputFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Resizes an image to the specified width and height asynchronously.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to resize.</param>
	/// <param name="outputStream">Stream to output resized image stream to.</param>
	/// <param name="resizeOptions">Settings for the resize operation.</param>
	/// <param name="outputFormat">Output format for the resized image.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ResizeImageAsync(Stream inputStream, Stream outputStream, ResizeOptions resizeOptions, SKEncodedImageFormat outputFormat,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ResizeImageBaseAsync(inputStream, outputStream, resizeOptions, null, null, null, outputFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level asynchronously.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to reduce quality of.</param>
	/// <param name="outputFilePath">Path to output reduced quality image file to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ReduceImageQualityAsync(string inputFilePath, string outputFilePath, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBaseAsync(inputFilePath, outputFilePath, quality, null, width, height, samplingOptions, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level asynchronously.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to reduce quality of.</param>
	/// <param name="outputFilePath">Path to output reduced quality image file to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ReduceImageQualityAsync(string inputFilePath, string outputFilePath, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBaseAsync(inputFilePath, outputFilePath, quality, resizeOptions, null, null, null, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified asynchronously.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to reduce quality of.</param>
	/// <param name="outputFilePath">Path to output reduced quality image file to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ReduceImageQualityAsync(string inputFilePath, string outputFilePath, SKEncodedImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBaseAsync(inputFilePath, outputFilePath, quality, null, width, height, samplingOptions, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified asynchronously.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to reduce quality of.</param>
	/// <param name="outputFilePath">Path to output reduced quality image file to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ReduceImageQualityAsync(string inputFilePath, string outputFilePath, SKEncodedImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBaseAsync(inputFilePath, outputFilePath, quality, resizeOptions, null, null, null, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level asynchronously.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ReduceImageQualityAsync(Stream inputStream, Stream outputStream, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBaseAsync(inputStream, outputStream, quality, null, width, height, samplingOptions, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level asynchronously.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ReduceImageQualityAsync(Stream inputStream, Stream outputStream, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBaseAsync(inputStream, outputStream, quality, resizeOptions, null, null, null, null, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified asynchronously.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="width">Optional: Width of resized image. If less than 1, will not resize.</param>
	/// <param name="height">Optional: Height of resized image. If less than 1, will not resize.</param>
	/// <param name="samplingOptions">Optional: Sampling options for resizing. If null, defaults to Mitchell bicubic.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ReduceImageQualityAsync(Stream inputStream, Stream outputStream, SKEncodedImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1,
		SKSamplingOptions? samplingOptions = null, bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBaseAsync(inputStream, outputStream, quality, null, width, height, samplingOptions, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Reduces the quality of an image to the specified quality level and outputs image of the type specified asynchronously.
	/// </summary>
	/// <param name="inputStream">Stream filled with image file to reduce quality of.</param>
	/// <param name="outputStream">Stream to output reduced quality image to.</param>
	/// <param name="outputImageFormat">The format of the output image.</param>
	/// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75.</param>
	/// <param name="resizeOptions">Optional: Settings for the resize operation.</param>
	/// <param name="useDimsAsMax">
	/// <para>Optional: Use dimensions as a maximum value so dimensions will scale keeping the same aspect ratio so both height and width fit within the provided values.</para>
	/// <para>If the provided dimensions are both larger than the current image dimensions, no scaling will occur.</para>
	/// </param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ReduceImageQualityAsync(Stream inputStream, Stream outputStream, SKEncodedImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null,
		bool useDimsAsMax = false, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return ReduceImageQualityBaseAsync(inputStream, outputStream, quality, resizeOptions, null, null, null, outputImageFormat, useDimsAsMax, mutate);
	}

	/// <summary>
	/// Attempt to detect the image type from the file path asynchronously.
	/// </summary>
	/// <param name="imagePath">Path to the image to detect image type of.</param>
	/// <returns>Image format if the image format was successfully read, otherwise null.</returns>
	public static Task<SKEncodedImageFormat?> TryDetectImageTypeAsync(string imagePath)
	{
		return Task.Run(() =>
		{
			TryDetectImageType(imagePath, out SKEncodedImageFormat? format);
			return format;
		});
	}

	/// <summary>
	/// Attempt to detect the image type from a stream of the image data asynchronously.
	/// </summary>
	/// <param name="imageStream">Stream containing the image data to detect image type of.</param>
	/// <returns>Image format if the image format was successfully read, otherwise null.</returns>
	public static Task<SKEncodedImageFormat?> TryDetectImageTypeAsync(Stream imageStream)
	{
		return Task.Run(() =>
		{
			TryDetectImageType(imageStream, out SKEncodedImageFormat? format);
			return format;
		});
	}

	/// <summary>
	/// Attempts to read metadata from an image file asynchronously.
	/// </summary>
	/// <param name="imagePath">Image path for the file to get metadata from.</param>
	/// <returns>Metadata if successfully read, otherwise null.</returns>
	public static Task<ImageInfo?> TryGetMetadataAsync(string imagePath)
	{
		return Task.Run<ImageInfo?>(() => TryGetMetadata(imagePath, out ImageInfo metadata) ? metadata : null);
	}

	/// <summary>
	/// Attempts to read metadata from an image stream asynchronously.
	/// </summary>
	/// <param name="imageStream">Stream containing the image data to get metadata from.</param>
	/// <returns>Metadata if successfully read, otherwise null.</returns>
	public static Task<ImageInfo?> TryGetMetadataAsync(Stream imageStream)
	{
		return Task.Run<ImageInfo?>(() => TryGetMetadata(imageStream, out ImageInfo metadata) ? metadata : null);
	}

	/// <summary>
	/// Converts an image from one format to another asynchronously, inferring the output format from the output file extension.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to re-format.</param>
	/// <param name="outputFilePath">Path to output re-formatted image file to.</param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ConvertImageFormatAsync(string inputFilePath, string outputFilePath, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return Task.Run(() => ConvertImageFormat(inputFilePath, outputFilePath, mutate));
	}

	/// <summary>
	/// Converts an image from one format to another asynchronously.
	/// </summary>
	/// <param name="inputFilePath">Path to image file to re-format.</param>
	/// <param name="outputFilePath">Path to output re-formatted image file to.</param>
	/// <param name="outputFormat">Image format to convert to.</param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ConvertImageFormatAsync(string inputFilePath, string outputFilePath, SKEncodedImageFormat outputFormat, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return Task.Run(() => ConvertImageFormat(inputFilePath, outputFilePath, outputFormat, mutate));
	}

	/// <summary>
	/// Converts an image from one format to another asynchronously.
	/// </summary>
	/// <param name="inputStream">Stream containing the image data to re-format.</param>
	/// <param name="outputStream">Stream to output re-formatted image to.</param>
	/// <param name="outputFormat">Image format to convert to.</param>
	/// <param name="mutate">Optional: Apply optional mutations to the image as a function that receives and returns an SKBitmap.</param>
	public static Task<bool> ConvertImageFormatAsync(Stream inputStream, Stream outputStream, SKEncodedImageFormat outputFormat, Func<SKBitmap, SKBitmap>? mutate = null)
	{
		return Task.Run(() => ConvertImageFormat(inputStream, outputStream, outputFormat, mutate));
	}

	#endregion

	/// <summary>
	/// Gets the <see cref="SKEncodedImageFormat"/> corresponding to a file extension.
	/// </summary>
	/// <param name="ext">File extension, with or without leading dot.</param>
	/// <returns>The corresponding <see cref="SKEncodedImageFormat"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the extension is null, empty, or too short.</exception>
	/// <exception cref="NotSupportedException">Thrown if the extension maps to a format that SkiaSharp cannot encode (e.g. GIF, TIFF) or is unknown.</exception>
	public static SKEncodedImageFormat GetImageFormatByExtension(string ext)
	{
		if (string.IsNullOrEmpty(ext) || ext.Length < 2)
		{
			throw new ArgumentException("Extension must be at least 2 characters long.", nameof(ext));
		}

		return (ext[0] != '.' ? ext.ToLowerInvariant() : ext[1..].ToLowerInvariant()) switch
		{
			"bmp" => SKEncodedImageFormat.Bmp,
			"jpeg" or "jpg" => SKEncodedImageFormat.Jpeg,
			"png" => SKEncodedImageFormat.Png,
			"webp" => SKEncodedImageFormat.Webp,
			"gif" => throw new NotSupportedException("GIF encoding is not supported by SkiaSharp."),
			"tiff" or "tif" => throw new NotSupportedException("TIFF encoding is not supported by SkiaSharp."),
			_ => throw new NotSupportedException($"Unsupported image format extension: {ext}"),
		};
	}
}
