using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Xabe.FFmpeg;
using static CommonNetFuncs.Core.Collections;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Core.UnitConversion;
using static CommonNetFuncs.Media.Ffmpeg.Constants;

namespace CommonNetFuncs.Media.Ffmpeg;

public static class Helpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Gets the total FPS from a dictionary of FPS values.
    /// </summary>
    /// <param name="fpsDict">Dictionary containing (typically active) conversion tasks with their task Id as the key and the associated FPS value.</param>
    /// <returns>Total frames per second</returns>
    public static decimal GetTotalFps(ConcurrentDictionary<int, decimal> fpsDict)
    {
        return fpsDict.Sum(x => x.Value);
    }

    /// <summary>
    /// Gets FPS from the ffmpeg log output.
    /// </summary>
    /// <param name="data">FFMpeg log line</param>
    /// <returns>Frames per second value</returns>
    public static decimal ParseFfmpegLogFps(this string data)
    {
        decimal fps = -1;
        try
        {
            int start = data.IndexOf(" fps=") + 5;
            int end = data.IndexOf(' ', start);
            if (start == end)
            {
                start = data.IndexOf(" fps= ") + 6;
                end = data.IndexOf(' ', start);
            }
            int length = end - start;
            fps = decimal.Parse(data.Substring(start, length));
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfException()} Error");
        }
        return fps;
    }

    /// <summary>
    /// / Gets the total file size difference from all conversion outputs
    /// </summary>
    /// <param name="conversionOutputs">Collection of results recorded in an object using RecordResults</param>
    /// <returns>The total difference between the size of all inputs and all outputs in bytes / KB / MB / GB</returns>
    public static string GetTotalFileDif(ConcurrentBag<string> conversionOutputs)
    {
        NumberFormatInfo format = new() { NegativeSign = "-" }; //Needed to do this so negatives are read correctly
        return conversionOutputs.Sum(x => x.Split(",").Where(y => y.Contains($"{EOutputTags.SizeDif}")).Select(y => long.Parse(y.Replace($"{EOutputTags.SizeDif}=", string.Empty), format)).FirstOrDefault()).GetFileSizeFromBytesWithUnits();
    }

    /// <summary>
    /// Records the results of the conversion process.
    /// </summary>
    /// <param name="fileName">Name of the file being processed.</param>
    /// <param name="success">Indicates whether the conversion was successful.</param>
    /// <param name="conversionOutputs">Collection of results to add result to.</param>
    /// <param name="logFile">Path to the log file to record results in.</param>
    /// <param name="originalSize">Original file size.</param>
    /// <param name="endSize">Final file size after FFMpeg conversion.</param>
    public static async Task RecordResults(string fileName, bool success, ConcurrentBag<string> conversionOutputs, string logFile, long? originalSize = null, long? endSize = null)
    {
        await RecordResults(fileName, success, conversionOutputs, originalSize, endSize, logFile).ConfigureAwait(false);
    }

    /// <summary>
    /// Records the results of the conversion process.
    /// </summary>
    /// <param name="fileName">Name of the file being processed.</param>
    /// <param name="success">Indicates whether the conversion was successful.</param>
    /// <param name="conversionOutputs">Collection of results to add result to.</param>
    /// <param name="originalSize">Original file size.</param>
    /// <param name="endSize">Final file size after FFMpeg conversion.</param>
    public static async Task RecordResults(string fileName, bool success, ConcurrentBag<string> conversionOutputs, long? originalSize = null, long? endSize = null)
    {
        await RecordResults(fileName, success, conversionOutputs, originalSize, endSize, null).ConfigureAwait(false);
    }

    private static async Task RecordResults(string fileName, bool success, ConcurrentBag<string> conversionOutputs, long? originalSize = null, long? endSize = null, string? logFile = null)
    {
        string outputString = $"{EOutputTags.FileName}={fileName},{EOutputTags.Success}={success},{EOutputTags.OriginalSize}={originalSize.GetFileSizeFromBytesWithUnits()},{EOutputTags.EndSize}={endSize.GetFileSizeFromBytesWithUnits()}";
        outputString += originalSize != null && endSize != null ? $",{EOutputTags.SizeRatio}={Math.Round((decimal)endSize / (decimal)originalSize, 2) * 100}%,{EOutputTags.SizeDif}={endSize - originalSize} bytes" : string.Empty;
        conversionOutputs.Add(outputString);

        if (!logFile.IsNullOrWhiteSpace())
        {
            if (!File.Exists(logFile))
            {
                File.Create(logFile).Dispose();
            }
            await using StreamWriter streamWriter = new(logFile, true);
            await streamWriter.WriteLineAsync(outputString).ConfigureAwait(false);
            await streamWriter.FlushAsync().ConfigureAwait(false);
            streamWriter.Close();
        }
    }

    /// <summary>
    /// Gets a single metadata value from a file. Requires Xabe to have the executables path set.
    /// </summary>
    /// <param name="fileName">Full video path and file name of the video file to get metadata from</param>
    /// <param name="videoMetadataItem">Metadata item to get</param>
    /// <returns>Value for the specified metadata item</returns>
    public static async Task<string?> GetVideoMetadata(this string fileName, EVideoMetadata videoMetadataItem)
    {
        try
        {
            if (fileName.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(fileName), "File name cannot be null or empty.");
            }

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException($"The file '{fileName}' does not exist.");
            }

            return await Probe.New().Start($@"-v quiet -select_streams v:0 -of default=noprint_wrappers=1:nokey=1 -show_entries stream={videoMetadataItem.ToString().ToLowerInvariant()} ""{fileName}""").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfException()} Error");
        }
        return null;
    }

    /// <summary>
    /// The frame rate of the given video file
    /// </summary>
    /// <param name="fileName">Full video path and file name of the video file to get key frame spacing of</param>
    /// <returns>The number of frames per second of the video</returns>
    public static async Task<decimal> GetFrameRate(this string fileName)
    {
        try
        {
            string? frameRate = await fileName.GetVideoMetadata(EVideoMetadata.Avg_Frame_Rate).ConfigureAwait(false);
            if (frameRate != null)
            {
                frameRate = frameRate.Replace(Environment.NewLine, string.Empty);
                return Math.Round(decimal.Parse(frameRate.Split("/")[0]) / decimal.Parse(frameRate.Split("/")[1]), 2, MidpointRounding.AwayFromZero);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfException()} Error");
        }
        return -1;
    }

    /// <summary>
    /// Gets the number of frames between key frames in the given video.
    /// </summary>
    /// <param name="fileName">Full video path and file name of the video file to get key frame spacing of</param>
    /// <remarks>Samples entire video which may take a long time. Use overload with numberOfSamples, sampleLengthSec to specify sample limits</remarks>
    /// <returns>The average number of frames between keyframes</returns>
    public static Task<decimal> GetKeyFrameSpacing(this string fileName)
    {
        return GetKeyFrameSpacing(fileName, -1, -1);
    }

    /// <summary>
    /// Gets the number of frames between key frames in the given video
    /// </summary>
    /// <param name="fileName">Full video path and file name of the video file to get key frame spacing of</param>
    /// <param name="numberOfSamples">Number of samples to take from the video. If -1, will sample the entire video</param>
    /// <param name="sampleLengthSec">Length of each sample in seconds. Default is -1, which means that the entire video will be sampled</param>
    /// <returns>The average number of frames between keyframes</returns>
    public static async Task<decimal> GetKeyFrameSpacing(this string fileName, int numberOfSamples, int sampleLengthSec)
    {
        decimal frameRate = await fileName.GetFrameRate().ConfigureAwait(false);
        try
        {
            if (numberOfSamples == -1)
            {
                string probeResult = (await Probe.New().Start($"-v quiet -hide_banner -skip_frame nokey -select_streams v:0 -show_entries frame=pts_time -of csv=p=0 \"{fileName}\"").ConfigureAwait(false)).Replace("\"", string.Empty).Replace(",", string.Empty);
                List<decimal> keyFrameTimeStamps = ParseKeyFrameProbe(probeResult);
                return GetAverageFramesToNextKeyFrame(keyFrameTimeStamps, frameRate);
            }
            else
            {
                if (sampleLengthSec <= 0)
                {
                    sampleLengthSec = 10;
                }

                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName).ConfigureAwait(false);
                IVideoStream? videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                TimeSpan videoTimespan = videoStream?.Duration ?? TimeSpan.FromSeconds(0);

                List<string> probeResults = [];
                if (numberOfSamples * sampleLengthSec >= videoTimespan.TotalSeconds)
                {
                    probeResults.Add((await Probe.New().Start($"-v quiet -hide_banner -skip_frame nokey -select_streams v:0 -show_entries frame=pts_time -of csv=p=0 \"{fileName}\"").ConfigureAwait(false)).Replace("\"", string.Empty).Replace(",", string.Empty));
                }
                else
                {
                    int secondsBetween = (int)Math.Floor(videoTimespan.TotalSeconds / numberOfSamples);
                    for (int i = 0; i < numberOfSamples; i++)
                    {
                        probeResults.Add((await Probe.New().Start($"-read_intervals {i * secondsBetween}%+{(i * secondsBetween) + sampleLengthSec} -v quiet -hide_banner -skip_frame nokey -select_streams v:0 -show_entries frame=pts_time -of csv=p=0 \"{fileName}\"").ConfigureAwait(false)).Replace("\"", string.Empty).Replace(",", string.Empty));
                    }
                }

                List<decimal> sampleAverages = [];
                foreach (string probeResult in probeResults)
                {
                    List<decimal> keyFrameTimeStamps = ParseKeyFrameProbe(probeResult);
                    sampleAverages.Add(GetAverageFramesToNextKeyFrame(keyFrameTimeStamps, frameRate));
                }
                return sampleAverages.AnyFast() ? sampleAverages.Average() : -1;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfException()} Error");
        }
        return -1;
    }

    public static void LogFfmpegOutput(this DataReceivedEventArgs args, ref DateTime lastOutput, ref DateTime lastSummaryOutput, ref bool conversionFailed, ref bool sizeFailure, FileInfo fileToConvert, TimeSpan videoTimespan,
        int conversionIndex, bool cancelIfLarger, string? taskDescription, string? additionalLogText, ConcurrentBag<string>? conversionOutputs, ConcurrentDictionary<int, decimal>? fpsDict,
        CancellationTokenSource? cancellationTokenSource)
    {
        if (DateTime.UtcNow > lastOutput.AddSeconds(5))
        {
            if (args.Data.IsNullOrWhiteSpace())
            {
                return;
            }

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

            if (!fpsDict?.Any(x => x.Key == conversionIndex) ?? true)
            {
                if (args.Data?.Contains(" fps=") ?? false)
                {
                    decimal fps = args.Data.ParseFfmpegLogFps();
                    if (fps >= 0)
                    {
                        fpsDict?.TryAdd(conversionIndex, fps);
                    }
                }
            }
            else
            {
                decimal? value = fpsDict?.FirstOrDefault(x => x.Key == conversionIndex).Value;
                if (value != null && (args.Data?.Contains(" fps=") ?? false))
                {
                    decimal fps = args.Data.ParseFfmpegLogFps();
                    if (fps >= 0)
                    {
                        fpsDict?.TryUpdate(conversionIndex, fps, (decimal)value);
                    }
                }
            }

            if (cancelIfLarger && fileToConvert.Length < outputSize) //Cancel if new file is larger if that option is enabled
            {
                logger.Warn($"Canceling conversion due to converted size being greater than the source for #{conversionIndex} [{fileToConvert.Name}]");
                conversionFailed = true;
                sizeFailure = true;
                cancellationTokenSource?.Cancel();
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

                StringBuilder stringBuilder = new();
                stringBuilder.Append('#');
                stringBuilder.Append(conversionIndex);
                stringBuilder.Append(" ETA =");
                stringBuilder.Append(timeLeftString);
                stringBuilder.Append(' ');
                stringBuilder.Append(normalizedData[..(normalizedData.Contains("bitrate=") ? normalizedData.IndexOf("bitrate=") : normalizedData.Length)]);
                stringBuilder.Append(" - [");
                stringBuilder.Append(fileToConvert.Name);
                stringBuilder.Append(']');
                stringBuilder.Append(!string.IsNullOrWhiteSpace(taskDescription) ? $"[{taskDescription}]" : string.Empty);
                stringBuilder.Append(!additionalLogText.IsNullOrWhiteSpace() ? $"[{additionalLogText}]" : string.Empty);
                stringBuilder.Append(conversionOutputs.AnyFast() ? $"[Total Diff: {GetTotalFileDif(conversionOutputs)}]" : string.Empty);
                stringBuilder.Append("[Total FPS: ");
                stringBuilder.Append(GetTotalFps(fpsDict ?? []));
                stringBuilder.Append(']');

                string logString = stringBuilder.ToString();
                logger.Debug(logString);

                if (DateTime.UtcNow > lastSummaryOutput.AddSeconds(30))
                {
                    logger.Info(logString);
                    lastSummaryOutput = DateTime.UtcNow;
                }
            }
            lastOutput = DateTime.UtcNow;
        }
    }

    private static List<decimal> ParseKeyFrameProbe(string probeResult)
    {
        List<decimal> keyFrameTimeStamps = [];

        //foreach (string result in probeResult.Split(Environment.NewLine).Where(x => !x.IsNullOrWhiteSpace()))
        foreach (string result in probeResult.SplitLines().Where(x => !x.IsNullOrWhiteSpace()))
        {
            //.Select(decimal.Parse).Order().ToList()
            if (decimal.TryParse(result, out decimal numericResult))
            {
                keyFrameTimeStamps.Add(numericResult);
            }
            else
            {
                logger.Warn($"\"{result}\" could not be parsed as a valid timestamp and was skipped");
            }
        }
        return keyFrameTimeStamps;
    }

    private static decimal GetAverageFramesToNextKeyFrame(List<decimal> keyFrameTimeStamps, decimal frameRate)
    {
        keyFrameTimeStamps = keyFrameTimeStamps.Order().ToList();
        List<decimal> framesToNextKeyFrame = [];
        for (int i = 0; i < keyFrameTimeStamps.Count; i++)
        {
            if (i != 0)
            {
                decimal timeDifferenceSec = keyFrameTimeStamps[i] - keyFrameTimeStamps[i - 1];
                framesToNextKeyFrame.Add(timeDifferenceSec * frameRate);
            }
        }
        return framesToNextKeyFrame.AnyFast() ? Math.Round(framesToNextKeyFrame.Average(), 2, MidpointRounding.AwayFromZero) : -1;
    }

    public enum EVideoMetadata
    {
        Index,
        Codec_Name,
        Codec_Long_Name,
        Profile,
        Codec_Type,
        Codec_Tag_String,
        Codec_Tag,
        Width,
        Height,
        Coded_Width,
        Coded_Height,
        Closed_Captions,
        Film_Grain,
        Has_B_Frames,
        Sample_Aspect_Ratio,
        Display_Aspect_Ratio,
        Pix_Fmt,
        Level,
        Color_Range,
        Color_Space,
        Color_Transformer,
        Color_Primaries,
        Chroma_Location,
        Field_Order,
        Refs,
        Id,
        R_Frame_Rate,
        Avg_Frame_Rate,
        Time_Base,
        Start_Pts,
        Start_Time,
        Duration_Ts,
        Duration,
        Bit_Rate,
        Max_Bit_Rate,
        Bits_Per_Raw_Sample,
        Nb_Frames,
        Nb_Read_Frames,
        Nb_Read_Packets,
        Extradata_Size,
    }
}
