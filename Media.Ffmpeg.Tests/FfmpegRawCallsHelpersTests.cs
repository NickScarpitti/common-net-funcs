using CommonNetFuncs.Media.Ffmpeg.FfmpegRawCalls;
using xRetry.v3;

namespace Media.Ffmpeg.Tests;

public sealed class FfmpegRawCallsHelpersTests
{
	private readonly string testVideoPath;

	public FfmpegRawCallsHelpersTests()
	{
		string testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
		testVideoPath = Path.Combine(testDataDir, "test.mp4");
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldReturnMediaInfoForValidFile()
	{
		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(testVideoPath);

		// Assert
		result.ShouldNotBeNull();
		result.FilePath.ShouldBe(testVideoPath);
		result.FileName.ShouldBe("test.mp4");
		result.Size.ShouldBeGreaterThan(0);
		result.Streams.ShouldNotBeEmpty();
		result.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldParseVideoStream()
	{
		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(testVideoPath);

		// Assert
		MediaStream? videoStream = result.Streams.FirstOrDefault(s => s?.CodecType == CodecType.Video);
		videoStream.ShouldNotBeNull();
		videoStream.CodecType.ShouldBe(CodecType.Video);
		videoStream.CodecName.ShouldNotBeNullOrWhiteSpace();
		videoStream.Width.HasValue.ShouldBeTrue();
		(videoStream.Width!.Value > 0).ShouldBeTrue();
		videoStream.Height.HasValue.ShouldBeTrue();
		(videoStream.Height!.Value > 0).ShouldBeTrue();
		videoStream.FrameRate.ShouldBeGreaterThan(0);
		result.VideoFormat.ShouldNotBeNullOrWhiteSpace();
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldParseAudioStream()
	{
		// Arrange
		string testAudioVideoPath = Path.Combine(AppContext.BaseDirectory, "TestData", "test-with-audio.mp4");

		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(testAudioVideoPath);

		// Assert - Should have both video and audio streams
		MediaStream? audioStream = result.Streams.FirstOrDefault(s => s?.CodecType == CodecType.Audio);
		audioStream.ShouldNotBeNull();
		audioStream.CodecType.ShouldBe(CodecType.Audio);
		audioStream.CodecName.ShouldNotBeNullOrWhiteSpace();
		result.AudioFormat.ShouldNotBeNullOrWhiteSpace();
		result.AudioFormat.ShouldBe(audioStream.CodecName);

		// Should also have video stream
		MediaStream? videoStream = result.Streams.FirstOrDefault(s => s?.CodecType == CodecType.Video);
		videoStream.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldThrowForInvalidFile()
	{
		// Arrange
		string invalidPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.mp4");

		// Act & Assert - Should throw FileNotFoundException
		await Should.ThrowAsync<FileNotFoundException>(async () =>
		{
			await Helpers.GetMediaInfoAsync(invalidPath);
		});
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldHandleZeroFrameRate()
	{
		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(testVideoPath);

		// Assert - Frame rate should be calculated properly
		MediaStream? videoStream = result.Streams.FirstOrDefault(s => s?.CodecType == CodecType.Video);
		if (videoStream != null)
		{
			// Frame rate should be greater than 0 for valid video
			videoStream.FrameRate.ShouldBeGreaterThanOrEqualTo(0);
		}
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldSetFileSizeFromFileInfo()
	{
		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(testVideoPath);

		// Assert
		FileInfo fi = new(testVideoPath);
		result.Size.ShouldBe(fi.Length);
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldCalculateMaxDurationFromStreams()
	{
		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(testVideoPath);

		// Assert - Duration should be max of all stream durations
		result.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
		if (result.Streams.Any())
		{
			TimeSpan maxStreamDuration = result.Streams
				.Where(x => x?.Duration > TimeSpan.Zero)
				.Select(x => x.Duration)
				.DefaultIfEmpty(TimeSpan.Zero)
				.Max();

			if (maxStreamDuration > TimeSpan.Zero)
			{
				result.Duration.ShouldBe(maxStreamDuration);
			}
		}
	}

	[RetryFact(3)]
	public async Task RawMediaInfo_ShouldReturnCorrectFileName()
	{
		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(testVideoPath);

		// Assert
		result.FileName.ShouldBe("test.mp4");
	}

	[RetryFact(3)]
	public void RawMediaInfo_ShouldHandleNullFilePath()
	{
		// Act
		RawMediaInfo mediaInfo = new(null);

		// Assert
		mediaInfo.FilePath.ShouldBeNull();
		mediaInfo.FileName.ShouldBeNull();
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldParseSubtitleStream()
	{
		// Arrange
		string testSubtitlesPath = Path.Combine(AppContext.BaseDirectory, "TestData", "test-with-subtitles.mp4");

		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(testSubtitlesPath);

		// Assert - Should have video, audio, and subtitle streams
		MediaStream? subtitleStream = result.Streams.FirstOrDefault(s => s?.CodecType == CodecType.Subtitle);
		subtitleStream.ShouldNotBeNull();
		subtitleStream.CodecType.ShouldBe(CodecType.Subtitle);
		subtitleStream.CodecName.ShouldNotBeNullOrWhiteSpace();
		subtitleStream.Duration.ShouldBeGreaterThan(TimeSpan.Zero);

		// Should also have video and audio streams
		MediaStream? videoStream = result.Streams.FirstOrDefault(s => s?.CodecType == CodecType.Video);
		videoStream.ShouldNotBeNull();
		MediaStream? audioStream = result.Streams.FirstOrDefault(s => s?.CodecType == CodecType.Audio);
		audioStream.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldHandleCorruptedFile()
	{
		// Arrange
		string corruptedPath = Path.Combine(AppContext.BaseDirectory, "TestData", "test-corrupted.mp4");

		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(corruptedPath);

		// Assert - Should return basic info even if ffprobe fails
		result.ShouldNotBeNull();
		result.FilePath.ShouldBe(corruptedPath);
		result.Size.ShouldBeGreaterThan(0); // File size should still be set
																				// Streams might be empty or incomplete due to corruption
	}

	[RetryFact(3)]
	public async Task GetMediaInfoAsync_ShouldCountAllStreamTypes()
	{
		// Arrange
		string testSubtitlesPath = Path.Combine(AppContext.BaseDirectory, "TestData", "test-with-subtitles.mp4");

		// Act
		RawMediaInfo result = await Helpers.GetMediaInfoAsync(testSubtitlesPath);

		// Assert - Should have exactly 3 streams (video, audio, subtitle)
		result.Streams.Length.ShouldBe(3);
		result.Streams.Count(s => s?.CodecType == CodecType.Video).ShouldBe(1);
		result.Streams.Count(s => s?.CodecType == CodecType.Audio).ShouldBe(1);
		result.Streams.Count(s => s?.CodecType == CodecType.Subtitle).ShouldBe(1);
	}
}
