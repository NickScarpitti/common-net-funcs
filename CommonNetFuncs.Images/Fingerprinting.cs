using SkiaSharp;

namespace CommonNetFuncs.Images;

/// <summary>
/// Supported perceptual hashing algorithms.
/// </summary>
public enum ImageHashAlgorithm
{
	/// <summary>Fast, good for exact/near-exact duplicates.</summary>
	AverageHash,
	/// <summary>Best general-purpose balance. Recommended default.</summary>
	DifferenceHash,
	/// <summary>DCT-based. Most robust against edits/compression.</summary>
	PerceptualHash
}

/// <summary>
/// A computed image fingerprint.
/// </summary>
public record ImageFingerprint(string FilePath, ulong Hash, ImageHashAlgorithm Algorithm);

/// <summary>
/// Result of a pairwise image comparison.
/// </summary>
public record SimilarityResult(
		string ImageA,
		string ImageB,
		double SimilarityPercent,
		bool IsDuplicate
);

/// <summary>
/// Perceptual image fingerprinting service using SkiaSharp only.
/// No SixLabors.ImageSharp or any other paid/non-free dependency required.
///
/// Implements three classic algorithms:
///   - AverageHash  : resize to 8x8, compare each pixel to mean luminance
///   - DifferenceHash: resize to 9x8, compare adjacent pixels per row
///   - PerceptualHash: resize to 32x32, apply DCT, compare to median
/// </summary>
public static class ImageFingerprinting
{
	// ── Public API ────────────────────────────────────────────────────────────

	/// <summary>Fingerprint a single image file.</summary>
	public static async Task<ImageFingerprint> FingerprintImage(this string imagePath, ImageHashAlgorithm algorithm = ImageHashAlgorithm.DifferenceHash)
	{
		if (!File.Exists(imagePath))
		{
			throw new FileNotFoundException($"Image not found: {imagePath}");
		}

		await using FileStream stream = File.OpenRead(imagePath);
		return FingerprintImage(stream, imagePath, algorithm);
	}

	/// <summary>Fingerprint an image from a stream.</summary>
	public static ImageFingerprint FingerprintImage(this Stream stream, string label = "<stream>", ImageHashAlgorithm algorithm = ImageHashAlgorithm.DifferenceHash)
	{
		using SKBitmap bitmap = SKBitmap.Decode(stream) ?? throw new InvalidDataException($"Could not decode image: {label}");
		return bitmap.FingerprintImage(label, algorithm);
	}

	/// <summary>Fingerprint an image from a bitmap.</summary>
	public static ImageFingerprint FingerprintImage(this SKBitmap bitmap, string label = "<bitmap>", ImageHashAlgorithm algorithm = ImageHashAlgorithm.DifferenceHash)
	{
		if (bitmap == null || bitmap.IsEmpty)
		{
			throw new ArgumentNullException(nameof(bitmap), "Bitmap cannot be null or empty.");
		}

		ulong hash = algorithm switch
		{
			ImageHashAlgorithm.AverageHash => ComputeAverageHash(bitmap),
			ImageHashAlgorithm.DifferenceHash => ComputeDifferenceHash(bitmap),
			ImageHashAlgorithm.PerceptualHash => ComputePerceptualHash(bitmap),
			_ => throw new ArgumentOutOfRangeException(nameof(algorithm), message: "Invalid hashing algorithm specified.")
		};

		return new ImageFingerprint(label, hash, algorithm);
	}

	/// <summary>Fingerprint all images in a directory.</summary>
	public static async Task<IReadOnlyList<ImageFingerprint>> FingerprintDirectory(string directoryPath, bool recursive = false, ImageHashAlgorithm algorithm = ImageHashAlgorithm.DifferenceHash)
	{
		HashSet<string> supported = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

		SearchOption option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

		List<string> files = Directory.GetFiles(directoryPath, "*.*", option).Where(x => supported.Contains(Path.GetExtension(x))).ToList();

		List<ImageFingerprint> results = new(files.Count);
		foreach (string file in files)
		{
			try
			{
				results.Add(await FingerprintImage(file, algorithm));
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[WARN] Skipping {Path.GetFileName(file)}: {ex.Message}");
			}
		}

		Console.WriteLine($"[INFO] Fingerprinted {results.Count} images using {algorithm}.");
		return results;
	}

	/// <summary>Compare two fingerprints. Both must use the same algorithm.</summary>
	public static SimilarityResult Compare(ImageFingerprint a, ImageFingerprint b, decimal duplicateThreshold = 90.0m)
	{
		if (a.Algorithm != b.Algorithm)
		{
			throw new InvalidOperationException("Cannot compare fingerprints computed with different algorithms.");
		}

		double similarity = HammingSimilarity(a.Hash, b.Hash);
		return new SimilarityResult(a.FilePath, b.FilePath, similarity, similarity >= (double)duplicateThreshold);
	}

	/// <summary>Find all duplicate pairs in a list of fingerprints.</summary>
	public static IReadOnlyList<SimilarityResult> FindDuplicates(IReadOnlyList<ImageFingerprint> fingerprints, decimal duplicateThreshold = 90.0m)
	{
		List<SimilarityResult> duplicates = new();
		for (int i = 0; i < fingerprints.Count; i++)
		{
			for (int j = i + 1; j < fingerprints.Count; j++)
			{
				SimilarityResult result = Compare(fingerprints[i], fingerprints[j], duplicateThreshold);
				if (result.IsDuplicate)
				{
					duplicates.Add(result);
				}
			}
		}
		return duplicates.OrderByDescending(r => r.SimilarityPercent).ToList();
	}

	/// <summary>Convenience: scan a directory and return all duplicate pairs.</summary>
	public static async Task<IReadOnlyList<SimilarityResult>> FindDuplicatesInDirectory(string directoryPath, bool recursive = false, ImageHashAlgorithm algorithm = ImageHashAlgorithm.DifferenceHash)
	{
		IReadOnlyList<ImageFingerprint> fingerprints = await FingerprintDirectory(directoryPath, recursive, algorithm);
		return FindDuplicates(fingerprints);
	}

	// ── Hash Algorithms ───────────────────────────────────────────────────────

	/// <summary>
	/// AverageHash: resize to 8x8 grayscale, compare each pixel to the mean.
	/// Produces a 64-bit hash. Fast but sensitive to gamma/brightness shifts.
	/// </summary>
	private static ulong ComputeAverageHash(SKBitmap source)
	{
		// Step 1: resize to 8x8 with high-quality downsampling
		using SKBitmap resizedBitmap = source.ResizeTo(8, 8);

		// Step 2: extract grayscale luminance for all 64 pixels
		byte[] pixels = new byte[64];
		for (int y = 0; y < 8; y++)
		{
			for (int x = 0; x < 8; x++)
			{
				pixels[y * 8 + x] = Luminance(resizedBitmap.GetPixel(x, y));
			}
		}

		// Step 3: compute mean luminance
		double mean = pixels.Average(p => (double)p);

		// Step 4: set bit=1 if pixel >= mean, else 0
		ulong hash = 0;
		for (int i = 0; i < 64; i++)
		{
			if (pixels[i] >= mean)
			{
				hash |= 1UL << i;
			}
		}

		return hash;
	}

	/// <summary>
	/// DifferenceHash: resize to 9x8 grayscale, compare adjacent pixels in each row.
	/// 9 columns → 8 comparisons per row × 8 rows = 64 bits.
	/// More robust than AverageHash for brightness variations.
	/// </summary>
	private static ulong ComputeDifferenceHash(SKBitmap source)
	{
		// Step 1: resize to 9 wide x 8 tall (one extra column for diff)
		using SKBitmap resizedBitmap = source.ResizeTo(9, 8);

		ulong hash = 0;
		int bit = 0;

		// Step 2: for each row, compare adjacent pixel pairs
		for (int y = 0; y < 8; y++)
		{
			for (int x = 0; x < 8; x++)
			{
				byte left = Luminance(resizedBitmap.GetPixel(x, y));
				byte right = Luminance(resizedBitmap.GetPixel(x + 1, y));

				// bit = 1 if left pixel is brighter than right
				if (left > right)
				{
					hash |= 1UL << bit;
				}
				bit++;
			}
		}

		return hash;
	}

	/// <summary>
	/// PerceptualHash (pHash): resize to 32x32, compute 2D DCT, take top-left
	/// 8x8 of DCT coefficients, compare each to the median.
	/// Most robust against JPEG compression, minor edits, watermarks.
	/// </summary>
	private static ulong ComputePerceptualHash(SKBitmap source)
	{
		const int dctSize = 32; // full DCT grid
		const int hashSize = 8;  // top-left sub-grid used for hash

		// Step 1: resize to 32x32 grayscale
		using SKBitmap resizedBitmap = source.ResizeTo(dctSize, dctSize);

		// Step 2: build luminance matrix
		double[,] luminanceMatrix = new double[dctSize, dctSize];
		for (int y = 0; y < dctSize; y++)
		{
			for (int x = 0; x < dctSize; x++)
			{
				luminanceMatrix[y, x] = Luminance(resizedBitmap.GetPixel(x, y));
			}
		}

		// Step 3: apply 2D DCT
		double[,] dct = ComputeDct2D(luminanceMatrix, dctSize);

		// Step 4: extract top-left 8x8 DCT coefficients (skip [0,0] DC component)
		double[] topLeft = new double[hashSize * hashSize];
		int idx = 0;
		for (int y = 0; y < hashSize; y++)
		{
			for (int x = 0; x < hashSize; x++)
			{
				topLeft[idx++] = dct[y, x];
			}
		}

		// Step 5: compute median of the 64 values
		double[] sorted = topLeft.OrderBy(v => v).ToArray();
		double median = (sorted[31] + sorted[32]) / 2.0;

		// Step 6: set bit=1 if coefficient >= median
		ulong hash = 0;
		for (int i = 0; i < 64; i++)
		{
			if (topLeft[i] >= median)
			{
				hash |= 1UL << i;
			}
		}

		return hash;
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	/// <summary>
	/// Compute perceptual luminance (grayscale value) from an SKColor.
	/// Uses the standard Rec. 601 luma coefficients.
	/// </summary>
	private static byte Luminance(SKColor color) => (byte)(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);

	/// <summary>
	/// Compute 2D Discrete Cosine Transform (DCT-II) on a square matrix.
	/// Used by PerceptualHash.
	/// </summary>
	private static double[,] ComputeDct2D(double[,] input, int size)
	{
		// Apply 1D DCT to each row
		double[,] temp = new double[size, size];
		for (int y = 0; y < size; y++)
		{
			double[] row = new double[size];
			for (int x = 0; x < size; x++)
			{
				row[x] = input[y, x];
			}

			double[] dctRow = Dct1D(row, size);
			for (int x = 0; x < size; x++)
			{
				temp[y, x] = dctRow[x];
			}
		}

		// Apply 1D DCT to each column of the row-transformed result
		double[,] output = new double[size, size];
		for (int x = 0; x < size; x++)
		{
			double[] col = new double[size];
			for (int y = 0; y < size; y++)
			{
				col[y] = temp[y, x];
			}

			double[] dctCol = Dct1D(col, size);
			for (int y = 0; y < size; y++)
			{
				output[y, x] = dctCol[y];
			}
		}

		return output;
	}

	/// <summary>1D DCT-II transform.</summary>
	private static double[] Dct1D(double[] input, int n)
	{
		double[] output = new double[n];
		for (int k = 0; k < n; k++)
		{
			double sum = 0;
			for (int i = 0; i < n; i++)
			{
				sum += input[i] * Math.Cos(Math.PI * k * (2 * i + 1) / (2.0 * n));
			}
			output[k] = sum * (k == 0 ? Math.Sqrt(1.0 / n) : Math.Sqrt(2.0 / n));
		}
		return output;
	}

	/// <summary>
	/// Compute similarity between two 64-bit hashes using Hamming distance.
	/// Returns a value between 0.0 (completely different) and 100.0 (identical).
	/// </summary>
	public static double HammingSimilarity(ulong hashA, ulong hashB)
	{
		ulong xor = hashA ^ hashB;
		int differentBits = 0;
		while (xor != 0)
		{
			differentBits += (int)(xor & 1); xor >>= 1;
		}
		return (1.0 - differentBits / 64.0) * 100.0;
	}
}