# CommonNetFuncs.Media.Ffmpeg

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Media.Ffmpeg)](https://www.nuget.org/packages/CommonNetFuncs.Media.Ffmpeg/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Media.Ffmpeg)](https://www.nuget.org/packages/CommonNetFuncs.Media.Ffmpeg/)

This project contains helper methods using Xabe.FFMpeg including file conversion and video metadata extraction.

## Contents

- [CommonNetFuncs.Media.Ffmpeg](#commonnetfuncsmediaffmpeg)
  - [Contents](#contents)
  - [ConversionTask](#conversiontask)
    - [ConversionTask Usage Examples](#conversiontask-usage-examples)
      - [FfmpegConversionTask](#ffmpegconversiontask)
  - [Helpers](#helpers)
    - [Helpers Usage Examples](#helpers-usage-examples)
      - [GetTotalFps](#gettotalfps)
      - [ParseFfmpegLogFps](#parseffmpeglogfps)
      - [GetTotalFileDif](#gettotalfiledif)
      - [RecordResults](#recordresults)
      - [GetVideoMetadata](#getvideometadata)
      - [GetFrameRate](#getframerate)
      - [GetKeyFrameSpacing](#getkeyframespacing)

---

## ConversionTask

[Description here]

### ConversionTask Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### FfmpegConversionTask

Run a task using FFmpeg CLI commands or built-in Xabe.FFmpeg configurations. Outputs standardized information emitted by FFmpeg during conversion.

```cs
bool success = await ConversionTask.FfmpegConversionTask(
  @"C:\path\to\source\file.mp4", // File to convert
  "ConvertedFile.mp4", // Output file name
  "-c:v libsvtav1 -c:a copy -row-mt true -crf 29 -preset 4 -g 290 -cpu-used 0 -movflags faststart -fpsmax 30 -svtav1-params tune=0:scd=1:scm=0:fast-decode=1 -pix_fmt yuv420p10le -y"; // FFMpeg command line arguments
); // Outputs progress and returns true if successful. "ConvertedFile.mp4" will be emitted
```

---

## Helpers

Helper methods to make using FFMpeg easier, including getting attributes using FFMpeg as well as parsing FFMpeg output.

### Helpers Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetTotalFps

Gets the total frames per second (FPS) conversion rate of all videos being converted and sharing the same fpsDict (ConcurrentDictionary<int, decimal> where the key is the ID of the conversion task and the value is the conversion rate in FPS for that task).

```cs
decimal totalFps = Helpers.GetTotalFps(fpsDict); // Sum of all FPS values in fpsDict
```

#### ParseFfmpegLogFps

Extracts the FPS value from a given FFMpeg log line.

```cs
decimal fps = ffmpegLogLine.ParseFfmpegLogFps(); // Gets the FPS value as a decimal
```

#### GetTotalFileDif

Gets the total difference in file size between all source and destination files that have been processed and had their output recorded with [RecordResults](#recordresults) in a ConcurrentBag<string> object.

```cs
string totalDifference = Helpers.GetTotalFileDif(conversionOutputs); // -543.21 MB <-- all outputs are 543.21 MB smaller than all of their source files
```

#### RecordResults

Records the results of a conversion task, including file name, success status, original file size, and converted file size, into a ConcurrentBag<string> for later retrieval and optionally to a text file as well.

```cs
Helpers.RecordResults(
  "FileToConvert.mp4", // Name of the file being converted
  true, // Success status
  conversionOutputs,// ConcurrentBag<string> to store results
  "ConversionResults.txt", // Optional file path to save results to
  1234567890, // Original file size in bytes
  987654321 // Converted file size in bytes
);

// Recorded output will be in the format:
// "FileName=FileToConvert.mp4,Success=true,OriginalSize=1.15 GB,EndSize=941.9 MB,SizeRatio=80%,SizeDif=-246,913,569"
```

#### GetVideoMetadata

Gets the specified metadata from a video file using FFMpeg. Valid metadata options are indicated using the EVideoMetadata enum.

```cs
@"C:\path\to\video.mp4".GetVideoMetadata(EVideoMetadata.Codec_Name); // Returns the codec name used to encode video.mp4
```

#### GetFrameRate

Gets the frame rate of the spcified video file using FFMpeg.

```cs
@"C:\path\to\video.mp4".GetFrameRate(); // Returns the frame rate of video.mp4 as a decimal ie 29.97
```

#### GetKeyFrameSpacing

Gets the key frame spacing of the specified video file using FFMpeg. Optionally specify

```cs
@"C:\path\to\video.mp4".GetKeyFrameSpacing(20, 30); // Returns the key frame spacing using the average of 20 samples that are 30 seconds long of video.mp4 as a decimal ie 30.0
```

</details>
