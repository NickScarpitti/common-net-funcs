using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CommonNetFuncs.Media.Ffmpeg.FfmpegRawCalls;

internal static partial class Helpers
{
    public static async Task<RawMediaInfo> GetMediaInfoAsync(string filePath)
    {
        RawMediaInfo mediaInfo = new();
        FileInfo fileInfo = new(filePath);
        mediaInfo.Size = fileInfo.Length;

        // Use ffprobe to get media information
        ProcessStartInfo startInfo = new()
        {
            FileName = "ffprobe",
            Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process != null)
            {
                string output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await process.WaitForExitAsync().ConfigureAwait(false);

                // Parse JSON output to extract duration and format info
                // This is a simplified parser - you might want to use System.Text.Json for robust parsing
                if (output.Contains("duration"))
                {
                    Match durationMatch = DurationRegex().Match(output);
                    if (durationMatch.Success && double.TryParse(durationMatch.Groups[1].Value, out double seconds))
                    {
                        mediaInfo.Duration = TimeSpan.FromSeconds(seconds);
                    }
                }

                if (output.Contains("codec_name"))
                {
                    Match videoCodecMatch = VideoCodecRegex().Match(output);
                    if (videoCodecMatch.Success)
                    {
                        mediaInfo.VideoFormat = videoCodecMatch.Groups[1].Value;
                    }

                    Match audioCodecMatch = AudioCodexRegex().Match(output);
                    if (audioCodecMatch.Success)
                    {
                        mediaInfo.AudioFormat = audioCodecMatch.Groups[1].Value;
                    }
                }
            }
        }
        catch
        {
            // If ffprobe fails, return basic info with file size
        }

        return mediaInfo;
    }

    [GeneratedRegex(@"""duration""\s*:\s*""([^""]+)""")]
    private static partial Regex DurationRegex();

    [GeneratedRegex(@"""codec_type""\s*:\s*""audio"".*?""codec_name""\s*:\s*""([^""]+)""", RegexOptions.Singleline)]
    private static partial Regex AudioCodexRegex();

    [GeneratedRegex(@"""codec_type""\s*:\s*""video"".*?""codec_name""\s*:\s*""([^""]+)""", RegexOptions.Singleline)]
    private static partial Regex VideoCodecRegex();
}

internal class RawMediaInfo(string? filePath = null)
{
    public TimeSpan Duration { get; set; }

    public string? VideoFormat { get; set; }

    public string? AudioFormat { get; set; }

    public long Size { get; set; }

    public string? FilePath { get; set; } = filePath;
}
