using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Media.Ffmpeg;
using CommonNetFuncs.Media.Ffmpeg.FfmpegRawCalls;
using Xabe.FFmpeg;

namespace Media.Ffmpeg.Tests;

public sealed class ConversionTaskTests : IDisposable
{
	private readonly Fixture _fixture;
	private readonly string _testVideoPath;
	private readonly string _workingDir;

	public ConversionTaskTests()
	{
		_fixture = new Fixture();
		_fixture.Customize(new AutoFakeItEasyCustomization());

		// Setup test paths
		string testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
		_testVideoPath = Path.Combine(testDataDir, "test.mp4");
		_workingDir = Path.Combine(Path.GetTempPath(), "ConversionTaskTests");

		// Ensure working directory exists
		Directory.CreateDirectory(_workingDir);

		// Ensure FFmpeg executables path is set for tests
		//FFmpeg.SetExecutablesPath(AppContext.BaseDirectory);
		FFmpeg.SetExecutablesPath(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/bin" : "C:\\Program Files\\ffmpeg\\bin");
	}

	private bool disposed;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				// Cleanup temporary files after tests
				Thread.Sleep(5000);
				if (Directory.Exists(_workingDir))
				{
					try
					{
						Directory.Delete(_workingDir, true);
					}
					catch (IOException ioex)
					{
						Console.WriteLine(ioex);
					}
				}
			}
			disposed = true;
		}
	}

	~ConversionTaskTests()
	{
		Dispose(false);
	}

	[Theory]
	[InlineData(VideoCodec.h264, Format.mp4)]
	[InlineData(VideoCodec.hevc, Format.matroska)]
	public async Task FfmpegConversionTask_WithBasicSettings_ShouldConvertSuccessfully(VideoCodec codec, Format format)
	{
		// Arrange
		FileInfo fileToConvert = new(_testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}{format}";

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, codec, format, ConversionPreset.UltraFast, workingPath: _workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(_workingDir, outputFileName)).ShouldBeTrue();
	}

	[Fact]
	public async Task FfmpegConversionTask_WithCustomCommand_ShouldExecuteCommand()
	{
		// Arrange
		FileInfo fileToConvert = new(_testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset medium -crf 50";

		// Act
		//bool result = await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, true, _workingDir);
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, _workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(_workingDir, outputFileName)).ShouldBeTrue();
	}

	[Fact]
	public async Task RawFfmpegConversionTask_WithCustomCommand_ShouldExecuteCommand()
	{
		// Arrange
		FileInfo fileToConvert = new(_testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset medium -crf 50";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, true, _workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(_workingDir, outputFileName)).ShouldBeTrue();
	}

	[Theory]
	[InlineData(ProcessPriorityClass.Normal)]
	[InlineData(ProcessPriorityClass.BelowNormal)]
	[InlineData(ProcessPriorityClass.Idle)]
	public async Task FfmpegConversionTask_WithDifferentPriorities_ShouldRespectPrioritySettings(ProcessPriorityClass priority)
	{
		// Arrange
		FileInfo fileToConvert = new(_testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: _workingDir, processPriority: priority);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task FfmpegConversionTask_WithCancellation_ShouldCancelConversion()
	{
		// Arrange
		FileInfo fileToConvert = new(_testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		CancellationTokenSource cts = new();

		// Act
		Task<bool> conversionTask = ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: _workingDir, cancellationTokenSource: cts);

		// Cancel after a brief delay
		await Task.Delay(100);
		await cts.CancelAsync();

		bool result = await conversionTask;

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task FfmpegConversionTask_WithHardwareAcceleration_ShouldUseHardwareSettings()
	{
		// Arrange
		FileInfo fileToConvert = new(_testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		HardwareAccelerationValues hwAccel = new()
		{
			hardwareAccelerator = HardwareAccelerator.auto,
			decoder = VideoCodec.h264,
			encoder = VideoCodec.h264_nvenc,
			device = 0
		};

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: _workingDir, hardwareAccelerationValues: hwAccel);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task FfmpegConversionTask_WithFpsTracking_ShouldUpdateFpsDictionary()
	{
		// Arrange
		FileInfo fileToConvert = new(_testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		ConcurrentDictionary<int, decimal> fpsDict = new();
		const int conversionIndex = 1;

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: _workingDir, conversionIndex: conversionIndex, fpsDict: fpsDict);

		// Assert
		result.ShouldBeTrue();
		fpsDict.TryGetValue(conversionIndex, out _).ShouldBeFalse(); // Should be removed after completion
	}

	[Fact]
	public void FfmpegConversionTask_WithInvalidInput_ShouldHandleError()
	{
		// Arrange
		FileInfo fileToConvert = new("nonexistent.mp4");
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act & Assert
		Should.Throw<ArgumentException>(async () => await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: _workingDir));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FfmpegConversionTask_WithStrictFlag_ShouldRespectStrictSetting(bool strict)
	{
		// Arrange
		FileInfo fileToConvert = new(_testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: _workingDir, strict: strict);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void HardwareAccelerationValues_ShouldHaveDefaultValues()
	{
		// Arrange & Act
		HardwareAccelerationValues values = new();

		// Assert
		values.hardwareAccelerator.ShouldBe(default);
		values.decoder.ShouldBe(default);
		values.encoder.ShouldBe(default);
		values.device.ShouldBe(default);
	}
}
