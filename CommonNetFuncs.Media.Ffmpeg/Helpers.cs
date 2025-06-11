using System.Collections.Concurrent;
using System.Globalization;
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

    public static decimal GetTotalFps(ConcurrentDictionary<int, decimal> fpsDict)
    {
        return fpsDict.Sum(x => x.Value);
    }

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

    public static string GetTotalFileDif(ConcurrentBag<string> conversionOutputs)
    {
        NumberFormatInfo format = new() { NegativeSign = "-" }; //Needed to do this so negatives are read correctly
        return conversionOutputs.Sum(x => x.Split(",").Where(y => y.Contains($"{EOutputTags.SizeDif}")).Select(y => long.Parse(y.Replace($"{EOutputTags.SizeDif}=", string.Empty), format)).FirstOrDefault()).GetFileSizeFromBytesWithUnits();
    }

    public static async Task RecordResults(string fileName, bool success, ConcurrentBag<string> conversionOutputs, string logFile, long? originalSize = null, long? endSize = null)
    {
        await RecordResults(fileName, success, conversionOutputs, originalSize, endSize, logFile).ConfigureAwait(false);
    }

    public static async Task RecordResults(string fileName, bool success, ConcurrentBag<string> conversionOutputs, long? originalSize = null, long? endSize = null)
    {
        await RecordResults(fileName, success, conversionOutputs, originalSize, endSize, null).ConfigureAwait(false);
    }

    private static async Task RecordResults(string fileName, bool success, ConcurrentBag<string> conversionOutputs, long? originalSize = null, long? endSize = null, string? logFile = null)
    {
        string outputString = $"{EOutputTags.FileName}={fileName},{EOutputTags.Success}={success},{EOutputTags.OriginalSize}={originalSize.GetFileSizeFromBytesWithUnits()},{EOutputTags.EndSize}={endSize.GetFileSizeFromBytesWithUnits()}";
        outputString += originalSize != null && endSize != null ? $"{EOutputTags.SizeRatio}={Math.Round((decimal)endSize / (decimal)originalSize, 2) * 100}%,{EOutputTags.SizeDif}={endSize - originalSize}" : string.Empty;
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
    /// Gets the number of frames between key frames in the given video
    /// </summary>
    /// <param name="fileName">Full video path and file name of the video file to get key frame spacing of</param>
    /// <returns>The average number of frames between keyframes</returns>
    public static async Task<decimal> GetKeyFrameSpacing(this string fileName, int numberOfSamples = -1, int sampleLengthSec = -1)
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
