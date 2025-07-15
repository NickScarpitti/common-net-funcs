using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace CommonNetFuncs.Images;

/// <summary>
/// Wrapper for ImageSharp image manipulation operations
/// </summary>
public static class Manipulation
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    internal static bool ResizeImageBase(string inputFilePath, string outputFilePath, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageEncoder? imageEncoder, IImageFormat? imageFormat)
    {
        try
        {
            using Image image = Image.Load(inputFilePath);
            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }
            else
            {
                throw new Exception("Either resizeOptions or width and height must be provided for resizing the image.");
            }

            if (imageEncoder == null)
            {
                image.Save(outputFilePath);
            }
            else
            {
                image.Save(outputFilePath, imageEncoder);
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error resizing image from {InputFilePath} to {OutputFilePath} with width {Width} and height {Height}", inputFilePath, outputFilePath, resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
        }
        return false;
    }

    internal static bool ResizeImageBase(Stream inputStream, Stream outputStream, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageEncoder? imageEncoder, IImageFormat? imageFormat)
    {
        try
        {
            using Image image = Image.Load(inputStream);

            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }
            else
            {
                throw new Exception("Either resizeOptions or width and height must be provided for resizing the image.");
            }

            if (imageEncoder != null)
            {
                image.Save(outputStream, imageEncoder);
            }
            else if (imageFormat != null)
            {
                image.Save(outputStream, imageFormat);
            }
            else
            {
                throw new ApplicationException("Either imageEncoder or imageFormat must be provided for saving the resized image.");
            }

            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            if (outputStream.CanSeek)
            {
                outputStream.Position = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error resizing image with width {Width} and height {Height}", resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
        }
        return false;
    }

    internal static bool ResizeImageBase(ReadOnlySpan<byte> inputSpan, Stream outputStream, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageEncoder? imageEncoder, IImageFormat? imageFormat)
    {
        try
        {
            using Image image = Image.Load(inputSpan);

            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }
            else
            {
                throw new Exception("Either resizeOptions or width and height must be provided for resizing the image.");
            }

            if (imageEncoder != null)
            {
                image.Save(outputStream, imageEncoder);
            }
            else if (imageFormat != null)
            {
                image.Save(outputStream, imageFormat);
            }
            else
            {
                throw new ApplicationException("Either imageEncoder or imageFormat must be provided for saving the resized image.");
            }

            if (outputStream.CanSeek)
            {
                outputStream.Position = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error resizing image with width {Width} and height {Height}", resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
        }
        return false;
    }

    internal static bool ReduceImageQualityBase(string inputFilePath, string outputFilePath, int quality, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageFormat? outputImageFormat, JpegEncoder? jpegEncoder)
    {
        if (quality is < 1 or > 100)
        {
            throw new ArgumentException($"{nameof(quality)} must be between 1 and 100 (inclusive)", nameof(quality));
        }

        try
        {
            using Image image = Image.Load(inputFilePath);

            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }

            if (outputImageFormat == null || outputImageFormat == JpegFormat.Instance)
            {
                image.Save(outputFilePath, jpegEncoder ?? new() { Quality = quality });
            }
            else
            {
                using Stream internalStream = new MemoryStream();
                image.Save(internalStream, jpegEncoder ?? new() { Quality = quality });
                internalStream.Position = 0;
                using Image reducedImage = Image.Load(internalStream);
                using MemoryStream reducedStream = new();
                reducedImage.Save(reducedStream, outputImageFormat);
                reducedStream.Position = 0;
                using FileStream fileStream = new(outputFilePath, FileMode.Create, FileAccess.Write);
                fileStream.Write(reducedStream.ToArray());
                fileStream.Flush();
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reducing image quality from {InputFilePath} to {OutputFilePath} with quality {Quality}", inputFilePath, outputFilePath, quality);
        }
        return false;
    }

    internal static bool ReduceImageQualityBase(Stream inputStream, Stream outputStream, int quality, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageFormat? outputImageFormat, JpegEncoder? jpegEncoder)
    {
        if (quality is < 1 or > 100)
        {
            throw new ArgumentException($"{nameof(quality)} must be between 1 and 100 (inclusive)", nameof(quality));
        }

        try
        {
            using Image image = Image.Load(inputStream);

            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }

            if (outputImageFormat == null || outputImageFormat == JpegFormat.Instance)
            {
                image.Save(outputStream, jpegEncoder ?? new() { Quality = quality });
            }
            else
            {
                using Stream internalStream = new MemoryStream();
                image.Save(internalStream, jpegEncoder ?? new() { Quality = quality });
                internalStream.Position = 0;
                using Image reducedImage = Image.Load(internalStream);
                using MemoryStream reducedStream = new();
                reducedImage.Save(outputStream, outputImageFormat);
            }

            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            if (outputStream.CanSeek)
            {
                outputStream.Position = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reducing image quality to {Quality} with width {Width} and height {Height}", quality, resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
        }
        return false;
    }

    internal static bool ReduceImageQualityBase(ReadOnlySpan<byte> inputSpan, Stream outputStream, int quality, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageFormat? outputImageFormat, JpegEncoder? jpegEncoder)
    {
        if (quality is < 1 or > 100)
        {
            throw new ArgumentException($"{nameof(quality)} must be between 1 and 100 (inclusive)", nameof(quality));
        }

        try
        {
            using Image image = Image.Load(inputSpan);

            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }

            if (outputImageFormat == null || outputImageFormat == JpegFormat.Instance)
            {
                image.Save(outputStream, jpegEncoder ?? new() { Quality = quality });
            }
            else
            {
                using Stream internalStream = new MemoryStream();
                image.Save(internalStream, jpegEncoder ?? new() { Quality = quality });
                internalStream.Position = 0;
                using Image reducedImage = Image.Load(internalStream);
                using MemoryStream reducedStream = new();
                reducedImage.Save(outputStream, outputImageFormat);
            }

            if (outputStream.CanSeek)
            {
                outputStream.Position = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reducing image quality to {Quality} with width {Width} and height {Height}", quality, resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
        }
        return false;
    }

    internal static async Task<bool> ResizeImageBaseAsync(string inputFilePath, string outputFilePath, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageEncoder? imageEncoder, IImageFormat? imageFormat)
    {
        try
        {
            using Image image = await Image.LoadAsync(inputFilePath).ConfigureAwait(false);
            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }
            else
            {
                throw new Exception("Either resizeOptions or width and height must be provided for resizing the image.");
            }

            if (imageEncoder == null)
            {
                await image.SaveAsync(outputFilePath).ConfigureAwait(false);
            }
            else
            {
                await image.SaveAsync(outputFilePath, imageEncoder).ConfigureAwait(false);
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error resizing image from {InputFilePath} to {OutputFilePath} with width {Width} and height {Height}", inputFilePath, outputFilePath, resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
        }
        return false;
    }

    internal static async Task<bool> ResizeImageBaseAsync(Stream inputStream, Stream outputStream, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageEncoder? imageEncoder, IImageFormat? imageFormat)
    {
        try
        {
            using Image image = await Image.LoadAsync(inputStream).ConfigureAwait(false);

            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }
            else
            {
                throw new Exception("Either resizeOptions or width and height must be provided for resizing the image.");
            }

            if (imageEncoder != null)
            {
                await image.SaveAsync(outputStream, imageEncoder).ConfigureAwait(false);
            }
            else if (imageFormat != null)
            {
                await image.SaveAsync(outputStream, imageFormat).ConfigureAwait(false);
            }
            else
            {
                throw new ApplicationException("Either imageEncoder or imageFormat must be provided for saving the resized image.");
            }

            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            if (outputStream.CanSeek)
            {
                outputStream.Position = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error resizing image with width {Width} and height {Height}", resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
        }
        return false;
    }

    internal static async Task<bool> ReduceImageQualityBaseAsync(string inputFilePath, string outputFilePath, int quality, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageFormat? outputImageFormat, JpegEncoder? jpegEncoder)
    {
        if (quality is < 1 or > 100)
        {
            throw new ArgumentException($"{nameof(quality)} must be between 1 and 100 (inclusive)", nameof(quality));
        }

        try
        {
            using Image image = await Image.LoadAsync(inputFilePath).ConfigureAwait(false);

            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }

            if (outputImageFormat == null || outputImageFormat == JpegFormat.Instance)
            {
                await image.SaveAsync(outputFilePath).ConfigureAwait(false);
            }
            else
            {
                await using Stream internalStream = new MemoryStream();
                await image.SaveAsync(internalStream, jpegEncoder ?? new() { Quality = quality }).ConfigureAwait(false);
                internalStream.Position = 0;
                using Image reducedImage = await Image.LoadAsync(internalStream).ConfigureAwait(false);
                await using MemoryStream reducedStream = new();
                await reducedImage.SaveAsync(reducedStream, outputImageFormat).ConfigureAwait(false);
                reducedStream.Position = 0;
                await using FileStream fileStream = new(outputFilePath, FileMode.Create, FileAccess.Write);
                await fileStream.WriteAsync(reducedStream.ToArray()).ConfigureAwait(false);
                await fileStream.FlushAsync().ConfigureAwait(false);
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reducing image quality to {Quality} with width {Width} and height {Height}", quality, resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
        }
        return false;
    }

    internal static async Task<bool> ReduceImageQualityBaseAsync(Stream inputStream, Stream outputStream, int quality, ResizeOptions? resizeOptions, int? width, int? height, IResampler? resampler, IImageFormat? outputImageFormat, JpegEncoder? jpegEncoder)
    {
        if (quality is < 1 or > 100)
        {
            throw new ArgumentException($"{nameof(quality)} must be between 1 and 100 (inclusive)", nameof(quality));
        }

        try
        {
            using Image image = await Image.LoadAsync(inputStream).ConfigureAwait(false);

            if (resizeOptions != null)
            {
                image.Mutate(x => x.Resize(resizeOptions));
            }
            else if (width != null && height != null && (width > 0 || height > 0))
            {
                image.Mutate(x => x.Resize((int)width, (int)height, resampler ?? KnownResamplers.Robidoux));
            }

            if (outputImageFormat == null || outputImageFormat == JpegFormat.Instance)
            {
                await image.SaveAsync(outputStream, jpegEncoder ?? new() { Quality = quality }).ConfigureAwait(false);
            }
            else
            {
                await using Stream internalStream = new MemoryStream();
                await image.SaveAsync(internalStream, jpegEncoder ?? new() { Quality = quality }).ConfigureAwait(false);
                internalStream.Position = 0;
                using Image reducedImage = await Image.LoadAsync(internalStream).ConfigureAwait(false);
                await reducedImage.SaveAsync(outputStream, outputImageFormat).ConfigureAwait(false);
            }

            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            if (outputStream.CanSeek)
            {
                outputStream.Position = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reducing image quality to {Quality} with width {Width} and height {Height}", quality, resizeOptions?.Size.Width ?? width, resizeOptions?.Size.Height ?? height);
        }
        return false;
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Optional: Encoder to use for the resizing operation.</param>
    /// <param name="resampler">Optional: Resampler to use for resizing. If null, defaults to Robidoux resampler.</param>
    public static bool ResizeImage(string inputFilePath, string outputFilePath, int width, int height, IImageEncoder? imageEncoder = null, IResampler? resampler = null)
    {
        return ResizeImageBase(inputFilePath, outputFilePath, null, width, height, resampler, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="resizeOptions">Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Optional: Encoder to use for the resizing operation.</param>
    public static bool ResizeImage(string inputFilePath, string outputFilePath, ResizeOptions resizeOptions, IImageEncoder? imageEncoder = null)
    {
        return ResizeImageBase(inputFilePath, outputFilePath, resizeOptions, null, null, null, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Encoder to use for the resizing operation.</param>
    /// <param name="resampler">Optional: Resampler to use for resizing. If null, defaults to Robidoux resampler.</param>
    public static bool ResizeImage(Stream inputStream, Stream outputStream, int width, int height, IImageEncoder imageEncoder, IResampler? resampler = null)
    {
        return ResizeImageBase(inputStream, outputStream, null, width, height, resampler, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="resizeOptions">Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Encoder to use for the resizing operation.</param>
    public static bool ResizeImage(Stream inputStream, Stream outputStream, ResizeOptions resizeOptions, IImageEncoder imageEncoder)
    {
        return ResizeImageBase(inputStream, outputStream, resizeOptions, null, null, null, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="imageFormat">Output format to use for the resizing operation.</param>
    /// <param name="resampler">Optional: Resampler to use for resizing. If null, defaults to Robidoux resampler.</param>
    public static bool ResizeImage(Stream inputStream, Stream outputStream, int width, int height, IImageFormat imageFormat, IResampler? resampler = null)
    {
        return ResizeImageBase(inputStream, outputStream, null, width, height, resampler, null, imageFormat);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="resizeOptions">Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="imageFormat">Output format to use for the resizing operation.</param>
    public static bool ResizeImage(Stream inputStream, Stream outputStream, ResizeOptions resizeOptions, IImageFormat imageFormat)
    {
        return ResizeImageBase(inputStream, outputStream, resizeOptions, null, null, null, null, imageFormat);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputSpan">Span filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Encoder to use for the resizing operation.</param>
    /// <param name="resampler">Optional: Resampler to use for resizing. If null, defaults to Robidoux resampler.</param>
    public static bool ResizeImage(ReadOnlySpan<byte> inputSpan, Stream outputStream, int width, int height, IImageEncoder imageEncoder, IResampler? resampler = null)
    {
        return ResizeImageBase(inputSpan, outputStream, null, width, height, resampler, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputSpan">Span filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="resizeOptions">Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Encoder to use for the resizing operation.</param>
    public static bool ResizeImage(ReadOnlySpan<byte> inputSpan, Stream outputStream, ResizeOptions resizeOptions, IImageEncoder imageEncoder)
    {
        return ResizeImageBase(inputSpan, outputStream, resizeOptions, null, null, null, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputSpan">Span filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="imageFormat">Output format to use for the resizing operation.</param>
    /// <param name="resampler">Optional: Resampler to use for resizing. If null, defaults to Robidoux resampler.</param>
    public static bool ResizeImage(ReadOnlySpan<byte> inputSpan, Stream outputStream, int width, int height, IImageFormat imageFormat, IResampler? resampler = null)
    {
        return ResizeImageBase(inputSpan, outputStream, null, width, height, resampler, null, imageFormat);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputSpan">Span filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="resizeOptions">Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="imageFormat">Output format to use for the resizing operation.</param>
    public static bool ResizeImage(ReadOnlySpan<byte> inputSpan, Stream outputStream, ResizeOptions resizeOptions, IImageFormat imageFormat)
    {
        return ResizeImageBase(inputSpan, outputStream, resizeOptions, null, null, null, null, imageFormat);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(string inputFilePath, string outputFilePath, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputFilePath, outputFilePath, quality, null, width, height, resampler, null, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(string inputFilePath, string outputFilePath, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputFilePath, outputFilePath, quality, resizeOptions, null, null, null, null, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(string inputFilePath, string outputFilePath, IImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputFilePath, outputFilePath, quality, null, width, height, resampler, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(string inputFilePath, string outputFilePath, IImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputFilePath, outputFilePath, quality, resizeOptions, null, null, null, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(Stream inputStream, Stream outputStream, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputStream, outputStream, quality, null, width, height, resampler, null, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(Stream inputStream, Stream outputStream, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputStream, outputStream, quality, resizeOptions, null, null, null, null, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(Stream inputStream, Stream outputStream, IImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputStream, outputStream, quality, null, width, height, resampler, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(Stream inputStream, Stream outputStream, IImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputStream, outputStream, quality, resizeOptions, null, null, null, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputSpan">Span filled with image file to reduce image quality of.</param>
    /// <param name="outputStream">Stream to output reduced quality image to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(ReadOnlySpan<byte> inputSpan, Stream outputStream, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputSpan, outputStream, quality, null, width, height, resampler, null, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputSpan">Span filled with image file to reduce image quality of.</param>
    /// <param name="outputStream">Stream to output reduced quality image to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(ReadOnlySpan<byte> inputSpan, Stream outputStream, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputSpan, outputStream, quality, resizeOptions, null, null, null, null, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputSpan">Span filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(ReadOnlySpan<byte> inputSpan, Stream outputStream, IImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputSpan, outputStream, quality, null, width, height, resampler, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputSpan">Span filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static bool ReduceImageQuality(ReadOnlySpan<byte> inputSpan, Stream outputStream, IImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBase(inputSpan, outputStream, quality, resizeOptions, null, null, null, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    /// Attempt to detect the image type from the file path.
    /// </summary>
    /// <param name="imagePath">Path to the image to detect image type of.</param>
    /// <param name="format">The format of the image if detected, otherwise null.</param>
    /// <returns>True if the image format was successfully read.</returns>
    public static bool TryDetectImageType(string imagePath, out IImageFormat? format)
    {
        format = null;
        try
        {
            if (imagePath.Length < 4)
            {
                return false; // Not enough data to determine format
            }
            using Image image = Image.Load(imagePath);
            format = image.Metadata.DecodedImageFormat;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error detecting image type for file {ImagePath}", imagePath);
        }
        return format != null;
    }

    /// <summary>
    /// Attempt to detect the image type from a stream of the image data.
    /// </summary>
    /// <param name="imageStream">Stream containing the image data to detect image type of.</param>
    /// <param name="format">The format of the image if detected, otherwise null.</param>
    /// <returns>True if the image format was successfully read.</returns>
    public static bool TryDetectImageType(Stream imageStream, out IImageFormat? format)
    {
        format = null;
        try
        {
            if (imageStream.Length < 4)
            {
                return false; // Not enough data to determine format
            }
            using Image image = Image.Load(imageStream);
            format = image.Metadata.DecodedImageFormat;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error detecting image type for stream");
        }

        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        return format != null;
    }

    /// <summary>
    /// Attempt to detect the image type from a stream of the image data.
    /// </summary>
    /// <param name="imageData">Span containing the image data to detect image type of.</param>
    /// <param name="format">The format of the image if detected, otherwise null.</param>
    /// <returns>True if the image format was successfully read.</returns>
    public static bool TryDetectImageType(ReadOnlySpan<byte> imageData, out IImageFormat? format)
    {
        format = null;
        if (imageData.Length < 4)
        {
            return false; // Not enough data to determine format
        }
        using Image image = Image.Load(imageData);
        format = image.Metadata.DecodedImageFormat;
        return format != null;
    }

    /// <summary>
    /// Attempts to read metadata from an image file.
    /// </summary>
    /// <param name="imagePath">Image path for the file to get metadata from.</param>
    /// <param name="metadata">Metadata read from the image.</param>
    /// <returns>True if the metadata was successfully read.</returns>
    public static bool TryGetMetadata(string imagePath, out ImageMetadata metadata)
    {
        metadata = new ImageMetadata();
        try
        {
            if (imagePath.Length < 4)
            {
                return false; // Not enough data to determine format
            }
            using Image image = Image.Load(imagePath);
            metadata = image.Metadata;
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reading metadata from image file {ImagePath}", imagePath);
        }
        return false;
    }

    /// <summary>
    /// Attempts to read metadata from an image file.
    /// </summary>
    /// <param name="imageStream">Stream containing the image data to get metadata from.</param>
    /// <param name="metadata">Metadata read from the image.</param>
    /// <returns>True if the metadata was successfully read.</returns>
    public static bool TryGetMetadata(Stream imageStream, out ImageMetadata metadata)
    {
        metadata = new ImageMetadata();
        try
        {
            if (imageStream.Length < 4)
            {
                return false; // Not enough data to determine format
            }
            using Image image = Image.Load(imageStream);
            metadata = image.Metadata;

            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reading metadata from image stream");
        }
        return false;
    }

    /// <summary>
    /// Attempts to read metadata from an image file.
    /// </summary>
    /// <param name="imageData">Span containing the image data to get metadata from.</param>
    /// <param name="metadata">Metadata read from the image.</param>
    /// <returns>True if the metadata was successfully read.</returns>
    public static bool TryGetMetadata(ReadOnlySpan<byte> imageData, out ImageMetadata metadata)
    {
        metadata = new ImageMetadata();
        try
        {
            if (imageData.Length < 4)
            {
                return false; // Not enough data to determine format
            }
            using Image image = Image.Load(imageData);
            metadata = image.Metadata;
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reading metadata from image data");
        }
        return metadata != null;
    }

    /// <summary>
    /// Converts an image from one format to another.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to re-format.</param>
    /// <param name="outputFilePath">Path to output re-formated image file to.</param>
    /// <param name="outputImageFormat">Image format to convert to</param>
    public static bool ConvertImageFormat(string inputFilePath, string outputFilePath, IImageFormat outputImageFormat)
    {
        try
        {
            using Image image = Image.Load(inputFilePath);
            using FileStream fileStream = new(outputFilePath, FileMode.Create, FileAccess.Write);
            image.Save(fileStream, outputImageFormat);
            fileStream.Flush();
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error converting image format from {InputFilePath} to {OutputFilePath} with output format {OutputImageFormat}", inputFilePath, outputFilePath, outputImageFormat.Name);
        }
        return false;
    }

    /// <summary>
    /// Converts an image from one format to another.
    /// </summary>
    /// <param name="inputStream">Stream containing the image data to re-format.</param>
    /// <param name="outputStream">Stream to output re-formated image file to.</param>
    /// <param name="outputImageFormat">Image format to convert to</param>
    public static bool ConvertImageFormat(Stream inputStream, Stream outputStream, IImageFormat outputImageFormat)
    {
        try
        {
            using Image image = Image.Load(inputStream);
            image.Save(outputStream, outputImageFormat);
            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            if (outputStream.CanSeek)
            {
                outputStream.Position = 0;
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error converting image format from stream to output stream with output format {OutputImageFormat}", outputImageFormat.Name);
        }
        return false;
    }

    /// <summary>
    /// Converts an image from one format to another.
    /// </summary>
    /// <param name="inputStream">Stream containing the image data to re-format.</param>
    /// <param name="outputStream">Stream to output re-formated image file to.</param>
    /// <param name="outputImageFormat">Image format to convert to</param>
    public static bool ConvertImageFormat(ReadOnlySpan<byte> inputStream, Stream outputStream, IImageFormat outputImageFormat)
    {
        try
        {
            using Image image = Image.Load(inputStream);
            image.Save(outputStream, outputImageFormat);

            if (outputStream.CanSeek)
            {
                outputStream.Position = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error converting image format from span to output stream with output format {OutputImageFormat}", outputImageFormat.Name);
        }
        return false;
    }

    #region Async

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Optional: Encoder to use for the resizing operation.</param>
    /// <param name="resampler">Optional: Resampler to use for resizing. If null, defaults to Robidoux resampler.</param>
    public static Task<bool> ResizeImageAsync(string inputFilePath, string outputFilePath, int width, int height, IImageEncoder? imageEncoder = null, IResampler? resampler = null)
    {
        return ResizeImageBaseAsync(inputFilePath, outputFilePath, null, width, height, resampler, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="resizeOptions">Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Optional: Encoder to use for the resizing operation.</param>
    public static Task<bool> ResizeImageAsync(string inputFilePath, string outputFilePath, ResizeOptions resizeOptions, IImageEncoder? imageEncoder = null)
    {
        return ResizeImageBaseAsync(inputFilePath, outputFilePath, resizeOptions, null, null, null, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Encoder to use for the resizing operation.</param>
    /// <param name="resampler">Optional: Resampler to use for resizing. If null, defaults to Robidoux resampler.</param>
    public static Task<bool> ResizeImageAsync(Stream inputStream, Stream outputStream, int width, int height, IImageEncoder imageEncoder, IResampler? resampler = null)
    {
        return ResizeImageBaseAsync(inputStream, outputStream, null, width, height, resampler, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="resizeOptions">Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="imageEncoder">Encoder to use for the resizing operation.</param>
    public static Task<bool> ResizeImageAsync(Stream inputStream, Stream outputStream, ResizeOptions resizeOptions, IImageEncoder imageEncoder)
    {
        return ResizeImageBaseAsync(inputStream, outputStream, resizeOptions, null, null, null, imageEncoder, null);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="width">Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="imageFormat">Output format to use for the resizing operation.</param>
    /// <param name="resampler">Optional: Resampler to use for resizing. If null, defaults to Robidoux resampler.</param>
    public static Task<bool> ResizeImageAsync(Stream inputStream, Stream outputStream, int width, int height, IImageFormat imageFormat, IResampler? resampler = null)
    {
        return ResizeImageBaseAsync(inputStream, outputStream, null, width, height, resampler, null, imageFormat);
    }

    /// <summary>
    /// Resizes an image to the specified width and height.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="resizeOptions">Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="imageFormat">Output format to use for the resizing operation.</param>
    public static Task<bool> ResizeImageAsync(Stream inputStream, Stream outputStream, ResizeOptions resizeOptions, IImageFormat imageFormat)
    {
        return ResizeImageBaseAsync(inputStream, outputStream, resizeOptions, null, null, null, null, imageFormat);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static Task<bool> ReduceImageQualityAsync(string inputFilePath, string outputFilePath, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBaseAsync(inputFilePath, outputFilePath, quality, null, width, height, resampler, null, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static Task<bool> ReduceImageQualityAsync(string inputFilePath, string outputFilePath, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBaseAsync(inputFilePath, outputFilePath, quality, resizeOptions, null, null, null, null, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static Task<bool> ReduceImageQualityAsync(string inputFilePath, string outputFilePath, IImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBaseAsync(inputFilePath, outputFilePath, quality, null, width, height, resampler, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs image of the type specified.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to resize.</param>
    /// <param name="outputFilePath">Path to output resized image file to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static Task<bool> ReduceImageQualityAsync(string inputFilePath, string outputFilePath, IImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBaseAsync(inputFilePath, outputFilePath, quality, resizeOptions, null, null, null, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static Task<bool> ReduceImageQualityAsync(Stream inputStream, Stream outputStream, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBaseAsync(inputStream, outputStream, quality, null, width, height, resampler, null, jpegEncoder);
    }

    /// <summary>
    /// Reduces the quality of an image to the specified quality level and outputs a JPEG encoded image.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static Task<bool> ReduceImageQualityAsync(Stream inputStream, Stream outputStream, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBaseAsync(inputStream, outputStream, quality, resizeOptions, null, null, null, null, jpegEncoder);
    }

    /// <summary>
    ///  Reduces the quality of an image to the specified quality level and outputs image of the type specified.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="width">Optional: Width of resized image. If 0, will scale to height, keeping original aspect ratio.</param>
    /// <param name="height">Optional: Height of resized image. If 0, will scale to width, keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static Task<bool> ReduceImageQualityAsync(Stream inputStream, Stream outputStream, IImageFormat outputImageFormat, int quality = 75, int width = -1, int height = -1, IResampler? resampler = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBaseAsync(inputStream, outputStream, quality, null, width, height, resampler, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    ///  Reduces the quality of an image to the specified quality level and outputs image of the type specified.
    /// </summary>
    /// <param name="inputStream">Stream filled with image file to resize.</param>
    /// <param name="outputStream">Stream to output resized image stream to.</param>
    /// <param name="quality">Optional: Value between 1 and 100 to indicate quality level %. Default is 75</param>
    /// <param name="resizeOptions">Optional: Settings for the resize operation. If width or height is 0, will scale to the non-zero dimension keeping original aspect ratio.</param>
    /// <param name="jpegEncoder">Optional: JPEG encoder to use for this operation. If unpopulated, will create a new JpegEncoder for the conversion</param>
    public static Task<bool> ReduceImageQualityAsync(Stream inputStream, Stream outputStream, IImageFormat outputImageFormat, int quality = 75, ResizeOptions? resizeOptions = null, JpegEncoder? jpegEncoder = null)
    {
        return ReduceImageQualityBaseAsync(inputStream, outputStream, quality, resizeOptions, null, null, null, outputImageFormat, jpegEncoder);
    }

    /// <summary>
    /// Attempt to detect the image type from the file path.
    /// </summary>
    /// <param name="imagePath">Path to the image to detect image type of.</param>
    /// <returns>Image format if the image format was successfully read, otherwise null.</returns>
    public static async Task<IImageFormat?> TryDetectImageTypeAsync(string imagePath)
    {
        IImageFormat? format = null;
        try
        {
            if (imagePath.Length < 4)
            {
                return format; // Not enough data to determine format
            }
            using Image image = await Image.LoadAsync(imagePath).ConfigureAwait(false);
            format = image.Metadata.DecodedImageFormat;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error detecting image type for file {ImagePath}", imagePath);
        }
        return format;
    }

    /// <summary>
    /// Attempt to detect the image type from a stream of the image data.
    /// </summary>
    /// <param name="imageStream">Stream containing the image data to detect image type of.</param>
    /// <returns>Image format if the image format was successfully read, otherwise null.</returns>
    public static async Task<IImageFormat?> TryDetectImageTypeAsync(Stream imageStream)
    {
        IImageFormat? format = null;
        try
        {
            if (imageStream.Length < 4)
            {
                return format; // Not enough data to determine format
            }
            using Image image = await Image.LoadAsync(imageStream).ConfigureAwait(false);
            format = image.Metadata.DecodedImageFormat;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error detecting image type for stream");
        }

        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        return format;
    }

    /// <summary>
    /// Attempts to read metadata from an image file.
    /// </summary>
    /// <param name="imagePath">Image path for the file to get metadata from.</param>
    /// <returns>Metadata was successfully read, otherwise null.</returns>
    public static async Task<ImageMetadata?> TryGetMetadataAsync(string imagePath)
    {
        ImageMetadata? metadata = null;
        try
        {
            if (imagePath.Length < 4)
            {
                return metadata; // Not enough data to determine format
            }
            using Image image = await Image.LoadAsync(imagePath).ConfigureAwait(false);
            metadata = image.Metadata;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reading metadata from image file {ImagePath}", imagePath);
        }
        return metadata;
    }

    /// <summary>
    /// Attempts to read metadata from an image file.
    /// </summary>
    /// <param name="imageStream">Stream containing the image data to get metadata from.</param>
    /// <returns>Metadata was successfully read, otherwise null.</returns>
    public static async Task<ImageMetadata?> TryGetMetadataAsync(Stream imageStream)
    {
        ImageMetadata? metadata = null;
        try
        {
            if (imageStream.Length < 4)
            {
                return metadata; // Not enough data to determine format
            }
            using Image image = await Image.LoadAsync(imageStream).ConfigureAwait(false);
            metadata = image.Metadata;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reading metadata from image stream");
        }

        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        return metadata;
    }

    /// <summary>
    /// Converts an image from one format to another.
    /// </summary>
    /// <param name="inputFilePath">Path to image file to re-format.</param>
    /// <param name="outputFilePath">Path to output re-formated image file to.</param>
    /// <param name="outputImageFormat">Image format to convert to</param>
    public static async Task<bool> ConvertImageFormatAsync(string inputFilePath, string outputFilePath, IImageFormat outputImageFormat)
    {
        try
        {
            using Image image = await Image.LoadAsync(inputFilePath).ConfigureAwait(false);
            await using FileStream fileStream = new(outputFilePath, FileMode.Create, FileAccess.Write);
            await image.SaveAsync(fileStream, outputImageFormat).ConfigureAwait(false);
            await fileStream.FlushAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error converting image format from {InputFilePath} to {OutputFilePath} with output format {OutputImageFormat}", inputFilePath, outputFilePath, outputImageFormat.Name);
        }
        return false;
    }

    /// <summary>
    /// Converts an image from one format to another.
    /// </summary>
    /// <param name="inputStream">Stream containing the image data to re-format.</param>
    /// <param name="outputStream">Stream to output re-formated image file to.</param>
    /// <param name="outputImageFormat">Image format to convert to</param>
    public static async Task<bool> ConvertImageFormatAsync(Stream inputStream, Stream outputStream, IImageFormat outputImageFormat)
    {
        try
        {
            using Image image = await Image.LoadAsync(inputStream).ConfigureAwait(false);
            await image.SaveAsync(outputStream, outputImageFormat);

            if (outputStream.CanSeek)
            {
                outputStream.Position = 0;
            }

            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error converting image format from stream to output stream with output format {OutputImageFormat}", outputImageFormat.Name);
        }
        return false;
    }

    #endregion

}
