using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using xRetry;

namespace Word.OpenXml.Tests;

public sealed class ChangeUrlTests : IDisposable
{
	private readonly string _testDocPath;
	private readonly string _tempDocPath;
	private readonly FileStream? _tempFileStream;

	private bool disposed;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				_tempFileStream?.Dispose();
			}
			disposed = true;
		}
		if (File.Exists(_tempDocPath))
		{
			File.Delete(_tempDocPath);
		}
	}

	~ChangeUrlTests()
	{
		Dispose(false);
	}

	public ChangeUrlTests()
	{
		// Set up test paths
		_testDocPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "TestDocument.docx");
		_tempDocPath = Path.GetTempFileName();

		// Copy test doc to temp location for each test
		File.Copy(_testDocPath, _tempDocPath, true);
		_tempFileStream = new FileStream(_tempDocPath, FileMode.Open, FileAccess.ReadWrite);
	}

	private static List<HyperlinkRelationship> GetHyperlinks(Stream docStream)
	{
		using WordprocessingDocument doc = WordprocessingDocument.Open(docStream, false);
		return doc.MainDocumentPart?.HyperlinkRelationships.ToList() ?? new List<HyperlinkRelationship>();
	}

	[RetryTheory(3)]
	[InlineData("http://TestUrl/1", "http://NewUrl/1", true)]
	[InlineData("http://TestUrl/2", "http://NewUrl/2", false)]
	public void ChangeUrlsInWordDoc_SingleUrl_ReplacesCorrectly(string urlToReplace, string newUrl, bool replaceAll)
	{
		// Act
		bool result = CommonNetFuncs.Word.OpenXml.ChangeUrls.ChangeUrlsInWordDoc(_tempFileStream!, newUrl, urlToReplace, replaceAll);
		_tempFileStream!.Position = 0;

		// Assert
		result.ShouldBeTrue();
		List<HyperlinkRelationship> hyperlinks = GetHyperlinks(_tempFileStream);

		// Verify the URL was replaced
		hyperlinks.Count(h => h.Uri.ToString().Equals(urlToReplace, StringComparison.InvariantCultureIgnoreCase))
				.ShouldBe(replaceAll ? 0 : 1);

		// Verify the new URL exists
		hyperlinks.Count(h => h.Uri.ToString().Equals(newUrl, StringComparison.InvariantCultureIgnoreCase))
				.ShouldBe(replaceAll ? 2 : 1);

		// Control URLs should remain unchanged
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("google.com"));
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("xkcd.com"));
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("github.com"));
	}

	[RetryFact(3)]
	public void ChangeUrlsInWordDoc_Dictionary_ReplacesMultipleUrls()
	{
		// Arrange
		Dictionary<string, string> urlsToUpdate = new()
				{
						{ "http://TestUrl/1", "http://NewUrl/1" },
						{ "http://TestUrl/2", "http://NewUrl/2" },
						{ "http://TestUrl/3", "http://NewUrl/3" }
				};

		// Act
		bool result = CommonNetFuncs.Word.OpenXml.ChangeUrls.ChangeUrlsInWordDoc(_tempFileStream!, urlsToUpdate);
		_tempFileStream!.Position = 0;

		// Assert
		result.ShouldBeTrue();
		IEnumerable<HyperlinkRelationship> hyperlinks = GetHyperlinks(_tempFileStream);

		// Verify all test URLs were replaced
		foreach (KeyValuePair<string, string> kvp in urlsToUpdate)
		{
			hyperlinks.ShouldNotContain(h => h.Uri.ToString().Equals(kvp.Key, StringComparison.InvariantCultureIgnoreCase));
			hyperlinks.ShouldContain(h => h.Uri.ToString().Equals(kvp.Value, StringComparison.InvariantCultureIgnoreCase));
		}

		// Control URLs should remain unchanged
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("google.com"));
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("xkcd.com"));
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("github.com"));
	}

	[RetryTheory(3)]
	[InlineData(@"(TestUrl/)(\d+)", "NewUrl/$2", true)]
	[InlineData("TestUrl/1", "NewUrl/First", false)]
	public void ChangeUrlsInWordDocRegex_SinglePattern_ReplacesCorrectly(string pattern, string replacement, bool replaceAll)
	{
		// Act
		bool result = CommonNetFuncs.Word.OpenXml.ChangeUrls.ChangeUrlsInWordDocRegex(_tempFileStream!, pattern, replacement, replaceAll, RegexOptions.IgnoreCase);
		_tempFileStream!.Position = 0;

		// Assert
		result.ShouldBeTrue();
		IEnumerable<HyperlinkRelationship> hyperlinks = GetHyperlinks(_tempFileStream);

		// Verify the pattern matches were replaced
		Regex regex = new(pattern, RegexOptions.IgnoreCase);
		int remainingMatches = hyperlinks.Count(h => regex.IsMatch(h.Uri.ToString()));
		remainingMatches.ShouldBe(replaceAll ? 0 : 1); // Should be 0 if replaceAll, otherwise 2 (as one was replaced)

		// Control URLs should remain unchanged
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("google.com"));
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("xkcd.com"));
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("github.com"));
	}

	[RetryTheory(3)]
	[InlineData("TestUrl", true, false)]
	[InlineData("TestUrl", false, true)]
	[InlineData("testurl", true, true)]
	[InlineData("testurl", false, true)]
	public void ChangeUrlsInWordDocRegex_Dictionary_ReplacesMultiplePatterns(string regexPattern, bool caseSensitive, bool shoudSucceed)
	{
		// Arrange
		Dictionary<string, string> patternsToUpdate = new()
				{
						{ $"{regexPattern}/1", "NewUrl/First" },
						{ $"{regexPattern}/2", "NewUrl/Second" },
						{ $"{regexPattern}/3", "NewUrl/Third" }
				};

		// Act
		bool result = CommonNetFuncs.Word.OpenXml.ChangeUrls.ChangeUrlsInWordDocRegex(_tempFileStream!, patternsToUpdate, regexOptions: caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
		_tempFileStream!.Position = 0;

		// Assert
		result.ShouldBeTrue();
		IEnumerable<HyperlinkRelationship> hyperlinks = GetHyperlinks(_tempFileStream);

		// Verify all patterns were replaced
		if (shoudSucceed)
		{
			foreach (KeyValuePair<string, string> kvp in patternsToUpdate)
			{
				hyperlinks.ShouldNotContain(h => h.Uri.ToString().Contains(kvp.Key, StringComparison.InvariantCultureIgnoreCase));
				hyperlinks.ShouldContain(h => h.Uri.ToString().Contains(kvp.Value, StringComparison.InvariantCultureIgnoreCase));
			}
		}
		else
		{
			foreach (KeyValuePair<string, string> kvp in patternsToUpdate)
			{
				hyperlinks.ShouldContain(h => h.Uri.ToString().Contains(kvp.Key, StringComparison.InvariantCultureIgnoreCase));
				hyperlinks.ShouldNotContain(h => h.Uri.ToString().Contains(kvp.Value, StringComparison.InvariantCultureIgnoreCase));
			}
		}

		// Control URLs should remain unchanged
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("google.com"));
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("xkcd.com"));
		hyperlinks.ShouldContain(h => h.Uri.ToString().Contains("github.com"));
	}

	[RetryFact(3)]
	public void ChangeUrlsInWordDoc_InvalidStream_ReturnsFalse()
	{
		// Arrange
		using MemoryStream invalidStream = new(new byte[] { 0x0 });

		// Act
		bool result = CommonNetFuncs.Word.OpenXml.ChangeUrls.ChangeUrlsInWordDoc(invalidStream, "http://NewUrl/1", "http://TestUrl/1");

		// Assert
		result.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ChangeUrlsInWordDocRegex_InvalidPattern_ReturnsFalse()
	{
		// Arrange
		const string invalidPattern = "["; // Invalid regex pattern

		// Act
		bool result = CommonNetFuncs.Word.OpenXml.ChangeUrls.ChangeUrlsInWordDocRegex(_tempFileStream!, invalidPattern, "replacement");

		// Assert
		result.ShouldBeFalse();
	}
}
