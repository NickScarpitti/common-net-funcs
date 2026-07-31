using System.Runtime.InteropServices;
using Xabe.FFmpeg;

namespace Ffmpeg.Tests;

/// <summary>
/// Shared setup/teardown for the ConversionTask test classes. Splitting the ConversionTask tests across several
/// classes lets xUnit treat each class as its own collection so they can run in parallel with each other, since each
/// test here spawns a real ffmpeg process.
/// </summary>
public abstract class ConversionTaskTestsBase : IDisposable
{
	protected readonly string testVideoPath;

	protected readonly string workingDir;

	protected ConversionTaskTestsBase(string workingDirName)
	{
		string testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
		testVideoPath = Path.Combine(testDataDir, "test.mp4");

		// Unique per-instance directory (xUnit creates a new class instance per test method) so cleanup never has to
		// contend with files from other tests, allowing deletion to be attempted immediately instead of after a fixed delay.
		workingDir = Path.Combine(Path.GetTempPath(), workingDirName, Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(workingDir);

		// Ensure FFmpeg executables path is set for tests
		FFmpeg.SetExecutablesPath(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/bin" : "C:\\Program Files\\ffmpeg\\bin");
	}

	private bool disposed;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				// Cleanup temporary files after tests. Each instance has its own unique directory, so deletion can be
				// attempted right away; retry briefly in case ffmpeg hasn't fully released a file handle yet.
				if (Directory.Exists(workingDir))
				{
					const int maxAttempts = 5;
					for (int attempt = 1; attempt <= maxAttempts; attempt++)
					{
						try
						{
							Directory.Delete(workingDir, true);
							break;
						}
						catch (IOException ioex)
						{
							if (attempt == maxAttempts)
							{
								Console.WriteLine(ioex);
								break;
							}
							Task.Delay(200).Wait();
						}
					}
				}
			}
			disposed = true;
		}
	}

	~ConversionTaskTestsBase()
	{
		Dispose(false);
	}
}
