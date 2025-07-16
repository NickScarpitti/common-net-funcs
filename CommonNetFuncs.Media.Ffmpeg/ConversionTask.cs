using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;
using static CommonNetFuncs.Core.Collections;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Media.Ffmpeg.Helpers;

namespace CommonNetFuncs.Media.Ffmpeg;

public sealed class HardwareAccelerationValues()
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
    /// <returns>True if conversion successfully completed</returns>
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
    /// <returns>True if conversion successfully completed</returns>
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
                //.UseHardwareAcceleration(HardwareAccelerator.auto, VideoCodec.h264, VideoCodec.av1) //This works
                //.UseHardwareAcceleration(HardwareAccelerator.auto, VideoCodec.h264_cuvid, VideoCodec.av1)
                //.UseHardwareAcceleration(HardwareAccelerator.auto, VideoCodec.h264_nvenc, VideoCodec.av1)
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
                    logger.Info($"#{conversionIndex} Progress:[{args.Duration}/{args.TotalLength}][{args.Percent}%]-[{fileToConvert.Name}]{(!string.IsNullOrWhiteSpace(taskDescription) ? $"[{taskDescription}]" : string.Empty)}{(!additionalLogText.IsNullOrWhiteSpace() ? $"[{additionalLogText}]" : string.Empty)}{(conversionOutputs.AnyFast() ? $"[Total Diff: {GetTotalFileDif(conversionOutputs)}]" : string.Empty)}[Total FPS: {GetTotalFps(fpsDict)}]");
                    lastOutput1 = DateTime.UtcNow;
                }
            };

            conversion.OnDataReceived += (sender, args) =>
            {
                if (DateTime.UtcNow > lastOutput2.AddSeconds(5))
                {
                    //Example output:
                    //frame=   48 fps=5.8 q=0.0 size=       1kB time=00:00:01.77 bitrate=   4.5kbits/s dup=0 drop=45 speed=0.215x
                    string unbrokenData = args.Data?.Replace(" ", string.Empty) ?? string.Empty;
                    int outputSize = 0;

                    if (unbrokenData.Contains("kBtime=") && unbrokenData.Contains("size="))
                    {
                        //Extract size from data
                        string sizeInkiB = unbrokenData[(unbrokenData.IndexOf("size=") + 5)..unbrokenData.IndexOf("kBtime=")];
                        if (int.TryParse(sizeInkiB, out int kiBSize))
                        {
                            outputSize = kiBSize * 1024; //Uses kibibytes
                        }
                    }

                    if (!fpsDict.Any(x => x.Key == conversionIndex))
                    {
                        if (args.Data?.Contains(" fps=") ?? false)
                        {
                            decimal fps = args.Data.ParseFfmpegLogFps();
                            if (fps >= 0)
                            {
                                fpsDict.TryAdd(conversionIndex, fps);
                            }
                        }
                    }
                    else
                    {
                        decimal? value = fpsDict.FirstOrDefault(x => x.Key == conversionIndex).Value;
                        if (value != null && (args.Data?.Contains(" fps=") ?? false))
                        {
                            decimal fps = args.Data.ParseFfmpegLogFps();
                            if (fps >= 0)
                            {
                                fpsDict.TryUpdate(conversionIndex, fps, (decimal)value);
                            }
                        }
                    }

                    if (cancelIfLarger && fileToConvert.Length < outputSize) //Cancel if new file is larger if that option is enabled
                    {
                        logger.Warn($"Canceling conversion due to converted size being greater than the source for #{conversionIndex} [{fileToConvert.Name}]");
                        conversionFailed = true;
                        sizeFailure = true;
                        cancellationTokenSource.Cancel();
                    }
                    else
                    {
                        string normalizedData = args.Data.NormalizeWhiteSpace().Replace("00:", string.Empty);

                        //round((totalTime - time) / speed)
                        decimal speed = 1m;
                        if (decimal.TryParse(unbrokenData[(unbrokenData.IndexOf("speed=") + 6)..(unbrokenData.Length - 2)], out decimal speedDecimal))
                        {
                            speed = speedDecimal;
                        }

                        //frame=   48 fps=5.8 q=0.0 size=       1kB time=00:00:01.77 bitrate=   4.5kbits/s dup=0 drop=45 speed=0.215x
                        string timeLeftString = "Unknown";
                        if (unbrokenData.Contains("time=") && unbrokenData.Contains("bitrate=") && speed != 0)
                        {
                            if (TimeSpan.TryParseExact(unbrokenData[(unbrokenData.IndexOf("time=") + 5)..unbrokenData.IndexOf("bitrate=")], @"hh\:mm\:ss\.ff", CultureInfo.InvariantCulture, out TimeSpan currentTime))
                            {
                                decimal offset = decimal.Parse(videoTimespan.Subtract(currentTime).TotalSeconds.ToString());
                                TimeSpan timeLeft = TimeSpan.FromSeconds((int)Math.Ceiling(offset / speed));
                                timeLeftString = timeLeft.ToString(@"hh\:mm\:ss");
                            }
                            else
                            {
                                timeLeftString = unbrokenData[(unbrokenData.IndexOf("time=") + 5)..unbrokenData.IndexOf("bitrate=")];
                                if (!timeLeftString.StrEq("N/A"))
                                {
                                    logger.Warn($"Unable to parse timeLeftString. Using raw value [{timeLeftString}] instead.\nFull unbroken output from ffmpeg = {unbrokenData}");
                                }
                            }
                        }

                        logger.Debug($"#{conversionIndex} ETA={timeLeftString} {normalizedData[..(normalizedData.Contains("bitrate=") ? normalizedData.IndexOf("bitrate=") : normalizedData.Length)]} - [{fileToConvert.Name}]{(!string.IsNullOrWhiteSpace(taskDescription) ? $"[{taskDescription}]" : string.Empty)}{(!additionalLogText.IsNullOrWhiteSpace() ? $"[{additionalLogText}]" : string.Empty)}{(conversionOutputs.AnyFast() ? $"[Total Diff: {GetTotalFileDif(conversionOutputs)}]" : string.Empty)}[Total FPS: {GetTotalFps(fpsDict)}]");

                        if (DateTime.UtcNow > lastOutput3.AddSeconds(30))
                        {
                            logger.Info($"#{conversionIndex} ETA={timeLeftString} {normalizedData[..(normalizedData.Contains("bitrate=") ? normalizedData.IndexOf("bitrate=") : normalizedData.Length)]} - [{fileToConvert.Name}]{(!string.IsNullOrWhiteSpace(taskDescription) ? $"[{taskDescription}]" : string.Empty)}{(!additionalLogText.IsNullOrWhiteSpace() ? $"[{additionalLogText}]" : string.Empty)}{(conversionOutputs.AnyFast() ? $"[Total Diff: {GetTotalFileDif(conversionOutputs)}]" : string.Empty)}[Total FPS: {GetTotalFps(fpsDict)}]");
                            lastOutput3 = DateTime.UtcNow;
                        }
                    }
                    lastOutput2 = DateTime.UtcNow;
                }
            };

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
