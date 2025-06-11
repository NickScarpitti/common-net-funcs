using System.Diagnostics;
using static System.Web.HttpUtility;
//using static CommonNetFuncs.Core.Strings;

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
    public static void ConvertToPdf(string libreOfficeExecutable, string fileName, string? outputPath = null, TimeSpan? conversionTimeout = null)
    {
        if (File.Exists(fileName))
        {
            string extension = Path.GetExtension(fileName).Replace(".", string.Empty);
            if (Enum.TryParse(typeof(EOfficeFileTypes), extension, true, out object? officeType))
            {
                string pdfCommand = string.Empty;
                switch ((EOfficeFileTypes)officeType)
                {
                    case EOfficeFileTypes.Xlsx: case EOfficeFileTypes.Xls: case EOfficeFileTypes.Csv:
                        pdfCommand = "calc_pdf_Export";
                        break;
                    case EOfficeFileTypes.Docx: case EOfficeFileTypes.Doc:
                        pdfCommand = "writer_pdf_Export";
                        break;
                    case EOfficeFileTypes.Pptx: case EOfficeFileTypes.Ppt:
                        pdfCommand = "impress_pdf_Export";
                        break;
                }

                ProcessStartInfo procStartInfo = new(libreOfficeExecutable, $@"--convert-to pdf:{pdfCommand} ""{fileName}"" --outdir ""{outputPath ?? Path.GetDirectoryName(fileName.Replace(Path.GetFileName(fileName), string.Empty))}""")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.CurrentDirectory
                };

                Process process = new() { StartInfo = procStartInfo };
                process.Start();

                if (conversionTimeout == null)
                {
                    process.WaitForExit();
                }
                else
                {
                    process.WaitForExit((TimeSpan)conversionTimeout);
                }

                // Check for failed exit code.
                if (process.ExitCode != 0)
                {
                    throw new LibreOfficeFailedException($"LibreOffice has failed with {process.ExitCode}");
                }
            }
            else
            {
                throw new ArgumentException($"Invalid extension on file to be converted to PDF. Valid extensions are:\n{string.Join(",\n", Enum.GetNames<EOfficeFileTypes>())}");
            }
        }
        else
        {
            throw new FileNotFoundException($"The file at '{UrlEncode(fileName)}' does not exist");
        }
    }

    public sealed class LibreOfficeFailedException : Exception
    {
        public LibreOfficeFailedException() { }

        public LibreOfficeFailedException(string message) : base(message) { }

        public LibreOfficeFailedException(string message, Exception inner) : base(message, inner) { }
    }
}
