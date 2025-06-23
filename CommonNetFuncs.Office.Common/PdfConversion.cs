using System.Diagnostics;
using static System.IO.Path;
using static System.Web.HttpUtility;

namespace CommonNetFuncs.Office.Common;

public static class PdfConversion
{
    private enum EOfficeFileTypes
    {
        Xlsx,
        Xls,
        Docx,
        Doc,
        Pptx,
        Ppt,
        Csv
    }

    private static readonly Lock conversionLock = new();
    private static readonly SemaphoreSlim semaphore = new(1);

    /// <summary>
    /// Converts an office formatted document into a PDF (Requires LibreOffice to be installed on the host machine)
    /// </summary>
    /// <param name="libreOfficeExecutable">Full file path or alias to the LibreOffice executable</param>
    /// <param name="fileName">File name including full file path to the file to convert to a PDF</param>
    /// <param name="outputPath">Optional: Path to output file to, defaults to the same path as fileName if null</param>
    /// <param name="conversionTimeout">Optional: Time limit for how long the conversion can take before being canceled</param>
    /// <exception cref="LibreOfficeFailedException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static void ConvertToPdf(string libreOfficeExecutable, string fileName, string? outputPath = null, TimeSpan? conversionTimeout = null, int maxRetries = 3)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"The file at '{UrlEncode(fileName)}' does not exist");
        }

        string? pdfCommand = fileName.GetPdfCommand();

        if (string.IsNullOrWhiteSpace(pdfCommand))
        {
            throw new ArgumentException($"Invalid extension on file to be converted to PDF. Valid extensions are:\n{string.Join(",\n", Enum.GetNames<EOfficeFileTypes>())}");
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
                        process.WaitForExit((TimeSpan)conversionTimeout);
                    }

                    if (!process.HasExited)
                    {
                        process.Kill(); // Forcefully terminate the process if it hasn't exited within the timeout.
                        throw new LibreOfficeFailedException("LibreOffice conversion process was killed due to timeout.");
                    }
                }

                if (process.HasExited && process.ExitCode != 0)
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

            MovePdfFile(pdfFileName, outputPath, fileName);
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
    /// <param name="libreOfficeExecutable">Full file path or alias to the LibreOffice executable</param>
    /// <param name="fileName">File name including full file path to the file to convert to a PDF</param>
    /// <param name="outputPath">Optional: Path to output file to, defaults to the same path as fileName if null</param>
    /// <param name="cancellationToken">Optional: Cancellation token for asynchronous conversion operation</param>
    /// <exception cref="LibreOfficeFailedException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static async Task ConvertToPdfAsync(string libreOfficeExecutable, string fileName, string? outputPath = null, CancellationToken? cancellationToken = null, int maxRetries = 3)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"The file at '{UrlEncode(fileName)}' does not exist");
        }

        string? pdfCommand = fileName.GetPdfCommand();

        if (string.IsNullOrWhiteSpace(pdfCommand))
        {
            throw new ArgumentException($"Invalid extension on file to be converted to PDF. Valid extensions are:\n{string.Join(",\n", Enum.GetNames<EOfficeFileTypes>())}");
        }

        (Process process, string pdfFileName, string tempFileName) = CreatePdfConversionProcess(fileName, libreOfficeExecutable, pdfCommand, ref outputPath);

        try
        {
            await semaphore.WaitAsync(cancellationToken ?? default);
            for (int i = 0; i <= maxRetries; i++)
            {
                process.Start();
                await process.WaitForExitAsync(cancellationToken ?? default);

                if (process.HasExited && process.ExitCode != 0)
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

            MovePdfFile(pdfFileName, outputPath, fileName);
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

    private static string? GetPdfCommand(this string fileName)
    {
        string? pdfCommand = null;
        string extension = GetExtension(fileName).Replace(".", string.Empty);
        if (Enum.TryParse(typeof(EOfficeFileTypes), extension, true, out object? officeType))
        {
            pdfCommand = officeType switch
            {
                EOfficeFileTypes.Xlsx or EOfficeFileTypes.Xls or EOfficeFileTypes.Csv => "calc_pdf_Export",
                EOfficeFileTypes.Docx or EOfficeFileTypes.Doc => "writer_pdf_Export",
                EOfficeFileTypes.Pptx or EOfficeFileTypes.Ppt => "impress_pdf_Export",
                _ => null
            };
        }
        return pdfCommand;
    }

    private static (Process Process, string PdfFileName, string TempFileName)CreatePdfConversionProcess(string fileName, string libreOfficeExecutable, string pdfCommand, ref string? outputPath)
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

    private static void MovePdfFile(string pdfFileName, string? outputPath, string fileName)
    {
        if (File.Exists(pdfFileName))
        {
            File.Move(pdfFileName, Combine(outputPath ?? string.Empty, $"{GetFileNameWithoutExtension(fileName)}.pdf"));
        }
    }
}
