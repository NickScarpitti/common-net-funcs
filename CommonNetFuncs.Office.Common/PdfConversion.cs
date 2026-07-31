using System.Diagnostics;
using static System.IO.Path;
using static System.Web.HttpUtility;

namespace CommonNetFuncs.Office.Common;

public static class PdfConversion
{
	private enum EOfficeFileTypes
	{
		xlsx,
		xls,
		docx,
		doc,
		pptx,
		ppt,
		csv
	}

#if NET9_0_OR_GREATER
	private static readonly Lock conversionLock = new();
#else
	private static readonly object conversionLock = new();
#endif
	private static readonly SemaphoreSlim semaphore = new(1);

	/// <summary>
	/// Converts an office formatted document into a PDF (Requires LibreOffice to be installed on the host machine)
	/// </summary>
	/// <param name="libreOfficeExecutable">Full file path or alias to the LibreOffice executable.</param>
	/// <param name="fileName">File name including full file path to the file to convert to a PDF</param>
	/// <param name="outputPath">Optional: Path to output file to, defaults to the same path as fileName if null</param>
	/// <param name="conversionTimeout">Optional: Time limit for how long the conversion can take before being canceled</param>
	/// <param name="maxRetries">Optional: Number of times to retry the conversion if it fails before throwing an exception, default is 3</param>
	/// <param name="overwriteExistingFile">Optional: Whether to overwrite the output file if it already exists, default is false</param>
	/// <exception cref="LibreOfficeFailedException"></exception>
	/// <exception cref="ArgumentException"></exception>
	/// <exception cref="FileNotFoundException"></exception>
	public static void ConvertToPdf(string libreOfficeExecutable, string fileName, string? outputPath = null, TimeSpan? conversionTimeout = null, int maxRetries = 3, bool overwriteExistingFile = false)
	{
		if (!File.Exists(fileName))
		{
			throw new FileNotFoundException($"The file at '{UrlEncode(fileName)}' does not exist");
		}

		string? pdfCommand = fileName.GetPdfCommand();

		if (string.IsNullOrWhiteSpace(pdfCommand))
		{
			throw new ArgumentException($"Invalid extension on file to be converted to PDF. Valid extensions are:\n{string.Join(",\n", Enum.GetNames(typeof(EOfficeFileTypes)))}");
		}

		(Process process, string pdfFileName, string tempFileName) = CreatePdfConversionProcess(fileName, libreOfficeExecutable, pdfCommand, ref outputPath);

		try
		{
			for (int i = 0; i <= maxRetries; i++)
			{
				lock (conversionLock)
				{
					process.Start();

					if (conversionTimeout == null)
					{
						process.WaitForExit();
					}
					else
					{
#if NET7_0_OR_GREATER
						process.WaitForExit((TimeSpan)conversionTimeout);
#else
						process.WaitForExit((int)((TimeSpan)conversionTimeout).TotalMilliseconds);
#endif
					}

					if (!process.HasExited)
					{
						process.Kill(); // Forcefully terminate the process if it hasn't exited within the timeout.
						throw new LibreOfficeFailedException("LibreOffice conversion process was killed due to timeout.");
					}
				}

				if (process.ExitCode != 0)
				{
					if (i < maxRetries)
					{
						Console.WriteLine($"LibreOffice conversion failed with exit code {process.ExitCode}. Retrying... ({i + 1}/{maxRetries})");
					}
					else
					{
						throw new LibreOfficeFailedException($"LibreOffice has failed with {process.ExitCode}");
					}
				}
				else
				{
					break; // Exit the loop if successful.
				}
			}

			MovePdfFile(pdfFileName, outputPath, fileName, overwriteExistingFile);
		}
		catch (Exception ex)
		{
			throw new LibreOfficeFailedException("Failed to run LibreOffice! Please make sure that the libreOfficeExecutable parameter is a valid reference to your installation of LibreOffice.", ex);
		}
		finally
		{
			CleanUpTempFiles(tempFileName, pdfFileName);
		}
	}

	/// <summary>
	/// Converts an office formatted document into a PDF (Requires LibreOffice to be installed on the host machine)
	/// </summary>
	/// <param name="libreOfficeExecutable">Full file path or alias to the LibreOffice executable.</param>
	/// <param name="fileName">File name including full file path to the file to convert to a PDF</param>
	/// <param name="outputPath">Optional: Path to output file to, defaults to the same path as fileName if null</param>
	/// <param name="maxRetries">Optional: Number of times to retry the conversion if it fails before throwing an exception, default is 3</param>
	/// <param name="overwriteExistingFile">Optional: Whether to overwrite the output file if it already exists, default is false</param>
	/// <param name="cancellationToken">Optional: Cancellation token for asynchronous conversion operation</param>
	/// <exception cref="LibreOfficeFailedException"></exception>
	/// <exception cref="ArgumentException"></exception>
	/// <exception cref="FileNotFoundException"></exception>
	public static async Task ConvertToPdfAsync(string libreOfficeExecutable, string fileName, string? outputPath = null, int maxRetries = 3, bool overwriteExistingFile = false, CancellationToken? cancellationToken = null)
	{
		if (!File.Exists(fileName))
		{
			throw new FileNotFoundException($"The file at '{UrlEncode(fileName)}' does not exist");
		}

		string? pdfCommand = fileName.GetPdfCommand();

		if (string.IsNullOrWhiteSpace(pdfCommand))
		{
			throw new ArgumentException($"Invalid extension on file to be converted to PDF. Valid extensions are:\n{string.Join(",\n", Enum.GetNames(typeof(EOfficeFileTypes)))}");
		}

		(Process process, string pdfFileName, string tempFileName) = CreatePdfConversionProcess(fileName, libreOfficeExecutable, pdfCommand, ref outputPath);

		try
		{
			await semaphore.WaitAsync(cancellationToken ?? default).ConfigureAwait(false);
			for (int i = 0; i <= maxRetries; i++)
			{
				process.Start();
#if NET5_0_OR_GREATER
				await process.WaitForExitAsync(cancellationToken ?? default).ConfigureAwait(false);
#else
				// Task.Run(action, token) only honors the token before the delegate starts running, not while
				// process.WaitForExit() is blocked, so register a callback to kill the process on cancellation instead.
				CancellationToken token = cancellationToken ?? default;
				using (token.Register(static state => TryKillProcess((Process)state!), process))
				{
					await Task.Run(() => process.WaitForExit(), CancellationToken.None).ConfigureAwait(false);
				}
				token.ThrowIfCancellationRequested();
#endif

				if (process.ExitCode != 0)
				{
					if (i < maxRetries)
					{
						Console.WriteLine($"LibreOffice conversion failed with exit code {process.ExitCode}. Retrying... ({i + 1}/{maxRetries})");
					}
					else
					{
						throw new LibreOfficeFailedException($"LibreOffice has failed with {process.ExitCode}");
					}
				}
				else
				{
					break; // Exit the loop if successful.
				}
			}

			MovePdfFile(pdfFileName, outputPath, fileName, overwriteExistingFile);
		}
		catch (OperationCanceledException)
		{
			throw new LibreOfficeFailedException("The PDF conversion was canceled.");
		}
		catch (Exception ex)
		{
			throw new LibreOfficeFailedException("Failed to run LibreOffice! Please make sure that the libreOfficeExecutable parameter is a valid reference to your installation of LibreOffice.", ex);
		}
		finally
		{
			CleanUpTempFiles(tempFileName, pdfFileName);
			semaphore.Release();
		}
	}

	public sealed class LibreOfficeFailedException : Exception
	{
		public LibreOfficeFailedException() { }

		public LibreOfficeFailedException(string message) : base(message) { }

		public LibreOfficeFailedException(string message, Exception inner) : base(message, inner) { }
	}

#if !NET5_0_OR_GREATER
	private static void TryKillProcess(Process process)
	{
		try
		{
			if (!process.HasExited)
			{
				process.Kill();
			}
		}
		catch (InvalidOperationException)
		{
			// Process already exited or never started; nothing to do.
		}
	}
#endif

	private static string? GetPdfCommand(this string fileName)
	{
		return GetExtension(fileName).Replace(".", string.Empty).ToLowerInvariant() switch
		{
			nameof(EOfficeFileTypes.xlsx) or nameof(EOfficeFileTypes.xls) or nameof(EOfficeFileTypes.csv) => "calc_pdf_Export",
			nameof(EOfficeFileTypes.docx) or nameof(EOfficeFileTypes.doc) => "writer_pdf_Export",
			nameof(EOfficeFileTypes.pptx) or nameof(EOfficeFileTypes.ppt) => "impress_pdf_Export",
			_ => null
		};
	}

	private static (Process Process, string PdfFileName, string TempFileName) CreatePdfConversionProcess(string fileName, string libreOfficeExecutable, string pdfCommand, ref string? outputPath)
	{
		string tempPath = GetTempPath();
		string tempFileName = Combine(tempPath, $"{Guid.NewGuid()}{GetExtension(fileName)}");
		string pdfFileName = Combine(tempPath, $"{GetFileNameWithoutExtension(tempFileName)}.pdf");
		File.Copy(fileName, tempFileName, true);

		outputPath ??= GetDirectoryName(fileName.Replace(GetFileName(fileName), string.Empty));
		ProcessStartInfo procStartInfo = new(libreOfficeExecutable, $@"--convert-to pdf:{pdfCommand} ""{tempFileName}"" --outdir ""{tempPath[..^1]}""")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = tempPath,
		};

		return (new() { StartInfo = procStartInfo }, pdfFileName, tempFileName);
	}

	private static void CleanUpTempFiles(string tempFileName, string pdfFileName)
	{
		if (File.Exists(tempFileName))
		{
			try
			{
				File.Delete(tempFileName);
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed.
				Console.WriteLine($"Failed to delete temporary file '{tempFileName}': {ex.Message}");
			}
		}
		if (File.Exists(pdfFileName))
		{
			try
			{
				File.Delete(pdfFileName);
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed.
				Console.WriteLine($"Failed to delete temporary file '{pdfFileName}': {ex.Message}");
			}
		}
	}

	private static void MovePdfFile(string pdfFileName, string? outputPath, string fileName, bool overwriteExistingFile = false)
	{
		if (File.Exists(pdfFileName))
		{
			string destination = Combine(outputPath ?? string.Empty, $"{GetFileNameWithoutExtension(fileName)}.pdf");
#if NET5_0_OR_GREATER
			File.Move(pdfFileName, destination, overwrite: overwriteExistingFile);
#else
			if (overwriteExistingFile && File.Exists(destination))
			{
				File.Delete(destination);
			}
			File.Move(pdfFileName, destination);
#endif
		}
	}
}
