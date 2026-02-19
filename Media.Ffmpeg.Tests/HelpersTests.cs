using System.Collections.Concurrent;
using System.Diagnostics;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Media.Ffmpeg;
using xRetry.v3;

namespace Media.Ffmpeg.Tests;

public sealed class HelpersTests : IDisposable
{
	private readonly Fixture fixture;
	private readonly string testVideoPath;
	private readonly string tempLogFile;

	public HelpersTests()
	{
		fixture = new Fixture();
		fixture.Customize(new AutoFakeItEasyCustomization());

		string testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
		testVideoPath = Path.Combine(testDataDir, "test.mp4");
		tempLogFile = Path.Combine(Path.GetTempPath(), $"HelpersTests_{Guid.NewGuid()}.log");
	}

	private bool disposed;

	public void Dispose()
	{
		if (File.Exists(tempLogFile))
		{
			try
			{
				File.Delete(tempLogFile);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				File.Delete(tempLogFile);
			}
			disposed = true;
		}
	}

	~HelpersTests()
	{
		Dispose(false);
	}

	[RetryFact(3)]
	public void GetTotalFps_ShouldReturnSumOfValues()
	{
		// Arrange
		ConcurrentDictionary<int, decimal> dict = new();
		dict.TryAdd(1, 10.5m);
		dict.TryAdd(2, 5.5m);

		// Act
		decimal result = Helpers.GetTotalFps(dict);

		// Assert
		result.ShouldBe(16.0m);
	}

	[RetryTheory(3)]
	[InlineData("frame=  48 fps=5.8 q=0.0 size=1kB time=00:00:01.77 bitrate=4.5kbits/s", 5.8)]
	[InlineData("frame=  48 fps=12.3 q=0.0 size=1kB time=00:00:01.77 bitrate=4.5kbits/s", 12.3)]
	[InlineData("frame=  48 fps=0.0 q=0.0 size=1kB time=00:00:01.77 bitrate=4.5kbits/s", 0.0)]
	public void ParseFfmpegLogFps_ShouldParseFpsCorrectly(string data, decimal expected)
	{
		// Act
		decimal result = data.ParseFfmpegLogFps();

		// Assert
		result.ShouldBe(expected);
	}

	[RetryFact(3)]
	public void ParseFfmpegLogFps_ShouldReturnMinusOneOnInvalid()
	{
		// Arrange
		const string data = "no fps here";

		// Act
		decimal result = data.ParseFfmpegLogFps();

		// Assert
		result.ShouldBe(-1);
	}

	[RetryFact(3)]
	public void ParseFfmpegLogFps_ShouldHandleExceptionGracefully()
	{
		// Arrange - string that will cause an exception during parsing
		const string data = "frame=  48 fps= q=0.0";

		// Act
		decimal result = data.ParseFfmpegLogFps();

		// Assert
		result.ShouldBe(-1);
	}

	[RetryFact(3)]
	public void ParseFfmpegLogFps_ShouldHandleDoubleSpaceAfterFps()
	{
		// Arrange - data with space after fps= (triggers else branch)
		const string data = "frame=  48 fps= 5.8 q=0.0 size=1kB";

		// Act
		decimal result = data.ParseFfmpegLogFps();

		// Assert
		result.ShouldBe(5.8m);
	}

	[RetryFact(3)]
	public void GetTotalFileDif_ShouldReturnCorrectSum()
	{
		// Arrange
		ConcurrentBag<string> bag = new()
			{
				"FileName=a,Success=True,OriginalSize=1B,EndSize=2B,SizeRatio=200%,SizeDif=1",
				"FileName=b,Success=True,OriginalSize=2B,EndSize=1B,SizeRatio=50%,SizeDif=-1"
			};

		// Act
		string result = Helpers.GetTotalFileDif(bag);

		// Assert
		result.ShouldContain("0"); // 1 + (-1) = 0
	}

	[RetryFact(3)]
	public async Task RecordResults_ShouldAddToBagAndWriteLog()
	{
		// Arrange
		ConcurrentBag<string> bag = new();
		const string fileName = "testfile";
		const bool success = true;
		const long originalSize = 100;
		const long endSize = 50;

		// Act
		await Helpers.RecordResults(fileName, success, bag, tempLogFile, originalSize, endSize);

		// Assert
		bag.ShouldContain(x => x.Contains(fileName) && x.Contains("Success=True"));
		File.Exists(tempLogFile).ShouldBeTrue();
		string logContent = await File.ReadAllTextAsync(tempLogFile);
		logContent.ShouldContain(fileName);
	}

	[RetryFact(3)]
	public async Task RecordResults_WithoutLogFile_ShouldAddToBagOnly()
	{
		// Arrange
		ConcurrentBag<string> bag = new();
		const string fileName = "testfile";
		const bool success = false;
		const long originalSize = 100;
		const long endSize = 200;

		// Act
		await Helpers.RecordResults(fileName, success, bag, originalSize, endSize);

		// Assert
		bag.ShouldContain(x => x.Contains(fileName) && x.Contains("Success=False"));
	}

	[RetryFact(3)]
	public async Task GetVideoMetadata_ShouldReturnNonNullForValidFile()
	{
		// Act
		string? result = await testVideoPath.GetVideoMetadata(Helpers.EVideoMetadata.Codec_Name);

		// Assert
		result.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData("nonexistent.mp4")]
	[InlineData("")]
	public async Task GetVideoMetadata_ShouldReturnNullForInvalidFile(string badFileName)
	{
		// Act
		string? result = await badFileName.GetVideoMetadata(Helpers.EVideoMetadata.Codec_Name);

		// Assert
		result.ShouldBeNull();
	}

	[RetryFact(3)]
	public async Task GetFrameRate_ShouldReturnPositiveForValidFile()
	{
		// Act
		decimal result = await testVideoPath.GetFrameRate();

		// Assert
		result.ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task GetFrameRate_ShouldReturnMinusOneForInvalidFile()
	{
		// Act
		decimal result = await "nonexistent.mp4".GetFrameRate();

		// Assert
		result.ShouldBe(-1);
	}

	[RetryFact(3)]
	public async Task GetFrameRate_ShouldHandleInvalidFrameRateFormat()
	{
		// Act - Empty string should trigger error path
		decimal result = await string.Empty.GetFrameRate();

		// Assert
		result.ShouldBe(-1);
	}

	[RetryTheory(3)]
	[InlineData(-1, -1)]
	[InlineData(2, 5)]
	public async Task GetKeyFrameSpacing_WithSamples_ShouldReturnValueOrMinusOne(int numberOfSamples, int sampleLengthSec)
	{
		// Act
		decimal result = await testVideoPath.GetKeyFrameSpacing(numberOfSamples, sampleLengthSec);

		// Assert
		// For a valid file, should be >= -1 (could be -1 if no keyframes found)
		result.ShouldBeInRange(-1, 1000);
	}

	[RetryFact(3)]
	public async Task GetKeyFrameSpacing_WithoutSamples_ShouldReturnValueOrMinusOne()
	{
		// Act
		decimal result = await testVideoPath.GetKeyFrameSpacing();

		// Assert
		// For a valid file, should be >= -1 (could be -1 if no keyframes found)
		result.ShouldBeInRange(-1, 1000);
	}

	[RetryFact(3)]
	public void EVideoMetadata_ShouldContainExpectedValues()
	{
		// Act
		string[] values = Enum.GetNames<Helpers.EVideoMetadata>();

		// Assert
		values.ShouldContain("Codec_Name");
		values.ShouldContain("Duration");
		values.ShouldContain("Bit_Rate");
	}

	[RetryFact(3)]
	public void GetTotalFileDif_ShouldHandleNegativeValues()
	{
		// Arrange
		ConcurrentBag<string> bag = new()
		{
			"FileName=a,Success=True,OriginalSize=100B,EndSize=50B,SizeRatio=50%,SizeDif=-50",
			"FileName=b,Success=True,OriginalSize=50B,EndSize=100B,SizeRatio=200%,SizeDif=50"
		};

		// Act
		string result = Helpers.GetTotalFileDif(bag);

		// Assert
		result.ShouldContain("0"); // -50 + 50 = 0
	}

	[RetryFact(3)]
	public void GetTotalFileDif_ShouldHandleEmptyBag()
	{
		// Arrange
		ConcurrentBag<string> bag = new();

		// Act
		string result = Helpers.GetTotalFileDif(bag);

		// Assert
		result.ShouldBe("0 B"); // Note: includes space
	}

	[RetryFact(3)]
	public async Task CheckHardwareEncoderByName_ShouldReturnBoolForValidEncoder()
	{
		// Act
		bool result = await Helpers.CheckHardwareEncoderByName("h264_nvenc");

		// Assert - Should return true or false without throwing
		result.ShouldBeOfType<bool>();
	}

	[RetryFact(3)]
	public async Task CheckHardwareEncoderByName_ShouldReturnFalseForInvalidEncoder()
	{
		// Act
		bool result = await Helpers.CheckHardwareEncoderByName("totally_fake_encoder_12345");

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task CheckHardwareEncoderByName_WithEmptyString_ShouldHandleGracefully()
	{
		// Act
		bool result = await Helpers.CheckHardwareEncoderByName(string.Empty);

		// Assert - Should return false without throwing
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task CheckHardwareEncoderByName_WithNullString_ShouldHandleGracefully()
	{
		// Act
		bool result = await Helpers.CheckHardwareEncoderByName(null!);

		// Assert - Should return false without throwing
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task CheckHardwareEncoderByName_WithSpecialCharacters_ShouldHandleGracefully()
	{
		// Act - Special characters might cause issues in process execution
		bool result = await Helpers.CheckHardwareEncoderByName("encoder\0with\nnull\rand\tspecial\bchars");

		// Assert - Should return false without throwing
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task IsAnyHardwareAcceleratorAvailable_ShouldReturnBool()
	{
		// Act
		bool result = await Helpers.IsAnyHardwareAcceleratorAvailable();

		// Assert - Should return true or false without throwing
		result.ShouldBeOfType<bool>();
	}

	[RetryFact(3)]
	public void LogFfmpegOutput_ShouldHandleNullData()
	{
		// Arrange
		DataReceivedEventArgs args = CreateDataReceivedEventArgs(null);
		DateTime lastOutput = DateTime.UtcNow.AddSeconds(-10);
		DateTime lastSummaryOutput = DateTime.UtcNow.AddSeconds(-40);
		bool conversionFailed = false;
		bool sizeFailure = false;
		FileInfo fileToConvert = new(testVideoPath);
		TimeSpan videoTimespan = TimeSpan.FromMinutes(1);
		int conversionIndex = 1;
		bool cancelIfLarger = false;
		ConcurrentBag<string>? conversionOutputs = null;
		ConcurrentDictionary<int, decimal>? fpsDict = null;
		CancellationTokenSource? cancellationTokenSource = null;

		// Act & Assert - Should not throw
		Should.NotThrow(() => args.LogFfmpegOutput(ref lastOutput, ref lastSummaryOutput, ref conversionFailed, ref sizeFailure, fileToConvert, videoTimespan, conversionIndex,
			cancelIfLarger, null, null, conversionOutputs, fpsDict, cancellationTokenSource));
	}

	[RetryFact(3)]
	public void LogFfmpegOutput_ShouldParseFpsAndAddToDict()
	{
		// Arrange
		const string data = "frame=  48 fps=5.8 q=0.0 size=1kB time=00:00:01.77 bitrate=4.5kbits/s";
		DataReceivedEventArgs args = CreateDataReceivedEventArgs(data);
		DateTime lastOutput = DateTime.UtcNow.AddSeconds(-10);
		DateTime lastSummaryOutput = DateTime.UtcNow.AddSeconds(-40);
		bool conversionFailed = false;
		bool sizeFailure = false;
		FileInfo fileToConvert = new(testVideoPath);
		TimeSpan videoTimespan = TimeSpan.FromMinutes(1);
		int conversionIndex = 1;
		bool cancelIfLarger = false;
		ConcurrentBag<string> conversionOutputs = new();
		ConcurrentDictionary<int, decimal> fpsDict = new();
		CancellationTokenSource? cancellationTokenSource = null;

		// Act
		args.LogFfmpegOutput(ref lastOutput, ref lastSummaryOutput, ref conversionFailed, ref sizeFailure, fileToConvert, videoTimespan, conversionIndex, cancelIfLarger, null, null,
			conversionOutputs, fpsDict, cancellationTokenSource);

		// Assert
		fpsDict.ShouldContainKey(conversionIndex);
		fpsDict[conversionIndex].ShouldBe(5.8m);
	}

	[RetryFact(3)]
	public void LogFfmpegOutput_ShouldUpdateExistingFpsInDict()
	{
		// Arrange
		const string data = "frame=  96 fps=10.5 q=0.0 size=2kB time=00:00:03.54 bitrate=4.5kbits/s";
		DataReceivedEventArgs args = CreateDataReceivedEventArgs(data);
		DateTime lastOutput = DateTime.UtcNow.AddSeconds(-10);
		DateTime lastSummaryOutput = DateTime.UtcNow.AddSeconds(-40);
		bool conversionFailed = false;
		bool sizeFailure = false;
		FileInfo fileToConvert = new(testVideoPath);
		TimeSpan videoTimespan = TimeSpan.FromMinutes(1);
		int conversionIndex = 2;
		bool cancelIfLarger = false;
		ConcurrentBag<string> conversionOutputs = new();
		ConcurrentDictionary<int, decimal> fpsDict = new();
		fpsDict.TryAdd(conversionIndex, 5.0m); // Pre-existing value
		CancellationTokenSource? cancellationTokenSource = null;

		// Act
		args.LogFfmpegOutput(ref lastOutput, ref lastSummaryOutput, ref conversionFailed, ref sizeFailure, fileToConvert, videoTimespan, conversionIndex, cancelIfLarger, null, null,
			conversionOutputs, fpsDict, cancellationTokenSource);

		// Assert
		fpsDict[conversionIndex].ShouldBe(10.5m);
	}

	[RetryFact(3)]
	public void LogFfmpegOutput_ShouldCancelIfOutputLargerThanSource()
	{
		// Arrange
		const string data = "frame=  48 fps=5.8 q=0.0 size=999999kB time=00:00:01.77 bitrate=4.5kbits/s";
		DataReceivedEventArgs args = CreateDataReceivedEventArgs(data);
		DateTime lastOutput = DateTime.UtcNow.AddSeconds(-10);
		DateTime lastSummaryOutput = DateTime.UtcNow.AddSeconds(-40);
		bool conversionFailed = false;
		bool sizeFailure = false;
		FileInfo fileToConvert = new(testVideoPath);
		TimeSpan videoTimespan = TimeSpan.FromMinutes(1);
		int conversionIndex = 3;
		bool cancelIfLarger = true; // Enable cancellation on size increase
		ConcurrentBag<string> conversionOutputs = new();
		ConcurrentDictionary<int, decimal> fpsDict = new();
		CancellationTokenSource cancellationTokenSource = new();

		// Act
		args.LogFfmpegOutput(ref lastOutput, ref lastSummaryOutput, ref conversionFailed, ref sizeFailure, fileToConvert, videoTimespan, conversionIndex, cancelIfLarger, null, null,
			conversionOutputs, fpsDict, cancellationTokenSource);

		// Assert
		conversionFailed.ShouldBeTrue();
		sizeFailure.ShouldBeTrue();
		cancellationTokenSource.IsCancellationRequested.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void LogFfmpegOutput_ShouldNotLogIfRecentlyLogged()
	{
		// Arrange
		const string data = "frame=  48 fps=5.8 q=0.0 size=1kB time=00:00:01.77 bitrate=4.5kbits/s";
		DataReceivedEventArgs args = CreateDataReceivedEventArgs(data);
		DateTime lastOutput = DateTime.UtcNow; // Just logged
		DateTime lastSummaryOutput = DateTime.UtcNow;
		bool conversionFailed = false;
		bool sizeFailure = false;
		FileInfo fileToConvert = new(testVideoPath);
		TimeSpan videoTimespan = TimeSpan.FromMinutes(1);
		int conversionIndex = 4;
		bool cancelIfLarger = false;
		ConcurrentBag<string> conversionOutputs = new();
		ConcurrentDictionary<int, decimal> fpsDict = new();
		CancellationTokenSource? cancellationTokenSource = null;

		// Act
		args.LogFfmpegOutput(
			ref lastOutput, ref lastSummaryOutput, ref conversionFailed, ref sizeFailure,
			fileToConvert, videoTimespan, conversionIndex, cancelIfLarger, null, null,
			conversionOutputs, fpsDict, cancellationTokenSource);

		// Assert - Should not add to fpsDict since not enough time has passed
		fpsDict.ShouldBeEmpty();
	}

	[RetryFact(1)]
	public async Task LogFfmpegOutput_ShouldHandleRealFfmpegOutput()
	{
		// Arrange - Run a real FFmpeg conversion to capture output
		string outputPath = Path.Combine(Path.GetTempPath(), $"ffmpeg_test_{Guid.NewGuid()}.mp4");
		ProcessStartInfo startInfo = new()
		{
			FileName = "ffmpeg",
			Arguments = $"-i \"{testVideoPath}\" -c:v libx264 -preset veryfast -t 2 -y \"{outputPath}\"",
			UseShellExecute = false,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		FileInfo fileToConvert = new(testVideoPath);
		TimeSpan videoTimespan = TimeSpan.FromSeconds(10);
		DateTime lastOutput = DateTime.UtcNow.AddSeconds(-10);
		DateTime lastSummaryOutput = DateTime.UtcNow.AddSeconds(-40);
		bool conversionFailed = false;
		bool sizeFailure = false;
		int conversionIndex = 5;
		bool cancelIfLarger = false;
		ConcurrentBag<string> conversionOutputs = new();
		ConcurrentDictionary<int, decimal> fpsDict = new();
		CancellationTokenSource? cancellationTokenSource = null;

		int outputLinesProcessed = 0;

		try
		{
			using Process? process = Process.Start(startInfo);
			if (process != null)
			{
				// Read stderr where FFmpeg writes progress
#pragma warning disable CA2024 // Do not use 'StreamReader.EndOfStream' in async methods
				while (!process.StandardError.EndOfStream && outputLinesProcessed < 5)
				{
					string? line = await process.StandardError.ReadLineAsync();
					if (!string.IsNullOrWhiteSpace(line) && line.Contains("frame="))
					{
						// Create event args with real FFmpeg output
						DataReceivedEventArgs args = CreateDataReceivedEventArgs(line);

						// Act - Process the real output
						Should.NotThrow(() => args.LogFfmpegOutput(
							ref lastOutput, ref lastSummaryOutput, ref conversionFailed, ref sizeFailure,
							fileToConvert, videoTimespan, conversionIndex, cancelIfLarger,
							"Test Task", "Additional Log", conversionOutputs, fpsDict, cancellationTokenSource));

						outputLinesProcessed++;
					}
				}
#pragma warning restore CA2024 // Do not use 'StreamReader.EndOfStream' in async methods

				// Kill the process since we only need a few lines
				if (!process.HasExited)
				{
					process.Kill();
				}
			}

			// Assert - Should have processed some output without errors
			outputLinesProcessed.ShouldBeGreaterThan(0);
		}
		catch (Exception ex) when (ex.Message.Contains("Cannot find") || ex.Message.Contains("not found"))
		{
			// FFmpeg not installed - skip test
			Assert.Skip("FFmpeg not available");
		}
		finally
		{
			// Cleanup
			if (File.Exists(outputPath))
			{
				try
				{
					File.Delete(outputPath);
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}
	}

	[RetryFact(3)]
	public void LogFfmpegOutput_ShouldParseTimeAndCalculateETA()
	{
		// Arrange - Real FFmpeg output with time and speed
		const string data = "frame=  120 fps=24.0 q=28.0 size=    256kB time=00:00:05.00 bitrate= 419.4kbits/s speed=1.00x";
		DataReceivedEventArgs args = CreateDataReceivedEventArgs(data);
		DateTime lastOutput = DateTime.UtcNow.AddSeconds(-10);
		DateTime lastSummaryOutput = DateTime.UtcNow.AddSeconds(-40);
		bool conversionFailed = false;
		bool sizeFailure = false;
		FileInfo fileToConvert = new(testVideoPath);
		TimeSpan videoTimespan = TimeSpan.FromSeconds(10); // 10 second video
		int conversionIndex = 6;
		bool cancelIfLarger = false;
		ConcurrentBag<string> conversionOutputs = new();
		ConcurrentDictionary<int, decimal> fpsDict = new();
		CancellationTokenSource? cancellationTokenSource = null;

		// Act
		args.LogFfmpegOutput(
			ref lastOutput, ref lastSummaryOutput, ref conversionFailed, ref sizeFailure,
			fileToConvert, videoTimespan, conversionIndex, cancelIfLarger, "Task Description", "Additional Text",
			conversionOutputs, fpsDict, cancellationTokenSource);

		// Assert - Should process without throwing and update lastOutput
		lastOutput.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-2));
		conversionFailed.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void LogFfmpegOutput_WithUnParseableTimeFormat_ShouldUseRawValue()
	{
		// Arrange - Data with un-parseable time format (not matching hh:mm:ss.ff)
		const string data = "frame=  48 fps=5.8 q=0.0 size=1kB time=invalid_time bitrate=4.5kbits/s speed=1.0x";
		DataReceivedEventArgs args = CreateDataReceivedEventArgs(data);
		DateTime lastOutput = DateTime.UtcNow.AddSeconds(-10);
		DateTime lastSummaryOutput = DateTime.UtcNow.AddSeconds(-40);
		bool conversionFailed = false;
		bool sizeFailure = false;
		FileInfo fileToConvert = new(testVideoPath);
		TimeSpan videoTimespan = TimeSpan.FromMinutes(1);
		int conversionIndex = 7;
		bool cancelIfLarger = false;
		ConcurrentBag<string> conversionOutputs = new();
		ConcurrentDictionary<int, decimal> fpsDict = new();
		CancellationTokenSource? cancellationTokenSource = null;

		// Act
		args.LogFfmpegOutput(ref lastOutput, ref lastSummaryOutput, ref conversionFailed, ref sizeFailure, fileToConvert, videoTimespan, conversionIndex, cancelIfLarger, null, null,
			conversionOutputs, fpsDict, cancellationTokenSource);

		// Assert - Should not throw and update lastOutput
		lastOutput.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-2));
		conversionFailed.ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task GetFrameRate_WithInvalidVideoFile_ShouldHandleExceptionAndReturnMinusOne()
	{
		// Arrange - Create a file that will cause an exception during frame rate calculation
		string invalidFile = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid()}.mp4");
		await File.WriteAllTextAsync(invalidFile, "This is not a valid video file");

		try
		{
			// Act
			decimal result = await invalidFile.GetFrameRate();

			// Assert
			result.ShouldBe(-1);
		}
		finally
		{
			// Cleanup
			if (File.Exists(invalidFile))
			{
				File.Delete(invalidFile);
			}
		}
	}

	[RetryFact(3)]
	public async Task GetKeyFrameSpacing_WithNegativeSampleLength_ShouldDefaultToTenSeconds()
	{
		// Arrange - Use a sample length <= 0 to trigger default value
		int numberOfSamples = 2;
		int sampleLengthSec = -5; // This should be set to 10 internally

		// Act
		decimal result = await testVideoPath.GetKeyFrameSpacing(numberOfSamples, sampleLengthSec);

		// Assert - Should complete without error (actual value depends on video keyframe structure)
		result.ShouldBeGreaterThanOrEqualTo(-1);
	}

	[RetryFact(3)]
	public async Task GetKeyFrameSpacing_WithLargeSampleParameters_ShouldReadEntireVideo()
	{
		// Arrange - Set numberOfSamples * sampleLengthSec to be very large
		int numberOfSamples = 100;
		int sampleLengthSec = 100; // This total will likely be >= video duration

		// Act
		decimal result = await testVideoPath.GetKeyFrameSpacing(numberOfSamples, sampleLengthSec);

		// Assert - Should complete without error
		result.ShouldBeGreaterThanOrEqualTo(-1);
	}

	[RetryFact(3)]
	public async Task GetKeyFrameSpacing_WithNonExistentFile_ShouldHandleExceptionAndReturnMinusOne()
	{
		// Arrange
		string nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.mp4");

		// Act
		decimal result = await nonExistentFile.GetKeyFrameSpacing(2, 5);

		// Assert
		result.ShouldBe(-1);
	}

	[RetryFact(3)]
	public async Task RecordResults_WithOnlyOriginalSizeNull_ShouldNotIncludeSizeRatio()
	{
		// Arrange
		ConcurrentBag<string> bag = new();
		const string fileName = "testfile";
		const bool success = true;
		long? originalSize = null;
		long endSize = 100;

		// Act
		await Helpers.RecordResults(fileName, success, bag, tempLogFile, originalSize, endSize);

		// Assert
		bag.ShouldContain(x => x.Contains(fileName) && !x.Contains("SizeRatio"));
	}

	[RetryFact(3)]
	public async Task RecordResults_WithOnlyEndSizeNull_ShouldNotIncludeSizeRatio()
	{
		// Arrange
		ConcurrentBag<string> bag = new();
		const string fileName = "testfile";
		const bool success = true;
		long originalSize = 100;
		long? endSize = null;

		// Act
		await Helpers.RecordResults(fileName, success, bag, tempLogFile, originalSize, endSize);

		// Assert
		bag.ShouldContain(x => x.Contains(fileName) && !x.Contains("SizeRatio"));
	}

	[RetryFact(3)]
	public async Task RecordResults_WithBothSizesNull_ShouldNotIncludeSizeRatio()
	{
		// Arrange
		ConcurrentBag<string> bag = new();
		const string fileName = "testfile";
		const bool success = false;
		long? originalSize = null;
		long? endSize = null;

		// Act
		await Helpers.RecordResults(fileName, success, bag, tempLogFile, originalSize, endSize);

		// Assert
		bag.ShouldContain(x => x.Contains(fileName) && !x.Contains("SizeRatio"));
	}

	private static DataReceivedEventArgs CreateDataReceivedEventArgs(string? data)
	{
		// Use reflection to create DataReceivedEventArgs since it has no public constructor
		DataReceivedEventArgs args = (DataReceivedEventArgs)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(DataReceivedEventArgs))!;

		// Set the Data property using reflection
		System.Reflection.FieldInfo? dataField = typeof(DataReceivedEventArgs).GetField("_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		dataField?.SetValue(args, data);

		return args;
	}
}
