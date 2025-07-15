using System.Numerics;
using CommonNetFuncs.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Images.Tests;

public class ManipulationTests : IDisposable
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
            if (disposing) { }
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

    [Theory]
    [InlineData("test.jpg", 100, 100)]
    [InlineData("test.jpeg", 75, 75)]
    [InlineData("test.png", 50, 50)]
    [InlineData("test.gif", 25, 25)]
    [InlineData("test.tiff", 33, 33)]
    [InlineData("test.bmp", 10, 10)]
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
            using Image img = Image.Load(outputPath);
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

    [Theory]
    [InlineData("test.jpg", 100, 100)]
    [InlineData("test.jpeg", 75, 75)]
    [InlineData("test.png", 50, 50)]
    [InlineData("test.gif", 25, 25)]
    [InlineData("test.tiff", 33, 33)]
    [InlineData("test.bmp", 10, 10)]
    public void ResizeImage_Stream_Succeeds(string fileName, int width, int height)
    {
        // Arrange
        using Stream input = GetTestImageStream(fileName);
        using MemoryStream output = new();

        // Act
        bool result = Manipulation.ResizeImage(input, output, width, height, new JpegEncoder());

        // Assert
        result.ShouldBeTrue();
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Width.ShouldBe(width);
        img.Height.ShouldBe(height);
    }

    [Theory]
    [InlineData("test.jpg", 100, 100)]
    [InlineData("test.jpeg", 75, 75)]
    [InlineData("test.png", 50, 50)]
    [InlineData("test.gif", 25, 25)]
    [InlineData("test.tiff", 33, 33)]
    [InlineData("test.bmp", 10, 10)]
    public void ResizeImage_Span_Succeeds(string fileName, int width, int height)
    {
        // Arrange
        byte[] bytes = GetTestImageBytes(fileName);
        using MemoryStream output = new();

        // Act
        bool result = Manipulation.ResizeImage(bytes, output, width, height, new JpegEncoder());

        // Assert
        result.ShouldBeTrue();
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Width.ShouldBe(width);
        img.Height.ShouldBe(height);
    }

    [Theory]
    [InlineData("test.jpg", 100)]
    [InlineData("test.jpeg", 75)]
    [InlineData("test.png", 50)]
    [InlineData("test.gif", 25)]
    [InlineData("test.tiff", 33)]
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
            using Image img = Image.Load(outputPath);
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

    [Theory]
    [InlineData("test.jpg", 100)]
    [InlineData("test.jpeg", 75)]
    [InlineData("test.png", 50)]
    [InlineData("test.gif", 25)]
    [InlineData("test.tiff", 33)]
    [InlineData("test.bmp", 10)]
    public void ReduceImageQuality_Stream_Succeeds(string fileName, int quality)
    {
        // Arrange
        using Stream input = GetTestImageStream(fileName);
        using MemoryStream output = new();

        // Act
        bool result = Manipulation.ReduceImageQuality(input, output, quality, null);

        // Assert
        result.ShouldBeTrue();
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);
    }

    [Theory]
    [InlineData("test.jpg", 100)]
    [InlineData("test.jpeg", 75)]
    [InlineData("test.png", 50)]
    [InlineData("test.gif", 25)]
    [InlineData("test.tiff", 33)]
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
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);
    }

    [Theory]
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
    public void ConvertImageFormat_FilePath_Succeeds(string fileName, string outExt)
    {
        // Arrange
        string inputPath = GetTestImagePath(fileName);
        string outputPath = GetTempFilePath(outExt);

        try
        {
            IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

            // Act
            bool result = Manipulation.ConvertImageFormat(inputPath, outputPath, format);

            // Assert
            result.ShouldBeTrue();
            File.Exists(outputPath).ShouldBeTrue();
            using Image img = Image.Load(outputPath);
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

    [Theory]
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
    public void ConvertImageFormat_Stream_Succeeds(string fileName, string outExt)
    {
        // Arrange
        using Stream input = GetTestImageStream(fileName);
        using MemoryStream output = new();
        IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

        // Act
        bool result = Manipulation.ConvertImageFormat(input, output, format);

        // Assert
        result.ShouldBeTrue();
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(format);
    }

    [Theory]
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
    public void ConvertImageFormat_Span_Succeeds(string fileName, string outExt)
    {
        // Arrange
        byte[] bytes = GetTestImageBytes(fileName);
        using MemoryStream output = new();
        IImageFormat format = Manipulation.GetImageFormatByExtension(outExt);

        // Act
        bool result = Manipulation.ConvertImageFormat(bytes, output, format);

        // Assert
        result.ShouldBeTrue();
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(format);
    }

    [Theory]
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

    [Theory]
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

    [Theory]
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

    [Theory]
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

    [Theory]
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

    [Theory]
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

    [Theory]
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

    [Theory]
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

    [Theory]
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

    [Theory]
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

    [Theory]
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
            using Image img = Image.Load(outputPath);
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

    [Theory]
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
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(format);
    }

    [Theory]
    [InlineData("test.jpg", 0, 0)]
    [InlineData("test.png", -1, -1)]
    [InlineData("test.gif", -100, -100)]
    public void ResizeImage_InvalidParams_Throws(string fileName, int width, int height)
    {
        // Arrange
        string inputPath = GetTestImagePath(fileName);
        string outputPath = GetTempFilePath(".jpg");

        // Act
        bool result = Manipulation.ResizeImageBase(inputPath, outputPath, null, width, height, null, null, null);

        //Assert
        result.ShouldBeFalse();
    }

    [Theory]
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
        Should.Throw<ArgumentException>(() => Manipulation.ReduceImageQualityBase(inputPath, outputPath, quality, null, null, null, null, null, null, null));
    }

    [Fact]
    public void TryDetectImageType_FilePath_TooShort_ReturnsFalse()
    {
        // Act
        bool result = Manipulation.TryDetectImageType("a", out IImageFormat? format);

        // Assert
        result.ShouldBeFalse();
        format.ShouldBeNull();
    }

    [Fact]
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

    [Fact]
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

    [Fact]
    public void TryGetMetadata_FilePath_TooShort_ReturnsFalse()
    {
        // Act
        bool result = Manipulation.TryGetMetadata("a", out ImageMetadata? metadata);

        // Assert
        result.ShouldBeFalse();
        metadata.ShouldNotBeNull();
    }

    [Fact]
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

    [Fact]
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

    [Theory]
    [InlineData("test.jpg", 120, 80)]
    [InlineData("test.jpeg", 75, 40)]
    [InlineData("test.png", 50, 25)]
    [InlineData("test.gif", 25, 13)]
    [InlineData("test.tiff", 33, 15)]
    [InlineData("test.bmp", 10, 5)]
    public void ResizeImage_FilePath_WithResizeOptions_Succeeds(string fileName, int width, int height)
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
            bool result = Manipulation.ResizeImage(inputPath, outputPath, options);

            // Assert
            result.ShouldBeTrue();
            File.Exists(outputPath).ShouldBeTrue();
            using Image img = Image.Load(outputPath);
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

    [Theory]
    [InlineData("test.jpg", 120, 80)]
    [InlineData("test.jpeg", 75, 40)]
    [InlineData("test.png", 50, 25)]
    [InlineData("test.gif", 25, 13)]
    [InlineData("test.tiff", 33, 15)]
    [InlineData("test.bmp", 10, 5)]
    public void ResizeImage_Stream_WithResizeOptions_Succeeds(string fileName, int width, int height)
    {
        // Arrange
        using Stream input = GetTestImageStream(fileName);
        using MemoryStream output = new();
        ResizeOptions options = new()
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max
        };

        // Act
        bool result = Manipulation.ResizeImage(input, output, options, new JpegEncoder());

        // Assert
        result.ShouldBeTrue();
        output.Position.ShouldBe(0);
        using Image img = Image.Load(output);
        img.Width.ShouldBeLessThanOrEqualTo(width);
        img.Height.ShouldBeLessThanOrEqualTo(height);
    }

    [Theory]
    [InlineData("test.jpg", 120, 80)]
    [InlineData("test.jpeg", 75, 40)]
    [InlineData("test.png", 50, 25)]
    [InlineData("test.gif", 25, 13)]
    [InlineData("test.tiff", 33, 15)]
    [InlineData("test.bmp", 10, 5)]
    public void ResizeImage_Span_WithResizeOptions_Succeeds(string fileName, int width, int height)
    {
        // Arrange
        byte[] bytes = GetTestImageBytes(fileName);
        using MemoryStream output = new();
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
        using Image img = Image.Load(output);
        img.Width.ShouldBeLessThanOrEqualTo(width);
        img.Height.ShouldBeLessThanOrEqualTo(height);
    }

    [Theory]
    [InlineData("test.jpg", 100)]
    [InlineData("test.jpeg", 75)]
    [InlineData("test.png", 50)]
    [InlineData("test.gif", 25)]
    [InlineData("test.tiff", 33)]
    [InlineData("test.bmp", 10)]
    public void ReduceImageQuality_FilePath_ToPng_Succeeds(string fileName, int quality)
    {
        // Arrange
        string inputPath = GetTestImagePath(fileName);
        string outputPath = GetTempFilePath(".png");

        try
        {
            // Act
            bool result = Manipulation.ReduceImageQuality(inputPath, outputPath, PngFormat.Instance, quality, null);

            // Assert
            result.ShouldBeTrue();
            File.Exists(outputPath).ShouldBeTrue();
            using Image img = Image.Load(outputPath);
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

    [Theory]
    [InlineData("test.jpg", 100)]
    [InlineData("test.jpeg", 75)]
    [InlineData("test.png", 50)]
    [InlineData("test.gif", 25)]
    [InlineData("test.tiff", 33)]
    [InlineData("test.bmp", 10)]
    public void ReduceImageQuality_Stream_ToPng_Succeeds(string fileName, int quality)
    {
        // Arrange
        using Stream input = GetTestImageStream(fileName);
        using MemoryStream output = new();

        // Act
        bool result = Manipulation.ReduceImageQuality(input, output, PngFormat.Instance, quality, null);

        // Assert
        result.ShouldBeTrue();
        output.Position.ShouldBe(0);
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
    }

    [Theory]
    [InlineData("test.jpg", 100)]
    [InlineData("test.jpeg", 75)]
    [InlineData("test.png", 50)]
    [InlineData("test.gif", 25)]
    [InlineData("test.tiff", 33)]
    [InlineData("test.bmp", 10)]
    public void ReduceImageQuality_Span_ToPng_Succeeds(string fileName, int quality)
    {
        // Arrange
        byte[] bytes = GetTestImageBytes(fileName);
        using MemoryStream output = new();

        // Act
        bool result = Manipulation.ReduceImageQuality(bytes, output, PngFormat.Instance, quality, null);

        // Assert
        result.ShouldBeTrue();
        output.Position.ShouldBe(0);
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
    }

    [Theory]
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
            using Image img = Image.Load(outputPath);
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

    [Theory]
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
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(PngFormat.Instance);
    }

    [Theory]
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
            using Image img = Image.Load(outputPath);
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

    [Theory]
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
        using Image img = Image.Load(output);
        img.Width.ShouldBeLessThanOrEqualTo(width);
        img.Height.ShouldBeLessThanOrEqualTo(height);
    }

    [Theory]
    [InlineData("test.jpg", 100, 100)]
    [InlineData("test.jpeg", 75, 75)]
    [InlineData("test.png", 50, 50)]
    [InlineData("test.gif", 25, 25)]
    [InlineData("test.tiff", 33, 33)]
    [InlineData("test.bmp", 10, 10)]
    public void ResizeImage_FilePath_Mutate_Succeeds(string fileName, int width, int height)
    {
        // Arrange
        string inputPath = GetTestImagePath(fileName);
        string outputPath = GetTempFilePath(".jpg");

        try
        {
            // Act
            bool result = Manipulation.ResizeImage(inputPath, outputPath, width, height, mutate: InvertMutate);

            // Assert
            result.ShouldBeTrue();
            File.Exists(outputPath).ShouldBeTrue();
            using Image img = Image.Load(outputPath);
            img.Width.ShouldBe(width);
            img.Height.ShouldBe(height);

            File.Delete(outputPath);

            // Now check if the image is inverted
            Manipulation.ResizeImage(inputPath, outputPath, width, height);

            // Spot check: pixel [0,0] should be inverted from original
            using Image<Rgb24> orig = Image.Load<Rgb24>(outputPath);
            using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();

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

    [Theory]
    [InlineData("test.jpg", 100, 100)]
    [InlineData("test.jpeg", 75, 75)]
    [InlineData("test.png", 50, 50)]
    [InlineData("test.gif", 25, 25)]
    [InlineData("test.tiff", 33, 33)]
    [InlineData("test.bmp", 10, 10)]
    public void ResizeImage_Stream_Mutate_Succeeds(string fileName, int width, int height)
    {
        // Arrange
        using MemoryStream input = GetTestImageStream(fileName);
        using MemoryStream output = new();
        using MemoryStream nonInvertedOutput = new();

        // Act
        bool result = Manipulation.ResizeImage(input, output, width, height, new JpegEncoder(), mutate: InvertMutate);
        Manipulation.ResizeImage(input, nonInvertedOutput, width, height, new JpegEncoder());

        // Assert
        result.ShouldBeTrue();
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Width.ShouldBe(width);
        img.Height.ShouldBe(height);

        using Image<Rgb24> orig = Image.Load<Rgb24>(nonInvertedOutput);
        using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();

        bool isInverted = IsInvertedVersion(orig, imgClone);

        isInverted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("test.jpg", 100, 100)]
    [InlineData("test.jpeg", 75, 75)]
    [InlineData("test.png", 50, 50)]
    [InlineData("test.gif", 25, 25)]
    [InlineData("test.tiff", 33, 33)]
    [InlineData("test.bmp", 10, 10)]
    public void ResizeImage_Span_Mutate_Succeeds(string fileName, int width, int height)
    {
        // Arrange
        byte[] bytes = GetTestImageBytes(fileName);
        using MemoryStream output = new();
        using MemoryStream nonInvertedOutput = new();

        // Act
        bool result = Manipulation.ResizeImage(bytes, output, width, height, new JpegEncoder(), mutate: InvertMutate);
        Manipulation.ResizeImage(bytes, nonInvertedOutput, width, height, new JpegEncoder());

        // Assert
        result.ShouldBeTrue();
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Width.ShouldBe(width);
        img.Height.ShouldBe(height);

        using Image<Rgb24> orig = Image.Load<Rgb24>(nonInvertedOutput);
        using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();

        bool isInverted = IsInvertedVersion(orig, imgClone);

        isInverted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("test.jpg", 100)]
    [InlineData("test.jpeg", 75)]
    [InlineData("test.png", 50)]
    [InlineData("test.gif", 25)]
    [InlineData("test.tiff", 33)]
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
            using Image img = Image.Load(outputPath);
            img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

            using Image orig = Image.Load(inputPath);
            using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
            using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();

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

    [Theory]
    [InlineData("test.jpg", 100)]
    [InlineData("test.jpeg", 75)]
    [InlineData("test.png", 50)]
    [InlineData("test.gif", 25)]
    [InlineData("test.tiff", 33)]
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
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

        using Image orig = Image.Load(input);
        using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
        using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();

        bool isInverted = IsInvertedVersion(origClone, imgClone);
        isInverted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("test.jpg", 100)]
    [InlineData("test.jpeg", 75)]
    [InlineData("test.png", 50)]
    [InlineData("test.gif", 25)]
    [InlineData("test.tiff", 33)]
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
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

        using Image orig = Image.Load(bytes);
        using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
        using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();

        bool isInverted = IsInvertedVersion(origClone, imgClone);
        isInverted.ShouldBeTrue();
    }

    [Theory]
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
            using Image img = Image.Load(invertedOutputPath);
            img.Metadata.DecodedImageFormat.ShouldBe(format);

            File.Delete(outputPath);

            // Now check if the image is inverted
            await Manipulation.ConvertImageFormatAsync(inputPath, outputPath, format);

            // Spot check: pixel [0,0] should be inverted from original
            using Image<Rgb24> orig = Image.Load<Rgb24>(outputPath);
            using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();

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

    [Theory]
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
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(format);

        using Image orig = Image.Load(input);
        using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
        using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();

        bool isInverted = IsInvertedVersion(origClone, imgClone);
        isInverted.ShouldBeTrue();
    }

    [Theory]
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
            using Image img = Image.Load(outputPath);
            img.Width.ShouldBe(width);
            img.Height.ShouldBe(height);

            File.Delete(outputPath);

            // Now check if the image is inverted
            Manipulation.ResizeImage(inputPath, outputPath, width, height);

            // Spot check: pixel [0,0] should be inverted from original
            using Image<Rgb24> orig = Image.Load<Rgb24>(outputPath);
            using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();

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

    [Theory]
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
        Manipulation.ResizeImage(input, nonInvertedOutput, width, height, new JpegEncoder());

        // Assert
        result.ShouldBeTrue();
        output.Position = 0;
        using Image img = Image.Load(output);
        img.Width.ShouldBe(width);
        img.Height.ShouldBe(height);

        using Image<Rgb24> orig = Image.Load<Rgb24>(nonInvertedOutput);
        using Image<Rgb24> imgClone = img.CloneAs<Rgb24>();

        bool isInverted = IsInvertedVersion(orig, imgClone);

        isInverted.ShouldBeTrue();
    }

    [Theory]
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
            using Image img = Image.Load(outputPath);
            img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

            using Image orig = Image.Load(inputPath);
            using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
            using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();

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

    [Theory]
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
        using Image img = Image.Load(output);
        img.Metadata.DecodedImageFormat.ShouldBe(JpegFormat.Instance);

        using Image orig = Image.Load(input);
        using Image<Rgba32> origClone = orig.CloneAs<Rgba32>();
        using Image<Rgba32> imgClone = img.CloneAs<Rgba32>();

        bool isInverted = IsInvertedVersion(origClone, imgClone);
        isInverted.ShouldBeTrue();
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
}
