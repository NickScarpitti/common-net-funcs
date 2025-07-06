# CommonNetFuncs.Images

[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Images)](https://www.nuget.org/packages/CommonNetFuncs.Images/)

This project contains helper methods for dealing with base64 image encoding and image optimization.

## Contents

- [CommonNetFuncs.Images](#commonnetfuncsimages)
  - [Contents](#contents)
  - [Base64](#base64)
    - [Base64 Usage Examples](#base64-usage-examples)
      - [ConvertImageFileToBase64](#convertimagefiletobase64)
      - [CleanImageValue](#cleanimagevalue)
      - [ImageSaveToFile](#imagesavetofile)
      - [IsValidBase64Image](#isvalidbase64image)
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

#### CleanImageValue

Attempts to clean a Base64 string by removing any metadata or unwanted characters that may come with it when reading from an HTML element.

```cs
string base64String = "data:image/png;base64, iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";
string? cleanedBase64 = CleanImageValue(base64String); // "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg=="
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
