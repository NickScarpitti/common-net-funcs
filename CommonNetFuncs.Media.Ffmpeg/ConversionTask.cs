using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;
using static CommonNetFuncs.Core.Collections;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Media.Ffmpeg.Helpers;

namespace CommonNetFuncs.Media.Ffmpeg;

public sealed class HardwareAccelerationValues
{
	public HardwareAccelerator hardwareAccelerator { get; set; }

	public VideoCodec decoder { get; set; }

	public VideoCodec encoder { get; set; }

	public int device { get; set; }
}

public static class ConversionTask
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Run basic ffmpeg conversion via Xabe.FFmpeg settings. Requires Xabe to have the executables path set.
	/// </summary>
	/// <param name="fileToConvert">Full file path and file name of the file to be converted</param>
	/// <param name="outputFileName">Name of the output file</param>
	/// <param name="codec">Codec to convert to</param>
	/// <param name="outputFormat">Format to convert to</param>
	/// <param name="conversionPreset">Ffmpeg encoding preset to use</param>
	/// <param name="workingPath">Directory for conversion to put temporary files</param>
	/// <param name="conversionIndex">Optional: Index of this task. Used to distinguish between multiple tasks. Defaults to 0 if null</param>
	/// <param name="fpsDict">Optional: Dictionary used to display total conversion FPS. Can be used to sum total FPS between multiple simultaneous conversion tasks. If null, will only show FPS for current conversion</param>
	/// <param name="mediaInfo">Optional: MediaInfo object for the file being converted. Used when MediaInfo has already been retrieved to prevent extra processing. Will be populated if left null.</param>
	/// <param name="numberOfThreads">Optional: Number of threads to use in conversion task</param>
	/// <param name="cancelIfLarger">Optional: Cancel the conversion task if the output becomes larger than the original file</param>
	/// <param name="taskDescription">Optional: Description to use in logging</param>
	/// <param name="strict">Optional: Use strict flag for conversion</param>
	/// <param name="overwriteOutput">Optional: Allows for overwriting a file with the same name as the conversion output</param>
	/// <param name="processPriority">Optional: Priority level to run the conversion process at</param>
	/// <param name="hardwareAccelerationValues">Optional: Parameters for hardware acceleration</param>
	/// <param name="conversionOutputs">Optional: Recorded results from CommonNetFuncs.Media.Ffmpeg.Helpers.RecordResults method. Used to display the total difference between original and converted files</param>
	/// <param name="additionalLogText">Optional: Additional text to include in the conversion output logs</param>
	/// <param name="cancellationTokenSource">Optional: Cancellation source for the conversion task</param>
	/// <returns><see langword="true"/> if conversion successfully completed</returns>
	public static Task<bool> FfmpegConversionTask(FileInfo fileToConvert, string outputFileName, VideoCodec codec, Format outputFormat = Format.mp4, ConversionPreset conversionPreset = ConversionPreset.Slower,
				string? workingPath = null, int conversionIndex = 0, ConcurrentDictionary<int, decimal>? fpsDict = null, IMediaInfo? mediaInfo = null, int numberOfThreads = 1, bool cancelIfLarger = true,
				string? taskDescription = null, bool strict = true, bool overwriteOutput = true, ProcessPriorityClass processPriority = ProcessPriorityClass.BelowNormal,
				HardwareAccelerationValues? hardwareAccelerationValues = null, ConcurrentBag<string>? conversionOutputs = null, string? additionalLogText = null,
				CancellationTokenSource? cancellationTokenSource = null)
	{
		return FfmpegConversionTask(fileToConvert, outputFileName, workingPath, codec, outputFormat, conversionPreset, conversionIndex, fpsDict, mediaInfo, null, numberOfThreads, cancelIfLarger,
						taskDescription, strict, overwriteOutput, processPriority, hardwareAccelerationValues, conversionOutputs, additionalLogText, cancellationTokenSource);
	}

	/// <summary>
	/// Run ffmpeg conversion via ffmpeg command. Requires Xabe to have the executables path set.
	/// </summary>
	/// <param name="fileToConvert">Full file path and file name of the file to be converted</param>
	/// <param name="outputFileName">Name of the output file</param>
	/// <param name="ffmpegCommand">Command to execute in ffmpeg. Input parameter should not be included in this command.</param>
	/// <param name="workingPath">Directory for conversion to put temporary files</param>
	/// <param name="conversionIndex">Optional: Index of this task. Used to distinguish between multiple tasks. Defaults to 0 if null</param>
	/// <param name="fpsDict">Optional: Dictionary used to display total conversion FPS. Can be used to sum total FPS between multiple simultaneous conversion tasks. If null, will only show FPS for current conversion</param>
	/// <param name="mediaInfo">Optional: MediaInfo object for the file being converted. Used when MediaInfo has already been retrieved to prevent extra processing. Will be populated if left null.</param>
	/// <param name="numberOfThreads">Optional: Number of threads to use in conversion task</param>
	/// <param name="cancelIfLarger">Optional: Cancel the conversion task if the output becomes larger than the original file</param>
	/// <param name="taskDescription">Optional: Description to use in logging</param>
	/// <param name="strict">Optional: Use strict flag for conversion</param>
	/// <param name="overwriteOutput">Optional: Allows for overwriting a file with the same name as the conversion output</param>
	/// <param name="processPriority">Optional: Priority level to run the conversion process at</param>
	/// <param name="hardwareAccelerationValues">Optional: Parameters for hardware acceleration</param>
	/// <param name="conversionOutputs">Optional: Recorded results from CommonNetFuncs.Media.Ffmpeg.Helpers.RecordResults method. Used to display the total difference between original and converted files</param>
	/// <param name="additionalLogText">Optional: Additional text to include in the conversion output logs</param>
	/// <param name="cancellationTokenSource">Optional: Cancellation source for the conversion task</param>
	/// <returns><see langword="true"/> if conversion successfully completed</returns>
	public static Task<bool> FfmpegConversionTask(FileInfo fileToConvert, string outputFileName, string? ffmpegCommand, string? workingPath = null, int conversionIndex = 0,
			ConcurrentDictionary<int, decimal>? fpsDict = null, IMediaInfo? mediaInfo = null, int numberOfThreads = 1, bool cancelIfLarger = true, string? taskDescription = null, bool strict = true,
			bool overwriteOutput = true, ProcessPriorityClass processPriority = ProcessPriorityClass.BelowNormal, HardwareAccelerationValues? hardwareAccelerationValues = null,
			ConcurrentBag<string>? conversionOutputs = null, string? additionalLogText = null, CancellationTokenSource? cancellationTokenSource = null)
	{
		return FfmpegConversionTask(fileToConvert, outputFileName, workingPath, null, null, null, conversionIndex, fpsDict, mediaInfo, ffmpegCommand, numberOfThreads, cancelIfLarger,
			taskDescription, strict, overwriteOutput, processPriority, hardwareAccelerationValues, conversionOutputs, additionalLogText, cancellationTokenSource);
	}

	private static async Task<bool> FfmpegConversionTask(FileInfo fileToConvert, string outputFileName, string? workingPath = null, VideoCodec? codec = null, Format? outputFormat = null,
			ConversionPreset? conversionPreset = null, int conversionIndex = 0, ConcurrentDictionary<int, decimal>? fpsDict = null, IMediaInfo? mediaInfo = null, string? ffmpegCommand = null,
			int numberOfThreads = 1, bool cancelIfLarger = true, string? taskDescription = null, bool strict = true, bool overwriteOutput = true, ProcessPriorityClass processPriority = ProcessPriorityClass.BelowNormal,
			HardwareAccelerationValues? hardwareAccelerationValues = null, ConcurrentBag<string>? conversionOutputs = null, string? additionalLogText = null, CancellationTokenSource? cancellationTokenSource = null)
	{
		bool conversionFailed = false;
		bool sizeFailure = false;
		fpsDict ??= new();
		cancellationTokenSource ??= new();
		try
		{
			DateTime lastOutput1 = DateTime.UtcNow.AddSeconds(-6);
			DateTime lastOutput2 = DateTime.UtcNow.AddSeconds(-6);
			DateTime lastOutput3 = DateTime.UtcNow.AddSeconds(-6);

			Conversion conversion = new();
			mediaInfo ??= await FFmpeg.GetMediaInfo($"{fileToConvert.@FullName}").ConfigureAwait(false);
			IVideoStream? videoStream = mediaInfo.VideoStreams.FirstOrDefault();
			IAudioStream? audioStream = mediaInfo.AudioStreams.FirstOrDefault();

			TimeSpan videoTimespan = videoStream?.Duration ?? TimeSpan.FromSeconds(0);

			conversion
				.AddStream(audioStream)
				.SetOutput(Path.GetFullPath(Path.Combine(workingPath ?? Path.GetTempPath(), outputFileName)))
				.SetOverwriteOutput(overwriteOutput)
				.UseMultiThread(numberOfThreads)
				.SetPriority(processPriority);

			if (string.IsNullOrWhiteSpace(ffmpegCommand))
			{
				conversion.AddStream(videoStream?.SetCodec((VideoCodec)codec!)).SetOutputFormat((Format)outputFormat!).SetPreset((ConversionPreset)conversionPreset!);
			}
			else
			{
				conversion.AddStream(videoStream).AddParameter(ffmpegCommand);
			}

			if (hardwareAccelerationValues != null)
			{
				conversion.UseHardwareAcceleration(hardwareAccelerationValues.hardwareAccelerator, hardwareAccelerationValues.decoder, hardwareAccelerationValues.encoder);
			}

			if (hardwareAccelerationValues != null)
			{
				conversion.UseHardwareAcceleration(hardwareAccelerationValues.hardwareAccelerator, hardwareAccelerationValues.decoder, hardwareAccelerationValues.encoder);
			}

			//Add log to OnProgress
			conversion.OnProgress += (sender, args) =>
			{
				if (DateTime.UtcNow > lastOutput1.AddSeconds(5))
				{
					//Show all output from FFmpeg to console
					StringBuilder stringBuilder = new();
					stringBuilder.Append('#');
					stringBuilder.Append(conversionIndex);
					stringBuilder.Append(" Progress:[");
					stringBuilder.Append(args.Duration);
					stringBuilder.Append('/');
					stringBuilder.Append(args.TotalLength);
					stringBuilder.Append("][");
					stringBuilder.Append(args.Percent);
					stringBuilder.Append("%]-[");
					stringBuilder.Append(fileToConvert.Name);
					stringBuilder.Append(!string.IsNullOrWhiteSpace(taskDescription) ? $"[{taskDescription}]" : string.Empty);
					stringBuilder.Append(!additionalLogText.IsNullOrWhiteSpace() ? $"[{additionalLogText}]" : string.Empty);
					stringBuilder.Append(conversionOutputs.AnyFast() ? $"[Total Diff: {GetTotalFileDif(conversionOutputs)}]" : string.Empty);
					stringBuilder.Append("[Total FPS: ");
					stringBuilder.Append(GetTotalFps(fpsDict));
					stringBuilder.Append(']');

					logger.Info(stringBuilder.ToString());
					lastOutput1 = DateTime.UtcNow;
				}
			};

			conversion.OnDataReceived += (sender, args) => args.LogFfmpegOutput(ref lastOutput2, ref lastOutput3, ref conversionFailed, ref sizeFailure, fileToConvert, videoTimespan, conversionIndex,
				cancelIfLarger, taskDescription, additionalLogText, conversionOutputs, fpsDict, cancellationTokenSource);

			//Start conversion
			logger.Info($"Starting ffmpeg conversion with command: {ffmpegCommand}");

			try
			{
				if (strict)
				{
					conversion.AddParameter("-strict -2");
					await conversion.Start(cancellationTokenSource.Token).ConfigureAwait(false);
				}
				else
				{
					await conversion.Start(cancellationTokenSource.Token).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException ex)
			{
				//Xabe seems to throw OperationCanceledException so this is explicitly handled here
				logger.Warn(ex, $"Conversion of file [{fileToConvert.Name}] successfully canceled in ffmpeg task for reason: {(sizeFailure ? "[Result Too Large]." : "[Unknown Failure]")}.");
				conversionFailed = true;
			}

			//await Console.Out.WriteLineAsync($"Finished conversion file [{fileToConvert.Name}]");
			logger.Info($"Finished conversion for #{conversionIndex} [{fileToConvert.Name}] with {(conversionFailed ? "[FAILED]" : "[SUCCESS]")} Status");
		}
		catch (ConversionException cex)
		{
			logger.Error(cex, "Conversion task failed!");
		}
		finally
		{
			fpsDict.Remove(conversionIndex, out _); //Remove FPS item for completed conversions
		}

		return !conversionFailed; //Returns as success status by inverting this value
	}
}
