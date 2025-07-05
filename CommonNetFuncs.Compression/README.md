# CommonNetFuncs.Compression

[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Compression)](https://www.nuget.org/packages/CommonNetFuncs.Compression/)

This project contains helper methods for compressing files into a zip file as well as compress and decompress streams.

## Contents

- [CommonNetFuncs.Compression](#commonnetfuncscompression)
  - [Contents](#contents)
  - [Files](#files)
    - [Files Usage Examples](#files-usage-examples)
  - [Streams](#streams)
    - [Streams Usage Examples](#streams-usage-examples)

---

## Files

Used for compressing file data into a ZipArchive class.

### Files Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

Add file to zip folder and write it to disk.

```cs
using static CommonNetFuncs.Compression.Files;
using static CommonNetFuncs.Excel.Npoi.Export;

public async Task CreatePeopleZipFile()
{
    List<Person> people = [];

    //Some code populating people list here

    await using MemoryStream zipStream = new();

    //Converts list to excel file in a MemoryStream (see Excel.Npoi)
    await using MemoryStream peopleExcelStream = await people.GenericExcelExport() ?? new();
    await (peopleExcelStream, "People.xlsx").ZipFile(zipStream, CompressionLevel.SmallestSize);
    peopleExcelStream.Dispose();
    zipStream.Position = 0;

    //Write the zip file to disk
    await using FileStream fs = new("People.zip", FileMode.Create, FileAccess.Write);
    await zipStream.CopyToAsync(fs);
    fs.Flush();
}
```

Add multiple files to a ZipArchive object and write it to disk.

```cs
public async Task CreatePeopleAndAddressesZipFile()
{
    List<Person> people = [];
    List<Address> addresses = [];

    //Some code populating people and addresses lists here

    await using MemoryStream zipStream = new();
    using ZipArchive archive = new(zipStream, ZipArchiveMode.Create, true);

    //Convert lists to excel file in a MemoryStream (see Excel.Npoi) then add them to a ZipArchive
    await using MemoryStream peopleExcelStream = await people.GenericExcelExport() ?? new();
    await peopleExcelStream.AddFileToZip(archive, "People.xlsx", CompressionLevel.SmallestSize);
    peopleExcelStream.Dispose();

    await using MemoryStream addressesExcelStream = await addresses.GenericExcelExport() ?? new();
    await addressesExcelStream.AddFileToZip(archive, "Addresses.xlsx", CompressionLevel.SmallestSize);
    addressesExcelStream.Dispose();

    archive.Dispose();

    await using FileStream fs = new("PeopleAndAddresses.zip", FileMode.Create, FileAccess.Write);
    await zipStream.CopyToAsync(fs);
    fs.Flush();
}
```

</details>

---

## Streams

Used for compressing and decompressing streams of data.
Currently supported compression algorithms:

- Brotli
- GZip
- Deflate
- ZLib

### Streams Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

Compress and then decompress a stream. [CommonNetFuncs.Web.Requests]((https://github.com/NickScarpitti/common-net-funcs/tree/main/CommonNetFuncs.Web.Requests)) has a more practical implementation decompressing compressed API responses.

```cs
public async Task CompressAndDecompressFile()
{
    //Create stream
    await using FileStream fileStream = new("TestFile.txt", FileMode.Open, FileAccess.Read);

    //Compress the stream
    await using MemoryStream compressedStream = new();
    await fileStream.DecompressStream(compressedStream, ECompressionType.Gzip);

    //Decompress the stream
    await using MemoryStream decompressedStream = new();
    await compressedStream.DecompressStream(decompressedStream, ECompressionType.Gzip);
}
```

</details>
