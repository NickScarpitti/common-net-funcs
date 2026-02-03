# CommonNetFuncs.Images

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Images)](https://www.nuget.org/packages/CommonNetFuncs.Images)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Images)](https://www.nuget.org/packages/CommonNetFuncs.Images/)

This project contains helper methods for dealing with base64 image encoding and image optimization.

## Contents

- [CommonNetFuncs.Images](#commonnetfuncsimages)
  - [Contents](#contents)
  - [Base64](#base64)
    - [Base64 Usage Examples](#base64-usage-examples)
      - [ConvertImageFileToBase64](#convertimagefiletobase64)
      - [CleanImageValue \[Obsolete, please use ExtractBase64\]](#cleanimagevalue-obsolete-please-use-extractbase64)
      - [ExtractBase64](#extractbase64)
      - [ImageSaveToFile](#imagesavetofile)
      - [IsValidBase64Image](#isvalidbase64image)
  - [Manipulation](#manipulation)
  - [Manipulation Usage Examples](#manipulation-usage-examples)
      - [ResizeImage](#resizeimage)
      - [ConvertImageFormat](#convertimageformat)
      - [ReduceImageQuality](#reduceimagequality)
      - [TryDetectImageType](#trydetectimagetype)
      - [TryGetMetadata](#trygetmetadata)
  - [Optimizer](#optimizer)
    - [Optimizer Usage Examples](#optimizer-usage-examples)
      - [OptimizeImage](#optimizeimage)

---

## Base64

Helper methods for dealing with Base64 image encoding.

### Base64 Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ConvertImageFileToBase64

Converts an image file or stream to a Base64 string.

```cs
string base64String = ConvertImageFileToBase64(@"C:\path\to\image.jpg"); // Returns the Base64 string representation of the image file.
```

#### CleanImageValue [Obsolete, please use [ExtractBase64](#extractbase64)]

Attempts to clean a Base64 string by removing any metadata or unwanted characters that may come with it when reading from an HTML element.

```cs
string base64String = "data:image/png;base64, iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";
string? cleanedBase64 = CleanImageValue(base64String); // "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg=="
```

#### ExtractBase64

Attempts to clean a CSS background image containing a Base64 string by removing any metadata or unwanted characters that may come with it when reading from an HTML element.
Validates that the Base64 string is a valid image format.

```cs
string base64String = "data:image/png;base64, iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";
string? cleanedBase64 = base64String.ExtractBase64(); // "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg=="
```

#### ImageSaveToFile

Save a Base64 string to an image file.

```cs
string base64String = "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";
ImageSaveToFile(base64String, @"C:\path\to\output_image.png"); // Saves the Base64 string as an image file at the specified path.
```

#### IsValidBase64Image

Checks to see if a Base64 string is a valid image format.

```cs
string base64String = "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";
bool isValid = IsValidBase64Image(base64String); // true
```

</details>

---

## Manipulation

Helper methods for manipulating images, such as resizing, and changing image quality.

## Manipulation Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ResizeImage

Resizes an image to the specified width and height, maintaining the aspect ratio if desired.

```cs
await ResizeImage(@"C:\path\to\input_image.jpg", @"C:\path\to\output_image.jpg", 800, 600); // "C:\path\to\output_image.jpg" contains the 800px x 600px resized image.
```

#### ConvertImageFormat

Converts an image from one format to another (e.g., JPEG to PNG).

```cs
await ConvertImageFormat(@"C:\path\to\input_image.jpg", @"C:\path\to\output_image.png", PngFormat.Instance); // "C:\path\to\output_image.png" contains the converted image in png format.
```

#### ReduceImageQuality

Reduces the quality of an image by applying a specified JPEG quality level, which can help in reducing file size. Neither input nor output are required to be JPEG format.
```cs
await ReduceImageQuality(@"C:\path\to\input_image.jpg", @"C:\path\to\output_image.jpg", 50); // "C:\path\to\output_image.jpg" contains the image with reduced 50% quality
```

#### TryDetectImageType
Detects the image format of a file based on its content, returning the format as a string.

```cs
TryDetectImageType(@"C:\path\to\input_image.jpg", out IImageFormat? format); // Returns true and format is the "JPEG" IImageFormat.
```

#### TryGetMetadata

Attempts to retrieve ImageMetadata from an image file.

```cs
TryGetMetadata(@"C:\path\to\input_image.jpg", out ImageMetadata metadata); // Returns ImageMetadata with properties like Width, Height, Format, etc.
```

</details>

---

## Optimizer

Helper methods for optimizing images.

### Optimizer Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### OptimizeImage

Optimizes an image by reducing its file size without sacrificing quality using gifsicle, jpegoptim, or optipng CLI tools depending on the image format.

```cs
await OptimizeImage(@"C:\path\to\input_image.jpg", @"C:\path\to\output_image.jpg"); // "C:\path\to\output_image.jpg" contains the optimized image.
```

</details>
## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Images
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.