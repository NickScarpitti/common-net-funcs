using CliWrap;
using CliWrap.Buffered;

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
        if (GifsicleExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
        {
            commandType = "gifsicle";
            if (gifsicleArgs == null)
            {
                gifsicleArgs = ["-b", "-O3", file];
            }
            else if (!gifsicleArgs.Any(x => string.Equals(x, file, StringComparison.InvariantCultureIgnoreCase)))
            {
                gifsicleArgs = gifsicleArgs.Append(file);
            }
            result = await Cli.Wrap(commandType).WithArguments(gifsicleArgs).ExecuteBufferedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else if (JpegoptimExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
        {
            commandType = "jpegoptim";

            if (jpegoptimArgs == null)
            {
                jpegoptimArgs = ["--preserve-perms", "--preserve", file];
            }
            else if (!jpegoptimArgs.Any(x => string.Equals(x, file, StringComparison.InvariantCultureIgnoreCase)))
            {
                jpegoptimArgs = jpegoptimArgs.Append(file);
            }
            result = await Cli.Wrap(commandType).WithArguments(jpegoptimArgs).ExecuteBufferedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else if (OptipngExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
        {
            commandType = "optipng";

            if (optipngArgs == null)
            {
                optipngArgs = ["-fix", "-o5", file];
            }
            else if (!optipngArgs.Any(x => string.Equals(x, file, StringComparison.InvariantCultureIgnoreCase)))
            {
                optipngArgs = optipngArgs.Append(file);
            }
            result = await Cli.Wrap(commandType).WithArguments(optipngArgs).ExecuteBufferedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(commandType))
        {
            FileInfo compressedFileInfo = new(file);
            if (result.IsSuccess)
            {
                logger.Info($"Image compression succeeded for [{file}]" +
                    $"{(!string.IsNullOrWhiteSpace(result.StandardOutput) ? $"\n\tStd Output: {result.StandardOutput}" : string.Empty)}" +
                    $"{(!string.IsNullOrWhiteSpace(result.StandardError) ? $"\n\tStd Error: {result.StandardError}" : string.Empty)}" +
                    $"\n\tRun Time:{result.RunTime}" +
                    $"\n\tOriginal size: {Math.Round(originalFileSize / 1024d, 2, MidpointRounding.AwayFromZero)}KB [{originalFileSize} B]" +
                    $"\n\tNew Size: {Math.Round(compressedFileInfo.Length / 1024d, 2, MidpointRounding.AwayFromZero)}KB [{compressedFileInfo.Length} B]" +
                    $"\n\tCompression Ratio: {Math.Round(compressedFileInfo.Length * 100m / originalFileSize, 2, MidpointRounding.AwayFromZero)}%");
            }
            else
            {
                logger.Warn("{msg}", $"Image compression failed for [{file}] with exit code {result.ExitCode}");
            }
        }
    }
}
