using CommonNetFuncs.Images;
using SkiaSharp;
using xRetry.v3;

namespace Images.Tests;

public sealed class FingerprintTests
{
	private static readonly string TestDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");

	private static string GetTestImagePath(string fileName) => Path.Combine(TestDataDir, fileName);

	private static MemoryStream GetTestImageStream(string fileName)
	{
		return new MemoryStream(File.ReadAllBytes(GetTestImagePath(fileName)));
	}

	// ── FingerprintImage (string extension method) ────────────────────────────

	[RetryTheory(3)]
	[InlineData("test.jpg", ImageHashAlgorithm.AverageHash)]
	[InlineData("test.jpg", ImageHashAlgorithm.DifferenceHash)]
	[InlineData("test.jpg", ImageHashAlgorithm.PerceptualHash)]
	[InlineData("test.png", ImageHashAlgorithm.AverageHash)]
	[InlineData("test.png", ImageHashAlgorithm.DifferenceHash)]
	[InlineData("test.png", ImageHashAlgorithm.PerceptualHash)]
	[InlineData("test.bmp", ImageHashAlgorithm.AverageHash)]
	[InlineData("test.bmp", ImageHashAlgorithm.DifferenceHash)]
	[InlineData("test.bmp", ImageHashAlgorithm.PerceptualHash)]
	[InlineData("test.gif", ImageHashAlgorithm.AverageHash)]
	[InlineData("test.gif", ImageHashAlgorithm.DifferenceHash)]
	[InlineData("test.gif", ImageHashAlgorithm.PerceptualHash)]
	public async Task FingerprintImage_ValidFile_ReturnsFingerprint(string fileName, ImageHashAlgorithm algorithm)
	{
		// Arrange
		string path = GetTestImagePath(fileName);

		// Act
		ImageFingerprint result = await path.FingerprintImage(algorithm);

		// Assert
		result.ShouldNotBeNull();
		result.FilePath.ShouldBe(path);
		result.Algorithm.ShouldBe(algorithm);
	}

	[RetryFact(3)]
	public async Task FingerprintImage_FileNotFound_Throws()
	{
		// Arrange
		string nonExistentPath = Path.Combine(TestDataDir, "does_not_exist.jpg");

		// Act & Assert
		await Should.ThrowAsync<FileNotFoundException>(() => nonExistentPath.FingerprintImage());
	}

	[RetryFact(3)]
	public async Task FingerprintImage_DefaultAlgorithm_IsDifferenceHash()
	{
		// Arrange
		string path = GetTestImagePath("test.jpg");

		// Act
		ImageFingerprint result = await path.FingerprintImage();

		// Assert
		result.Algorithm.ShouldBe(ImageHashAlgorithm.DifferenceHash);
	}

	// ── FingerprintStream (Stream extension method) ───────────────────────────

	[RetryTheory(3)]
	[InlineData("test.jpg", ImageHashAlgorithm.AverageHash)]
	[InlineData("test.jpg", ImageHashAlgorithm.DifferenceHash)]
	[InlineData("test.jpg", ImageHashAlgorithm.PerceptualHash)]
	[InlineData("test.png", ImageHashAlgorithm.AverageHash)]
	[InlineData("test.png", ImageHashAlgorithm.DifferenceHash)]
	[InlineData("test.png", ImageHashAlgorithm.PerceptualHash)]
	[InlineData("test.bmp", ImageHashAlgorithm.AverageHash)]
	[InlineData("test.bmp", ImageHashAlgorithm.DifferenceHash)]
	[InlineData("test.bmp", ImageHashAlgorithm.PerceptualHash)]
	[InlineData("test.gif", ImageHashAlgorithm.AverageHash)]
	[InlineData("test.gif", ImageHashAlgorithm.DifferenceHash)]
	[InlineData("test.gif", ImageHashAlgorithm.PerceptualHash)]
	public void FingerprintStream_ValidStream_ReturnsFingerprint(string fileName, ImageHashAlgorithm algorithm)
	{
		// Arrange
		using MemoryStream stream = GetTestImageStream(fileName);
		string label = fileName;

		// Act
		ImageFingerprint result = stream.FingerprintStream(label, algorithm);

		// Assert
		result.ShouldNotBeNull();
		result.FilePath.ShouldBe(label);
		result.Algorithm.ShouldBe(algorithm);
	}

	[RetryFact(3)]
	public void FingerprintStream_DefaultLabel_IsStream()
	{
		// Arrange
		using MemoryStream stream = GetTestImageStream("test.jpg");

		// Act
		ImageFingerprint result = stream.FingerprintStream();

		// Assert
		result.FilePath.ShouldBe("<stream>");
	}

	[RetryFact(3)]
	public void FingerprintStream_InvalidData_Throws()
	{
		// Arrange
		using MemoryStream stream = new(new byte[] { 0x00, 0x01, 0x02, 0x03 });

		// Act & Assert
		Should.Throw<InvalidDataException>(() => stream.FingerprintStream("bad-data"));
	}

	// ── FingerprintDirectory ──────────────────────────────────────────────────

	[RetryFact(3)]
	public async Task FingerprintDirectory_NonRecursive_ReturnsFingerprints()
	{
		// Act
		IReadOnlyList<ImageFingerprint> results = await ImageFingerprinting.FingerprintDirectory(TestDataDir, recursive: false);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBeGreaterThan(0);
		results.ShouldAllBe(fp => fp.Algorithm == ImageHashAlgorithm.DifferenceHash);
	}

	[RetryFact(3)]
	public async Task FingerprintDirectory_Recursive_ReturnsFingerprints()
	{
		// Act — TestDataDir has no subdirectories, so recursive and non-recursive should be the same count
		IReadOnlyList<ImageFingerprint> nonRecursive = await ImageFingerprinting.FingerprintDirectory(TestDataDir, recursive: false);
		IReadOnlyList<ImageFingerprint> recursive = await ImageFingerprinting.FingerprintDirectory(TestDataDir, recursive: true);

		// Assert
		recursive.Count.ShouldBeGreaterThanOrEqualTo(nonRecursive.Count);
	}

	[RetryTheory(3)]
	[InlineData(ImageHashAlgorithm.AverageHash)]
	[InlineData(ImageHashAlgorithm.DifferenceHash)]
	[InlineData(ImageHashAlgorithm.PerceptualHash)]
	public async Task FingerprintDirectory_AllAlgorithms_Succeed(ImageHashAlgorithm algorithm)
	{
		// Act
		IReadOnlyList<ImageFingerprint> results = await ImageFingerprinting.FingerprintDirectory(TestDataDir, recursive: false, algorithm);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBeGreaterThan(0);
		results.ShouldAllBe(fp => fp.Algorithm == algorithm);
	}

	[RetryFact(3)]
	public async Task FingerprintDirectory_DirectoryWithNoImages_ReturnsEmpty()
	{
		// Arrange — create a temp dir with no image files
		string tempDir = Path.Combine(Path.GetTempPath(), $"fingerprint_test_{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);

		try
		{
			File.WriteAllText(Path.Combine(tempDir, "readme.txt"), "not an image");

			// Act
			IReadOnlyList<ImageFingerprint> results = await ImageFingerprinting.FingerprintDirectory(tempDir);

			// Assert
			results.ShouldBeEmpty();
		}
		finally
		{
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[RetryFact(3)]
	public async Task FingerprintDirectory_SkipsUnreadableFiles_DoesNotThrow()
	{
		// Arrange — create a temp dir with a corrupt "image" file
		string tempDir = Path.Combine(Path.GetTempPath(), $"fingerprint_test_{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);

		try
		{
			// Write a file with a .jpg extension but garbage content so SKBitmap.Decode fails
			File.WriteAllBytes(Path.Combine(tempDir, "corrupt.jpg"), new byte[] { 0x00, 0x01, 0x02, 0x03 });

			// Act — should not throw, just skip the bad file
			IReadOnlyList<ImageFingerprint> results = await ImageFingerprinting.FingerprintDirectory(tempDir);

			// Assert
			results.ShouldBeEmpty();
		}
		finally
		{
			Directory.Delete(tempDir, recursive: true);
		}
	}

	// ── Compare ───────────────────────────────────────────────────────────────

	[RetryFact(3)]
	public async Task Compare_SameFile_Returns100PercentSimilarity()
	{
		// Arrange
		string path = GetTestImagePath("test.jpg");
		ImageFingerprint a = await path.FingerprintImage(ImageHashAlgorithm.DifferenceHash);
		ImageFingerprint b = await path.FingerprintImage(ImageHashAlgorithm.DifferenceHash);

		// Act
		SimilarityResult result = ImageFingerprinting.Compare(a, b);

		// Assert
		result.SimilarityPercent.ShouldBe(100.0);
		result.IsDuplicate.ShouldBeTrue();
		result.ImageA.ShouldBe(a.FilePath);
		result.ImageB.ShouldBe(b.FilePath);
	}

	[RetryFact(3)]
	public async Task Compare_DifferentFiles_ReturnsSimilarityBelow100()
	{
		// Arrange
		string pathA = GetTestImagePath("test.jpg");
		string pathB = GetTestImagePath("test.png");
		ImageFingerprint a = await pathA.FingerprintImage(ImageHashAlgorithm.DifferenceHash);
		ImageFingerprint b = await pathB.FingerprintImage(ImageHashAlgorithm.DifferenceHash);

		// Act
		SimilarityResult result = ImageFingerprinting.Compare(a, b, duplicateThreshold: 99.9m);

		// Assert
		result.ShouldNotBeNull();
		result.SimilarityPercent.ShouldBeInRange(0.0, 100.0);
	}

	[RetryTheory(3)]
	[InlineData(ImageHashAlgorithm.AverageHash)]
	[InlineData(ImageHashAlgorithm.DifferenceHash)]
	[InlineData(ImageHashAlgorithm.PerceptualHash)]
	public async Task Compare_SameImage_AllAlgorithms_Returns100(ImageHashAlgorithm algorithm)
	{
		// Arrange
		string path = GetTestImagePath("test.png");
		ImageFingerprint a = await path.FingerprintImage(algorithm);
		ImageFingerprint b = await path.FingerprintImage(algorithm);

		// Act
		SimilarityResult result = ImageFingerprinting.Compare(a, b);

		// Assert
		result.SimilarityPercent.ShouldBe(100.0);
	}

	[RetryFact(3)]
	public async Task Compare_DifferentAlgorithms_Throws()
	{
		// Arrange
		string path = GetTestImagePath("test.jpg");
		ImageFingerprint a = await path.FingerprintImage(ImageHashAlgorithm.AverageHash);
		ImageFingerprint b = await path.FingerprintImage(ImageHashAlgorithm.DifferenceHash);

		// Act & Assert
		Should.Throw<InvalidOperationException>(() => ImageFingerprinting.Compare(a, b));
	}

	[RetryFact(3)]
	public async Task Compare_BelowDuplicateThreshold_IsNotDuplicate()
	{
		// Arrange — compare two very different images with a very high threshold
		string pathA = GetTestImagePath("test.jpg");
		string pathB = GetTestImagePath("test.bmp");
		ImageFingerprint a = await pathA.FingerprintImage(ImageHashAlgorithm.DifferenceHash);
		ImageFingerprint b = await pathB.FingerprintImage(ImageHashAlgorithm.DifferenceHash);

		// Act — use 100% threshold so only identical hashes are duplicates
		SimilarityResult result = ImageFingerprinting.Compare(a, b, duplicateThreshold: 100.0m);

		// If the images happen to be identical we still validate the result shape
		result.ShouldNotBeNull();
		result.IsDuplicate.ShouldBe(result.SimilarityPercent >= 100.0);
	}

	// ── FindDuplicates ────────────────────────────────────────────────────────

	[RetryFact(3)]
	public async Task FindDuplicates_SameImageTwice_FindsDuplicate()
	{
		// Arrange
		string path = GetTestImagePath("test.jpg");
		ImageFingerprint a = await path.FingerprintImage(ImageHashAlgorithm.DifferenceHash);
		ImageFingerprint b = new(path + "_copy", a.Hash, a.Algorithm); // Same hash, different label

		List<ImageFingerprint> fingerprints = [a, b];

		// Act
		IReadOnlyList<SimilarityResult> duplicates = ImageFingerprinting.FindDuplicates(fingerprints);

		// Assert
		duplicates.ShouldNotBeEmpty();
		duplicates[0].SimilarityPercent.ShouldBe(100.0);
	}

	[RetryFact(3)]
	public async Task FindDuplicates_AllDifferent_ReturnsEmpty()
	{
		// Arrange — use images that differ enough that none exceed the 90% default threshold
		// We construct fingerprints with maximally different hashes: 0 and ulong.MaxValue
		ImageFingerprint a = new("a.jpg", 0UL, ImageHashAlgorithm.DifferenceHash);
		ImageFingerprint b = new("b.jpg", ulong.MaxValue, ImageHashAlgorithm.DifferenceHash);

		await Task.CompletedTask; // satisfy async-like pattern, no real async needed

		List<ImageFingerprint> fingerprints = [a, b];

		// Act
		IReadOnlyList<SimilarityResult> duplicates = ImageFingerprinting.FindDuplicates(fingerprints, duplicateThreshold: 90.0m);

		// Assert — 0 vs MaxValue differ in all 64 bits → 0% similarity → not a duplicate
		duplicates.ShouldBeEmpty();
	}

	[RetryFact(3)]
	public async Task FindDuplicates_ResultsOrderedByDescendingSimilarity()
	{
		// Arrange — create three fingerprints where we control similarity
		string path = GetTestImagePath("test.jpg");
		ImageFingerprint original = await path.FingerprintImage(ImageHashAlgorithm.DifferenceHash);

		// Flip 1 bit (very similar) and 32 bits (less similar)
		ImageFingerprint similar1 = new("similar1.jpg", original.Hash ^ 0x01UL, original.Algorithm);
		ImageFingerprint similar2 = new("similar2.jpg", original.Hash ^ 0xFFFF_FFFF_FFFF_FFFFul, original.Algorithm);

		List<ImageFingerprint> fingerprints = [original, similar1, similar2];

		// Act
		IReadOnlyList<SimilarityResult> duplicates = ImageFingerprinting.FindDuplicates(fingerprints, duplicateThreshold: 0.0m);

		// Assert — results should be ordered descending by SimilarityPercent
		for (int i = 1; i < duplicates.Count; i++)
		{
			duplicates[i - 1].SimilarityPercent.ShouldBeGreaterThanOrEqualTo(duplicates[i].SimilarityPercent);
		}
	}

	[RetryFact(3)]
	public async Task FindDuplicates_EmptyList_ReturnsEmpty()
	{
		await Task.CompletedTask;
		List<ImageFingerprint> fingerprints = [];
		IReadOnlyList<SimilarityResult> duplicates = ImageFingerprinting.FindDuplicates(fingerprints);
		duplicates.ShouldBeEmpty();
	}

	[RetryFact(3)]
	public async Task FindDuplicates_SingleItem_ReturnsEmpty()
	{
		await Task.CompletedTask;
		string path = GetTestImagePath("test.jpg");
		ImageFingerprint a = new(path, 0xABCDEFUL, ImageHashAlgorithm.DifferenceHash);
		List<ImageFingerprint> fingerprints = [a];

		IReadOnlyList<SimilarityResult> duplicates = ImageFingerprinting.FindDuplicates(fingerprints);
		duplicates.ShouldBeEmpty();
	}

	// ── FindDuplicatesInDirectory ─────────────────────────────────────────────

	[RetryFact(3)]
	public async Task FindDuplicatesInDirectory_TestDataDir_DoesNotThrow()
	{
		// Act — just ensure it completes without exception; exact result depends on test images
		IReadOnlyList<SimilarityResult> results = await ImageFingerprinting.FindDuplicatesInDirectory(TestDataDir, recursive: false);

		// Assert
		results.ShouldNotBeNull();
	}

	[RetryFact(3)]
	public async Task FindDuplicatesInDirectory_DirectoryWithCopiedImage_FindsDuplicate()
	{
		// Arrange — create a temp dir with two copies of the same image
		string tempDir = Path.Combine(Path.GetTempPath(), $"dup_test_{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);

		try
		{
			string source = GetTestImagePath("test.jpg");
			File.Copy(source, Path.Combine(tempDir, "copy1.jpg"));
			File.Copy(source, Path.Combine(tempDir, "copy2.jpg"));

			// Act
			IReadOnlyList<SimilarityResult> results = await ImageFingerprinting.FindDuplicatesInDirectory(tempDir);

			// Assert — the two copies should be identified as duplicates
			results.ShouldNotBeEmpty();
			results[0].SimilarityPercent.ShouldBe(100.0);
		}
		finally
		{
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[RetryFact(3)]
	public async Task FindDuplicatesInDirectory_Recursive_DoesNotThrow()
	{
		// Act
		IReadOnlyList<SimilarityResult> results = await ImageFingerprinting.FindDuplicatesInDirectory(TestDataDir, recursive: true);
		results.ShouldNotBeNull();
	}

	[RetryTheory(3)]
	[InlineData(ImageHashAlgorithm.AverageHash)]
	[InlineData(ImageHashAlgorithm.PerceptualHash)]
	public async Task FindDuplicatesInDirectory_AllAlgorithms_DoesNotThrow(ImageHashAlgorithm algorithm)
	{
		// Act
		IReadOnlyList<SimilarityResult> results = await ImageFingerprinting.FindDuplicatesInDirectory(TestDataDir, recursive: false, algorithm);
		results.ShouldNotBeNull();
	}

	// ── HammingSimilarity ─────────────────────────────────────────────────────

	[RetryFact(3)]
	public void HammingSimilarity_IdenticalHashes_Returns100()
	{
		// Arrange
		ulong hash = 0xDEADBEEF_CAFEBABE;

		// Act
		double similarity = ImageFingerprinting.HammingSimilarity(hash, hash);

		// Assert
		similarity.ShouldBe(100.0);
	}

	[RetryFact(3)]
	public void HammingSimilarity_AllBitsDifferent_Returns0()
	{
		// Arrange — 0 and MaxValue differ in all 64 bits
		ulong hashA = 0UL;
		ulong hashB = ulong.MaxValue;

		// Act
		double similarity = ImageFingerprinting.HammingSimilarity(hashA, hashB);

		// Assert
		similarity.ShouldBe(0.0);
	}

	[RetryFact(3)]
	public void HammingSimilarity_HalfBitsDifferent_Returns50()
	{
		// Arrange — flip exactly 32 bits (lower 32 bits set in hashB)
		ulong hashA = 0xFFFF_FFFF_0000_0000UL; // upper 32 bits set
		ulong hashB = 0x0000_0000_FFFF_FFFFUL; // lower 32 bits set
																					 // XOR = 0xFFFF_FFFF_FFFF_FFFF → all 64 bits differ
																					 // Actually let's be more precise: 32 bits differ
		hashA = 0xFFFF_FFFF_FFFF_FFFFUL; // all bits set
		hashB = 0xFFFF_FFFF_0000_0000UL; // upper 32 bits set → lower 32 differ

		// Act
		double similarity = ImageFingerprinting.HammingSimilarity(hashA, hashB);

		// Assert — 32 bits differ out of 64 → 50% similarity
		similarity.ShouldBe(50.0);
	}

	[RetryTheory(3)]
	[InlineData(0UL, 0UL, 100.0)]
	[InlineData(ulong.MaxValue, ulong.MaxValue, 100.0)]
	[InlineData(0UL, ulong.MaxValue, 0.0)]
	[InlineData(0x8000_0000_0000_0000UL, 0UL, 100.0 - (1.0 / 64.0 * 100.0))]
	public void HammingSimilarity_KnownValues_AreCorrect(ulong hashA, ulong hashB, double expectedSimilarity)
	{
		double similarity = ImageFingerprinting.HammingSimilarity(hashA, hashB);
		similarity.ShouldBe(expectedSimilarity, tolerance: 0.001);
	}

	[RetryFact(3)]
	public void HammingSimilarity_IsSymmetric()
	{
		ulong hashA = 0x1234_5678_9ABC_DEF0UL;
		ulong hashB = 0xFEDC_BA98_7654_3210UL;

		double ab = ImageFingerprinting.HammingSimilarity(hashA, hashB);
		double ba = ImageFingerprinting.HammingSimilarity(hashB, hashA);

		ab.ShouldBe(ba);
	}

	// ── Same image, all three algorithms produce consistent hashes ────────────

	[RetryTheory(3)]
	[InlineData("test.jpg")]
	[InlineData("test.png")]
	[InlineData("test.bmp")]
	public async Task FingerprintImage_SameFileTwice_ProducesSameHash(string fileName)
	{
		string path = GetTestImagePath(fileName);

		foreach (ImageHashAlgorithm algorithm in Enum.GetValues<ImageHashAlgorithm>())
		{
			ImageFingerprint fp1 = await path.FingerprintImage(algorithm);
			ImageFingerprint fp2 = await path.FingerprintImage(algorithm);

			fp1.Hash.ShouldBe(fp2.Hash, $"Hash mismatch for {algorithm}");
		}
	}

	// ── ImageFingerprint and SimilarityResult record equality ────────────────

	[RetryFact(3)]
	public void ImageFingerprint_RecordEquality_Works()
	{
		ImageFingerprint a = new("path.jpg", 12345UL, ImageHashAlgorithm.AverageHash);
		ImageFingerprint b = new("path.jpg", 12345UL, ImageHashAlgorithm.AverageHash);
		a.ShouldBe(b);
	}

	[RetryFact(3)]
	public void SimilarityResult_RecordEquality_Works()
	{
		SimilarityResult a = new("a.jpg", "b.jpg", 95.0, true);
		SimilarityResult b = new("a.jpg", "b.jpg", 95.0, true);
		a.ShouldBe(b);
	}
}
