using System.Diagnostics;
using System.Text.Json;
using CommonNetFuncs.Core;

namespace CommonNetFuncs.Media.Ffmpeg.FfmpegRawCalls;

internal static partial class Helpers
{
  public static async Task<RawMediaInfo> GetMediaInfoAsync(string filePath)
  {
    RawMediaInfo mediaInfo = new(filePath);
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

        using JsonDocument? jsonDocument = JsonSerializer.Deserialize<JsonDocument>(output);
        if (jsonDocument == null)
        {
          return mediaInfo; // Return early if JSON parsing fails
        }

        if (!jsonDocument.RootElement.TryGetProperty("streams", out JsonElement formatElement))
        {
          return mediaInfo; // Return early if streams property is missing
        }

        int streamCount = formatElement.GetArrayLength();
        if (streamCount == 0)
        {
          return mediaInfo; // Return early if no streams are found
        }

        mediaInfo.Streams = new MediaStream[streamCount];
        int index = 0;
        foreach (JsonElement stream in formatElement.EnumerateArray())
        {
          if (stream.TryGetProperty("codec_type", out JsonElement codecType))
          {
            string codecTypeString = codecType.GetString() ?? string.Empty;
            if (codecTypeString.StrEq("video"))
            {
              mediaInfo.Streams[index] = new()
                            {
                                CodecType = CodecType.Video,
                                Width = stream.TryGetProperty("width", out JsonElement width) ? width.GetInt32() : null,
                                Height = stream.TryGetProperty("height", out JsonElement height) ? height.GetInt32() : null,
                                BitRate = stream.TryGetProperty("bit_rate", out JsonElement bitRate) ? bitRate.GetString().ToNInt() : null,
                                Duration = stream.TryGetProperty("duration", out JsonElement duration) ? TimeSpan.FromSeconds(duration.GetString().ToNDouble() ?? 0) : TimeSpan.Zero,
                                PixelFormat = stream.TryGetProperty("pix_fmt", out JsonElement pixelFormat) ? pixelFormat.GetString() : null,
                                CodecName = stream.TryGetProperty("codec_name", out JsonElement codecName) ? codecName.GetString() : null
                            };

              const string defaultFrameRate = "0/0";
              string averageFrameRate = (stream.TryGetProperty("avg_frame_rate", out JsonElement avgFrameRate) ? avgFrameRate.GetString() : defaultFrameRate) ?? defaultFrameRate;
              if (averageFrameRate == "0/0")
              {
                averageFrameRate = (stream.TryGetProperty("r_frame_rate", out JsonElement rFrameRate) ? rFrameRate.GetString() : defaultFrameRate) ?? defaultFrameRate;
              }

              string[]? frameRateParts = averageFrameRate.Split('/');
              mediaInfo.Streams[index].FrameRate = averageFrameRate != "0/0" && decimal.Parse(frameRateParts[1]) > 0 ? decimal.Parse(frameRateParts[0]) / decimal.Parse(frameRateParts[1]) : 0m;
            }
            else if (codecTypeString.StrEq("audio"))
            {
              mediaInfo.Streams[index] = new MediaStream
                            {
                                CodecType = CodecType.Audio,
                                BitRate = stream.TryGetProperty("bit_rate", out JsonElement bitRate) ? bitRate.GetString().ToNInt() : null,
                                Duration = stream.TryGetProperty("duration", out JsonElement duration) ? TimeSpan.FromSeconds(duration.GetString().ToNDouble() ?? 0) : TimeSpan.Zero,
                                PixelFormat = stream.TryGetProperty("pix_fmt", out JsonElement pixelFormat) ? pixelFormat.GetString() : null,
                                CodecName = stream.TryGetProperty("codec_name", out JsonElement codecName) ? codecName.GetString() : null
                            };
            }
            else if (codecTypeString.StrEq("subtitle"))
            {
              mediaInfo.Streams[index] = new MediaStream
                            {
                                CodecType = CodecType.Subtitle,
                                Duration = stream.TryGetProperty("duration", out JsonElement duration) ? TimeSpan.FromSeconds(duration.GetString().ToNDouble() ?? 0) : TimeSpan.Zero,
                                CodecName = stream.TryGetProperty("codec_name", out JsonElement codecName) ? codecName.GetString() : null
                            };
            }
          }
          index++;
        }
      }
    }
    catch
    {
      // If ffprobe fails, return basic info with file size
    }

    mediaInfo.Duration = mediaInfo.Streams
            .Where(x => x?.Duration > TimeSpan.Zero)
            .Select(x => x.Duration)
            .DefaultIfEmpty(TimeSpan.Zero)
            .Max();

    mediaInfo.VideoFormat = mediaInfo.Streams
            .Where(x => x?.CodecType == CodecType.Video)
            .Select(x => x.CodecName)
            .FirstOrDefault();

    mediaInfo.AudioFormat = mediaInfo.Streams
            .Where(x => x?.CodecType == CodecType.Audio)
            .Select(x => x.CodecName)
            .FirstOrDefault();
    return mediaInfo;
  }
}

internal class RawMediaInfo(string? filePath = null)
{
  public TimeSpan Duration { get; set; }

  public string? VideoFormat { get; set; }

  public string? AudioFormat { get; set; }

  public long Size { get; set; }

  public string? FilePath { get; set; } = filePath;

  public string? FileName => FilePath != null ? Path.GetFileName(FilePath) : null;

  public MediaStream[] Streams { get; set; } = [];
}

internal class MediaStream
{
  public CodecType? CodecType { get; set; }

  public string? CodecName { get; set; }

  public int? Width { get; set; }

  public int? Height { get; set; }

  public int? BitRate { get; set; }

  public TimeSpan Duration { get; set; }

  public decimal FrameRate { get; set; }

  public string? PixelFormat { get; set; }
}

public enum CodecType
{
  Video,
  Audio,
  Subtitle
}
