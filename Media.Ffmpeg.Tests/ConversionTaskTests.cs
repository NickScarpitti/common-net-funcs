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
				Thread.Sleep(5000);
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
		//bool result = await RawConversionTask.FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommand, true, _workingDir);
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
		CancellationTokenSource cts = new();

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
		string outputFileName = "output.mp4";

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
		string outputFileName = "output.mp4";

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
		(command.Contains(expectedCodecName, StringComparison.OrdinalIgnoreCase) ||
		 command.Contains(codec.ToString(), StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
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
		string outputFileName = "output.mp4";

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
		string outputFileName = "output.mp4";
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
		string outputFileName = "output.mp4";

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
		string outputFileName = "output.mp4";
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
		string outputFileName = "output.mp4";

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
		string outputFileName = "output.mp4";

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
		string outputFileName = "output.mp4";

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
		string outputFileName = "output.mp4";
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
	[InlineData(VideoCodec.hevc, Format.matroska)]
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
		CancellationTokenSource cts = new();

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

	[RetryFact(3)]
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

}
