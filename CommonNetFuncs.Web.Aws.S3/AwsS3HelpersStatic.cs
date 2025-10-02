using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using NLog;
using static Amazon.S3.Util.AmazonS3Util;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Web.Aws.S3;

public static class AwsS3HelpersStatic
{
  private static readonly Logger logger = LogManager.GetCurrentClassLogger();
  private static readonly ConcurrentDictionary<string, bool> ValidatedBuckets = [];
  internal const long MultipartThreshold = 10 * 1024 * 1024; // 10MB

  /// <summary>
  /// Upload a file to S3 bucket.
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">Name of the S3 bucket to upload file to.</param>
  /// <param name="fileName">Name to save the file as in the S3 bucket.</param>
  /// <param name="fileData">Stream containing the data for the file to be uploaded.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <param name="thresholdForMultiPartUpload">Optional: The threshold size (in bytes) for using multipart upload. Default is 10MB.</param>
  /// <param name="compressSteam">Optional: If <see langword="true"/>, will compress stream sent to S3 bucket. Default is <see langword="true"/></param>
  /// <param name="compressionType">Optional: Specifies which compression type to use when compressSteam = <see langword="true"/>. Does nothing if compressSteam = <see langword="false"/>.  Valid values are GZip and Deflate</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
  /// <returns><see langword="true"/> if file was successfully uploaded.</returns>
  public static async Task<bool> UploadS3File(this IAmazonS3 s3Client, string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null,
        long thresholdForMultiPartUpload = MultipartThreshold, bool compressSteam = true, ECompressionType compressionType = ECompressionType.Gzip,
        LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    if (compressSteam && compressionType is not ECompressionType.Gzip and not ECompressionType.Deflate)
    {
      throw new NotSupportedException($"Compression type {compressionType} is not valid for this method.\n\tSupported types are:\n\t\t{ECompressionType.Gzip},\n\t\t{ECompressionType.Deflate}");
    }

    bool logTrace = logLevel != null && logLevel < LogLevel.Trace;
    bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

    Stopwatch? sw = null;
    if (logTrace)
    {
      sw = Stopwatch.StartNew();
      logger.Warn("Starting UploadS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
    }

    bool success = false;
    try
    {
      validatedBuckets ??= new(ValidatedBuckets);
      if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets, logLevel).ConfigureAwait(false))
      {
        if (logTrace)
        {
          sw!.Stop();
          logger.Warn("Finished bucket validation in UploadS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName);
        }

        await CheckForExistingFile(s3Client, bucketName, fileName, logLevel, cancellationToken).ConfigureAwait(false);

        ECompressionType currentCompression = await fileData.DetectCompressionType().ConfigureAwait(false);
        Stream? decompressedStream = fileData;
        switch (currentCompression)
        {
          case ECompressionType.Brotli: case ECompressionType.ZLib:
            decompressedStream = fileData.Decompress(currentCompression, true);
            break;
        }

        if (fileData.Length < thresholdForMultiPartUpload)
        {
          await using MemoryStream uploadStream = (MemoryStream)(compressSteam ? new MemoryStream() : decompressedStream);
          string? contentEncoding = null;

          if (compressSteam)
          {
            await decompressedStream.CompressStream(uploadStream, compressionType, cancellationToken).ConfigureAwait(false);
          }
          else if (currentCompression != ECompressionType.None)
          {
            contentEncoding = currentCompression.ToString().ToLowerInvariant();
          }

          PutObjectRequest request = new()
            {
              BucketName = bucketName,
              Key = fileName,
              InputStream = uploadStream,
              BucketKeyEnabled = true,
              ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

          if (contentEncoding != null)
          {
            request.Headers["Content-Encoding"] = contentEncoding;
          }

          await using MemoryStream lengthStream = new();
          await uploadStream.CopyToAsync(lengthStream, cancellationToken).ConfigureAwait(false);
          request.Headers["Content-Length"] = lengthStream.Length.ToString();

          if (logDebug)
          {
            logger.Info("Starting upload of {fileName} to bucket {bucketName}", fileName, bucketName);
            sw = Stopwatch.StartNew();
          }

          PutObjectResponse? response = await s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);

          if (logDebug)
          {
            sw!.Stop();
            logger.Info("Finished upload of {fileName} to bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
          }

          success = response?.HttpStatusCode == HttpStatusCode.OK;

          if (!success && logLevel != null && logLevel <= LogLevel.Info)
          {
            logger.Info("{msg}", response?.HttpStatusCode.ToString());
          }
        }
        else
        {
          if (logDebug)
          {
            logger.Info("Starting upload of {fileName} to bucket {bucketName}", fileName, bucketName);
            sw = Stopwatch.StartNew();
          }

          success = await s3Client.UploadMultipartAsync(bucketName, fileName, fileData, logLevel, cancellationToken).ConfigureAwait(false);

          if (logDebug)
          {
            sw!.Stop();
            logger.Info("Finished upload of {fileName} to bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
          }
        }
      }
    }
    catch (AmazonS3Exception awsEx)
    {
      logger.Error(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    }
    return success;
  }

  /// <summary>
  /// Upload a file to S3 bucket.
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">Name of the S3 bucket to upload file to.</param>
  /// <param name="fileName">Name to save the file as in the S3 bucket.</param>
  /// <param name="filePath">Path of the file to be uploaded.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <param name="thresholdForMultiPartUpload">Optional: The threshold size (in bytes) for using multipart upload. Default is 10MB.</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
  /// <returns><see langword="true"/> if file was successfully uploaded.</returns>
  public static async Task<bool> UploadS3File(this IAmazonS3 s3Client, string bucketName, string fileName, string filePath, ConcurrentDictionary<string, bool>? validatedBuckets = null,
        long thresholdForMultiPartUpload = MultipartThreshold, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    if (fileName.IsNullOrWhiteSpace())
    {
      throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));
    }

    if (filePath.IsNullOrWhiteSpace() || !File.Exists(filePath))
    {
      throw new FileNotFoundException($"File not found at path: {filePath}", filePath);
    }

    bool logTrace = logLevel != null && logLevel < LogLevel.Trace;
    bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

    Stopwatch? sw = null;
    if (logTrace)
    {
      sw = Stopwatch.StartNew();
      logger.Warn("Starting UploadS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
    }

    bool success = false;
    try
    {
      validatedBuckets ??= new(ValidatedBuckets);
      if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets, logLevel).ConfigureAwait(false))
      {
        if (logTrace)
        {
          sw!.Stop();
          logger.Warn("Finished bucket validation in UploadS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName);
        }

        await CheckForExistingFile(s3Client, bucketName, fileName, logLevel, cancellationToken).ConfigureAwait(false);

        await using FileStream fileData = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        if (fileData.Length < thresholdForMultiPartUpload)
        {
          PutObjectRequest request = new()
            {
              BucketName = bucketName,
              Key = fileName,
              InputStream = fileData,
              BucketKeyEnabled = true,
              ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

          request.Headers["Content-Length"] = request.InputStream.Length.ToString();

          if (logDebug)
          {
            logger.Info("Starting upload of {fileName} to bucket {bucketName}", fileName, bucketName);
            sw = Stopwatch.StartNew();
          }

          PutObjectResponse? response = await s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);

          if (logDebug)
          {
            sw!.Stop();
            logger.Info("Finished upload of {fileName} to bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
          }

          success = response?.HttpStatusCode == HttpStatusCode.OK;
          if (!success && logLevel != null && logLevel <= LogLevel.Info)
          {
            logger.Info("AWS Request Status: {msg}", response?.HttpStatusCode.ToString());
          }
        }
        else
        {
          if (logDebug)
          {
            logger.Info("Starting upload of {fileName} to bucket {bucketName}", fileName, bucketName);
            sw = Stopwatch.StartNew();
          }

          success = await s3Client.UploadMultipartAsync(bucketName, fileName, fileData, logLevel, cancellationToken).ConfigureAwait(false);

          if (logDebug)
          {
            sw!.Stop();
            logger.Info("Finished upload of {fileName} to bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
          }
        }

        fileData.Close();
      }
    }
    catch (AmazonS3Exception awsEx)
    {
      logger.Error(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    }
    return success;
  }

  private static async Task CheckForExistingFile(IAmazonS3 s3Client, string bucketName, string fileName, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    Stopwatch? sw = null;
    Stopwatch? delteSw = null;

    bool logTrace = logLevel != null && logLevel < LogLevel.Trace;

    if (logTrace)
    {
      sw = Stopwatch.StartNew();
      logger.Warn("Starting check for existing file in CheckForExistingFile method for {fileName} from bucket {bucketName}", fileName, bucketName);
    }

    if (await s3Client.S3FileExists(bucketName, fileName, logLevel: logLevel, cancellationToken: cancellationToken).ConfigureAwait(false))
    {
      if (logTrace)
      {
        delteSw = Stopwatch.StartNew();
        logger.Warn("Starting delete of existing file in CheckForExistingFile method for {fileName} from bucket {bucketName}", fileName, bucketName);
      }

      await s3Client.DeleteS3File(bucketName, fileName, logLevel: logLevel, cancellationToken: cancellationToken).ConfigureAwait(false);

      if (logTrace)
      {
        delteSw!.Stop();
        logger.Warn("Finished delete of existing file in CheckForExistingFile method for {fileName} from bucket {bucketName} in {time}ms", delteSw.ElapsedMilliseconds, fileName, bucketName);
      }
    }

    if (logTrace)
    {
      sw!.Stop();
      logger.Warn("Finished check for existing file in CheckForExistingFile method for {fileName} from bucket {bucketName} in {time}ms", sw.ElapsedMilliseconds, fileName, bucketName);
    }
  }

  /// <summary>
  /// Uploads a file to S3 using multipart upload (typically for large files only).
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">The name of the S3 bucket.</param>
  /// <param name="fileName">The name of the file to upload.</param>
  /// <param name="stream">The stream containing the file data.</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <param name="cancellationToken">The cancellation token for this operation.</param>
  /// <returns><see langword="true"/> if the upload was successful.</returns>
  /// <exception cref="InvalidOperationException">Thrown when the upload fails.</exception>
  public static async Task<bool> UploadMultipartAsync(this IAmazonS3 s3Client, string bucketName, string fileName, Stream stream, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    string? uploadId = null;
    List<PartETag> partETags = new();

    bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

    try
    {
      // Initiate multipart upload
      InitiateMultipartUploadRequest initiateRequest = new()
        {
          BucketName = bucketName,
          Key = fileName,
          BucketKeyEnabled = true,
          ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
        };

      InitiateMultipartUploadResponse initiateResponse = await s3Client.InitiateMultipartUploadAsync(initiateRequest, cancellationToken).ConfigureAwait(false);
      uploadId = initiateResponse.UploadId;

      // Calculate chunk size (minimum 5MB for S3, maximum 5GB)
      const long minChunkSize = 5 * 1024 * 1024; // 5MB
      const long maxChunkSize = 5L * 1024 * 1024 * 1024; // 5GB
      const int maxParts = 10000; // S3 limit

      long totalSize = stream.Length;
      long chunkSize = Math.Max(minChunkSize, totalSize / maxParts);
      chunkSize = Math.Min(chunkSize, maxChunkSize);

      // Ensure chunk size is reasonable for parallel processing
      chunkSize = Math.Max(chunkSize, 10 * 1024 * 1024); // At least 10MB for efficiency

      // Calculate number of parts
      int totalParts = (int)Math.Ceiling((double)totalSize / chunkSize);

      if (logLevel == LogLevel.Info)
      {
        logger.Info("Starting multipart upload: {totalSize} bytes in {totalParts} parts of {chunkSize} bytes each", totalSize, totalParts, chunkSize);
      }

      // Create semaphore to limit concurrent uploads (adjust based on your needs)
      using SemaphoreSlim semaphore = new(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);

      Stopwatch? sw = null;
      if (logDebug)
      {
        sw = Stopwatch.StartNew();
        logger.Info("Starting multi-part upload of {fileName} to bucket {bucketName}", fileName, bucketName);
      }

      // Upload parts in parallel
      Task<PartETag?>[] uploadTasks = new Task<PartETag?>[totalParts];
      for (int i = 1; i <= totalParts; i++)
      {
        uploadTasks[i - 1] = s3Client.UploadPartAsync(bucketName, fileName, uploadId, stream, i, chunkSize, totalSize, semaphore, logLevel, cancellationToken);
      }

      // Wait for all uploads to complete
      PartETag?[] results = await Task.WhenAll(uploadTasks).ConfigureAwait(false);

      // Check if all parts uploaded successfully
      partETags = results.Where(r => r != null).Cast<PartETag>().OrderBy(x => x.PartNumber).ToList();

      if (partETags.Count != totalParts)
      {
        throw new InvalidOperationException($"Only {partETags.Count} of {totalParts} parts uploaded successfully");
      }

      // Complete multipart upload
      CompleteMultipartUploadRequest completeRequest = new()
        {
          BucketName = bucketName,
          Key = fileName,
          UploadId = uploadId,
          PartETags = partETags
        };

      CompleteMultipartUploadResponse completeResponse = await s3Client.CompleteMultipartUploadAsync(completeRequest, cancellationToken).ConfigureAwait(false);

      if (logDebug)
      {
        sw!.Stop();
        logger.Info("Finished multi-part upload of {fileName} to bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
      }

      if (logLevel == LogLevel.Info)
      {
        logger.Info("Multipart upload completed successfully for {fileName}", fileName);
      }

      return completeResponse.HttpStatusCode == HttpStatusCode.OK;
    }
    catch (Exception ex)
    {
      // Abort multipart upload on failure
      if (!string.IsNullOrEmpty(uploadId))
      {
        try
        {
          await s3Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
            {
              BucketName = bucketName,
              Key = fileName,
              UploadId = uploadId
            }, cancellationToken).ConfigureAwait(false);

          logger.Warn("Aborted multipart upload {uploadId} due to error", uploadId);
        }
        catch (Exception abortEx)
        {
          logger.Warn(abortEx, "Failed to abort multipart upload {uploadId}", uploadId);
        }
      }

      logger.Error(ex, "Multipart upload failed for {fileName}", fileName);
      return false;
    }
  }

  private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;

  internal static async Task<PartETag?> UploadPartAsync(this IAmazonS3 s3Client, string bucketName, string fileName, string uploadId, Stream sourceStream, int partNumber, long chunkSize,
        long totalSize, SemaphoreSlim semaphore, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

    bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

    try
    {
      long startPosition = (partNumber - 1) * chunkSize;
      long actualChunkSize = Math.Min(chunkSize, totalSize - startPosition);
      byte[] buffer = BufferPool.Rent((int)actualChunkSize); // Create a buffer for this chunk
      try
      {
        // Read the chunk from the source stream (thread-safe)
        int totalBytesRead;
        lock (sourceStream)
        {
          sourceStream.Seek(startPosition, SeekOrigin.Begin);
          totalBytesRead = 0;
          int bytesRead;

          while (totalBytesRead < actualChunkSize && (bytesRead = sourceStream.Read(buffer, totalBytesRead, (int)(actualChunkSize - totalBytesRead))) > 0)
          {
            totalBytesRead += bytesRead;
          }
        }

        if (totalBytesRead != actualChunkSize)
        {
          throw new InvalidOperationException($"Expected to read {actualChunkSize} bytes but only read {totalBytesRead} bytes for part {partNumber}");
        }

        // Upload the part
        //await using MemoryStream partStream = new(buffer);
        await using MemoryStream partStream = new(buffer, 0, totalBytesRead, writable: false);

        UploadPartRequest uploadPartRequest = new()
          {
            BucketName = bucketName,
            Key = fileName,
            UploadId = uploadId,
            PartNumber = partNumber,
            InputStream = partStream,
            PartSize = actualChunkSize,
            //IsLastPart = isLastPart,
          };

        Stopwatch? sw = null;
        if (logDebug)
        {
          sw = Stopwatch.StartNew();
          logger.Info("Starting upload of part #{partNumber} ({actualChunkSize} bytes) {fileName} to bucket {bucketName}", partNumber, actualChunkSize, fileName, bucketName);
        }

        UploadPartResponse response = await s3Client.UploadPartAsync(uploadPartRequest, cancellationToken).ConfigureAwait(false);

        if (logDebug)
        {
          sw!.Stop();
          logger.Info("Finished upload of part #{partNumber} ({actualChunkSize} bytes) {fileName} to bucket {bucketName} in {time}ms", partNumber, actualChunkSize, fileName, bucketName, sw.ElapsedMilliseconds);
        }

        if (response.HttpStatusCode == HttpStatusCode.OK)
        {
          logger.Debug("Successfully uploaded part {partNumber} ({actualChunkSize} bytes)", partNumber, actualChunkSize);
          return new PartETag(partNumber, response.ETag);
        }
        else
        {
          logger.Error("Failed to upload part {partNumber}. Status: {statusCode}", partNumber, response.HttpStatusCode);
          return null;
        }
      }
      finally
      {
        BufferPool.Return(buffer, true);
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "Error uploading part {partNumber}", partNumber);
      return null;
    }
    finally
    {
      semaphore.Release();
    }
  }

  /// <summary>
  /// Retrieve a file from the specified S3 bucket.
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">Name of the S3 bucket to get the file from.</param>
  /// <param name="fileName">Name of the file to retrieve from the S3 bucket.</param>
  /// <param name="fileData">Stream to receive the file data retrieved from the S3 bucket.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <param name="decompressGzipData">Optional: Whether to decompress Gzip data or not if the S3 bucket returns GZip encoded data.</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
  public static async Task GetS3File(this IAmazonS3 s3Client, string bucketName, string? fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null,
      bool decompressGzipData = true, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    Stopwatch? sw = null;
    bool logTrace = logLevel != null && logLevel < LogLevel.Trace;
    bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

    try
    {
      if (logTrace)
      {
        sw = Stopwatch.StartNew();
        logger.Warn("Starting GetS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
      }

      validatedBuckets ??= new(ValidatedBuckets);
      if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets, logLevel).ConfigureAwait(false))
      {
        if (logTrace)
        {
          sw!.Stop();
          logger.Warn("Finished validating bucket in GetS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
        }

        GetObjectRequest request = new()
          {
            BucketName = bucketName,
            Key = fileName
          };

        if (logDebug)
        {
          sw = Stopwatch.StartNew();
          logger.Info("Starting download of {fileName} from bucket {bucketName}", fileName, bucketName);
        }

        using GetObjectResponse? response = await s3Client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);

        if (logDebug)
        {
          sw!.Stop();
          logger.Info("Finished download of {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
        }

        if (response != null)
        {
          if (decompressGzipData && response.Headers.ContentEncoding.ContainsInvariant([ "gzip", "deflate" ]))
          {
            await response.ResponseStream.Decompress(response.Headers.ContentEncoding.ContainsInvariant("gzip") ? ECompressionType.Gzip : ECompressionType.Deflate, true)
              .CopyToAsync(fileData, cancellationToken).ConfigureAwait(false);
          }
          else
          {
            //ECompressionType currentCompression = await response.ResponseStream.DetectCompressionType().ConfigureAwait(false);
            (ECompressionType currentCompression, Stream resetStream) = await DetectCompressionTypeAndReset(response.ResponseStream).ConfigureAwait(false);
            if (currentCompression != ECompressionType.None)
            {
              await resetStream.Decompress(currentCompression, true).CopyToAsync(fileData, cancellationToken).ConfigureAwait(false);
            }
            else
            {
              //await responseStream.CopyToAsync(fileData, cancellationToken).ConfigureAwait(false);
              await resetStream.CopyToAsync(fileData, cancellationToken).ConfigureAwait(false);
            }

            fileData.Position = 0;
          }
        }
      }
    }
    catch (AmazonS3Exception awsEx)
    {
      if (awsEx.StatusCode != HttpStatusCode.NotFound)
      {
        logger.Error(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
      }
      else
      {
        logger.Trace(awsEx, "{msg}", $"Unable to get file {fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} from {bucketName.UrlEncodeReadable(cancellationToken: cancellationToken)} bucket in {awsEx.GetLocationOfException()}");
        logger.Warn("{msg}", $"Unable to get file {fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} from {bucketName.UrlEncodeReadable(cancellationToken: cancellationToken)} bucket in {awsEx.GetLocationOfException()}");
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    }
  }

  /// <summary>
  /// Retrieve a file from the specified S3 bucket.
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">Name of the S3 bucket to get the file from.</param>
  /// <param name="fileName">Name of the file to retrieve from the S3 bucket.</param>
  /// <param name="filePath">File path to put the downloaded file from the S3 bucket.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <paramref name="cancellationToken"/>>Optional: The cancellation token for this request.</param>
  public static async Task GetS3File(this IAmazonS3 s3Client, string bucketName, string? fileName, string filePath, ConcurrentDictionary<string, bool>? validatedBuckets = null,
    LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    try
    {
      Stopwatch? sw = null;
      bool logTrace = logLevel != null && logLevel < LogLevel.Trace;
      bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

      if (logTrace)
      {
        logger.Warn("Opening FileStream in GetS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
      }

      await using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

      if (logTrace)
      {
        sw = Stopwatch.StartNew();
        logger.Warn("Starting GetS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
      }

      validatedBuckets ??= new(ValidatedBuckets);
      if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets, logLevel).ConfigureAwait(false))
      {
        if (logTrace)
        {
          sw!.Stop();
          logger.Warn("Finished validating bucket in GetS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
        }

        GetObjectRequest request = new()
          {
            BucketName = bucketName,
            Key = fileName
          };

        if (logLevel <= LogLevel.Debug)
        {
          sw = Stopwatch.StartNew();
          logger.Info("Starting download of {fileName} from bucket {bucketName}", fileName, bucketName);
        }

        using GetObjectResponse? response = await s3Client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);

        if (logDebug)
        {
          sw!.Stop();
          logger.Info("Finished download of {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
        }

        if (response != null)
        {
          try
          {
            await response.ResponseStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
          }
          finally
          {
            await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            fileStream.Position = 0;
            await fileStream.DisposeAsync().ConfigureAwait(false);
          }
        }
      }
    }
    catch (AmazonS3Exception awsEx)
    {
      if (awsEx.StatusCode != HttpStatusCode.NotFound)
      {
        logger.Error(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
      }
      else
      {
        logger.Trace(awsEx, "{msg}", $"Unable to get file {fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} from {bucketName.UrlEncodeReadable(cancellationToken: cancellationToken)} bucket in {awsEx.GetLocationOfException()}");
        logger.Warn("{msg}", $"Unable to get file {fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} from {bucketName.UrlEncodeReadable(cancellationToken: cancellationToken)} bucket in {awsEx.GetLocationOfException()}");
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    }
  }

  /// <summary>
  /// Deletes a file from the specified S3 bucket.
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">Name of the S3 bucket to delete the file from.</param>
  /// <param name="fileName">Name of the file to delete from the S3 bucket.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
  /// <returns><see langword="true"/> if file was deleted successfully.</returns>
  public static async Task<bool> DeleteS3File(this IAmazonS3 s3Client, string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null,
    LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    bool success = false;

    bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

    try
    {
      Stopwatch? sw = null;
      bool logTrace = logLevel != null && logLevel < LogLevel.Trace;

      if (logTrace)
      {
        sw = Stopwatch.StartNew();
        logger.Warn("Starting DeleteS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
      }

      validatedBuckets ??= new(ValidatedBuckets);
      if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets, logLevel).ConfigureAwait(false) &&
          await s3Client.S3FileExists(bucketName, fileName, logLevel: logLevel, cancellationToken: cancellationToken).ConfigureAwait(false))
      {
        if (logTrace)
        {
          sw!.Stop();
          logger.Warn("Finished validating bucket in DeleteS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
        }

        if (logDebug)
        {
          sw = Stopwatch.StartNew();
          logger.Info("Starting deletion of {fileName} from bucket {bucketName}", fileName, bucketName);
        }

        await s3Client.DeleteObjectAsync(bucketName, fileName, cancellationToken).ConfigureAwait(false);

        if (logDebug)
        {
          sw!.Stop();
          logger.Info("Finished deletion of {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
        }

        success = true;
      }
    }
    catch (AmazonS3Exception awsEx)
    {
      if (awsEx.StatusCode == HttpStatusCode.NotFound)
      {
        logger.Warn("{msg}", $"{fileName.UrlEncodeReadable(cancellationToken: cancellationToken)} not found for deletion");
      }
      else
      {
        logger.Error(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    }
    return success;
  }

  /// <summary>
  /// Check to see if a file exists within the given S3 bucket.
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">Name of the S3 bucket to check for the file.</param>
  /// <param name="fileName">Name of the file to look for in the S3 bucket.</param>
  /// <param name="versionId">Optional: Version ID for the file being searched for.</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
  /// <returns><see langword="true"/> if the file exists within the given S3 bucket.</returns>
  public static async Task<bool> S3FileExists(this IAmazonS3 s3Client, string bucketName, string fileName, string? versionId = null, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    bool success = false;

    bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

    try
    {
      if (!fileName.IsNullOrWhiteSpace())
      {
        GetObjectMetadataRequest request = new()
          {
            BucketName = bucketName,
            Key = fileName,
            VersionId = !versionId.IsNullOrEmpty() ? versionId : null
          };

        Stopwatch? sw = null;
        if (logDebug)
        {
          logger.Info("Starting file exists check for {fileName} in bucket {bucketName}", fileName, bucketName);
          sw = Stopwatch.StartNew();
        }

        GetObjectMetadataResponse? response = await s3Client.GetObjectMetadataAsync(request, cancellationToken).ConfigureAwait(false);

        if (logDebug)
        {
          sw!.Stop();
          logger.Info("Finished file exists check for {fileName} in bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
        }

        success = response?.HttpStatusCode == HttpStatusCode.OK;
      }
    }
    catch (AmazonS3Exception awsEx)
    {
      if (awsEx.StatusCode != HttpStatusCode.NotFound)
      {
        logger.Error(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    }
    return success;
  }

  /// <summary>
  /// Get a list containing the names of every file within an S3 bucket.
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">Name of the S3 bucket to get file list from.</param>
  /// <param name="maxKeysPerQuery">Number of records to return per request.</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <paramref name="cancellationToken"/>>Optional: The cancellation token for this request.</paramref>
  /// <returns><see cref="List{T}"/> containing the names of every file within the given S3 bucket.</returns>
  public static async Task<List<string>?> GetAllS3BucketFiles(this IAmazonS3 s3Client, string bucketName, int maxKeysPerQuery = 1000, LogLevel? logLevel = null, CancellationToken cancellationToken = default)
  {
    List<string> fileNames = [];

    bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

    try
    {
      if (!bucketName.IsNullOrWhiteSpace())
      {
        ListObjectsV2Response? response = new();
        ListObjectsV2Request request = new()
          {
            BucketName = bucketName,
            MaxKeys = maxKeysPerQuery
          };

        Stopwatch? sw = null;
        if (logDebug)
        {
          logger.Info("Starting file list download from bucket {bucketName}", bucketName);
          sw = Stopwatch.StartNew();
        }

        do
        {
          response = await s3Client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);
          fileNames.AddRange(response?.S3Objects.ConvertAll(x => x.Key) ?? []);
          request.ContinuationToken = response?.NextContinuationToken;
        } while (response?.IsTruncated ?? false);

        if (logDebug)
        {
          sw!.Stop();
          logger.Info("Finished downloading file list from bucket {bucketName} in {time}ms", bucketName, sw.ElapsedMilliseconds);
        }
      }
    }
    catch (AmazonS3Exception awsEx)
    {
      if (awsEx.StatusCode != HttpStatusCode.NotFound)
      {
        logger.Error(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    }
    return fileNames;
  }

  /// <summary>
  /// Get the URL corresponding to a single file within an S3 bucket.
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">Name of the S3 bucket to get the file URL from.</param>
  /// <param name="fileName">Name of the file to retrieve the URL for.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <returns>String containing the URL for the specified file.</returns>
  public static async Task<string?> GetS3Url(this IAmazonS3 s3Client, string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null)
  {
    string? url = null;
    try
    {
      Stopwatch? sw = null;
      bool logTrace = logLevel != null && logLevel < LogLevel.Trace;
      bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

      if (logTrace)
      {
        sw = Stopwatch.StartNew();
        logger.Warn("Starting GetS3Url method for {fileName} from bucket {bucketName}", fileName, bucketName);
      }

      validatedBuckets ??= new(ValidatedBuckets);
      if (await s3Client.IsBucketValid(bucketName, validatedBuckets, logLevel).ConfigureAwait(false))
      {
        if (logTrace)
        {
          sw!.Stop();
          logger.Warn("Finished validating bucket in GetS3Url method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
        }

        GetPreSignedUrlRequest request = new()
          {
            BucketName = bucketName,
            Key = fileName,
            Expires = DateTime.UtcNow + TimeSpan.FromMinutes(1),
          };

        if (logDebug)
        {
          logger.Info("Starting URL generation for {fileName} from bucket {bucketName}", fileName, bucketName);
          sw = Stopwatch.StartNew();
        }

        url = s3Client.GetPreSignedURL(request);

        if (logDebug)
        {
          sw!.Stop();
          logger.Info("Finished URL generation for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
        }
      }
    }
    catch (AmazonS3Exception awsEx)
    {
      if (awsEx.StatusCode != HttpStatusCode.NotFound)
      {
        logger.Error(awsEx, "{msg}", $"{awsEx.GetLocationOfException()} AWS S3 Error");
      }
      else
      {
        logger.Warn("{msg}", $"Unable to get URL for {fileName.UrlEncodeReadable()} from {bucketName.UrlEncodeReadable()}");
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    }
    return url;
  }

  /// <summary>
  /// Checks whether an S3 bucket exists and is reachable or not.
  /// </summary>
  /// <param name="s3Client">The S3 client to use for the operation.</param>
  /// <param name="bucketName">Name of the S3 bucket to validate exists and is reachable.</param>
  /// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
  /// <param name="logLevel">
  /// <para>Optional: The log level to use for logging. If <see langword="null"/>, no extra logging will be performed.<para/>
  /// <para>If Info is selected, non-success codes for requests to AWS will be logged.<para/>
  /// <para>If Debug is selected, non-success codes for requests to AWS will be logged as well as performance metrics for the request to AWS.<para/>
  /// </param>
  /// <returns><see langword="true"/> if the S3 bucket exists and is reachable.</returns>
  public static async Task<bool> IsBucketValid(this IAmazonS3 s3Client, string bucketName, ConcurrentDictionary<string, bool>? validatedBuckets = null, LogLevel? logLevel = null)
  {
    bool isValid = false;

    Stopwatch? sw = null;
    bool logTrace = logLevel != null && logLevel < LogLevel.Trace;
    bool logDebug = logLevel != null && logLevel <= LogLevel.Debug;

    try
    {
      if (logTrace)
      {
        logger.Info("Starting IsBucketValid method for bucket {bucketName}", bucketName);
      }

      validatedBuckets ??= new(ValidatedBuckets);
      if (validatedBuckets != null)
      {
        if (validatedBuckets.Any(x => x.Key.StrEq(bucketName)))
        {
          isValid = validatedBuckets.Where(x => x.Key.StrEq(bucketName)).Select(x => x.Value).First();
        }
        else
        {
          if (logDebug)
          {
            logger.Info("Starting bucket validation of {bucketName}", bucketName);
            sw = Stopwatch.StartNew();
          }

          isValid = await DoesS3BucketExistV2Async(s3Client, bucketName).ConfigureAwait(false);

          if (logDebug)
          {
            sw!.Stop();
            logger.Info("Finished bucket validation of {bucketName} in {time}ms", bucketName, sw.ElapsedMilliseconds);
          }

          validatedBuckets.TryAdd(bucketName, isValid);
        }
      }

      if (!isValid) //Retry in case of intermittent outage
      {
        if (logDebug)
        {
          sw = Stopwatch.StartNew();
          logger.Info("Starting re-try for bucket validation of {bucketName}", bucketName);
        }

        isValid = await DoesS3BucketExistV2Async(s3Client, bucketName).ConfigureAwait(false);

        if (logDebug)
        {
          sw!.Stop();
          logger.Info("Finished re-try for bucket validation of {bucketName} in {time}ms", bucketName, sw.ElapsedMilliseconds);
        }

        if (validatedBuckets != null)
        {
          validatedBuckets[bucketName] = isValid;
        }
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    }
    return isValid;
  }
}
