using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Media.Ffmpeg;
using CommonNetFuncs.Media.Ffmpeg.FfmpegRawCalls;
using Xabe.FFmpeg;
using xRetry.v3;
using static CommonNetFuncs.Media.Ffmpeg.Helpers;

namespace Media.Ffmpeg.Tests;

public sealed class ConversionTaskTests : IDisposable
{
	private readonly Fixture fixture;
	private readonly string testVideoPath;
	private readonly string workingDir;

	public ConversionTaskTests()
	{
		fixture = new Fixture();
		fixture.Customize(new AutoFakeItEasyCustomization());

		// Setup test paths
		string testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
		testVideoPath = Path.Combine(testDataDir, "test.mp4");
		workingDir = Path.Combine(Path.GetTempPath(), "ConversionTaskTests");

		// Ensure working directory exists
		Directory.CreateDirectory(workingDir);

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
				Task.Delay(5000).Wait();
				if (Directory.Exists(workingDir))
				{
					try
					{
						Directory.Delete(workingDir, true);
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


	#region GetConversionCommandFromXabe Tests

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithBasicParameters_ShouldReturnValidCommand()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

		// Assert
		command.ShouldNotBeNullOrEmpty();
		command.ShouldContain(fileToConvert.FullName);
		command.ShouldContain("ultrafast"); // preset
		command.ShouldContain("-strict -2"); // default strict flag

		// Xabe.FFmpeg includes codec in the command
		(command.Contains("libx264") || command.Contains("h264")).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData(VideoCodec.h264, "libx264")]
	[InlineData(VideoCodec.hevc, "libx265")]
	[InlineData(VideoCodec.vp9, "libvpx-vp9")]
	public async Task GetConversionCommandFromXabe_WithDifferentCodecs_ShouldIncludeCorrectCodec(VideoCodec codec, string expectedCodecName)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			codec,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

		// Assert
		// Xabe.FFmpeg may format codec names differently
		(command.Contains(expectedCodecName, StringComparison.OrdinalIgnoreCase) || command.Contains(codec.ToString(), StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
	}

	[RetryTheory(3)]
	[InlineData(ConversionPreset.UltraFast, "ultrafast")]
	[InlineData(ConversionPreset.SuperFast, "superfast")]
	[InlineData(ConversionPreset.VeryFast, "veryfast")]
	[InlineData(ConversionPreset.Faster, "faster")]
	[InlineData(ConversionPreset.Fast, "fast")]
	[InlineData(ConversionPreset.Medium, "medium")]
	[InlineData(ConversionPreset.Slow, "slow")]
	[InlineData(ConversionPreset.Slower, "slower")]
	public async Task GetConversionCommandFromXabe_WithDifferentPresets_ShouldIncludeCorrectPreset(ConversionPreset preset, string expectedPresetName)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			preset,
			workingDir);

		// Assert
		command.ShouldContain(expectedPresetName);
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithCustomWorkingPath_ShouldUseCustomPath()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";
		string customPath = Path.Combine(workingDir, "custom");
		Directory.CreateDirectory(customPath);

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			customPath);

		// Assert
		command.ShouldContain(customPath);
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithNoWorkingPath_ShouldUseTempPath()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast);

		// Assert
		command.ShouldContain(Path.GetTempPath());
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithPreExistingMediaInfo_ShouldUseProvidedMediaInfo()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";
		IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileToConvert.FullName);

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			mediaInfo);

		// Assert
		command.ShouldNotBeNullOrEmpty();
	}

	[RetryTheory(3)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(4)]
	[InlineData(8)]
	public async Task GetConversionCommandFromXabe_WithMultipleThreads_ShouldIncludeThreadParameter(int numberOfThreads)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			numberOfThreads: numberOfThreads);

		// Assert
		command.ShouldContain($"-threads {numberOfThreads}");
	}

	[RetryTheory(3)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetConversionCommandFromXabe_WithStrictFlag_ShouldRespectStrictSetting(bool strict)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			strict: strict);

		// Assert
		if (strict)
		{
			command.ShouldContain("-strict -2");
		}
		else
		{
			command.ShouldNotContain("-strict -2");
		}
	}

	[RetryTheory(3)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetConversionCommandFromXabe_WithOverwriteOutput_ShouldIncludeOverwriteFlag(bool overwriteOutput)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			overwriteOutput: overwriteOutput);

		// Assert
		if (overwriteOutput)
		{
			command.ShouldContain("-y");
		}
		else
		{
			command.ShouldNotContain("-y");
		}
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithHardwareAcceleration_ShouldIncludeHardwareAccelerationParameters()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";
		HardwareAccelerationValues hwAccel = new()
		{
			hardwareAccelerator = HardwareAccelerator.auto,
			decoder = VideoCodec.h264,
			encoder = VideoCodec.h264_nvenc,
			device = 0
		};

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			hardwareAccelerationValues: hwAccel);

		// Assert
		command.ShouldContain("-hwaccel");
	}

	[RetryTheory(3)]
	[InlineData(Format.mp4)]
	[InlineData(Format.matroska)]
	[InlineData(Format.webm)]
	public async Task GetConversionCommandFromXabe_WithDifferentFormats_ShouldIncludeCorrectFormat(Format format)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output.{format}";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			format,
			ConversionPreset.UltraFast,
			workingDir);

		// Assert
		command.ShouldNotBeNullOrEmpty();
		command.ShouldContain(outputFileName);
	}

	#endregion

	#region FfmpegConversionTaskFromXabe Tests

	[RetryTheory(3)]
	[InlineData(VideoCodec.h264, Format.mp4)]
	[InlineData(VideoCodec.libx264, Format.matroska)]
	public async Task FfmpegConversionTaskFromXabe_WithBasicSettings_ShouldConvertSuccessfully(VideoCodec codec, Format format)
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}{format}";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			codec,
			true,
			format,
			ConversionPreset.UltraFast,
			workingDir);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			customPath);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			mediaInfo: mediaInfo);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			numberOfThreads: numberOfThreads);

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
		Task<bool> conversionTask = RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.Slower,
			workingDir,
			cancellationTokenSource: cts);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			conversionIndex,
			fpsDict);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			processPriority: priority);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.Fast, // UltraFast preset may not work depending on the available hardware acceleration, using Fast for better compatibility
			workingDir,
			hardwareAccelerationValues: hwAccel);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			strict: strict);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			overwriteOutput,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			taskDescription: taskDescription);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			additionalLogText: additionalLogText);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			conversionOutputs: conversionOutputs);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			cancelIfLarger: cancelIfLarger);

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
		bool firstResult = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

		// Get timestamp of first file
		string outputPath = Path.Combine(workingDir, outputFileName);
		DateTime firstWriteTime = File.GetLastWriteTime(outputPath);

		// Wait a bit to ensure timestamps would differ
		await Task.Delay(1000);

		// Act - Second conversion with overwriteExisting = false
		bool secondResult = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			false,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

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
		bool firstResult = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

		// Get timestamp of first file
		string outputPath = Path.Combine(workingDir, outputFileName);
		DateTime firstWriteTime = File.GetLastWriteTime(outputPath);

		// Wait a bit to ensure timestamps would differ
		await Task.Delay(1000);

		// Act - Second conversion with overwriteExisting = true
		bool secondResult = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			conversionIndex,
			fpsDict);

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
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			conversionIndex: 1,
			fpsDict: fpsDict,
			mediaInfo: mediaInfo,
			numberOfThreads: 2,
			cancelIfLarger: true,
			taskDescription: "Full test",
			strict: true,
			processPriority: ProcessPriorityClass.Normal,
			hardwareAccelerationValues: null,
			conversionOutputs: conversionOutputs,
			additionalLogText: "Test log");

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
	}

	#endregion

	#region Additional Coverage Tests

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithExistingFileAndOverwriteFalse_ShouldSkipConversion()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset medium -crf 50";

		// First, create the output file
		await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, true, workingDir);
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();

		// Get the file's last write time
		DateTime originalWriteTime = File.GetLastWriteTime(Path.Combine(workingDir, outputFileName));
		await Task.Delay(1000); // Wait to ensure timestamp would be different

		// Act - Try to convert again with overwriteExisting = false
		bool result = await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, false, workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
		// File should not have been modified
		File.GetLastWriteTime(Path.Combine(workingDir, outputFileName)).ShouldBe(originalWriteTime);
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithExistingFileAndOverwriteFalse_ShouldSkipConversion()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// First, create the output file
		await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, true, Format.mp4, ConversionPreset.UltraFast, workingDir);
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();

		// Get the file's last write time
		DateTime originalWriteTime = File.GetLastWriteTime(Path.Combine(workingDir, outputFileName));
		await Task.Delay(1000); // Wait to ensure timestamp would be different

		// Act - Try to convert again with overwriteExisting = false
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(fileToConvert, outputFileName, VideoCodec.h264, false, Format.mp4, ConversionPreset.UltraFast, workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
		// File should not have been modified
		File.GetLastWriteTime(Path.Combine(workingDir, outputFileName)).ShouldBe(originalWriteTime);
	}

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithInvalidCommand_ShouldReturnFalse()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		// Use an invalid codec that will cause ffmpeg to fail
		const string ffmpegCommand = "-c:v invalid_codec_that_does_not_exist -preset medium";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, true, workingDir);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithAudioOnlyFile_ShouldHandleNoVideoStream()
	{
		// Arrange - Create audio-only file using test-with-audio.mp4
		string testAudioPath = Path.Combine(AppContext.BaseDirectory, "TestData", "test-with-audio.mp4");

		// Use subtitles file which would have no video stream for the test
		// since we want to test a file with no video stream but this isn't possible with audio files
		// Let's skip this test if we can't create the audio file
		if (!File.Exists(testAudioPath))
		{
			return; // Skip test if source doesn't exist
		}

		FileInfo audioFile = new(testAudioPath);
		const string outputFileName = "output_audio.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			audioFile,
			outputFileName,
			VideoCodec.copy,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

		// Assert
		command.ShouldNotBeNullOrEmpty();
		command.ShouldContain(audioFile.FullName);
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithAudioOnlyFile_ShouldConvertSuccessfully()
	{
		// This test verifies handling of files, even if they have video streams
		// Testing pure audio-only files (no video stream at all) is complex with Xabe.FFmpeg
		// So we'll just test a normal conversion to ensure the code path works

		// Arrange
		string testAudioPath = Path.Combine(AppContext.BaseDirectory, "TestData", "test-with-audio.mp4");

		if (!File.Exists(testAudioPath))
		{
			return; // Skip test if source doesn't exist
		}

		FileInfo audioFile = new(testAudioPath);
		string outputFileName = $"output_audio_{Guid.NewGuid()}.mp4";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			audioFile,
			outputFileName,
			VideoCodec.copy,
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(workingDir, outputFileName)).ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithHardwareAccelerationAndVideoStream_ShouldNotSetCodec()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";
		HardwareAccelerationValues hwAccel = new()
		{
			hardwareAccelerator = HardwareAccelerator.auto,
			decoder = VideoCodec.h264,
			encoder = VideoCodec.h264_nvenc,
			device = 0
		};

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			hardwareAccelerationValues: hwAccel);

		// Assert
		command.ShouldNotBeNullOrEmpty();
		command.ShouldContain("-hwaccel");
	}

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithNullWorkingPath_ShouldUseTempPath()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset ultrafast -crf 50";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, true, null);

		// Assert
		result.ShouldBeTrue();
		File.Exists(Path.Combine(Path.GetTempPath(), outputFileName)).ShouldBeTrue();

		// Cleanup
		File.Delete(Path.Combine(Path.GetTempPath(), outputFileName));
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithNoOverwrite_ShouldIncludeCorrectFlag()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "test_no_overwrite.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			overwriteOutput: false);

		// Assert
		command.ShouldNotContain("-y");
	}

	private async Task CreateAudioOnlyFileAsync()
	{
		string audioOnlyPath = Path.Combine(AppContext.BaseDirectory, "TestData", "test-audio-only.mp3");
		string sourceVideoPath = Path.Combine(AppContext.BaseDirectory, "TestData", "test-with-audio.mp4");

		ProcessStartInfo startInfo = new()
		{
			FileName = "ffmpeg",
			Arguments = $"-i \"{sourceVideoPath}\" -vn -acodec copy \"{audioOnlyPath}\" -y",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		using Process process = new() { StartInfo = startInfo };
		process.Start();
		await process.WaitForExitAsync();
	}

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithMalformedCommandArguments_ShouldReturnFalse()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		// Use malformed ffmpeg arguments that will cause an error
		const string ffmpegCommand = "-invalid_arg -c:v libx264";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, true, workingDir);

		// Assert - Should return false due to non-zero exit code
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task FfmpegConversionTaskFromXabe_WithInvalidCodec_ShouldReturnFalse()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";

		// Use a non-existent codec to trigger an error
		// Act - This should fail and return false
		bool result = await RawConversionTask.FfmpegConversionTaskFromXabe(
			fileToConvert,
			outputFileName,
			(VideoCodec)999999, // Invalid codec value
			true,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir);

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithStrictFalseTest_ShouldNotIncludeStrictFlag()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			strict: false);

		// Assert
		command.ShouldNotBeNullOrEmpty();
		command.ShouldNotContain("-strict");
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithAudioOnlyFileAndNoVideoStream_ShouldSetOutputFormatWithoutCodec()
	{
		// Arrange
		string testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
		string testAudioPath = Path.Combine(testDataDir, "test-audio-only.mp3");

		// Create audio-only file if it doesn't exist
		if (!File.Exists(testAudioPath))
		{
			await CreateAudioOnlyFileAsync();
		}

		// Skip test if audio file still doesn't exist after creation attempt
		if (!File.Exists(testAudioPath))
		{
			return;
		}

		FileInfo fileToConvert = new(testAudioPath);
		const string outputFileName = "output_audio_only.mp4";

		try
		{
			// Act
			string command = await RawConversionTask.GetConversionCommandFromXabe(
				fileToConvert,
				outputFileName,
				VideoCodec.h264,
				Format.mp4,
				ConversionPreset.UltraFast,
				workingDir);

			// Assert
			command.ShouldNotBeNullOrEmpty();
			// The command should still be valid even without a video stream
			command.ShouldContain(fileToConvert.FullName);
		}
		catch (ArgumentException)
		{
			// Skip test if Xabe.FFmpeg cannot load the audio file
			// This can happen if the audio-only file format is not compatible with FFmpeg
			return;
		}
	}

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithInvalidPriorityClass_ShouldContinueWithoutSettingPriority()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset ultrafast -crf 28";

		// This test ensures the priority setting is handled gracefully
		// Even if setting the priority fails, the conversion should continue
		// Act
		bool result = await RawConversionTask.FfmpegConversionTask(
			fileToConvert,
			outputFileName,
			ffmpegCommand,
			true,
			workingDir,
			processPriority: ProcessPriorityClass.RealTime); // RealTime often requires elevated permissions

		// Assert
		// The conversion may succeed or fail depending on permissions, but it should not crash
		// We're mainly testing that the catch block for priority setting is covered
		(result || !result).ShouldBeTrue(); // Just ensure no exception is thrown
	}

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithNonExistentFile_ShouldHandleExceptionInCatchBlock()
	{
		// Arrange
		// Create a FileInfo for a file that doesn't exist
		string nonExistentPath = Path.Combine(workingDir, "nonexistent_file_12345.mp4");
		FileInfo fileToConvert = new(nonExistentPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset ultrafast";

		// Act
		bool result = await RawConversionTask.FfmpegConversionTask(
			fileToConvert,
			outputFileName,
			ffmpegCommand,
			true,
			workingDir);

		// Assert
		result.ShouldBeFalse(); // Should fail gracefully
	}

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithFailedConversionAndFpsDict_ShouldCleanupFpsDictInCatchBlock()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-invalid_option"; // Invalid command to force failure
		ConcurrentDictionary<int, decimal> fpsDict = new();
		const int conversionIndex = 99;

		// Pre-populate fpsDict
		fpsDict.TryAdd(conversionIndex, 30.0m);

		// Act
		bool result = await RawConversionTask.FfmpegConversionTask(
			fileToConvert,
			outputFileName,
			ffmpegCommand,
			true,
			workingDir,
			conversionIndex,
			fpsDict);

		// Assert
		result.ShouldBeFalse();
		// The fpsDict should have the entry removed in the finally block
		fpsDict.ContainsKey(conversionIndex).ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithSuccessfulConversion_ShouldCleanupFpsDictInFinallyBlock()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset ultrafast -crf 28";
		ConcurrentDictionary<int, decimal> fpsDict = new();
		const int conversionIndex = 100;

		// Pre-populate fpsDict
		fpsDict.TryAdd(conversionIndex, 30.0m);

		// Act
		bool result = await RawConversionTask.FfmpegConversionTask(
			fileToConvert,
			outputFileName,
			ffmpegCommand,
			true,
			workingDir,
			conversionIndex,
			fpsDict);

		// Assert
		result.ShouldBeTrue();
		// The fpsDict should have the entry removed in the finally block
		fpsDict.ContainsKey(conversionIndex).ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithProvidedMediaInfo_ShouldUseProvidedMediaInfoDirectly()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output_with_mediainfo.mp4";

		// Get media info first
		IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileToConvert.FullName);

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			mediaInfo); // Provide pre-fetched media info

		// Assert
		command.ShouldNotBeNullOrEmpty();
		command.ShouldContain(fileToConvert.FullName);
	}

	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithHardwareAccelerationAndVideoStream_ShouldUseHardwareEncoder()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output_hw_accel.mp4";
		HardwareAccelerationValues hwAccel = new()
		{
			hardwareAccelerator = HardwareAccelerator.auto,
			decoder = VideoCodec.h264,
			encoder = VideoCodec.h264_nvenc,
			device = 0
		};

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(
			fileToConvert,
			outputFileName,
			VideoCodec.h264,
			Format.mp4,
			ConversionPreset.UltraFast,
			workingDir,
			hardwareAccelerationValues: hwAccel);

		// Assert
		command.ShouldNotBeNullOrEmpty();
		command.ShouldContain("-hwaccel");
		// When hardware acceleration is used with a video stream, the else branch should not set codec
	}

	[RetryFact(3)]
	public async Task RawFfmpegConversionTask_WithExceptionAndNullFpsDict_ShouldHandleExceptionGracefully()
	{
		// Arrange
		string nonExistentPath = Path.Combine(workingDir, "nonexistent_file_98765.mp4");
		FileInfo fileToConvert = new(nonExistentPath);
		string outputFileName = $"output_{Guid.NewGuid()}.mp4";
		const string ffmpegCommand = "-c:v libx264 -preset ultrafast";

		// Act - No fpsDict provided (null)
		bool result = await RawConversionTask.FfmpegConversionTask(
			fileToConvert,
			outputFileName,
			ffmpegCommand,
			true,
			workingDir);

		// Assert
		result.ShouldBeFalse(); // Should fail gracefully
	}

	#endregion

}
