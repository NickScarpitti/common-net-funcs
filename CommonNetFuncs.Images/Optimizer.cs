using CliWrap;
using CliWrap.Buffered;

namespace CommonNetFuncs.Images;

/// <summary>
/// Requires CLI Wrap NuGet package and the following tools installed on the system:
/// - gifsicle
/// - jpegoptim
/// - optipng
/// </summary>
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
	/// <param name="gifsicleArgs">Optional: Arguments to pass to gifsicle command. If null, defaults to "-b -O3 {file}"</param>
	/// <param name="jpegoptimArgs">Optional: Arguments to pass to jpegoptim command. If null, defaults to "--preserve-perms --preserve {file}"</param>
	/// <param name="optipngArgs">Optional: Arguments to pass to optipng command. If null, defaults to "-fix -o5 {file}"</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <remarks>Need to have gifsicle, jpegoptim, and optipng installed for these to work</remarks>
	public static async Task OptimizeImage(string file, IEnumerable<string>? gifsicleArgs = null, IEnumerable<string>? jpegoptimArgs = null, IEnumerable<string>? optipngArgs = null, CancellationToken cancellationToken = default)
	{
		if (!File.Exists(file))
		{
			throw new FileNotFoundException($"File not found: {file}");
		}

		FileInfo originalFileInfo = new(file);
		long originalFileSize = originalFileInfo.Length;
		string commandType = string.Empty;
		string extension = Path.GetExtension(file).Replace(".", string.Empty);
		BufferedCommandResult result;

		string workingFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{extension}");
		File.Copy(file, workingFile, true);

		//Compress
		try
		{
			if (GifsicleExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
			{
				commandType = "gifsicle";
				if (gifsicleArgs == null)
				{
					gifsicleArgs = ["-b", "-O3", workingFile];
				}
				else if (!gifsicleArgs.Any(x => string.Equals(x, workingFile, StringComparison.InvariantCultureIgnoreCase)))
				{
					gifsicleArgs = gifsicleArgs.Append(workingFile);
				}
				result = await Cli.Wrap(commandType).WithArguments(gifsicleArgs).ExecuteBufferedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			else if (JpegoptimExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
			{
				commandType = "jpegoptim";

				if (jpegoptimArgs == null)
				{
					jpegoptimArgs = ["--preserve-perms", "--preserve", workingFile];
				}
				else if (!jpegoptimArgs.Any(x => string.Equals(x, workingFile, StringComparison.InvariantCultureIgnoreCase)))
				{
					jpegoptimArgs = jpegoptimArgs.Append(workingFile);
				}
				result = await Cli.Wrap(commandType).WithArguments(jpegoptimArgs).ExecuteBufferedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			else if (OptipngExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
			{
				commandType = "optipng";

				if (optipngArgs == null)
				{
					optipngArgs = ["-fix", "-o5", workingFile];
				}
				else if (!optipngArgs.Any(x => string.Equals(x, workingFile, StringComparison.InvariantCultureIgnoreCase)))
				{
					optipngArgs = optipngArgs.Append(workingFile);
				}
				result = await Cli.Wrap(commandType).WithArguments(optipngArgs).ExecuteBufferedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			else
			{
				logger.Warn("Unsupported file extension for optimization: {extension}", extension);
				return;
			}

			if (!string.IsNullOrWhiteSpace(commandType))
			{
				FileInfo compressedFileInfo = new(workingFile);
				if (result.IsSuccess)
				{
					logger.Info($"Image compression succeeded for [{file}]" +
						$"{(!string.IsNullOrWhiteSpace(result.StandardOutput) ? $"\n\tStd Output: {result.StandardOutput}" : string.Empty)}" +
						$"{(!string.IsNullOrWhiteSpace(result.StandardError) ? $"\n\tStd Error: {result.StandardError}" : string.Empty)}" +
						$"\n\tRun Time:{result.RunTime}" +
						$"\n\tOriginal size: {Math.Round(originalFileSize / 1024d, 2, MidpointRounding.AwayFromZero)}KB [{originalFileSize} B]" +
						$"\n\tNew Size: {Math.Round(compressedFileInfo.Length / 1024d, 2, MidpointRounding.AwayFromZero)}KB [{compressedFileInfo.Length} B]" +
						$"\n\tCompression Ratio: {Math.Round(compressedFileInfo.Length * 100m / originalFileSize, 2, MidpointRounding.AwayFromZero)}%");

					File.Copy(workingFile, file, true);
				}
				else
				{
					logger.Warn("{msg}", $"Image compression failed for [{file}] with exit code {result.ExitCode}");
				}
			}
		}
		finally
		{
			try
			{
				File.Delete(workingFile);
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Failed to delete temporary file: {file}", workingFile);
			}
		}
	}
}
