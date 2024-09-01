using CliWrap;
using CliWrap.Buffered;
using CommonNetFuncs.Convert;
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
    public static async Task OptimizeImage(string file)
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
            result = await Cli.Wrap(commandType).WithArguments(["-b", "-O3", file]).ExecuteBufferedAsync();
        }
        else if (JpegoptimExtensions.ContainsInvariant(extension))
        {
            commandType = "jpegoptim";
            result = await Cli.Wrap(commandType).WithArguments(["--preserve-perms", "--preserve", file]).ExecuteBufferedAsync();
        }
        else if (OptipngExtensions.ContainsInvariant(extension))
        {
            commandType = "optipng";
            result = await Cli.Wrap(commandType).WithArguments(["-fix", "-o5", file]).ExecuteBufferedAsync();
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
