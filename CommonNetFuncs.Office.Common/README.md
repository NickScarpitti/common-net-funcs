# CommonNetFuncs.Images

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Office.Common)](https://www.nuget.org/packages/CommonNetFuncs.Office.Common/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Office.Common)](https://www.nuget.org/packages/CommonNetFuncs.Office.Common/)

This project contains helper methods to convert compatible file types (`.xlsx`, `.xls`, `.docx`, `.doc`, `.pptx`, `.ppt`, and `.csv`) to PDFs using LibreOffice.

## Contents

- [CommonNetFuncs.Images](#commonnetfuncsimages)
  - [Contents](#contents)
  - [PdfConversion](#pdfconversion)
    - [PdfConversion Usage Examples](#pdfconversion-usage-examples)
      - [ConvertToPdf](#converttopdf)

---

## PdfConversion

This class provides methods to convert various file types to PDF using LibreOffice.

### PdfConversion Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### ConvertToPdf

Converts the specified file to PDF using LibreOffice.

```cs
ConvertToPdf(@"C:\Program Files\LibreOffice\program\soffice.com", @"C:\path\to\file.xlsx", @"C:\path\to\output.pdf"); // Converts file.xlsx to output.pdf using LibreOffice
await ConvertToPdfAsync(@"C:\Program Files\LibreOffice\program\soffice.com", @"C:\path\to\file.xlsx", @"C:\path\to\output.pdf"); // Asynchronously converts file.xlsx to output.pdf using LibreOffice
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Office.Common
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.