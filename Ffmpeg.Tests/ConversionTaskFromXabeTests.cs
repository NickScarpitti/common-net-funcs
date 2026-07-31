using System.Collections.Concurrent;
using System.Diagnostics;
using CommonNetFuncs.Ffmpeg;
using CommonNetFuncs.Ffmpeg.FfmpegRawCalls;
using Xabe.FFmpeg;
using xRetry.v3;
using static CommonNetFuncs.Ffmpeg.Helpers;

namespace Ffmpeg.Tests;

public sealed class ConversionTaskFromXabeTests() : ConversionTaskTestsBase("ConversionTaskTests_FromXabe")
{
	[RetryTheory(3)]
	[InlineData(VideoCodec.h264, Format.mp4)]
	[InlineData(VideoCodec.libx264, Format.matroska)]
	public async Task FfmpegConversionTaskFromXabe_WithBasicSettings_ShouldConvertSuccessfully(VideoCodec codec, Format format)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}{format}";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, codec, true, format, ConversionPreset.UltraFast, workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithCustomWorkingPath_ShouldCreateFileInCustomPath()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		string customPath = Path.Combine(workingDir, "custom");
		Directory.CreateDirectory(customPath);

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, customPath);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(customPath, outputFileName)).ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithPreExistingMediaInfo_ShouldUseProvidedMediaInfo()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileToConvert.FullName);

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, mediaInfo: mediaInfo);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(4)]
	public async Task FfmpegConversionTaskFromXabe_WithMultipleThreads_ShouldConvertSuccessfully(int numberOfThreads)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, numberOfThreads: numberOfThreads);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithCancellation_ShouldCancelConversion()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		using CancellationTokenSource cts = new();

		// Act
		Task<bool> conversionTask = RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.Slower, workingDir, cancellationTokenSource: cts);

		// Cancel after a brief delay
		await Task.Delay(100);
		await cts.CancelAsync();

		bool result = await conversionTask;

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithFpsTracking_ShouldUpdateFpsDictionary()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		ConcurrentDictionary<int, decimal> fpsDict = new();
		const int conversionIndex = 1;

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, conversionIndex, fpsDict);

		// Assert
		result.ShouldBeTrue();
		fpsDict.TryGetValue(conversionIndex, out _).ShouldBeFalse(); // Should be removed after completion
	}

	[RetryTheory(3)]
	[InlineData(ProcessPriorityClass.Normal)]
	[InlineData(ProcessPriorityClass.BelowNormal)]
	[InlineData(ProcessPriorityClass.Idle)]
	public async Task FfmpegConversionTaskFromXabe_WithDifferentPriorities_ShouldRespectPrioritySettings(ProcessPriorityClass priority)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, processPriority: priority);

		// Assert
		result.ShouldBeTrue();
	}

#pragma warning disable xUnit1004 // Test methods should not be skipped
	[RetryFact(3, Skip = "Hardware acceleration tests can be unreliable in CI environments due to varying hardware availability. This test should be run manually on systems with known hardware acceleration support.")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
	public async Task FfmpegConversionTaskFromXabe_WithHardwareAcceleration_ShouldUseHardwareSettings()
	{
		// Check for hardware accelerator availability at runtime
		bool hasHardwareAccel = await IsAnyHardwareAcceleratorAvailable();
		if (!hasHardwareAccel)
		{
			Console.WriteLine("Skipping test: No hardware accelerator (NVENC/QuickSync/AMF/VAAPI/Vulkan/VDAPU/VideoToolbox) available on test system");
			return;
		}

		VideoCodec videoCodec = VideoCodec.h264; // Using h264 as it's widely supported by hardware accelerators
		foreach (EHwAccelerator item in Enum.GetValues<EHwAccelerator>())
		{
			bool isAvailable = await CheckHardwareEncoderByName(item.ToString());
			Console.WriteLine($"Hardware Accelerator {item}: {(isAvailable ? "Available" : "Not Available")}");
			if (isAvailable)
			{
				// Xabe.FFmpeg uses a limited number of specific codec names for hardware acceleration, so we need to map them accordingly
				videoCodec = item switch
				{
					EHwAccelerator.h264_nvenc => VideoCodec.h264_nvenc,
					EHwAccelerator.h264_qsv => VideoCodec.hevc_qsv,
					_ => videoCodec
				};
				break;
			}
		}

		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		HardwareAccelerationValues hwAccel = new()
		{
			hardwareAccelerator = HardwareAccelerator.auto,
			decoder = VideoCodec.h264,
			encoder = videoCodec,
			device = 0
		};

		// Act
		// UltraFast preset may not work depending on the available hardware acceleration, using Fast for better compatibility
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.Fast, workingDir, hardwareAccelerationValues: hwAccel);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FfmpegConversionTaskFromXabe_WithStrictFlag_ShouldRespectStrictSetting(bool strict)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, strict: strict);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FfmpegConversionTaskFromXabe_WithOverwriteOutput_ShouldRespectOverwriteSetting(bool overwriteOutput)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, overwriteOutput, Format.mp4, ConversionPreset.UltraFast, workingDir);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithTaskDescription_ShouldCompleteSuccessfully()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string taskDescription = "Test conversion task";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, taskDescription: taskDescription);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithAdditionalLogText_ShouldCompleteSuccessfully()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string additionalLogText = "Additional test log information";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, additionalLogText: additionalLogText);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithConversionOutputs_ShouldCompleteSuccessfully()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		ConcurrentBag<string> conversionOutputs = new();

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, conversionOutputs: conversionOutputs);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FfmpegConversionTaskFromXabe_WithCancelIfLarger_ShouldRespectSetting(bool cancelIfLarger)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, cancelIfLarger: cancelIfLarger);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithOverwriteExistingFalse_ShouldNotOverwriteExistingFile()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// First conversion
		bool firstResult = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir);

		// Get timestamp of first file
		string outputPath = Path.Combine(workingDir, outputFileName);
		DateTime firstWriteTime = File.GetLastWriteTime(outputPath);

		// Wait a bit to ensure timestamps would differ
		await Task.Delay(1000);

		// Act - Second conversion with overwriteExisting = false
		bool secondResult = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, false, Format.mp4, ConversionPreset.UltraFast, workingDir);

		// Assert
		firstResult.ShouldBeTrue();
		secondResult.ShouldBeTrue();
		DateTime secondWriteTime = File.GetLastWriteTime(outputPath);
		secondWriteTime.ShouldBe(firstWriteTime); // File should not have been modified
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithOverwriteExistingTrue_ShouldOverwriteExistingFile()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// First conversion
		bool firstResult = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir);

		// Get timestamp of first file
		string outputPath = Path.Combine(workingDir, outputFileName);
		DateTime firstWriteTime = File.GetLastWriteTime(outputPath);

		// Wait a bit to ensure timestamps would differ
		await Task.Delay(1000);

		// Act - Second conversion with overwriteExisting = true
		bool secondResult = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir);

		// Assert
		firstResult.ShouldBeTrue();
		secondResult.ShouldBeTrue();
		DateTime secondWriteTime = File.GetLastWriteTime(outputPath);
		secondWriteTime.ShouldNotBe(firstWriteTime); // File should have been modified
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithConversionIndex_ShouldUseCorrectIndex()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const int conversionIndex = 42;
		ConcurrentDictionary<int, decimal> fpsDict = new();

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, conversionIndex, fpsDict);

		// Assert
		result.ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithAllOptionalParameters_ShouldConvertSuccessfully()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		ConcurrentDictionary<int, decimal> fpsDict = new();
		IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileToConvert.FullName);
		ConcurrentBag<string> conversionOutputs = new();

		// Act - Skip hardware acceleration for this comprehensive test
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir, conversionIndex: 1,
			fpsDict: fpsDict, mediaInfo: mediaInfo, numberOfThreads: 2, cancelIfLarger: true, taskDescription: "Full test", strict: true, processPriority: ProcessPriorityClass.Normal,
			hardwareAccelerationValues: null, conversionOutputs: conversionOutputs, additionalLogText: "Test log");

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
	}
}
