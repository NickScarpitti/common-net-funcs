using CommonNetFuncs.Ffmpeg;
using CommonNetFuncs.Ffmpeg.FfmpegRawCalls;
using Xabe.FFmpeg;
using xRetry.v3;

namespace Ffmpeg.Tests;

public sealed class ConversionTaskCommandBuilderTests() : ConversionTaskTestsBase("ConversionTaskTests_CommandBuilder")
{
	[RetryFact(3)]
	public async Task GetConversionCommandFromXabe_WithBasicParameters_ShouldReturnValidCommand()
	{
		// Arrange
		FileInfo fileToConvert = new(testVideoPath);
		const string outputFileName = "output.mp4";

		// Act
		string command = await RawConversionTask.GetConversionCommandFromXabe(fileToConvert, outputFileName, VideoCodec.h264, Format.mp4, ConversionPreset.UltraFast, workingDir);

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
		string command = await RawConversionTask.GetConversionCommandFromXabe(fileToConvert, outputFileName, VideoCodec.h264, Format.mp4, preset, workingDir);

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
		string command = await RawConversionTask.GetConversionCommandFromXabe(fileToConvert, outputFileName, VideoCodec.h264, Format.mp4, ConversionPreset.UltraFast, customPath);

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
		string command = await RawConversionTask.GetConversionCommandFromXabe(fileToConvert, outputFileName, VideoCodec.h264, Format.mp4, ConversionPreset.UltraFast);

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
		string command = await RawConversionTask.GetConversionCommandFromXabe(fileToConvert, outputFileName, VideoCodec.h264, Format.mp4, ConversionPreset.UltraFast, workingDir, mediaInfo);

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
		string command = await RawConversionTask.GetConversionCommandFromXabe(fileToConvert, outputFileName, VideoCodec.h264, Format.mp4, ConversionPreset.UltraFast, workingDir, numberOfThreads: numberOfThreads);

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
		string command = await RawConversionTask.GetConversionCommandFromXabe(fileToConvert, outputFileName, VideoCodec.h264, Format.mp4, ConversionPreset.UltraFast, workingDir, strict: strict);

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
		string command = await RawConversionTask.GetConversionCommandFromXabe(fileToConvert, outputFileName, VideoCodec.h264, Format.mp4, ConversionPreset.UltraFast, workingDir, overwriteOutput: overwriteOutput);

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
		string command = await RawConversionTask.GetConversionCommandFromXabe(fileToConvert, outputFileName, VideoCodec.h264, Format.mp4, ConversionPreset.UltraFast, workingDir, hardwareAccelerationValues: hwAccel);

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
			fileToConvert, outputFileName, VideoCodec.h264, format, ConversionPreset.UltraFast, workingDir);

		// Assert
		command.ShouldNotBeNullOrEmpty();
		command.ShouldContain(outputFileName);
	}
}
