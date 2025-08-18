﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;
using static CommonNetFuncs.Core.Collections;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Media.Ffmpeg.Helpers;

namespace CommonNetFuncs.Media.Ffmpeg.FfmpegRawCalls;

internal static partial class ConversionTask
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
    public static async Task<bool> FfmpegConversionTaskFromXabe(FileInfo fileToConvert, string outputFileName, VideoCodec codec, Format outputFormat = Format.mp4, ConversionPreset conversionPreset = ConversionPreset.Slower,
        string? workingPath = null, int conversionIndex = 0, ConcurrentDictionary<int, decimal>? fpsDict = null, IMediaInfo? mediaInfo = null, int numberOfThreads = 1, bool cancelIfLarger = true,
        string? taskDescription = null, bool strict = true, bool overwriteOutput = true, ProcessPriorityClass processPriority = ProcessPriorityClass.BelowNormal,
        HardwareAccelerationValues? hardwareAccelerationValues = null, ConcurrentBag<string>? conversionOutputs = null, string? additionalLogText = null,
        CancellationTokenSource? cancellationTokenSource = null)
    {
        string ffmpegCommandArguments = await GetConversionCommandFromXabe(fileToConvert, outputFileName, codec, outputFormat, conversionPreset, workingPath, mediaInfo, numberOfThreads,
            strict, overwriteOutput, processPriority, hardwareAccelerationValues).ConfigureAwait(false);

        return await FfmpegConversionTask(fileToConvert, outputFileName, ffmpegCommandArguments, workingPath, conversionIndex, fpsDict, cancelIfLarger, taskDescription, conversionOutputs,
            additionalLogText, processPriority, cancellationTokenSource).ConfigureAwait(false);
    }

    public static async Task<string> GetConversionCommandFromXabe(FileInfo fileToConvert, string outputFileName, VideoCodec codec, Format outputFormat, ConversionPreset conversionPreset,
        string? workingPath = null, IMediaInfo? mediaInfo = null, int numberOfThreads = 1, bool strict = true, bool overwriteOutput = true, ProcessPriorityClass processPriority = ProcessPriorityClass.BelowNormal,
        HardwareAccelerationValues? hardwareAccelerationValues = null)
    {
        Conversion conversion = new();
        mediaInfo ??= await FFmpeg.GetMediaInfo($"{fileToConvert.@FullName}").ConfigureAwait(false);
        IVideoStream? videoStream = mediaInfo.VideoStreams.FirstOrDefault();
        IAudioStream? audioStream = mediaInfo.AudioStreams.FirstOrDefault();

        conversion
            .AddStream(audioStream)
            .SetOutput(Path.GetFullPath(Path.Combine(workingPath ?? Path.GetTempPath(), outputFileName)))
            .SetOverwriteOutput(overwriteOutput)
            .UseMultiThread(numberOfThreads)
            .SetPriority(processPriority);

        conversion.AddStream(videoStream?.SetCodec(codec!)).SetOutputFormat(outputFormat!).SetPreset(conversionPreset!);

        if (hardwareAccelerationValues != null)
        {
            conversion.UseHardwareAcceleration(hardwareAccelerationValues.hardwareAccelerator, hardwareAccelerationValues.decoder, hardwareAccelerationValues.encoder);
        }

        if (hardwareAccelerationValues != null)
        {
            conversion.UseHardwareAcceleration(hardwareAccelerationValues.hardwareAccelerator, hardwareAccelerationValues.decoder, hardwareAccelerationValues.encoder);
        }

        if (strict)
        {
            conversion.AddParameter("-strict -2");
        }

        return conversion.Build();
    }

    /// <summary>
    /// Run ffmpeg conversion via ffmpeg command. Requires ffmpeg to be in PATH or specified location.
    /// </summary>
    /// <param name="fileToConvert">Full file path and file name of the file to be converted</param>
    /// <param name="outputFileName">Name of the output file</param>
    /// <param name="ffmpegCommandArguments">All arguments that would come after "ffmpeg" in an ffmpeg CLI command</param>
    /// <param name="workingPath">Directory for conversion to put temporary files</param>
    /// <param name="conversionIndex">Optional: Index of this task. Used to distinguish between multiple tasks. Defaults to 0 if null</param>
    /// <param name="fpsDict">Optional: Dictionary used to display total conversion FPS. Can be used to sum total FPS between multiple simultaneous conversion tasks. If null, will only show FPS for current conversion</param>
    /// <param name="cancelIfLarger">Optional: Cancel the conversion task if the output becomes larger than the original file</param>
    /// <param name="taskDescription">Optional: Description to use in logging</param>
    /// <param name="conversionOutputs">Optional: Recorded results from CommonNetFuncs.Media.Ffmpeg.Helpers.RecordResults method. Used to display the total difference between original and converted files</param>
    /// <param name="additionalLogText">Optional: Additional text to include in the conversion output logs</param>
    /// <param name="processPriority">Optional: Priority level to run the conversion process at</param>
    /// <param name="cancellationTokenSource">Optional: Cancellation source for the conversion task</param>
    /// <returns><see langword="true"/> if conversion successfully completed</returns>
    public static async Task<bool> FfmpegConversionTask(FileInfo fileToConvert, string outputFileName, string ffmpegCommandArguments, string? workingPath = null, int conversionIndex = 0, ConcurrentDictionary<int, decimal>? fpsDict = null,
        bool cancelIfLarger = true, string? taskDescription = null, ConcurrentBag<string>? conversionOutputs = null, string? additionalLogText = null,
        ProcessPriorityClass processPriority = ProcessPriorityClass.BelowNormal, CancellationTokenSource ? cancellationTokenSource = null)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "ffmpeg",
                Arguments = ffmpegCommandArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process process = new() { StartInfo = startInfo };

            // Set up progress tracking
            DateTime lastProgressUpdate = DateTime.Now;
            Regex progressRegex = ProgressRegex();

            RawMediaInfo mediaInfo = await Helpers.GetMediaInfoAsync(fileToConvert.FullName).ConfigureAwait(false);

            string outputPath = Path.Combine(workingPath ?? Path.GetTempPath(), outputFileName);

            // TODO:: Check to make sure this is the correct regex for my ffmpeg version
            //TODO:: Check to make sure that this is the correct event to use for progress updates
            process.ErrorDataReceived += (sender, e) =>
            {
                if (DateTime.Now - lastProgressUpdate >= TimeSpan.FromSeconds(5))
                {
                    if (string.IsNullOrEmpty(e.Data))
                    {
                        return;
                    }

                    // Parse progress from FFmpeg output
                    Match match = progressRegex.Match(e.Data);
                    if (match.Success)
                    {
                        int hours = int.Parse(match.Groups[1].Value);
                        int minutes = int.Parse(match.Groups[2].Value);
                        int seconds = int.Parse(match.Groups[3].Value);
                        int centiseconds = int.Parse(match.Groups[4].Value);
                        decimal fps = decimal.Parse(match.Groups[5].Value);

                        TimeSpan currentTime = new(0, hours, minutes, seconds, centiseconds * 10);
                        decimal progress = mediaInfo.Duration.TotalSeconds > 0 ? (decimal)(currentTime.TotalSeconds / mediaInfo.Duration.TotalSeconds * 100) : 0;

                        // Update FPS dictionary
                        #pragma warning disable RCS1163 // Unused parameter
                        fpsDict?.AddOrUpdate(conversionIndex, fps, (key, oldValue) => fps);
                        #pragma warning restore RCS1163 // Unused parameter

                        // Log progress every 5 seconds (mimicking Xabe behavior)
                        string taskDescriptionText = !string.IsNullOrWhiteSpace(taskDescription) ? $"[{taskDescription}]" : string.Empty;
                        string additionalLogTextStr = !additionalLogText.IsNullOrWhiteSpace() ? $"[{additionalLogText}]" : string.Empty;
                        string totalDiffString = conversionOutputs.AnyFast() ? $"[Total Diff: {GetTotalFileDif(conversionOutputs)}]" : string.Empty;
                        decimal totalFps = fpsDict?.Values.Sum() ?? fps;
                        logger.Info($"#{conversionIndex}: Progress:[{currentTime.TotalSeconds}/{mediaInfo.Duration.TotalSeconds}][{progress:F1}%]-[{fileToConvert.Name}]{taskDescriptionText}{additionalLogTextStr}{totalDiffString} FPS:[{fps:F1}] Total FPS:[{totalFps:F1}]");
                        lastProgressUpdate = DateTime.Now;
                    }

                    // TODO:: Check for cancelIfLarger and size being larger than original file and if true for both, cancel the task

                    // TODO:: Display est remaining conversion time

                    // Check output file size if requested
                    if (cancelIfLarger && File.Exists(outputPath))
                    {
                        FileInfo outputInfo = new(outputPath);
                        if (outputInfo.Length > fileToConvert.Length)
                        {
                            logger.Info($"Task {conversionIndex}: Output file is larger than input, deleting output");
                            File.Delete(outputPath);
                            cancellationTokenSource?.Cancel();
                            logger.Info($"Task {conversionIndex}: Conversion cancelled due to output size");
                            process.Kill();
                        }
                    }
                }
            };

            process.Start();
            process.PriorityClass = processPriority;
            process.BeginErrorReadLine();

            // Wait for completion or cancellation
            Task completionTask = process.WaitForExitAsync();
            Task<bool> cancellationTask = cancellationTokenSource?.Token.WaitHandle.WaitOneAsync() ?? Task.FromResult(false);

            Task completedTask = await Task.WhenAny(completionTask, cancellationTask).ConfigureAwait(false);

            if (completedTask == cancellationTask)
            {
                process.Kill();
                logger.Info($"Task {conversionIndex}: Conversion cancelled");
                return false;
            }

            await completionTask;

            // Check if conversion was successful
            if (process.ExitCode != 0)
            {
                logger.Error($"Task {conversionIndex}: FFmpeg exited with code {process.ExitCode}");
                return false;
            }

            // Clean up FPS dictionary
            fpsDict?.TryRemove(conversionIndex, out _);

            logger.Info($"Task {conversionIndex}: Conversion completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Task {conversionIndex}: Error during conversion");

            // Clean up FPS dictionary on error
            fpsDict?.TryRemove(conversionIndex, out _);

            return false;
        }
        finally
        {
            fpsDict?.TryRemove(conversionIndex, out _);
        }
    }

    [GeneratedRegex(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2}).*?fps=\s*(\d+(?:\.\d+)?)")]
    private static partial Regex ProgressRegex();
}

// Extension method for WaitHandle
public static class WaitHandleExtensions
{
    public static Task<bool> WaitOneAsync(this WaitHandle waitHandle)
    {
        TaskCompletionSource<bool> tcs = new();

        #pragma warning disable RCS1163 // Unused parameter
        ThreadPool.RegisterWaitForSingleObject(waitHandle, (state, timedOut) => tcs.SetResult(!timedOut), null, -1, true);
        #pragma warning restore RCS1163 // Unused parameter

        return tcs.Task;
    }
}
