using CliWrap;
using CliWrap.Buffered;
using CommonNetFuncs.Core;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Images;

public static class Optimizer
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private static readonly string[] GifsicleExtensions = ["gif"];
    private static readonly string[] JpegoptimExtensions = ["jpg", "jpeg"];
    private static readonly string[] OptipngExtensions = ["png", "bmp", "pnm", "tiff"];

    /// <summary>
    /// Optimizes image to be smaller size if possible
    /// </summary>
    /// <param name="file">Path to the file that will be optimized</param>
    /// <remarks>Need to have gifsicle, jpegoptim, and optipng installed for these to work</remarks>
    public static async Task OptimizeImage(string file, IEnumerable<string>? gifsicleArgs = null, IEnumerable<string>? jpegoptimArgs = null, IEnumerable<string>? optipngArgs = null, CancellationToken cancellationToken = default)
    {
        FileInfo originalFileInfo = new(file);
        long originalFileSize = originalFileInfo.Length;
        string commandType = string.Empty;
        string extension = Path.GetExtension(file).Replace(".", string.Empty);
        BufferedCommandResult result = new(0, DateTimeOffset.Now, DateTimeOffset.Now, string.Empty, string.Empty);

        //Compress
        if (GifsicleExtensions.ContainsInvariant(extension))
        {
            commandType = "gifsicle";
            if (gifsicleArgs == null)
            {
                gifsicleArgs = ["-b", "-O3", file];
            }
            else if (!gifsicleArgs.Any(x => x.StrEq(file)))
            {
                gifsicleArgs = gifsicleArgs.Append(file);
            }
            result = await Cli.Wrap(commandType).WithArguments(gifsicleArgs).ExecuteBufferedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else if (JpegoptimExtensions.ContainsInvariant(extension))
        {
            commandType = "jpegoptim";

            if (jpegoptimArgs == null)
            {
                jpegoptimArgs = ["--preserve-perms", "--preserve", file];
            }
            else if (!jpegoptimArgs.Any(x => x.StrEq(file)))
            {
                jpegoptimArgs = jpegoptimArgs.Append(file);
            }
            result = await Cli.Wrap(commandType).WithArguments(jpegoptimArgs).ExecuteBufferedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else if (OptipngExtensions.ContainsInvariant(extension))
        {
            commandType = "optipng";

            if (optipngArgs == null)
            {
                optipngArgs = ["-fix", "-o5", file];
            }
            else if (!optipngArgs.Any(x => x.StrEq(file)))
            {
                optipngArgs = optipngArgs.Append(file);
            }
            result = await Cli.Wrap(commandType).WithArguments(optipngArgs).ExecuteBufferedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (!commandType.IsNullOrWhiteSpace())
        {
            FileInfo compressedFileInfo = new(file);
            if (result.IsSuccess)
            {
                logger.Info($"Image compression succeeded for [{file}]" +
                    $"{(!result.StandardOutput.IsNullOrWhiteSpace() ? $"\n\tStd Output: {result.StandardOutput}" : string.Empty)}" +
                    $"{(!result.StandardError.IsNullOrWhiteSpace() ? $"\n\tStd Error: {result.StandardError}" : string.Empty)}" +
                    $"\n\tRun Time:{result.RunTime}" +
                    $"\n\tOriginal size: {originalFileSize.GetFileSizeFromBytesWithUnits(2)} [{originalFileSize} B]" +
                    $"\n\tNew Size: {compressedFileInfo.Length.GetFileSizeFromBytesWithUnits(2)} [{compressedFileInfo.Length} B]" +
                    $"\n\tCompression Ratio: {Math.Round(compressedFileInfo.Length * 100m / originalFileSize, 2, MidpointRounding.AwayFromZero)}%");
            }
            else
            {
                logger.Warn("{msg}", $"Image compression failed for [{file}] with exit code {result.ExitCode}");
            }
        }
    }
}
