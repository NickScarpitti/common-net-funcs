using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
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
	private const string BeginUploadTemplate = "Starting upload of {fileName} to bucket { bucketName }";
	private const string CompleteUploadTemplate = "Finished upload of {fileName} to bucket {bucketName} in {time}ms";
	private const string AwsErrorLocationTemplate = "{ErrorLocation} AWS S3 Error";
	private const string UnableToGetFileTemplate = "Unable to get file {FileName} from {BucketName} bucket in {ErrorLocation}";

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
	/// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
	/// <returns><see langword="true"/> if file was successfully uploaded.</returns>
	public static async Task<bool> UploadS3File(this IAmazonS3 s3Client, string bucketName, string fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null,
			long thresholdForMultiPartUpload = MultipartThreshold, bool compressSteam = true, ECompressionType compressionType = ECompressionType.Gzip,
			CancellationToken cancellationToken = default)
	{
		if (compressSteam && compressionType is not ECompressionType.Gzip and not ECompressionType.Deflate)
		{
			throw new NotSupportedException($"Compression type {compressionType} is not valid for this method.\n\tSupported types are:\n\t\t{ECompressionType.Gzip},\n\t\t{ECompressionType.Deflate}");
		}

		bool logTrace = logger.IsTraceEnabled;
		bool logDebug = logger.IsDebugEnabled;
		bool logInfo = logger.IsInfoEnabled;

		Stopwatch? sw = null;
		if (logger.IsTraceEnabled)
		{
			sw = Stopwatch.StartNew();
			logger.Trace("Starting UploadS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
		}

		bool success = false;
		try
		{
			validatedBuckets ??= new(ValidatedBuckets);
			if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false))
			{
				if (logTrace)
				{
					sw!.Stop();
					logger.Trace("Finished bucket validation in UploadS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName);
				}

				await CheckForExistingFile(s3Client, bucketName, fileName, cancellationToken).ConfigureAwait(false);

				ECompressionType currentCompression = await fileData.DetectCompressionType().ConfigureAwait(false);
				Stream? decompressedStream = fileData;
				switch (currentCompression)
				{
					case ECompressionType.Brotli:
					case ECompressionType.ZLib:
						decompressedStream = fileData.Decompress(currentCompression, true);
						break;
				}

				if (fileData.Length < thresholdForMultiPartUpload)
				{
					await using MemoryStream uploadStream = (MemoryStream)(compressSteam ? new MemoryStream() : decompressedStream);
					string? contentEncoding = null;

					if (compressSteam)
					{
						await decompressedStream.CompressStream(uploadStream, compressionType, CompressionLevel.Optimal, cancellationToken).ConfigureAwait(false);
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
						logger.Debug(BeginUploadTemplate, fileName, bucketName);
						sw = Stopwatch.StartNew();
					}

					PutObjectResponse? response = await s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);

					if (logDebug)
					{
						sw!.Stop();
						logger.Debug(CompleteUploadTemplate, fileName, bucketName, sw.ElapsedMilliseconds);
					}

					success = response?.HttpStatusCode == HttpStatusCode.OK;

					if (!success && logInfo)
					{
						logger.Info("{StatusCode}", response?.HttpStatusCode.ToString());
					}
				}
				else
				{
					if (logDebug)
					{
						logger.Debug(BeginUploadTemplate, fileName, bucketName);
						sw = Stopwatch.StartNew();
					}

					success = await s3Client.UploadMultipartAsync(bucketName, fileName, fileData, cancellationToken).ConfigureAwait(false);

					if (logDebug)
					{
						sw!.Stop();
						logger.Debug(CompleteUploadTemplate, fileName, bucketName, sw.ElapsedMilliseconds);
					}
				}
			}
		}
		catch (AmazonS3Exception awsEx)
		{
			logger.Error(awsEx, AwsErrorLocationTemplate, awsEx.GetLocationOfException());
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

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
	/// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
	/// <returns><see langword="true"/> if file was successfully uploaded.</returns>
	public static async Task<bool> UploadS3File(this IAmazonS3 s3Client, string bucketName, string fileName, string filePath, ConcurrentDictionary<string, bool>? validatedBuckets = null,
				long thresholdForMultiPartUpload = MultipartThreshold, CancellationToken cancellationToken = default)
	{
		if (fileName.IsNullOrWhiteSpace())
		{
			throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));
		}

		if (filePath.IsNullOrWhiteSpace() || !File.Exists(filePath))
		{
			throw new FileNotFoundException($"File not found at path: {filePath}", filePath);
		}

		bool logTrace = logger.IsTraceEnabled;
		bool logDebug = logger.IsDebugEnabled;
		bool logInfo = logger.IsInfoEnabled;

		Stopwatch? sw = null;
		if (logTrace)
		{
			sw = Stopwatch.StartNew();
			logger.Trace("Starting UploadS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
		}

		bool success = false;
		try
		{
			validatedBuckets ??= new(ValidatedBuckets);
			if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false))
			{
				if (logTrace)
				{
					sw!.Stop();
					logger.Trace("Finished bucket validation in UploadS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName);
				}

				await CheckForExistingFile(s3Client, bucketName, fileName, cancellationToken).ConfigureAwait(false);

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
						logger.Debug(BeginUploadTemplate, fileName, bucketName);
						sw = Stopwatch.StartNew();
					}

					PutObjectResponse? response = await s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);

					if (logDebug)
					{
						sw!.Stop();
						logger.Debug(CompleteUploadTemplate, fileName, bucketName, sw.ElapsedMilliseconds);
					}

					success = response?.HttpStatusCode == HttpStatusCode.OK;
					if (!success && logInfo)
					{
						logger.Info("AWS Request Status: {msg}", response?.HttpStatusCode.ToString());
					}
				}
				else
				{
					if (logDebug)
					{
						logger.Debug(BeginUploadTemplate, fileName, bucketName);
						sw = Stopwatch.StartNew();
					}

					success = await s3Client.UploadMultipartAsync(bucketName, fileName, fileData, cancellationToken).ConfigureAwait(false);

					if (logDebug)
					{
						sw!.Stop();
						logger.Debug(CompleteUploadTemplate, fileName, bucketName, sw.ElapsedMilliseconds);
					}
				}

				fileData.Close();
			}
		}
		catch (AmazonS3Exception awsEx)
		{
			logger.Error(awsEx, AwsErrorLocationTemplate, awsEx.GetLocationOfException());
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return success;
	}

	private static async Task CheckForExistingFile(IAmazonS3 s3Client, string bucketName, string fileName, CancellationToken cancellationToken = default)
	{
		Stopwatch? sw = null;
		Stopwatch? deleteSw = null;

		bool logTrace = logger.IsTraceEnabled;

		if (logTrace)
		{
			sw = Stopwatch.StartNew();
			logger.Trace("Starting check for existing file in CheckForExistingFile method for {fileName} from bucket {bucketName}", fileName, bucketName);
		}

		if (await s3Client.S3FileExists(bucketName, fileName, cancellationToken: cancellationToken).ConfigureAwait(false))
		{
			if (logTrace)
			{
				deleteSw = Stopwatch.StartNew();
				logger.Trace("Starting delete of existing file in CheckForExistingFile method for {fileName} from bucket {bucketName}", fileName, bucketName);
			}

			await s3Client.DeleteS3File(bucketName, fileName, cancellationToken: cancellationToken).ConfigureAwait(false);

			if (logTrace)
			{
				deleteSw!.Stop();
				logger.Trace("Finished delete of existing file in CheckForExistingFile method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, deleteSw.ElapsedMilliseconds);
			}
		}

		if (logTrace)
		{
			sw!.Stop();
			logger.Trace("Finished check for existing file in CheckForExistingFile method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
		}
	}

	/// <summary>
	/// Uploads a file to S3 using multipart upload (typically for large files only).
	/// </summary>
	/// <param name="s3Client">The S3 client to use for the operation.</param>
	/// <param name="bucketName">The name of the S3 bucket.</param>
	/// <param name="fileName">The name of the file to upload.</param>
	/// <param name="stream">The stream containing the file data.</param>
	/// <param name="cancellationToken">The cancellation token for this operation.</param>
	/// <returns><see langword="true"/> if the upload was successful.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the upload fails.</exception>
	public static async Task<bool> UploadMultipartAsync(this IAmazonS3 s3Client, string bucketName, string fileName, Stream stream, CancellationToken cancellationToken = default)
	{
		string? uploadId = null;
		List<PartETag> partETags;

		bool logDebug = logger.IsDebugEnabled;
		bool logInfo = logger.IsInfoEnabled;

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

			if (logInfo)
			{
				logger.Info("Starting multipart upload: {totalSize} bytes in {totalParts} parts of {chunkSize} bytes each", totalSize, totalParts, chunkSize);
			}

			// Create semaphore to limit concurrent uploads (adjust based on your needs)
			using SemaphoreSlim semaphore = new(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);

			Stopwatch? sw = null;
			if (logDebug)
			{
				sw = Stopwatch.StartNew();
				logger.Debug("Starting multi-part upload of {fileName} to bucket {bucketName}", fileName, bucketName);
			}

			// Upload parts in parallel
			Task<PartETag?>[] uploadTasks = new Task<PartETag?>[totalParts];
			for (int i = 1; i <= totalParts; i++)
			{
				uploadTasks[i - 1] = s3Client.UploadPartAsync(bucketName, fileName, uploadId, stream, i, chunkSize, totalSize, semaphore, cancellationToken);
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
				logger.Debug("Finished multi-part upload of {fileName} to bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
			}

			if (logInfo)
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
				long totalSize, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
	{
		await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

		bool logDebug = logger.IsDebugEnabled;

		try
		{
			long startPosition = (partNumber - 1) * chunkSize;
			long actualChunkSize = Math.Min(chunkSize, totalSize - startPosition);
			byte[] buffer = BufferPool.Rent((int)actualChunkSize); // Create a buffer for this chunk
			try
			{
				// Read the chunk from the source stream (thread-safe)
				int totalBytesRead;
#pragma warning disable S3998 // Threads should not lock on objects with weak identity
				lock (sourceStream)
				{
					sourceStream.Seek(startPosition, SeekOrigin.Begin);
					totalBytesRead = 0;
					int bytesRead;

					while (totalBytesRead < actualChunkSize && (bytesRead = sourceStream.ReadAsync(buffer, totalBytesRead, (int)(actualChunkSize - totalBytesRead), cancellationToken).Result) > 0)
					//while (totalBytesRead < actualChunkSize && (bytesRead = sourceStream.Read(buffer, totalBytesRead, (int)(actualChunkSize - totalBytesRead))) > 0)
					{
						totalBytesRead += bytesRead;
					}
				}
#pragma warning restore S3998 // Threads should not lock on objects with weak identity

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
					logger.Debug("Starting upload of part #{partNumber} ({actualChunkSize} bytes) {fileName} to bucket {bucketName}", partNumber, actualChunkSize, fileName, bucketName);
				}

				UploadPartResponse response = await s3Client.UploadPartAsync(uploadPartRequest, cancellationToken).ConfigureAwait(false);

				if (logDebug)
				{
					sw!.Stop();
					logger.Debug("Finished upload of part #{partNumber} ({actualChunkSize} bytes) {fileName} to bucket {bucketName} in {time}ms", partNumber, actualChunkSize, fileName, bucketName, sw.ElapsedMilliseconds);
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
	/// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
	public static async Task GetS3File(this IAmazonS3 s3Client, string bucketName, string? fileName, Stream fileData, ConcurrentDictionary<string, bool>? validatedBuckets = null,
			bool decompressGzipData = true, CancellationToken cancellationToken = default)
	{
		Stopwatch? sw = null;
		bool logTrace = logger.IsTraceEnabled;
		bool logDebug = logger.IsDebugEnabled;

		try
		{
			if (logTrace)
			{
				sw = Stopwatch.StartNew();
				logger.Trace("Starting GetS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
			}

			validatedBuckets ??= new(ValidatedBuckets);
			if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false))
			{
				if (logTrace)
				{
					sw!.Stop();
					logger.Trace("Finished validating bucket in GetS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
				}

				GetObjectRequest request = new()
				{
					BucketName = bucketName,
					Key = fileName
				};

				if (logDebug)
				{
					sw = Stopwatch.StartNew();
					logger.Debug("Starting download of {fileName} from bucket {bucketName}", fileName, bucketName);
				}

				using GetObjectResponse? response = await s3Client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);

				if (logDebug)
				{
					sw!.Stop();
					logger.Debug("Finished download of {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
				}

				if (response != null)
				{
					if (decompressGzipData && response.Headers.ContentEncoding.ContainsInvariant(["gzip", "deflate"]))
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
				logger.Error(awsEx, AwsErrorLocationTemplate, awsEx.GetLocationOfException());
			}
			else
			{
				logger.Debug(awsEx, UnableToGetFileTemplate, fileName.UrlEncodeReadable(cancellationToken: cancellationToken),
					bucketName.UrlEncodeReadable(cancellationToken: cancellationToken), awsEx.GetLocationOfException());

				logger.Warn(UnableToGetFileTemplate, fileName.UrlEncodeReadable(cancellationToken: cancellationToken),
					bucketName.UrlEncodeReadable(cancellationToken: cancellationToken), awsEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

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
	/// <paramref name="cancellationToken"/>>Optional: The cancellation token for this request.</param>
	public static async Task GetS3File(this IAmazonS3 s3Client, string bucketName, string? fileName, string filePath, ConcurrentDictionary<string, bool>? validatedBuckets = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			Stopwatch? sw = null;
			bool logTrace = logger.IsTraceEnabled;
			bool logDebug = logger.IsDebugEnabled;

			if (logTrace)
			{
				logger.Trace("Opening FileStream in GetS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
			}

			await using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

			if (logTrace)
			{
				sw = Stopwatch.StartNew();
				logger.Trace("Starting GetS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
			}

			validatedBuckets ??= new(ValidatedBuckets);
			if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false))
			{
				if (logTrace)
				{
					sw!.Stop();
					logger.Trace("Finished validating bucket in GetS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
				}

				GetObjectRequest request = new()
				{
					BucketName = bucketName,
					Key = fileName
				};

				if (logDebug)
				{
					sw = Stopwatch.StartNew();
					logger.Debug("Starting download of {fileName} from bucket {bucketName}", fileName, bucketName);
				}

				using GetObjectResponse? response = await s3Client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);

				if (logDebug)
				{
					sw!.Stop();
					logger.Debug("Finished download of {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
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
				logger.Error(awsEx, AwsErrorLocationTemplate, awsEx.GetLocationOfException());
			}
			else
			{
				logger.Debug(awsEx, UnableToGetFileTemplate, fileName.UrlEncodeReadable(cancellationToken: cancellationToken),
					bucketName.UrlEncodeReadable(cancellationToken: cancellationToken), awsEx.GetLocationOfException());

				logger.Warn(UnableToGetFileTemplate, fileName.UrlEncodeReadable(cancellationToken: cancellationToken),
					bucketName.UrlEncodeReadable(cancellationToken: cancellationToken), awsEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
	}

	/// <summary>
	/// Deletes a file from the specified S3 bucket.
	/// </summary>
	/// <param name="s3Client">The S3 client to use for the operation.</param>
	/// <param name="bucketName">Name of the S3 bucket to delete the file from.</param>
	/// <param name="fileName">Name of the file to delete from the S3 bucket.</param>
	/// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
	/// <returns><see langword="true"/> if file was deleted successfully.</returns>
	public static async Task<bool> DeleteS3File(this IAmazonS3 s3Client, string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null,
		CancellationToken cancellationToken = default)
	{
		bool success = false;

		bool logDebug = logger.IsDebugEnabled;

		try
		{
			Stopwatch? sw = null;
			bool logTrace = logger.IsTraceEnabled;

			if (logTrace)
			{
				sw = Stopwatch.StartNew();
				logger.Trace("Starting DeleteS3File method for {fileName} from bucket {bucketName}", fileName, bucketName);
			}

			validatedBuckets ??= new(ValidatedBuckets);
			if (!fileName.IsNullOrWhiteSpace() && await s3Client.IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false) &&
					await s3Client.S3FileExists(bucketName, fileName, cancellationToken: cancellationToken).ConfigureAwait(false))
			{
				if (logTrace)
				{
					sw!.Stop();
					logger.Trace("Finished validating bucket in DeleteS3File method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
				}

				if (logDebug)
				{
					sw = Stopwatch.StartNew();
					logger.Debug("Starting deletion of {fileName} from bucket {bucketName}", fileName, bucketName);
				}

				await s3Client.DeleteObjectAsync(bucketName, fileName, cancellationToken).ConfigureAwait(false);

				if (logDebug)
				{
					sw!.Stop();
					logger.Debug("Finished deletion of {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
				}

				success = true;
			}
		}
		catch (AmazonS3Exception awsEx)
		{
			if (awsEx.StatusCode == HttpStatusCode.NotFound)
			{
				logger.Warn("{FileName} not found for deletion", fileName.UrlEncodeReadable(cancellationToken: cancellationToken));
			}
			else
			{
				logger.Error(awsEx, AwsErrorLocationTemplate, awsEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

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
	/// <param name="cancellationToken">Optional: The cancellation token for this request.</param>
	/// <returns><see langword="true"/> if the file exists within the given S3 bucket.</returns>
	public static async Task<bool> S3FileExists(this IAmazonS3 s3Client, string bucketName, string fileName, string? versionId = null, CancellationToken cancellationToken = default)
	{
		bool success = false;

		bool logDebug = logger.IsDebugEnabled;

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
					logger.Debug("Starting file exists check for {fileName} in bucket {bucketName}", fileName, bucketName);
					sw = Stopwatch.StartNew();
				}

				GetObjectMetadataResponse? response = await s3Client.GetObjectMetadataAsync(request, cancellationToken).ConfigureAwait(false);

				if (logDebug)
				{
					sw!.Stop();
					logger.Debug("Finished file exists check for {fileName} in bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
				}

				success = response?.HttpStatusCode == HttpStatusCode.OK;
			}
		}
		catch (AmazonS3Exception awsEx)
		{
			if (awsEx.StatusCode != HttpStatusCode.NotFound)
			{
				logger.Error(awsEx, AwsErrorLocationTemplate, awsEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return success;
	}

	/// <summary>
	/// Get a list containing the names of every file within an S3 bucket.
	/// </summary>
	/// <param name="s3Client">The S3 client to use for the operation.</param>
	/// <param name="bucketName">Name of the S3 bucket to get file list from.</param>
	/// <param name="maxKeysPerQuery">Number of records to return per request.</param>
	/// <paramref name="cancellationToken"/>>Optional: The cancellation token for this request.</paramref>
	/// <returns><see cref="List{TObj}"/> containing the names of every file within the given S3 bucket.</returns>
	public static async Task<List<string>?> GetAllS3BucketFiles(this IAmazonS3 s3Client, string bucketName, int maxKeysPerQuery = 1000, CancellationToken cancellationToken = default)
	{
		List<string> fileNames = [];

		bool logDebug = logger.IsDebugEnabled;

		try
		{
			if (!bucketName.IsNullOrWhiteSpace())
			{
				ListObjectsV2Response? response;
				ListObjectsV2Request request = new()
				{
					BucketName = bucketName,
					MaxKeys = maxKeysPerQuery
				};

				Stopwatch? sw = null;
				if (logDebug)
				{
					logger.Debug("Starting file list download from bucket {bucketName}", bucketName);
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
					logger.Debug("Finished downloading file list from bucket {bucketName} in {time}ms", bucketName, sw.ElapsedMilliseconds);
				}
			}
		}
		catch (AmazonS3Exception awsEx)
		{
			if (awsEx.StatusCode != HttpStatusCode.NotFound)
			{
				logger.Error(awsEx, AwsErrorLocationTemplate, awsEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

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
	/// <returns>String containing the URL for the specified file.</returns>
	public static async Task<string?> GetS3Url(this IAmazonS3 s3Client, string bucketName, string fileName, ConcurrentDictionary<string, bool>? validatedBuckets = null)
	{
		string? url = null;
		try
		{
			Stopwatch? sw = null;
			bool logTrace = logger.IsTraceEnabled;
			bool logDebug = logger.IsDebugEnabled;

			if (logTrace)
			{
				sw = Stopwatch.StartNew();
				logger.Trace("Starting GetS3Url method for {fileName} from bucket {bucketName}", fileName, bucketName);
			}

			validatedBuckets ??= new(ValidatedBuckets);
			if (await s3Client.IsBucketValid(bucketName, validatedBuckets).ConfigureAwait(false))
			{
				if (logTrace)
				{
					sw!.Stop();
					logger.Trace("Finished validating bucket in GetS3Url method for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
				}

				GetPreSignedUrlRequest request = new()
				{
					BucketName = bucketName,
					Key = fileName,
					Expires = DateTime.UtcNow + TimeSpan.FromMinutes(1),
				};

				if (logDebug)
				{
					logger.Debug("Starting URL generation for {fileName} from bucket {bucketName}", fileName, bucketName);
					sw = Stopwatch.StartNew();
				}

				url = await s3Client.GetPreSignedURLAsync(request);

				if (logDebug)
				{
					sw!.Stop();
					logger.Debug("Finished URL generation for {fileName} from bucket {bucketName} in {time}ms", fileName, bucketName, sw.ElapsedMilliseconds);
				}
			}
		}
		catch (AmazonS3Exception awsEx)
		{
			if (awsEx.StatusCode != HttpStatusCode.NotFound)
			{
				logger.Error(awsEx, AwsErrorLocationTemplate, awsEx.GetLocationOfException());
			}
			else
			{
				logger.Debug(awsEx, "Unable to get URL for {FileName} from {BucketName}", fileName.UrlEncodeReadable(), bucketName.UrlEncodeReadable());
				logger.Warn("Unable to get URL for {FileName} from {BucketName}", fileName.UrlEncodeReadable(), bucketName.UrlEncodeReadable());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return url;
	}

	/// <summary>
	/// Checks whether an S3 bucket exists and is reachable or not.
	/// </summary>
	/// <param name="s3Client">The S3 client to use for the operation.</param>
	/// <param name="bucketName">Name of the S3 bucket to validate exists and is reachable.</param>
	/// <param name="validatedBuckets">Optional: Dictionary containing bucket names and their validation status.</param>
	/// <returns><see langword="true"/> if the S3 bucket exists and is reachable.</returns>
	public static async Task<bool> IsBucketValid(this IAmazonS3 s3Client, string bucketName, ConcurrentDictionary<string, bool>? validatedBuckets = null)
	{
		bool isValid = false;

		Stopwatch? sw = null;
		bool logTrace = logger.IsTraceEnabled;
		bool logDebug = logger.IsDebugEnabled;

		try
		{
			if (logTrace)
			{
				logger.Trace("Starting IsBucketValid method for bucket {bucketName}", bucketName);
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
						logger.Debug("Starting bucket validation of {bucketName}", bucketName);
						sw = Stopwatch.StartNew();
					}

					isValid = await DoesS3BucketExistV2Async(s3Client, bucketName).ConfigureAwait(false);

					if (logDebug)
					{
						sw!.Stop();
						logger.Debug("Finished bucket validation of {bucketName} in {time}ms", bucketName, sw.ElapsedMilliseconds);
					}

					validatedBuckets.TryAdd(bucketName, isValid);
				}
			}

			if (!isValid) //Retry in case of intermittent outage
			{
				if (logDebug)
				{
					sw = Stopwatch.StartNew();
					logger.Debug("Starting re-try for bucket validation of {bucketName}", bucketName);
				}

				isValid = await DoesS3BucketExistV2Async(s3Client, bucketName).ConfigureAwait(false);

				if (logDebug)
				{
					sw!.Stop();
					logger.Debug("Finished re-try for bucket validation of {bucketName} in {time}ms", bucketName, sw.ElapsedMilliseconds);
				}

				validatedBuckets?[bucketName] = isValid;
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return isValid;
	}
}
