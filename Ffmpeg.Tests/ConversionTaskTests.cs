using System.Collections.Concurrent;
using System.Diagnostics;
using CommonNetFuncs.Ffmpeg;
using CommonNetFuncs.Ffmpeg.FfmpegRawCalls;
using Xabe.FFmpeg;
using xRetry.v3;

namespace Ffmpeg.Tests;

public sealed class ConversionTaskTests() : ConversionTaskTestsBase("ConversionTaskTests")
{
	[RetryTheory(3)]
	[InlineData(VideoCodec.h264, Format.mp4)]
	[InlineData(VideoCodec.hevc, Format.matroska)]
	public async Task FfmpegConversionTask_WithBasicSettings_ShouldConvertSuccessfully(VideoCodec codec, Format format)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}{format}";

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, codec, format, ConversionPreset.UltraFast, workingPath: workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTask_WithCustomCommand_ShouldExecuteCommand()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset medium -crf 50";

		// Act
		//bool result = await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, true, workingDir);
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithCustomCommand_ShouldExecuteCommand()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset medium -crf 50";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, true, workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData(ProcessPriorityClass.Normal)]
	[InlineData(ProcessPriorityClass.BelowNormal)]
	[InlineData(ProcessPriorityClass.Idle)]
	public async Task FfmpegConversionTask_WithDifferentPriorities_ShouldRespectPrioritySettings(ProcessPriorityClass priority)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: workingDir, processPriority: priority);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTask_WithCancellation_ShouldCancelConversion()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		using CancellationTokenSource cts = new();

		// Act
		Task<bool> conversionTask = ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: workingDir, cancellationTokenSource: cts);

		// Cancel after a brief delay
		await Task.Delay(100);
		await cts.CancelAsync();

		bool result = await conversionTask;

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTask_WithHardwareAcceleration_ShouldUseHardwareSettings()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		HardwareAccelerationValues hwAccel = new()
		{
			hardwareAccelerator = HardwareAccelerator.auto,
			decoder = VideoCodec.h264,
			encoder = VideoCodec.h264_nvenc,
			device = 0
		};

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: workingDir, hardwareAccelerationValues: hwAccel);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTask_WithFpsTracking_ShouldUpdateFpsDictionary()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		ConcurrentDictionary<int, decimal> fpsDict = new();
		const int conversionIndex = 1;

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: workingDir, conversionIndex: conversionIndex, fpsDict: fpsDict);

		// Assert
		result.ShouldBeTrue();
		fpsDict.TryGetValue(conversionIndex, out _).ShouldBeFalse(); // Should be removed after completion
	}

	[RetryFact(3)]
	public void FfmpegConversionTask_WithInvalidInput_ShouldHandleError()
	{
		// Arrange
		FileInfo fileToConvert = new("nonexistent.mp4");
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act & Assert
		Should.Throw<ArgumentException>(async () => await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: workingDir));
	}

	[RetryTheory(3)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FfmpegConversionTask_WithStrictFlag_ShouldRespectStrictSetting(bool strict)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await ConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, VideoCodec.h264, conversionPreset: ConversionPreset.UltraFast, workingPath: workingDir, strict: strict);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
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
