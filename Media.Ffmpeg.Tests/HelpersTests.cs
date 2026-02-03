using System.Collections.Concurrent;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Media.Ffmpeg;
using xRetry.v3;

namespace Media.Ffmpeg.Tests;

public sealed class HelpersTests : IDisposable
{
	private readonly Fixture fixture;
	private readonly string _testVideoPath;
	private readonly string _tempLogFile;

	public HelpersTests()
	{
		fixture = new Fixture();
		fixture.Customize(new AutoFakeItEasyCustomization());

		string testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
		_testVideoPath = Path.Combine(testDataDir, "test.mp4");
		_tempLogFile = Path.Combine(Path.GetTempPath(), $"HelpersTests_{Guid.NewGuid()}.log");
	}

	private bool disposed;

	public void Dispose()
	{
		if (File.Exists(_tempLogFile))
		{
			try
			{
				File.Delete(_tempLogFile);
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
				File.Delete(_tempLogFile);
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
		await Helpers.RecordResults(fileName, success, bag, _tempLogFile, originalSize, endSize);

		// Assert
		bag.ShouldContain(x => x.Contains(fileName) && x.Contains("Success=True"));
		File.Exists(_tempLogFile).ShouldBeTrue();
		string logContent = await File.ReadAllTextAsync(_tempLogFile);
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
		string? result = await _testVideoPath.GetVideoMetadata(Helpers.EVideoMetadata.Codec_Name);

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
		decimal result = await _testVideoPath.GetFrameRate();

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

	[RetryTheory(3)]
	[InlineData(-1, -1)]
	[InlineData(2, 5)]
	public async Task GetKeyFrameSpacing_WithSamples_ShouldReturnValueOrMinusOne(int numberOfSamples, int sampleLengthSec)
	{
		// Act
		decimal result = await _testVideoPath.GetKeyFrameSpacing(numberOfSamples, sampleLengthSec);

		// Assert
		// For a valid file, should be >= -1 (could be -1 if no keyframes found)
		result.ShouldBeInRange(-1, 1000);
	}

	[RetryFact(3)]
	public async Task GetKeyFrameSpacing_WithoutSamples_ShouldReturnValueOrMinusOne()
	{
		// Act
		decimal result = await _testVideoPath.GetKeyFrameSpacing();

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
}
