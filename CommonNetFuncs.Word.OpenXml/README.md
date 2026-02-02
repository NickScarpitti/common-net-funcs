# CommonNetFuncs.Word.OpenXml

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Word.OpenXml)](https://www.nuget.org/packages/CommonNetFuncs.Word.OpenXml/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Word.OpenXml)](https://www.nuget.org/packages/CommonNetFuncs.Word.OpenXml/)

This lightweight project contains helper methods related to MS Word formatted documents (.doc & .docx).

## Contents

- [CommonNetFuncs.Word.OpenXml](#commonnetfuncswordopenxml)
  - [Contents](#contents)
  - [ChangeUrls](#changeurls)
    - [Common Usage Examples](#common-usage-examples)
      - [ChangeUrlsInWordDoc](#changeurlsinworddoc)
      - [ChangeUrlsInWordDocRegex](#changeurlsinworddocregex)

---

## ChangeUrls

Helpers that search for and change URLs in .doc and .docx files.

### Common Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ChangeUrlsInWordDoc

Look for specific URLs in a word document and change them to a different URL.

```cs
FileStream stream = new(@"C:\Documents\UrlTest.docx");
bool success = ChangeUrls.ChangeUrlsInWordDoc(stream, "http://google.com", "http://yahoo.com", true); // Replaces all "yahoo.com" URLs in the document with "google.com"

// Alternatively, use a dictionary for replacements
Dictionary<string, string> urlsToUpdate = new()
{
    { "http://google.com", "http://yahoo.com" },
    { "http://facebook.com", "http://meta.com" },
    { "http://tumblr.com", "http://imgr.com" }
};

bool success = ChangeUrls.ChangeUrlsInWordDoc(stream, urlsToUpdate); // Replaces all key URLs in the dictionary with their associated value
```

#### ChangeUrlsInWordDocRegex

Look for URLs that match a regex pattern in a word document and replace the matched string with a new one.

```cs
FileStream stream = new(@"C:\Documents\UrlTest.docx");
bool success = ChangeUrls.ChangeUrlsInWordDoc(stream, @"^(http:\/\/)", "https://", true); // Replaces "http://" with "https://" in all URLs in the document

// Alternatively, use a dictionary for replacements
Dictionary<string, string> urlsToUpdate = new()
{
    { @"^(http:\/\/)", "https://" },
    { @"(yahoo\.com)", "google.com" }
};

bool success = ChangeUrls.ChangeUrlsInWordDoc(stream, urlsToUpdate); // Replaces all regex matches with the keys in the dictionary with their associated values
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Word.OpenXml
```

## Dependencies

- DocumentFormat.OpenXml (>= 3.3.0)
- CommonNetFuncs.Core
- CommonNetFuncs.Office.Common

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.