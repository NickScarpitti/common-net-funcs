using System.IO.Compression;
using static CommonNetFuncs.Compression.Streams;

namespace CommonNetFuncs.Compression;

public static class Files
{
  private const int ChunkSize = 1024 * 1024; // 1 MB

  /// <summary>
    /// Compress a file in the form of a stream into a memory stream
    /// </summary>
    /// <param name="file">Stream and file name to compress into a zipped file</param>
    /// <param name="zipFileStream">Memory stream to receive the zipped file</param>
    /// <param name="compressionLevel">Optional: Configure compression preference. Default is "Optimal".</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
  public static async Task ZipFile(this (Stream? fileStream, string fileName) file, MemoryStream zipFileStream, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
  {
    List<(Stream? fileStream, string fileName)> files = [ file ];
    await files.ZipFiles(zipFileStream, compressionLevel, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
    /// Compress a file in the form of a stream into a memory stream
    /// </summary>
    /// <param name="file">Stream and file name to compress into a zipped file</param>
    /// <param name="compressionLevel">Optional: Configure compression preference. Default is "Optimal"</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>Memory stream containing the zipped file data.</returns>
  public static async Task<MemoryStream> ZipFile(this (Stream? fileStream, string fileName) file, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
  {
    List<(Stream? fileStream, string fileName)> files = [ file ];
    return await files.ZipFiles(compressionLevel, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
    /// Compress multiple files in the form of a stream into a memory stream
    /// </summary>
    /// <param name="files">Streams and associated file names to compress into a zipped file</param>
    /// <param name="zipFileStream">Memory stream to receive the zipped files</param>
    /// <param name="compressionLevel">Optional: Configure compression preference. Default is "Optimal"</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>Memory stream containing the data of the zipped files.</returns>
  public static async Task<MemoryStream> ZipFiles(this IEnumerable<(Stream? fileStream, string fileName)> files, MemoryStream zipFileStream, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
  {
    if (!zipFileStream.CanWrite)
    {
      throw new ArgumentException($"{nameof(zipFileStream)} must be writable", nameof(zipFileStream));
    }

    if (files.Any())
    {
      using ZipArchive archive = new(zipFileStream, ZipArchiveMode.Create, true);
      await files.AddFilesToZip(archive, compressionLevel, cancellationToken).ConfigureAwait(false);
      zipFileStream.Position = 0;
    }
    return zipFileStream ?? new();
  }

  /// <summary>
    /// Compress multiple files in the form of a stream into a memory stream
    /// </summary>
    /// <param name="files">Streams and associated file names to compress into a zipped file</param>
    /// <param name="compressionLevel">Optional: Configure compression preference. Default is "Optimal"</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>Memory stream containing the data of the zipped files.</returns>
  public static async Task<MemoryStream> ZipFiles(this IEnumerable<(Stream? fileStream, string fileName)> files, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
  {
    MemoryStream zipFileStream = new();
    if (files.Any())
    {
      using ZipArchive archive = new(zipFileStream, ZipArchiveMode.Create, true);
      await files.AddFilesToZip(archive, compressionLevel, cancellationToken).ConfigureAwait(false);
      zipFileStream.Position = 0;
    }
    return zipFileStream ?? new();
  }

  /// <summary>
    /// Compress multiple files in the form of a stream into a ZipArchive
    /// </summary>
    /// <param name="files">Streams and associated file names to compress into a zipped file</param>
    /// <param name="archive">ZipArchive to add zipped files to</param>
    /// <param name="compressionLevel">Optional: Configure compression preference. Default is "Optimal"</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
  public static async Task AddFilesToZip(this IEnumerable<(Stream? fileStream, string fileName)> files, ZipArchive archive, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
  {
    foreach ((Stream? fileStream, string fileName) in files)
        {
      await fileStream.AddFileToZip(archive, fileName, compressionLevel, cancellationToken).ConfigureAwait(false);
        }
  }

  /// <summary>
    /// Compress a file in the form of a stream into a ZipArchive
    /// </summary>
    /// <param name="fileStream">Stream to compress into a zipped file</param>
    /// <param name="archive">ZipArchive to add zipped files to</param>
    /// <param name="fileName">Name to use for zipped file</param>
    /// <param name="compressionLevel">Optional: Configure compression preference. Default is "Optimal"</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
  public static async Task AddFileToZip(this Stream? fileStream, ZipArchive archive, string fileName, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
  {
    if (fileStream != null)
    {
      fileStream.Position = 0; //Must have this to prevent errors writing data to the attachment
      ZipArchiveEntry entry = archive.CreateEntry(fileName ?? $"File {archive.Entries.Count}", compressionLevel);
      await using Stream entryStream = entry.Open();
      await fileStream.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
      await entryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
  }

  /// <summary>
    /// Compress a file directly to another file using streams (memory efficient)
    /// </summary>
    /// <param name="inputFilePath">Path to the input file</param>
    /// <param name="outputFilePath">Path to the compressed output file</param>
    /// <param name="compressionType">Type of compression to use</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
  public static async Task CompressFile(string inputFilePath, string outputFilePath, ECompressionType compressionType, CancellationToken cancellationToken = default)
  {
    if (!File.Exists(inputFilePath))
    {
      throw new FileNotFoundException($"Input file not found: {inputFilePath}");
    }

    // Ensure output directory exists
    string? outputDir = Path.GetDirectoryName(outputFilePath);
    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
    {
      Directory.CreateDirectory(outputDir);
    }

    await using FileStream inputStream = new(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkSize, FileOptions.SequentialScan);
    await using FileStream outputStream = new(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, ChunkSize, FileOptions.SequentialScan);

    await inputStream.CompressStream(outputStream, compressionType, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
    /// Decompress a file directly to another file using streams (memory efficient)
    /// </summary>
    /// <param name="compressedFilePath">Path to the compressed input file</param>
    /// <param name="outputFilePath">Path to the decompressed output file</param>
    /// <param name="compressionType">Type of compression used</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
  public static async Task DecompressFile(string compressedFilePath, string outputFilePath, ECompressionType compressionType, CancellationToken cancellationToken = default)
  {
    if (!File.Exists(compressedFilePath))
    {
      throw new FileNotFoundException($"Compressed file not found: {compressedFilePath}");
    }

    // Ensure output directory exists
    string? outputDir = Path.GetDirectoryName(outputFilePath);
    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
    {
      Directory.CreateDirectory(outputDir);
    }

    await using FileStream inputStream = new(compressedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkSize, FileOptions.SequentialScan);
    await using FileStream outputStream = new(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, ChunkSize, FileOptions.SequentialScan);

    await inputStream.DecompressStream(outputStream, compressionType, cancellationToken).ConfigureAwait(false);
  }
}
