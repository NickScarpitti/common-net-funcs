using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FastExpressionCompiler;
using static System.Convert;
using static System.Web.HttpUtility;
using static CommonNetFuncs.Core.MathHelpers;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Core;

public enum EYesNo
{
	Yes,
	No
}

public enum EHashAlgorithm
{
	SHA1,
	SHA256,
	SHA384,
	SHA512,
	MD5
}

public enum EComparisonType
{
	OR,
	AND
}

public enum TitleCaseUppercaseWordHandling
{
	IgnoreUppercase,
	ConvertAllUppercase,
	ConvertByLength
}

/// <summary>
/// Methods for complex string manipulation
/// </summary>
public static partial class Strings
{
	public const string TimestampUrlFormat = "yyyyMMddHHmmssFFF";
	public const string DateOnlyUrlFormat = "yyyyMMdd";

	[GeneratedRegex(@"\s+")]
	private static partial Regex MultiSpaceRegex();

	[GeneratedRegex("^[a-zA-Z0-9]*$")]
	private static partial Regex AlphanumericRegex();

	[GeneratedRegex(@"^[a-zA-Z0-9\s]*$")]
	private static partial Regex AlphanumericWithSpacesRegex();

	[GeneratedRegex("^[a-zA-Z]*$")]
	private static partial Regex AlphaOnlyRegex();

	[GeneratedRegex(@"^[a-zA-Z\s]*$")]
	private static partial Regex AlphaOnlyWithSpacesRegex();

	[GeneratedRegex("^[0-9]*$")]
	private static partial Regex NumericOnlyRegex();

	[GeneratedRegex(@"^[0-9\s]*$")]
	private static partial Regex NumericOnlyWithSpacesRegex();

	[GeneratedRegex(@"\D+")]
	private static partial Regex ExtractNumbersRegex();

	[GeneratedRegex(@"(\d{3})(\d{4})")]
	private static partial Regex SevenDigitPhoneNumberRegex();

	[GeneratedRegex(@"(\d{3})(\d{3})(\d{4})")]
	private static partial Regex TenDigitPhoneNumberRegex();

	[GeneratedRegex(@"(\d{1})(\d{3})(\d{3})(\d{4})")]
	private static partial Regex ElevenDigitPhoneNumberRegex();

	[GeneratedRegex(@"(\d{2})(\d{3})(\d{3})(\d{4})")]
	private static partial Regex TwelveDigitPhoneNumberRegex();

	[GeneratedRegex("[A-Za-z]")]
	private static partial Regex RemoveLettersRegex();

	[GeneratedRegex("[0-9]")]
	private static partial Regex RemoveNumbersRegex();

	[GeneratedRegex("[A-Za-z ]")]
	private static partial Regex LettersOnlyRegex();

	[GeneratedRegex(@"[0-9]*\.?[0-9]+")]
	private static partial Regex NumbersOnlyRegex();

	[GeneratedRegex(@"[0-9 ]*\.?[0-9]+((\/|\\)[0-9 ]*\.?[0-9]+)?")]
	private static partial Regex NumbersWithFractionsOnlyRegex();

	[GeneratedRegex(@"(\s+|[^\w\s])")]
	private static partial Regex TitleCaseSplitRegex();

	[GeneratedRegex(@"\w")]
	private static partial Regex TitleCaseWordRegex();

	/// <summary>
	/// Clone of VBA Left() function that gets n characters from the left side of the string
	/// </summary>
	/// <param name="s">String to get left substring from</param>
	/// <param name="numChars">Number of characters to take from the right side of the string</param>
	/// <returns>String of the length indicated from the left side of the source string</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static string? Left(this string? s, int numChars)
	{
		if (s == null)
		{
			return null;
		}
		else if (s.Length == 0)
		{
			return string.Empty;
		}
		else if (numChars <= s.Length)
		{
			return s[..numChars];
		}
		else
		{
			return s;
		}
	}

	/// <summary>
	/// Clone of VBA Left() function that gets n characters from the left side of the string
	/// </summary>
	/// <param name="s">String to get left substring from</param>
	/// <param name="numChars">Number of characters to take from the right side of the string</param>
	/// <returns>String of the length indicated from the left side of the source string</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static ReadOnlySpan<char> Left(this ReadOnlySpan<char> s, int numChars)
	{
		if (s.IsEmpty)
		{
			return string.Empty;
		}
		else if (numChars <= s.Length)
		{
			return s[..numChars];
		}
		else
		{
			return s;
		}
	}

	/// <summary>
	/// Clone of VBA Right() function that gets n characters from the right side of the string
	/// </summary>
	/// <param name="s">String to extract right substring from</param>
	/// <param name="numChars">Number of characters to take from the right side of the string</param>
	/// <returns>String of the length indicated from the right side of the source string</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static string? Right(this string? s, int numChars)
	{
		if (s == null)
		{
			return null;
		}
		else if (s.Length == 0)
		{
			return string.Empty;
		}
		else if (numChars <= s.Length)
		{
			return s.Substring(s.Length - numChars, numChars);
		}
		else
		{
			return s;
		}
	}

	/// <summary>
	/// Clone of VBA Right() function that gets n characters from the right side of the string
	/// </summary>
	/// <param name="s">String to extract right substring from</param>
	/// <param name="numChars">Number of characters to take from the right side of the string</param>
	/// <returns>String of the length indicated from the right side of the source string</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static ReadOnlySpan<char> Right(this ReadOnlySpan<char> s, int numChars)
	{
		if (s.IsEmpty)
		{
			return string.Empty;
		}
		else if (numChars <= s.Length)
		{
			return s[^numChars..];
		}
		else
		{
			return s;
		}
	}

	/// <summary>
	/// Extract the string between two string values
	/// </summary>
	/// <param name="s">String value to extract value from</param>
	/// <param name="sStart">Text that ends immediately before the end of the string you wish to extract</param>
	/// <param name="sEnd">Text that starts immediately after the end of the string you wish to extract</param>
	/// <returns>Extracted string found between the two given string values</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static string? ExtractBetween(this string? s, string sStart, string sEnd)
	{
		string? result = null;
		if (s != null)
		{
			int sStartStartIndex = s.IndexOf(sStart);//Find the beginning index of the word1
			int sStartEndIndex = sStartStartIndex + sStart.Length;//Add the length of the word1 to starting index to find the end of the word1
			int sEndStartIndex = s.LastIndexOf(sEnd);//Find the beginning index of word2
			int length = sEndStartIndex - sStartEndIndex;//Length of the sub string by subtracting index beginning of word2 from the end of word1
			if (sStartStartIndex != -1 && sEndStartIndex != -1 && length > 0 && sStartEndIndex + length <= s.Length - 1)
			{
				ReadOnlySpan<char> textToSlice = s.AsSpan();
				result = textToSlice.Slice(sStartEndIndex, length).ToString();//Get the substring based on the end of word1 and length
			}
		}
		return result;
	}

	/// <summary>
	/// Extract the string between two string values
	/// </summary>
	/// <param name="s">String value to extract value from</param>
	/// <param name="sStart">Text that ends immediately before the end of the string you wish to extract</param>
	/// <param name="sEnd">Text that starts immediately after the end of the string you wish to extract</param>
	/// <returns>Extracted string found between the two given string values</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static ReadOnlySpan<char> ExtractBetween(this ReadOnlySpan<char> s, string sStart, string sEnd)
	{
		string? result = string.Empty;
		if (!s.IsEmpty)
		{
			int sStartStartIndex = s.IndexOf(sStart);//Find the beginning index of the word1
			int sStartEndIndex = sStartStartIndex + sStart.Length;//Add the length of the word1 to starting index to find the end of the word1
			int sEndStartIndex = s.LastIndexOf(sEnd);//Find the beginning index of word2
			int length = sEndStartIndex - sStartEndIndex;//Length of the sub string by subtracting index beginning of word2 from the end of word1
			if (sStartStartIndex != -1 && sEndStartIndex != -1 && length > 0 && sStartEndIndex + length <= s.Length - 1)
			{
				result = s.Slice(sStartEndIndex, length).ToString();//Get the substring based on the end of word1 and length
			}
		}
		return result;
	}

	/// <summary>
	/// Makes a string with the word "null" into a null value
	/// </summary>
	/// <param name="s">String to change to null if it contains the word "null"</param>
	/// <returns>Null if the string passed in is null or is the word null with no other text characters other than whitespace</returns>
	public static string? MakeNullNull(this string? s)
	{
		return !s.IsNullOrWhiteSpace() && (s?.StrEq("Null") != false || s.ToUpperInvariant().Replace("NULL", string.Empty)?.Length == 0 || s.Trim().StrEq("Null")) ? null : s;
	}

	/// <summary>
	/// Parses a string that is using pascal casing (works with camel case as well) so that each word is separated by a space
	/// </summary>
	/// <param name="s">String to parse</param>
	/// <returns>Original string with spaces between all words starting with a capital letter</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static string? ParsePascalCase(this string? s)
	{
		if (s.IsNullOrWhiteSpace())
		{
			return s;
		}

		StringBuilder result = new(s.Length + 10); // Pre-allocate with buffer
		bool first = true;

		foreach (char c in s.AsSpan())
		{
			if (char.IsUpper(c) && !first)
			{
				result.Append(' ');
			}

			result.Append(c);
			first = false;
		}

		return result.ToString();
	}

	/// <summary>
	/// Parses a string that is using pascal casing (works with camel case as well) so that each word is separated by a space
	/// </summary>
	/// <param name="s">String to parse</param>
	/// <returns>Original string with spaces between all words starting with a capital letter</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static ReadOnlySpan<char> ParsePascalCase(this ReadOnlySpan<char> s)
	{
		if (s.IsEmpty)
		{
			return s;
		}

		StringBuilder result = new(s.Length + 10); // Pre-allocate with buffer
		bool first = true;

		foreach (char c in s)
		{
			if (char.IsUpper(c) && !first)
			{
				result.Append(' ');
			}

			result.Append(c);
			first = false;
		}

		return result.ToString();
	}

	/// <summary>
	/// Converts a string to title case with options for handling uppercase words.
	/// </summary>
	/// <param name="input">The input string to convert.</param>
	/// <param name="uppercaseHandling">How to handle uppercase words.</param>
	/// <param name="minLengthToConvert">Minimum length of uppercase words to convert (used only with UppercaseHandling.ConvertByLength).</param>
	/// <returns>The title-cased string.</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static string? ToTitleCase(this string? input, string cultureString = "en-US", TitleCaseUppercaseWordHandling uppercaseHandling = TitleCaseUppercaseWordHandling.IgnoreUppercase, int minLengthToConvert = 0, CancellationToken cancellationToken = default)
	{
		if (input.IsNullOrWhiteSpace())
		{
			return input;
		}

		// Use TextInfo for culture-aware title casing
		TextInfo textInfo = new CultureInfo(cultureString, false).TextInfo;

		if (uppercaseHandling == TitleCaseUppercaseWordHandling.IgnoreUppercase)
		{
			return textInfo.ToTitleCase(input);
		}

		// Split the input by word boundaries
		string[] words = TitleCaseSplitRegex().Split(input);
		StringBuilder result = new();

		foreach (string word in words)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrEmpty(word))
			{
				continue;
			}

			bool isAllUppercase = word.Length > 0 && word.StrComp(word.ToUpper()) && !word.StrComp(word.ToLower());
			if (isAllUppercase)
			{
				switch (uppercaseHandling)
				{
					case TitleCaseUppercaseWordHandling.ConvertAllUppercase:
						result.Append(textInfo.ToTitleCase(word.ToLower()));
						break;
					case TitleCaseUppercaseWordHandling.ConvertByLength:
						if (word.Length >= minLengthToConvert)
						{
							result.Append(textInfo.ToTitleCase(word.ToLower()));
						}
						else
						{
							result.Append(word); // Leave casing as is
						}
						break;
				}
			}
			else
			{
				// For non-uppercase words, always convert to title case
				if (TitleCaseWordRegex().IsMatch(word))
				{
					result.Append(textInfo.ToTitleCase(word.ToLower()));
				}
				else
				{
					// Preserve non-word characters
					result.Append(word);
				}
			}
		}

		return result.ToString();
	}

	/// <summary>
	/// Trims a string removing all extra leading and trailing spaces as well as reducing multiple consecutive spaces to only 1 space
	/// </summary>
	/// <param name="s">String to remove extra spaces from</param>
	/// <returns>String without leading, trailing or multiple consecutive spaces</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static string? TrimFull(this string? s)
	{
		if (!s.IsNullOrWhiteSpace())
		{
			s = MultiSpaceRegex().Replace(s.Trim(), " ");
		}
		return s?.Trim();
	}

	/// <summary>
	/// Indicates whether a specified string is null, a zero length string, or consists only of white-space characters
	/// </summary>
	/// <param name="s">The string to test</param>
	/// <returns><see langword="true"/> if s is null, a zero length string, or consists only of white-space characters</returns>
	public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? s)
	{
		return string.IsNullOrWhiteSpace(s);
	}

	/// <summary>
	/// Indicates whether a specified string is null or a zero length string
	/// </summary>
	/// <param name="s">The string to test</param>
	/// <returns><see langword="true"/> if s is null or a zero length string</returns>
	public static bool IsNullOrEmpty([NotNullWhen(false)] this string? s)
	{
		return string.IsNullOrEmpty(s);
	}

	/// <summary>
	/// Indicates whether a specified string is null or a zero length string
	/// </summary>
	/// <param name="enumerable">Collection to check if it's null or has no elements</param>
	/// <returns><see langword="true"/> if s is null or a zero length string</returns>
	public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? enumerable)
	{
		return enumerable?.Any() != true;
	}

	/// <summary>
	/// Checks if the given string contains a specific string regardless of culture or case
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="textToFind">String to find in s</param>
	/// <returns><see langword="true"/> if s contains the string textToFind in any form</returns>
	public static bool ContainsInvariant(this string? s, string? textToFind)
	{
		return textToFind != null && (s?.Contains(textToFind, StringComparison.InvariantCultureIgnoreCase) ?? false);
	}

	/// <summary>
	/// Checks if the given string contains a specific string regardless of culture or case
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="textToFind">String to find in s</param>
	/// <returns><see langword="true"/> if s contains the string textToFind in any form</returns>
	public static bool ContainsInvariant(this ReadOnlySpan<char> s, ReadOnlySpan<char> textToFind)
	{
		return !s.IsEmpty && !textToFind.IsEmpty && s.IndexOf(textToFind, StringComparison.InvariantCultureIgnoreCase) >= 0;
	}

	/// <summary>
	/// Checks if the any of the values in a collection of strings contains a specific string regardless of culture or case
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="textToFind">String to find in s</param>
	/// <returns><see langword="true"/> if s contains the string textToFind in any form</returns>
	public static bool ContainsInvariant(this IEnumerable<string?>? s, string? textToFind)
	{
		return s?.Contains(textToFind, StringComparer.InvariantCultureIgnoreCase) ?? false;
	}

	/// <summary>
	/// Checks if the any of the values in a collection of strings contains a specific string regardless of culture or case
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="textToFind">String to find in s</param>
	/// <returns><see langword="true"/> if s contains the string textToFind in any form</returns>
	public static bool ContainsInvariant(this IEnumerable<string?>? s, ReadOnlySpan<char> textToFind)
	{
		if (s == null || textToFind.IsEmpty)
		{
			return false;
		}


#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
		foreach (ReadOnlySpan<char> item in s)
		{
			if (item.ContainsInvariant(textToFind))
			{
				return true;
			}
		}
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
		return false;
	}

	/// <summary>
	/// Checks if the given string contains a specific string regardless of culture or case
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="textsToFind">Strings to find in s</param>
	/// <param name="useOrComparison">
	/// <para>If <see langword="true"/>, will check if any of the textsToFind values are in s. (OR configuration)</para> <para>If false, will check if all of the textsToFind values are in s. (AND configuration)</para>
	/// </param>
	/// <returns>
	/// <para>True if s contains any of the strings in textsToFind in any form when useOrComparison = True</para> <para>True if s contains all of the strings in textsToFind when useOrComparison =
	/// False</para>
	/// </returns>
	public static bool ContainsInvariant(this string? s, IEnumerable<string> textsToFind, bool useOrComparison = true)
	{
		if (s.IsNullOrWhiteSpace())
		{
			return false;
		}

		if (useOrComparison)
		{
			return textsToFind.Any(s.ContainsInvariant);
		}
		else
		{
			return textsToFind.All(s.ContainsInvariant);
		}
	}

	/// <summary>
	/// Checks if the given string contains a specific string regardless of culture or case
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="textsToFind">Strings to find in s</param>
	/// <param name="useOrComparison">
	/// <para>If <see langword="true"/>, will check if any of the textsToFind values are in s. (OR configuration)</para> <para>If false, will check if all of the textsToFind values are in s. (AND configuration)</para>
	/// </param>
	/// <returns>
	/// <para>True if s contains any of the strings in textsToFind in any form when useOrComparison = True</para> <para>True if s contains all of the strings in textsToFind when useOrComparison =
	/// False</para>
	/// </returns>
	public static bool ContainsInvariant(this ReadOnlySpan<char> s, IEnumerable<string> textsToFind, bool useOrComparison = true)
	{
		if (s.IsEmpty)
		{
			return false;
		}

		if (useOrComparison)
		{
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
			foreach (ReadOnlySpan<char> textToFind in textsToFind)
			{
				if (s.ContainsInvariant(textToFind))
				{
					return true;
				}
			}
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
			return false;
		}
		else
		{
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
			foreach (ReadOnlySpan<char> textToFind in textsToFind)
			{
				if (!s.ContainsInvariant(textToFind))
				{
					return false;
				}
			}
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
			return true;
		}
	}

	/// <summary>
	/// Checks if the given string begins with a specific string regardless of culture or case
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="textToFind">String to find in s</param>
	/// <returns><see langword="true"/> if s contains the string textToFind in any form</returns>
	public static bool StartsWithInvariant(this string? s, string? textToFind)
	{
		return textToFind != null && (s?.StartsWith(textToFind, StringComparison.InvariantCultureIgnoreCase) ?? false);
	}

	/// <summary>
	/// Checks if the given string begins with a specific string regardless of culture or case
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="textToFind">String to find in s</param>
	/// <returns><see langword="true"/> if s contains the string textToFind in any form</returns>
	public static bool StartsWithInvariant(this ReadOnlySpan<char> s, ReadOnlySpan<char> textToFind)
	{
		return !textToFind.IsEmpty && s.StartsWith(textToFind, StringComparison.InvariantCultureIgnoreCase);
	}

	/// <summary>
	/// Checks if the given string contains a specific string regardless of culture or case
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="textToFind">String to find in s</param>
	/// <returns><see langword="true"/> if s contains the string textToFind in any form</returns>
	public static bool EndsWithInvariant(this string? s, string? textToFind)
	{
		return textToFind != null && (s?.EndsWith(textToFind, StringComparison.InvariantCultureIgnoreCase) ?? false);
	}

	/// <summary>
	/// Checks if the given <see cref="ReadOnlySpan{T}"/> contains a specific <see cref="ReadOnlySpan{T}"/> regardless of culture or case.
	/// </summary>
	/// <param name="s"><see cref="ReadOnlySpan{T}"/> to search.</param>
	/// <param name="textToFind"><see cref="ReadOnlySpan{T}"/> to find in s.</param>
	/// <returns><see langword="true"/> if <paramref name="s"/> contains <paramref name="textToFind"/> in any form, otherwise <see langword="false">.</returns>
	public static bool EndsWithInvariant(this ReadOnlySpan<char> s, ReadOnlySpan<char> textToFind)
	{
		return !textToFind.IsEmpty && s.EndsWith(textToFind, StringComparison.InvariantCultureIgnoreCase);
	}

	/// <summary>
	/// Searches <paramref name="s"/> for <paramref name="textToFind"/> invarient of culture or case and returns its index if found.
	/// </summary>
	/// <param name="s">String to search.</param>
	/// <param name="textToFind">String to find in s.</param>
	/// <returns>The zero-based index of first occurrence of <paramref name="textToFind"/> if <paramref name="s"/> contains <paramref name="textToFind"/> in any form or -1 if <paramref name="s"/> is <see langword="null"/> or <paramref name="textToFind"/> is not found.</returns>
	public static int IndexOfInvariant(this string? s, string? textToFind)
	{
		return textToFind != null ? s?.IndexOf(textToFind, StringComparison.InvariantCultureIgnoreCase) ?? -1 : -1;
	}

	/// <summary>
	/// Searches <paramref name="s"/> for <paramref name="charToFind"/> invarient of culture or case and returns its index if found.
	/// </summary>
	/// <param name="s">String to search.</param>
	/// <param name="charToFind">Char to find in s.</param>
	/// <returns>The zero-based index of first occurrence of <paramref name="charToFind"/> if <paramref name="s"/> contains <paramref name="charToFind"/> in any form or -1 if <paramref name="s"/> is <see langword="null"/> or <paramref name="textToFind"/> is not found.</returns>
	public static int IndexOfInvariant(this string? s, char? charToFind)
	{
		return charToFind != null ? s?.IndexOf((char)charToFind, StringComparison.InvariantCultureIgnoreCase) ?? -1 : -1;
	}

	/// <summary>
	/// Searches <paramref name="s"/> for <paramref name="textToFind"/> invarient of culture or case and returns its index if found.
	/// </summary>
	/// <param name="s"><see cref="ReadOnlySpan{T}"/> to search.</param>
	/// <param name="textToFind"><see cref="ReadOnlySpan{T}"/> to find in s.</param>
	/// <returns>The zero-based index of first occurrence of <paramref name="textToFind"/> if <paramref name="s"/> contains <paramref name="textToFind"/> in any form or -1 if <paramref name="s"/> is empty or <paramref name="textToFind"/> is not found.</returns>
	public static int IndexOfInvariant(this ReadOnlySpan<char> s, ReadOnlySpan<char> textToFind)
	{
		return !textToFind.IsEmpty ? s.IndexOf(textToFind, StringComparison.InvariantCultureIgnoreCase) : -1;
	}

	/// <summary>
	/// Searches <paramref name="s"/> for <paramref name="charToFind"/> invarient of culture or case and returns its index if found.
	/// </summary>
	/// <param name="s">String to search.</param>
	/// <param name="charToFind">Character to find in s.</param>
	/// <returns>The zero-based index of first occurrence of <paramref name="charToFind"/> if <paramref name="s"/> contains <paramref name="charToFind"/> in any form or -1 if <paramref name="s"/> is empty or <paramref name="charToFind"/> is not found.</returns>
	public static int IndexOfInvariant(this ReadOnlySpan<char> s, char? charToFind)
	{
		return charToFind != null ? s.IndexOf((char)charToFind) : -1;
	}

	/// <summary>
	/// Checks if the given string contains at least one or all of the strings in a collection of strings, regardless of culture or case.
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="stringsToFind">Strings to find in s</param>
	/// <param name="useOrComparison">
	/// <para>If <see langword="true"/>, will check if any of the stringsToFind values are in s. (OR configuration)</para>
	/// <para>If false, will check if all of the stringsToFind values are in s. (AND configuration)</para>
	/// </param>
	/// <returns>
	/// <para>True if s contains any of the strings in stringsToFind in any form when useOrComparison = True</para>
	/// <para>True if s contains all of the strings in stringsToFind when useOrComparison = False</para>
	/// </returns>
	public static bool Contains(this string? s, IEnumerable<string> stringsToFind, bool useOrComparison = true)
	{
		if (s.IsNullOrWhiteSpace())
		{
			return false;
		}

		if (useOrComparison)
		{
			return stringsToFind.Any(s.Contains);
		}
		else
		{
			return stringsToFind.All(s.Contains);
		}
	}

	/// <summary>
	/// Checks if the given string contains at least one or all of the strings in a collection of strings, regardless of culture or case.
	/// </summary>
	/// <param name="s">String to search</param>
	/// <param name="stringsToFind">Strings to find in s</param>
	/// <param name="useOrComparison">
	/// <para>If <see langword="true"/>, will check if any of the stringsToFind values are in s. (OR configuration)</para>
	/// <para>If false, will check if all of the stringsToFind values are in s. (AND configuration)</para>
	/// </param>
	/// <returns>
	/// <para>True if s contains any of the strings in stringsToFind in any form when useOrComparison = True</para>
	/// <para>True if s contains all of the strings in stringsToFind when useOrComparison = False</para>
	/// </returns>
	public static bool Contains(this ReadOnlySpan<char> s, IEnumerable<string> stringsToFind, bool useOrComparison = true, StringComparison stringComparison = StringComparison.Ordinal)
	{
		if (s.IsEmpty)
		{
			return false;
		}

		if (useOrComparison)
		{
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
			foreach (ReadOnlySpan<char> textToFind in stringsToFind)
			{
				if (s.Contains(textToFind, stringComparison))
				{
					return true;
				}
			}
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
			return false;
		}
		else
		{
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
			foreach (ReadOnlySpan<char> textToFind in stringsToFind)
			{
				if (!s.Contains(textToFind, stringComparison))
				{
					return false;
				}
			}
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
			return true;
		}
	}

	/// <summary>
	/// Replace a substring with another string, ignoring the case and culture when finding the substring to replace
	/// </summary>
	/// <param name="s">String to search for substring to replace</param>
	/// <param name="oldValue">Substring to search for in string s, ignoring culture and case</param>
	/// <param name="newValue">String to replace any substrings matching oldValue with</param>
	/// <returns></returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static string? ReplaceInvariant(this string? s, string oldValue, string newValue, bool replaceAllInstances = true, CancellationToken cancellationToken = default)
	{
		return s.ReplaceInvariant([oldValue], newValue, replaceAllInstances, cancellationToken);
	}

	/// <summary>
	/// Replace multiple substrings with another string, ignoring the case and culture when finding the substrings to replace.
	/// </summary>
	/// <param name="s">String to search for substrings to replace.</param>
	/// <param name="oldValues">Collection of substrings to search for in string s, ignoring culture and case.</param>
	/// <param name="newValue">String to replace any substrings matching any value in oldValues with.</param>
	/// <returns>String with all occurrences of substrings in oldValues replaced by newValue.</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static string? ReplaceInvariant(this string? s, IEnumerable<string> oldValues, string newValue, bool replaceAllInstances = true, CancellationToken cancellationToken = default)
	{
		if (s.IsNullOrEmpty() || oldValues.All(x => x.IsNullOrEmpty()))
		{
			return s;
		}

		// Use StringBuilder to avoid creating multiple string copies
		StringBuilder stringBuilder = new(s);

		foreach (string oldValue in oldValues.Where(x => !x.IsNullOrEmpty()))
		{
			cancellationToken.ThrowIfCancellationRequested();
			int index = stringBuilder.ToString().IndexOf(oldValue, StringComparison.InvariantCultureIgnoreCase);
			while (index != -1)
			{
				// Replace the oldValue with newValue
				stringBuilder.Remove(index, oldValue.Length);
				stringBuilder.Insert(index, newValue);

				// Continue searching for the next occurrence
				string currentString = stringBuilder.ToString();
				if (currentString.IsNullOrEmpty())
				{
					return currentString; //No string left so stop processing
				}
				if (!replaceAllInstances)
				{
					break;
				}

				index = currentString.IndexOf(oldValue, index + newValue.Length, StringComparison.InvariantCultureIgnoreCase);
			}
		}

		return stringBuilder.ToString();
	}

	/// <summary>
	/// Compare two strings ignoring culture and case
	/// </summary>
	/// <param name="s1">First string to compare</param>
	/// <param name="s2">Second string to compare</param>
	/// <returns><see langword="true"/> if the strings are equal when ignoring culture and case</returns>
	public static bool StrEq(this string? s1, string? s2)
	{
		return string.Equals(s1?.Trim() ?? string.Empty, s2?.Trim() ?? string.Empty, StringComparison.InvariantCultureIgnoreCase);
	}

	/// <summary>
	/// Compare two strings for string equality
	/// </summary>
	/// <param name="s1">First string to compare</param>
	/// <param name="s2">Second string to compare</param>
	/// <returns><see langword="true"/> if the strings are equal</returns>
	public static bool StrComp(this string? s1, string? s2)
	{
		return string.Equals(s1 ?? string.Empty, s2 ?? string.Empty);
	}

	/// <summary>
	/// Compare two strings with optional stringComparison parameter
	/// </summary>
	/// <param name="s1">First string to compare</param>
	/// <param name="s2">Second string to compare</param>
	/// <returns><see langword="true"/> if the strings are equal based on the stringComparison value</returns>
	public static bool StrComp(this string? s1, string? s2, StringComparison stringComparison)
	{
		return string.Equals(s1 ?? string.Empty, s2 ?? string.Empty, stringComparison);
	}

	/// <summary>
	/// Check string to see if a string only contains letters and numbers (a-Z A-Z 0-9). Null returns false.
	/// </summary>
	/// <param name="testString">String to check if it only contains alphanumeric characters</param>
	/// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
	/// <returns><see langword="true"/> if testString contains only letters and numbers and optionally spaces</returns>
	public static bool IsAlphanumeric(this string? testString, bool allowSpaces = false)
	{
		return testString != null && (!allowSpaces ? AlphanumericRegex().IsMatch(testString) : AlphanumericWithSpacesRegex().IsMatch(testString));
	}

	/// <summary>
	/// Check string to see if a string only contains letters and numbers (a-Z A-Z 0-9). Null returns false.
	/// </summary>
	/// <param name="testString">String to check if it only contains alphanumeric characters</param>
	/// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
	/// <returns><see langword="true"/> if testString contains only letters and numbers and optionally spaces</returns>
	public static bool IsAlphanumeric(this ReadOnlySpan<char> testString, bool allowSpaces = false)
	{
		return !testString.IsEmpty && (!allowSpaces ? AlphanumericRegex().IsMatch(testString) : AlphanumericWithSpacesRegex().IsMatch(testString));
	}

	/// <summary>
	/// Check string to see if a string only contains letters (a-z A-Z). Null returns false.
	/// </summary>
	/// <param name="testString">String to check if it only contains alphabetical characters</param>
	/// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
	/// <returns><see langword="true"/> if testString only contains letters and optionally spaces</returns>
	public static bool IsAlphaOnly(this string? testString, bool allowSpaces = false)
	{
		return testString != null && (!allowSpaces ? AlphaOnlyRegex().IsMatch(testString) : AlphaOnlyWithSpacesRegex().IsMatch(testString));
	}

	/// <summary>
	/// Check string to see if a string only contains letters (a-z A-Z). Null returns false.
	/// </summary>
	/// <param name="testString">String to check if it only contains alphabetical characters</param>
	/// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
	/// <returns><see langword="true"/> if testString only contains letters and optionally spaces</returns>
	public static bool IsAlphaOnly(this ReadOnlySpan<char> testString, bool allowSpaces = false)
	{
		return !testString.IsEmpty && (!allowSpaces ? AlphaOnlyRegex().IsMatch(testString) : AlphaOnlyWithSpacesRegex().IsMatch(testString));
	}

	/// <summary>
	/// Check string to see if a string only contains numbers (0-9). Null returns false.
	/// </summary>
	/// <param name="testString">String to check if it only contains numeric characters</param>
	/// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
	/// <returns><see langword="true"/> if testString only contains numbers and optionally spaces</returns>
	public static bool IsNumericOnly(this string? testString, bool allowSpaces = false)
	{
		return !testString.IsNullOrWhiteSpace() && (!allowSpaces ? NumericOnlyRegex().IsMatch(testString) : NumericOnlyWithSpacesRegex().IsMatch(testString));
	}

	/// <summary>
	/// Check string to see if a string only contains numbers (0-9). Null returns false.
	/// </summary>
	/// <param name="testString">String to check if it only contains numeric characters</param>
	/// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
	/// <returns><see langword="true"/> if testString only contains numbers and optionally spaces</returns>
	public static bool IsNumericOnly(this ReadOnlySpan<char> testString, bool allowSpaces = false)
	{
		return !testString.IsEmpty && (!allowSpaces ? NumericOnlyRegex().IsMatch(testString) : NumericOnlyWithSpacesRegex().IsMatch(testString));
	}

	/// <summary>
	/// Gets string up until before the last instance of a character (exclusive)
	/// </summary>
	/// <param name="s">String to extract from</param>
	/// <param name="charToFind">Character to find last instance of</param>
	/// <returns>String up until the last instance of charToFind (exclusive)</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static string? ExtractToLastInstance(this string? s, char charToFind)
	{
		if (s == null)
		{
			return null;
		}
		int lastIndex = s.LastIndexOf(charToFind);
		return lastIndex != -1 ? s[..lastIndex] : s;
	}

	/// <summary>
	/// Gets string up until before the last instance of a character (exclusive)
	/// </summary>
	/// <param name="s">String to extract from</param>
	/// <param name="charToFind">Character to find last instance of</param>
	/// <returns>String up until the last instance of charToFind (exclusive)</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static ReadOnlySpan<char> ExtractToLastInstance(this ReadOnlySpan<char> s, char charToFind)
	{
		if (s.IsEmpty)
		{
			return ReadOnlySpan<char>.Empty;
		}
		int lastIndex = s.LastIndexOf(charToFind);
		return lastIndex != -1 ? s[..lastIndex] : s;
	}

	/// <summary>
	/// Gets string remaining after the last instance of a character (exclusive)
	/// </summary>
	/// <param name="s">String to extract from</param>
	/// <param name="charToFind">Character to find last instance of</param>
	/// <returns>Remaining string after the last instance of charToFind (exclusive)</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static string? ExtractFromLastInstance(this string? s, char charToFind)
	{
		if (s == null)
		{
			return null;
		}
		int lastIndex = s.LastIndexOf(charToFind);
		return lastIndex != -1 ? s[(lastIndex + 1)..] : s;
	}

	/// <summary>
	/// Gets string remaining after the last instance of a character (exclusive)
	/// </summary>
	/// <param name="s">String to extract from</param>
	/// <param name="charToFind">Character to find last instance of</param>
	/// <returns>Remaining string after the last instance of charToFind (exclusive)</returns>
	[return: NotNullIfNotNull(nameof(s))]
	public static ReadOnlySpan<char> ExtractFromLastInstance(this ReadOnlySpan<char> s, char charToFind)
	{
		if (s.IsEmpty)
		{
			return ReadOnlySpan<char>.Empty;
		}
		int lastIndex = s.LastIndexOf(charToFind);
		return lastIndex != -1 ? s[(lastIndex + 1)..] : s;
	}

	///// <summary>
	///// Removes excess spaces in string properties inside of an object
	///// </summary>
	///// <typeparam name="T">Type of object to trim strings in</typeparam>
	///// <param name="obj">Object containing string properties to be trimmed</param>
	//[return: NotNullIfNotNull(nameof(obj))]
	//[Obsolete("Please use TrimObjectStrings instead")]
	//public static T? TrimObjectStringsR<T>(this T? obj) where T : class
	//{
	//	if (obj != null)
	//	{
	//		IEnumerable<PropertyInfo> props = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.PropertyType == typeof(string));
	//		if (props.Any())
	//		{
	//			foreach (PropertyInfo prop in props)
	//			{
	//				string? value = (string?)prop.GetValue(obj);
	//				if (!value.IsNullOrEmpty())
	//				{
	//					prop.SetValue(obj, value.TrimFull());
	//				}
	//			}
	//		}
	//	}
	//	return obj;
	//}

	private static readonly ConcurrentDictionary<(Type, bool), Delegate> trimObjectStringsCache = new();

	/// <summary>
	/// Removes excess spaces in string properties inside of an object
	/// </summary>
	/// <typeparam name="T">Type of object to trim strings in</typeparam>
	/// <param name="obj">Object containing string properties to be trimmed</param>
	/// <param name="recursive">If <see langword="true"/>, will recursively apply string trimming to nested object</param>
	[return: NotNullIfNotNull(nameof(obj))]
	public static T? TrimObjectStrings<T>(this T? obj, bool recursive = false) where T : class
	{
		if (obj == null)
		{
			return obj;
		}

		Type type = typeof(T);
		(Type type, bool recursive) key = (type, recursive);

		Action<T> action = (Action<T>)trimObjectStringsCache.GetOrAdd(key, _ => CreateTrimObjectStringsExpression<T>(recursive).CompileFast());
		action(obj);

		return obj;
	}

	private static Expression<Action<T>> CreateTrimObjectStringsExpression<T>(bool recursive)
	{
		ParameterExpression objParam = Expression.Parameter(typeof(T), "obj");
		List<Expression> expressions = [];
		List<ParameterExpression> variables = [];

		//foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.PropertyType == typeof(string) || (recursive && x.PropertyType.IsClass)))
		foreach (PropertyInfo prop in GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.PropertyType == typeof(string) || (recursive && x.PropertyType.IsClass)))
		{
			if (prop.PropertyType == typeof(string))
			{
				MemberExpression propExpr = Expression.Property(objParam, prop);

				MethodInfo makeTrimFull = typeof(Strings).GetMethod(nameof(TrimFull))!;
				MethodCallExpression callTrimFull = Expression.Call(makeTrimFull, propExpr);
				expressions.Add(Expression.Assign(propExpr, callTrimFull));
			}
			else if (recursive && prop.PropertyType.IsClass)
			{
				MemberExpression propExpr = Expression.Property(objParam, prop);
				MethodInfo makeTrimObjectMethod = typeof(Strings).GetMethod(nameof(TrimObjectStrings))!;
				MethodInfo genericMethod = makeTrimObjectMethod.MakeGenericMethod(prop.PropertyType);
				MethodCallExpression callMakeTrimObject = Expression.Call(genericMethod, propExpr, Expression.Constant(true));
				expressions.Add(callMakeTrimObject);
			}
		}

		BlockExpression body = Expression.Block(variables, expressions);
		return Expression.Lambda<Action<T>>(body, objParam);
	}

	public static T? NormalizeObjectStringsR<T>(this T? obj, bool enableTrim = true, NormalizationForm normalizationForm = NormalizationForm.FormKD) where T : class
	{
		if (obj != null)
		{
			IEnumerable<PropertyInfo> props = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.PropertyType == typeof(string));
			if (props.Any())
			{
				foreach (PropertyInfo prop in props)
				{
					string? value = (string?)prop.GetValue(obj);
					if (!value.IsNullOrEmpty())
					{
						if (enableTrim)
						{
							prop.SetValue(obj, value.TrimFull().Normalize(normalizationForm));
						}
						else
						{
							prop.SetValue(obj, value.Normalize(normalizationForm));
						}
					}
				}
			}
		}
		return obj;
	}

	#region Caching

	private static readonly CacheManager<(Type, bool, NormalizationForm, bool), Delegate> NormalizeObjectStringsCache = new();

	public static ICacheManagerApi<(Type, bool, NormalizationForm, bool), Delegate> CacheManager => NormalizeObjectStringsCache;

	/// <summary>
	/// Gets or adds a function from the deep copy cache based on the source and destination types.
	/// </summary>
	/// <param name="key">Cache key</param>
	/// <returns>Function for executing deep copy</returns>
	private static Delegate GetOrAddNormalizeObjectStringsCache<T>((Type, bool, NormalizationForm, bool) key, bool enableTrim = true, NormalizationForm normalizationForm = NormalizationForm.FormKD, bool recursive = false)
	{
		bool isLimitedCache = CacheManager.IsUsingLimitedCache();
		if (isLimitedCache ? CacheManager.GetLimitedCache().TryGetValue(key, out Delegate? function) :
						CacheManager.GetCache().TryGetValue(key, out function))
		{
			return function!;
		}

		function = CreateNormalizeObjectStringsExpression<T>(enableTrim, normalizationForm, recursive, true).CompileFast();
		if (isLimitedCache)
		{
			CacheManager.TryAddLimitedCache(key, function);
		}
		else
		{
			CacheManager.TryAddCache(key, function);
		}
		return function;
	}

	#endregion

	/// <summary>
	/// Removes excess spaces in string properties inside of an object with the option to also trim them
	/// </summary>
	/// <typeparam name="T">Type of object to normalize strings in</typeparam>
	/// <param name="obj">Object containing string properties to be normalized</param>
	/// <param name="enableTrim">If <see langword="true"/>, will trim all object strings</param>
	/// <param name="normalizationForm">String normalization setting</param>
	/// <param name="recursive">If <see langword="true"/>, will recursively apply string normalization to nested object</param>
	[return: NotNullIfNotNull(nameof(obj))]
	public static T? NormalizeObjectStrings<T>(this T? obj, bool enableTrim = true, NormalizationForm normalizationForm = NormalizationForm.FormKD, bool recursive = false, bool useCache = true) where T : class
	{
		if (obj == null)
		{
			return obj;
		}

		Type type = typeof(T);
		(Type type, bool enableTrim, NormalizationForm normalizationForm, bool recursive) key = (type, enableTrim, normalizationForm, recursive);

		Action<T> action = useCache ? (Action<T>)GetOrAddNormalizeObjectStringsCache<T>(key, enableTrim, normalizationForm, recursive) :
						CreateNormalizeObjectStringsExpression<T>(enableTrim, normalizationForm, recursive, useCache).CompileFast();
		action(obj);

		return obj;
	}

	/// <summary>
	/// Creates an <see cref="Expression{TDelegate}"/> for normalizing <see cref="string"/> properties in an object.
	/// </summary>
	/// <typeparam name="T">Type of the object.</typeparam>
	/// <param name="enableTrim">If <see langword="true"/>, will trim all object strings.</param>
	/// <param name="normalizationForm">String normalization setting</param>
	/// <param name="recursive">If <see langword="true"/>, will recursively apply string normalization to nested object.</param>
	/// <param name="useCache">If <see langword="true"/>, will use cache for the expression.</param>
	/// <returns><see cref="Expression{TDelegate}"/> for normalizing <see cref="string"/> properties.</returns>
	private static Expression<Action<T>> CreateNormalizeObjectStringsExpression<T>(bool enableTrim, NormalizationForm normalizationForm, bool recursive, bool useCache)
	{
		ParameterExpression objParam = Expression.Parameter(typeof(T), "obj");
		List<Expression> expressions = [];
		List<ParameterExpression> variables = [];

		foreach (PropertyInfo prop in GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.PropertyType == typeof(string) || (recursive && x.PropertyType.IsClass)))
		{
			if (prop.PropertyType == typeof(string) && prop.CanWrite)
			{
				MemberExpression propExpr = Expression.Property(objParam, prop);

				// Create a local variable to store the property value
				ParameterExpression localVar = Expression.Variable(typeof(string), prop.Name);
				variables.Add(localVar);
				expressions.Add(Expression.Assign(localVar, propExpr));

				// Create the null check
				Expression nullCheck = Expression.NotEqual(localVar, Expression.Constant(null, typeof(string)));

				Expression stringOperations;
				if (enableTrim)
				{
					MethodInfo trimMethod = typeof(Strings).GetMethod(nameof(TrimFull))!;
					MethodCallExpression callTrimMethod = Expression.Call(trimMethod, localVar);
					MethodInfo makeNormalizeMethod = typeof(string).GetMethod(nameof(string.Normalize), [typeof(NormalizationForm)])!;
					stringOperations = Expression.Call(callTrimMethod, makeNormalizeMethod, Expression.Constant(normalizationForm));
				}
				else
				{
					MethodInfo makeNormalizeMethod = typeof(string).GetMethod(nameof(string.Normalize), [typeof(NormalizationForm)])!;
					stringOperations = Expression.Call(localVar, makeNormalizeMethod, Expression.Constant(normalizationForm));
				}

				// Combine the null check with the string operations
				Expression conditionalOperation = Expression.Condition(nullCheck, stringOperations, localVar);

				// Assign the result back to the property
				expressions.Add(Expression.Assign(propExpr, conditionalOperation));
			}
			else if (recursive && prop.PropertyType.IsClass)
			{
				MemberExpression propExpr = Expression.Property(objParam, prop);
				MethodInfo normalizeStringsMethod = typeof(Strings).GetMethod(nameof(NormalizeObjectStrings))!;
				MethodInfo genericMethod = normalizeStringsMethod.MakeGenericMethod(prop.PropertyType);
				MethodCallExpression callMakeObjectNullNull = Expression.Call(genericMethod, propExpr, Expression.Constant(enableTrim), Expression.Constant(normalizationForm), Expression.Constant(true), Expression.Constant(useCache));

				// Add null check for recursive call
				Expression nullCheck = Expression.NotEqual(propExpr, Expression.Constant(null));
				Expression conditionalCall = Expression.IfThen(nullCheck, callMakeObjectNullNull);
				expressions.Add(conditionalCall);
			}
		}

		BlockExpression body = Expression.Block(variables, expressions);
		return Expression.Lambda<Action<T>>(body, objParam);
	}

	/// <summary>
	/// Makes string properties in an object with the word "null" into a null value
	/// </summary>
	/// <param name="obj">Object containing string properties to be set to null if null</param>
	/// <returns>Objects with properties set to null if the string property is null or is the word "null" with no other text characters other than whitespace</returns>
	[return: NotNullIfNotNull(nameof(obj))]
	public static T? MakeObjectNullNullR<T>(this T? obj) where T : class
	{
		if (obj != null)
		{
			IEnumerable<PropertyInfo> props = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.PropertyType == typeof(string));
			if (props.Any())
			{
				foreach (PropertyInfo prop in props)
				{
					string? value = (string?)prop.GetValue(obj);
					prop.SetValue(obj, value.MakeNullNull());
				}
			}
		}
		return obj;
	}

	private static readonly ConcurrentDictionary<(Type, bool), Delegate> makeObjectNullNullCache = new();

	/// <summary>
	/// Makes string properties in an object with the word "null" into a null value
	/// </summary>
	/// <param name="obj">Object containing string properties to be set to null if null</param>
	/// <param name="recursive">If <see langword="true"/>, will recursively apply nullification to nested objects</param>
	/// <returns>Objects with properties set to null if the string property is null or is the word "null" with no other text characters other than whitespace</returns>
	[return: NotNullIfNotNull(nameof(obj))]
	public static T? MakeObjectNullNull<T>(this T? obj, bool recursive = false) where T : class
	{
		if (obj == null)
		{
			return obj;
		}

		Type type = typeof(T);
		(Type type, bool recursive) key = (type, recursive);

		Action<T> action = (Action<T>)makeObjectNullNullCache.GetOrAdd(key, _ => CreateMakeObjectNullNullExpression<T>(recursive).CompileFast());
		action(obj);

		return obj;
	}

	private static Expression<Action<T>> CreateMakeObjectNullNullExpression<T>(bool recursive)
	{
		ParameterExpression objParam = Expression.Parameter(typeof(T), "obj");
		List<Expression> expressions = [];

		foreach (PropertyInfo prop in GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.PropertyType == typeof(string) || (recursive && x.PropertyType.IsClass)))
		{
			if (prop.PropertyType == typeof(string))
			{
				MemberExpression propExpr = Expression.Property(objParam, prop);
				MethodInfo makeNullNullMethod = typeof(Strings).GetMethod(nameof(MakeNullNull))!;
				MethodCallExpression callMakeNullNull = Expression.Call(makeNullNullMethod, propExpr);
				expressions.Add(Expression.Assign(propExpr, callMakeNullNull));
			}
			else //if (recursive && prop.PropertyType.IsClass) //Can use else here since property filter means only valid properties that are not strings will make it here
			{
				MemberExpression propExpr = Expression.Property(objParam, prop);
				MethodInfo makeObjectNullNullMethod = typeof(Strings).GetMethod(nameof(MakeObjectNullNull))!;
				MethodInfo genericMethod = makeObjectNullNullMethod.MakeGenericMethod(prop.PropertyType);
				MethodCallExpression callMakeObjectNullNull = Expression.Call(genericMethod, propExpr, Expression.Constant(true));
				expressions.Add(callMakeObjectNullNull);
			}
		}

		BlockExpression body = Expression.Block(expressions);
		return Expression.Lambda<Action<T>>(body, objParam);
	}

	/// <summary>
	/// Converts Nullable DateTime to string using the passed in formatting
	/// </summary>
	/// <param name="value">DateTime to convert to string</param>
	/// <param name="format">Date time format</param>
	/// <returns>Formatted string representation of the passed in nullable DateTime</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNString(this DateTime? value, string? format = null)
	{
		string? output = null;
		if (value != null)
		{
			DateTime dtActual = (DateTime)value;
			output = dtActual.ToString(format);
		}
		return output;
	}

	/// <summary>
	/// Converts Nullable DateTime to string using the passed in formatting
	/// </summary>
	/// <param name="value">DateOnly to convert to string</param>
	/// <param name="format">Date format</param>
	/// <returns>Formatted string representation of the passed in nullable DateOnly</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNString(this DateOnly? value, string? format = null)
	{
		string? output = null;
		if (value != null)
		{
			DateOnly dtActual = (DateOnly)value;
			output = dtActual.ToString(format);
		}
		return output;
	}

	/// <summary>
	/// Converts Nullable DateTime to string using the passed in formatting
	/// </summary>
	/// <param name="value">Timespan to convert to string</param>
	/// <param name="format">Timespan format</param>
	/// <returns>Formatted string representation of the passed in nullable Timespan</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNString(this TimeSpan? value, string? format = null)
	{
		string? output = null;
		if (value != null)
		{
			TimeSpan tsActual = (TimeSpan)value;
			output = tsActual.ToString(format);
		}
		return output;
	}

	/// <summary>
	/// Converts nullable int to string
	/// </summary>
	/// <param name="value">Integer to convert to string</param>
	/// <returns>String representation of the passed in nullable int</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNString(this int? value)
	{
		return value?.ToString();
	}

	/// <summary>
	/// Converts nullable long to string
	/// </summary>
	/// <param name="value">Long to convert to string</param>
	/// <returns>String representation of the passed in nullable long</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNString(this long? value)
	{
		return value?.ToString();
	}

	/// <summary>
	/// Converts nullable double to string
	/// </summary>
	/// <param name="value">Double to convert to string</param>
	/// <returns>String representation of the passed in nullable double</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNString(this double? value)
	{
		return value?.ToString();
	}

	/// <summary>
	/// Converts nullable decimal to string
	/// </summary>
	/// <param name="value">Decimal to convert to string</param>
	/// <returns>String representation of the passed in nullable decimal</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNString(this decimal? value)
	{
		return value?.ToString();
	}

	/// <summary>
	/// Converts nullable object to string
	/// </summary>
	/// <param name="value">Boolean to turn into a string</param>
	/// <returns>String representation of the passed in nullable object</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNString(this bool? value)
	{
		return value?.ToString();
	}

	/// <summary>
	/// Converts nullable object to string
	/// </summary>
	/// <param name="value">Object to turn into a string</param>
	/// <returns>String representation of the passed in nullable object</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNString(this object? value)
	{
		return value?.ToString();
	}

	/// <summary>
	/// Converts list of string representations of integers into list of integers
	/// </summary>
	/// <param name="values">Collection of strings to be converted to integers</param>
	/// <returns><see cref="List{T}"/> of integers where the strings could be parsed to integers and not null</returns>
	public static IEnumerable<int> ToListInt(this IEnumerable<string> values)
	{
		return values.Select(x => int.TryParse(x, out int i) ? i : (int?)null).Where(i => i.HasValue).Select(i => i!.Value);
	}

	/// <summary>
	/// Converts list of string representations of integers into list of integers
	/// </summary>
	/// <param name="values">Collection of strings to be converted to integers</param>
	/// <returns><see cref="List{T}"/> of integers where the strings could be parsed to integers and not null</returns>
	public static List<int> ToListInt(this IList<string> values)
	{
		return values.Select(x => int.TryParse(x, out int i) ? i : (int?)null).Where(i => i.HasValue).Select(i => i!.Value).ToList();
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable integers
	/// </summary>
	/// <param name="value">String value to be converted to nullable int</param>
	/// <returns>Nullable int parsed from a string</returns>
	public static int? ToNInt(this string? value)
	{
		return !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out int i) ? i : null;
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable integers
	/// </summary>
	/// <param name="value">String value to be converted to nullable int</param>
	/// <returns>Nullable int parsed from a string</returns>
	public static int? ToNInt(this ReadOnlySpan<char> value)
	{
		return !value.IsEmpty && int.TryParse(value, out int i) ? i : null;
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable doubles
	/// </summary>
	/// <param name="value">String value to be converted to nullable double</param>
	/// <returns>Nullable double parsed from a string</returns>
	public static double? ToNDouble(this string? value)
	{
		return !string.IsNullOrWhiteSpace(value) && double.TryParse(value, out double i) ? i : null;
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable doubles
	/// </summary>
	/// <param name="value">String value to be converted to nullable double</param>
	/// <returns>Nullable double parsed from a string</returns>
	public static double? ToNDouble(this ReadOnlySpan<char> value)
	{
		return !value.IsEmpty && double.TryParse(value, out double i) ? i : null;
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable decimals
	/// </summary>
	/// <param name="value">String value to be converted to nullable decimal</param>
	/// <returns>Nullable decimal parsed from a string</returns>
	public static decimal? ToNDecimal(this string? value)
	{
		return !string.IsNullOrWhiteSpace(value) && decimal.TryParse(value, out decimal i) ? i : null;
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable decimals
	/// </summary>
	/// <param name="value">String value to be converted to nullable decimal</param>
	/// <returns>Nullable decimal parsed from a string</returns>
	public static decimal? ToNDecimal(this ReadOnlySpan<char> value)
	{
		return !value.IsEmpty && decimal.TryParse(value, out decimal i) ? i : null;
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable DateTimes
	/// </summary>
	/// <param name="value">String to parse into a DateTime</param>
	/// <returns>Nullable DateTime parsed from a string</returns>
	public static DateTime? ToNDateTime(this string? value, IFormatProvider? formatProvider = null)
	{
		DateTime? dtn = null;
		if (DateTime.TryParse(value, formatProvider ?? CultureInfo.InvariantCulture, out DateTime dt))
		{
			dtn = dt;
		}
		else if (double.TryParse(value, out double dbl))
		{
			dtn = DateTime.FromOADate(dbl);
		}
		return dtn;
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable DateTimes
	/// </summary>
	/// <param name="value">String to parse into a DateTime</param>
	/// <returns>Nullable DateTime parsed from a string</returns>
	public static DateTime? ToNDateTime(this ReadOnlySpan<char> value, IFormatProvider? formatProvider = null)
	{
		DateTime? dtn = null;
		if (DateTime.TryParse(value, formatProvider ?? CultureInfo.InvariantCulture, out DateTime dt))
		{
			dtn = dt;
		}
		else if (double.TryParse(value, out double dbl))
		{
			dtn = DateTime.FromOADate(dbl);
		}
		return dtn;
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable DateOnlys
	/// </summary>
	/// <param name="value">String to parse into a DateOnly</param>
	/// <returns>Nullable DateOnly parsed from a string</returns>
	public static DateOnly? ToNDateOnly(this string? value, IFormatProvider? formatProvider = null)
	{
		DateOnly? dtn = null;
		if (DateOnly.TryParse(value, formatProvider ?? CultureInfo.InvariantCulture, out DateOnly dt))
		{
			dtn = dt;
		}
		else if (double.TryParse(value, out double dbl))
		{
			dtn = DateOnly.FromDateTime(DateTime.FromOADate(dbl));
		}
		return dtn;
	}

	/// <summary>
	/// Used to reduce boilerplate code for parsing strings into nullable DateOnlys
	/// </summary>
	/// <param name="value">String to parse into a DateOnly</param>
	/// <returns>Nullable DateOnly parsed from a string</returns>
	public static DateOnly? ToNDateOnly(this ReadOnlySpan<char> value, IFormatProvider? formatProvider = null)
	{
		DateOnly? dtn = null;
		if (DateOnly.TryParse(value, formatProvider ?? CultureInfo.InvariantCulture, out DateOnly dt))
		{
			dtn = dt;
		}
		else if (double.TryParse(value, out double dbl))
		{
			dtn = DateOnly.FromDateTime(DateTime.FromOADate(dbl));
		}
		return dtn;
	}

	/// <summary>
	/// Convert string "Yes"/"No" value into bool
	/// </summary>
	/// <param name="value">"Yes"/"No" string to convert into a boolean</param>
	/// <returns>Bool representation of string value passed in</returns>
	public static bool YesNoToBool(this string? value)
	{
		return string.Equals(value?.Trim() ?? string.Empty, nameof(EYesNo.Yes), StringComparison.InvariantCultureIgnoreCase);
	}

	/// <summary>
	/// Convert string "Y"/"N" value into bool
	/// </summary>
	/// <param name="value">"Y"/"N" string to convert into a boolean</param>
	/// <returns>Bool representation of string value passed in</returns>
	public static bool YNToBool(this string? value)
	{
		return string.Equals(value?.Trim() ?? string.Empty, "Y", StringComparison.InvariantCultureIgnoreCase);
	}

	/// <summary>
	/// Convert bool to "Yes" or "No"
	/// </summary>
	/// <param name="value">Boolean to convert to "Yes" or "No"</param>
	/// <returns>"Yes" if true, "No" if false</returns>
	public static string BoolToYesNo(this bool value)
	{
		return value ? nameof(EYesNo.Yes) : nameof(EYesNo.No);
	}

	/// <summary>
	/// Convert bool to "Y" or "N"
	/// </summary>
	/// <param name="value">Boolean to convert to "Yes" or "No"</param>
	/// <returns>"Y" if true, "N" if false</returns>
	public static string BoolToYN(this bool value)
	{
		return value ? "Y" : "N";
	}

	/// <summary>
	/// Convert bool to 1 or 0
	/// </summary>
	/// <param name="value">Integer to convert to "Yes" or "No"</param>
	/// <returns>"Yes" if true, "No" if false</returns>
	public static int BoolToInt(this bool value)
	{
		return ToInt32(value);
	}

	/// <summary>
	/// Get file name safe date in the chosen format
	/// </summary>
	/// <param name="dateFormat">Base format to get date in before doing text replacement</param>
	/// <returns>File name safe formatted date</returns>
	public static string GetSafeDate(string dateFormat)
	{
		return DateTime.Today.ToString(dateFormat).Replace("/", "-");
	}

	/// <summary>
	/// Get file name safe date in the chosen format
	/// </summary>
	/// <param name="dateFormat">Base format to get date in before doing text replacement</param>
	/// <returns>File name safe formatted date</returns>
	public static string GetSafeDate(this DateTime? date, string dateFormat)
	{
		return (date ?? DateTime.Today).ToString(dateFormat).Replace("/", "-");
	}

	/// <summary>
	/// Get file name safe date in the chosen format
	/// </summary>
	/// <param name="dateFormat">Base format to get date in before doing text replacement</param>
	/// <returns>File name safe formatted date</returns>
	public static string GetSafeDate(this DateOnly? date, string dateFormat)
	{
		return (date ?? DateOnly.FromDateTime(DateTime.Today)).ToString(dateFormat).Replace("/", "-");
	}

	/// <summary>
	/// Adds number in () at the end of a file name if it would create a duplicate in the savePath
	/// </summary>
	/// <param name="savePath">Path to get unique name for without trailing slash</param>
	/// <param name="fileName">File name to make unique (including extension)</param>
	/// <param name="extension">Optional: File extension (without '.'), otherwise will infer extension from fileName</param>
	/// <returns>Unique file name string</returns>
	public static string MakeExportNameUnique(string savePath, string fileName, string? extension = null)
	{
		extension ??= Path.GetExtension(fileName)[1..];
		int i = 0;
		string outputName = fileName;
		while (File.Exists(Path.Combine(savePath, outputName)))
		{
			outputName = $"{fileName[..(fileName.Length - extension.Length - 1)]} ({i}).{extension}";
			i++;
		}
		return outputName;
	}

	/// <summary>
	/// Remove unnecessary characters and components of a timespan to make it more readable
	/// </summary>
	/// <param name="t">Timespan to convert to shortened string</param>
	/// <returns>Shortened string representation of the timespan</returns>
	public static string TimespanToShortForm(this TimeSpan? t)
	{
		string shortForm = string.Empty;
		if (t != null)
		{
			shortForm = ((TimeSpan)t).TimespanToShortForm();
		}
		return shortForm;
	}

	/// <summary>
	/// Remove unnecessary characters and components of a timespan to make it more readable
	/// </summary>
	/// <param name="t">Timespan to convert to shortened string</param>
	/// <returns>Shortened string representation of the timespan</returns>
	public static string TimespanToShortForm(this in TimeSpan t)
	{
		string stringForm = t.ToString();

		if (t.Milliseconds > 0)
		{
			stringForm = stringForm.Replace($".{stringForm.Split(".")[^1]}", string.Empty); //Remove milliseconds component
		}

		stringForm = stringForm.Split(".")[^1];
		string days = string.Empty;

		if (t.Days > 0)
		{
			days = t.Days.ToString();
			if (days[..1].StrComp("0"))
			{
				days = days[1..];
			}
		}
		else
		{
			if (stringForm[..3].StrComp("00:"))
			{
				stringForm = stringForm[3..];  //Remove hours if there aren't any
				if (stringForm[..1].StrComp("0"))
				{
					stringForm = stringForm[1..]; //Remove leading 0 in minutes
				}
			}
		}

		return string.IsNullOrWhiteSpace(days) ? stringForm : $"{days}:{stringForm}";
	}

	/// <summary>
	/// Takes in a string and returns the hashed value of it using the passed in hashing algorithm
	/// </summary>
	/// <param name="originalString">String to be hashed</param>
	/// <param name="algorithm">Which algorithm to use for the hash operation</param>
	/// <returns>Hash string</returns>
	public static string GetHash(this string originalString, EHashAlgorithm algorithm)
	{
		ReadOnlySpan<byte> bytes = algorithm switch
		{
			EHashAlgorithm.SHA1 => SHA1.HashData(Encoding.UTF8.GetBytes(originalString)),
			EHashAlgorithm.SHA256 => SHA256.HashData(Encoding.UTF8.GetBytes(originalString)),
			EHashAlgorithm.SHA384 => SHA384.HashData(Encoding.UTF8.GetBytes(originalString)),
			EHashAlgorithm.MD5 => MD5.HashData(Encoding.UTF8.GetBytes(originalString)),
			_ => SHA512.HashData(Encoding.UTF8.GetBytes(originalString)),
		};
		StringBuilder builder = new();
		for (int i = 0; i < bytes.Length; i++)
		{
			builder.Append(bytes[i].ToString("x2"));
		}
		return builder.ToString();
	}

	/// <summary>
	/// Remove extra whitespace from a string preserving inner whitespace as a single space
	/// </summary>
	/// <param name="input">String to have whitespace normalized for</param>
	/// <returns>String without excess whitespace</returns>
	public static string NormalizeWhiteSpace(this string? input)
	{
		if (input == null)
		{
			return string.Empty;
		}

		input = input.Trim().Replace("\t", string.Empty);

		int len = input.Length;
		int index = 0;
		int i = 0;

		char[] sourceCharArray = input.ToCharArray();
		bool skip = false;
		char character;

		for (; i < len; i++)
		{
			character = sourceCharArray[i];
			switch (character)
			{
				case '\u0020':
				case '\u00A0':
				case '\u1680':
				case '\u2000':
				case '\u2001':
				case '\u2002':
				case '\u2003':
				case '\u2004':
				case '\u2005':
				case '\u2006':
				case '\u2007':
				case '\u2008':
				case '\u2009':
				case '\u200A':
				case '\u202F':
				case '\u205F':
				case '\u3000':
				case '\u2028':
				case '\u2029':
				case '\u0009':
				case '\u000A':
				case '\u000B':
				case '\u000C':
				case '\u000D':
				case '\u0085':
					if (skip)
					{
						continue;
					}

					sourceCharArray[index++] = character;
					skip = true;
					continue;

				default:
					skip = false;
					sourceCharArray[index++] = character;
					continue;
			}
		}
		return new(sourceCharArray, 0, index);
	}

	/// <summary>
	/// Take any format of a date time string and convert it to a different format
	/// </summary>
	/// <param name="dateString">Input date string to be converted</param>
	/// <param name="sourceFormat">Format of dateString string</param>
	/// <param name="outputFormat">Format to convert to. Defaults to MM/dd/yyyy</param>
	/// <returns>Date formatted as a string following the output format</returns>
	[return: NotNullIfNotNull(nameof(dateString))]
	public static string? FormatDateString(this string? dateString, string sourceFormat, string outputFormat = "MM/dd/yyyy")
	{
		return dateString == null ? null : DateTime.ParseExact(dateString, sourceFormat, CultureInfo.InvariantCulture).ToString(string.IsNullOrWhiteSpace(outputFormat) ? "MM/dd/yyyy" : outputFormat);
	}

	/// <summary>
	/// Take any format of a date time string and convert it to a different format
	/// </summary>
	/// <param name="dateString">Input date string to be converted</param>
	/// <param name="sourceFormat">Format of dateString string</param>
	/// <param name="outputFormat">Format to convert to. Defaults to MM/dd/yyyy</param>
	/// <returns>Date formatted as a string following the output format</returns>
	[return: NotNullIfNotNull(nameof(dateString))]
	public static ReadOnlySpan<char> FormatDateString(this ReadOnlySpan<char> dateString, string sourceFormat, string outputFormat = "MM/dd/yyyy")
	{
		return dateString.IsEmpty ? ReadOnlySpan<char>.Empty : DateTime.ParseExact(dateString, sourceFormat, CultureInfo.InvariantCulture).ToString(string.IsNullOrWhiteSpace(outputFormat) ? "MM/dd/yyyy" : outputFormat);
	}

	/// <summary>
	/// Replaces any characters that don't match the provided regexPattern with specified replacement string.
	/// </summary>
	/// <param name="input">String to apply regex / replacement to</param>
	/// <param name="regexPattern">Regex pattern used to white list characters in input</param>
	/// <param name="replacement">String to replace any characters that aren't matched by the regex pattern</param>
	/// <param name="matchFirstOnly">If <see langword="true"/>, will only white list the first match of the regex pattern. If false, all matches with the regex pattern are white listed</param>
	/// <returns>String with any non-matching characters replaced by the replacement string</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static string? ReplaceInverse(this string? input, string regexPattern, string? replacement = "", bool matchFirstOnly = false)
	{
		if (input.IsNullOrEmpty())
		{
			return input;
		}

		Regex regex = new(regexPattern);
		return regex.ReplaceInverse(input, replacement, matchFirstOnly);
	}

	/// <summary>
	/// Replaces any characters that don't match the provided regexPattern with specified replacement string.
	/// </summary>
	/// <param name="regex">Regex used to white list characters in input</param>
	/// <param name="input">String to apply regex / replacement to</param>
	/// <param name="replacement">String to replace any characters that aren't matched by the regex pattern</param>
	/// <param name="matchFirstOnly">If <see langword="true"/>, will only white list the first match of the regex pattern. If false, all matches with the regex pattern are white listed</param>
	/// <returns>String with any non-matching characters replaced by the replacement string</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static string? ReplaceInverse(this Regex regex, string? input, string? replacement = "", bool matchFirstOnly = false)
	{
		if (input.IsNullOrEmpty())
		{
			return input;
		}

		replacement ??= string.Empty;

		// Use StringBuilder to build the result
		StringBuilder result = new();
		int lastMatchEnd = 0;

		foreach (Match match in regex.Matches(input))
		{
			// Append non-matching parts before the current match
			if (match.Index > lastMatchEnd && replacement.Length > 0)
			{
				result.Append(replacement);
			}
			// Append the matched value
			result.Append(match.Value);
			lastMatchEnd = match.Index + match.Length;

			if (matchFirstOnly)
			{
				break;
			}
		}

		// Append any remaining non-matching characters after the last match
		if (lastMatchEnd < input.Length && replacement.Length > 0)
		{
			result.Append(replacement);
		}

		return result.ToString();
	}

	/// <summary>
	/// URL Encodes a string but then replaces specific escape sequences with their decoded character. This method is mainly for logging user defined values in a safe manner.
	/// </summary>
	/// <param name="input">Input string to be URL encoded</param>
	/// <param name="replaceEscapeSequences">
	/// <para>List of key value pairs where the key is the escape sequence to replace and the value is the value to replace the escape sequence with.</para> <para>If null or empty, will use default escape
	/// sequence replacements "%20" -> " ", "%2F" -> "/", "%5C" -> @"\", "%7C" -> "|", "%28" -> "(", "%29" -> "(", and "%2A" -> "*"</para>
	/// </param>
	/// <param name="appendDefaultEscapeSequences">
	/// <para>If <see langword="true"/>, will append the default escape sequence replacements to any passed in through replaceEscapeSequences</para> <para>The default escape sequence replacements are "%20" -> " ", "%2F" ->
	/// "/", "%5C" -> @"\", "%7C" -> "|", "%28" -> "(", "%29" -> "(", and "%2A" -> "*"</para>
	/// </param>
	/// <returns>URL encoded string with the specified escape sequences replaced with their given values</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static string? UrlEncodeReadable(this string? input, List<KeyValuePair<string, string>>? replaceEscapeSequences = null, bool appendDefaultEscapeSequences = true, CancellationToken cancellationToken = default)
	{
		if (input.IsNullOrWhiteSpace())
		{
			return input;
		}

		List<KeyValuePair<string, string>> defaultEscapeSequences = [new("%20", " "), new("+", " "), new("%2F", "/"), new("%5C", @"\"), new("%7C", "|"), new("%28", "("), new("%29", "("), new("%2A", "*")];
		if (replaceEscapeSequences == null || replaceEscapeSequences.Count == 0)
		{
			replaceEscapeSequences = defaultEscapeSequences;
		}
		else if (appendDefaultEscapeSequences)
		{
			replaceEscapeSequences.AddRange(defaultEscapeSequences.Where(x => !replaceEscapeSequences.Any(y => y.Key.StrEq(x.Key))));
		}

		string output = UrlEncode(input);
		foreach (KeyValuePair<string, string> replaceEscapeSequence in replaceEscapeSequences)
		{
			output = output.ReplaceInvariant(replaceEscapeSequence.Key, replaceEscapeSequence.Value, cancellationToken: cancellationToken);
		}

		return output;
	}

	/// <summary>
	/// Formats a string as a phone number
	/// </summary>
	/// <param name="input">String to be formatted as phone number</param>
	/// <param name="separator">Character to be used to separate segments of the phone number (country code excluded)</param>
	/// <param name="addParenToAreaCode">If <see langword="true"/>, will add parentheses around the area code, eg. +1 (123)-456-7890 instead of +1 123-456-7890</param>
	/// <returns>String formatted as a phone number</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static string? FormatPhoneNumber(this string? input, string separator = "-", bool addParenToAreaCode = false)
	{
		if (input.IsNullOrWhiteSpace())
		{
			return input;
		}

		string[] phoneParts = input.ToLowerInvariant().Split("x");
		string? extension = phoneParts.Length > 1 ? phoneParts[1] : null;

		input = string.Concat(ExtractNumbersRegex().Split(phoneParts[0]));

		Regex? phoneParser;
		string format;

		switch (input.Length)
		{
			case 7:
				phoneParser = SevenDigitPhoneNumberRegex();
				format = $"$1{separator}$2";
				break;

			case 10:
				phoneParser = TenDigitPhoneNumberRegex();
				format = !addParenToAreaCode ? $"$1{separator}$2{separator}$3" : $"($1){separator}$2{separator}$3";
				break;

			case 11:
				phoneParser = ElevenDigitPhoneNumberRegex();
				format = !addParenToAreaCode ? $"+$1 $2{separator}$3{separator}$4" : $"+$1 ($2){separator}$3{separator}$4";
				break;

			case 12:
				phoneParser = TwelveDigitPhoneNumberRegex();
				format = !addParenToAreaCode ? $"+$1 $2{separator}$3{separator}$4" : $"+$1 ($2){separator}$3{separator}$4";
				break;
			default:
				if (extension != null)
				{
					input += $"x{extension}";
				}
				return input;
		}
		input = phoneParser.Replace(input, format);
		if (extension != null)
		{
			input += $"x{extension}";
		}
		return input;
	}

	/// <summary>
	/// Splits a string into lines based on line breaks, yielding each line as an individual string.
	/// </summary>
	/// <param name="input">The input string to split into lines.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>An enumerable collection of strings, each representing a line from the input.</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static IEnumerable<string> SplitLines(this string? input, CancellationToken cancellationToken = default)
	{
		if (input == null)
		{
			yield break;
		}
		string? line;
		using StringReader sr = new(input);
		while ((line = sr.ReadLine()) != null)
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return line;
		}
	}

	/// <summary>
	/// Converts a nullable numeric value to a string representation in fractional format reduced to simplest form using greatest common denominator.
	/// </summary>
	/// <param name="number">The nullable value to convert.</param>
	/// <param name="maxNumberOfDecimalsToConsider">The maximum number of decimal places to consider when calculating the fractional representation. Must be a positive integer.</param>
	/// <returns>A string representing the fractional format of the number. The format includes the whole number part followed by the fractional part (e.g. 3.25 -> "3 1/4"). Null if number is null.</returns>
	[return: NotNullIfNotNull(nameof(number))]
	public static string? ToFractionString(this decimal? number, int maxNumberOfDecimalsToConsider)
	{
		if (number == null)
		{
			return null;
		}

		int wholeNumberPart = (int)number;
		decimal decimalNumberPart = (decimal)number - ToDecimal(wholeNumberPart);
		long denominator = (long)Math.Pow(10, maxNumberOfDecimalsToConsider);
		long numerator = (long)(decimalNumberPart * denominator);
		GreatestCommonDenominator(ref numerator, ref denominator, out long _);
		return $"{wholeNumberPart} {numerator}/{denominator}";
	}

	/// <summary>
	/// Converts a numeric value to a string representation in fractional format reduced to simplest form using greatest common denominator.
	/// </summary>
	/// <param name="number">The value to convert.</param>
	/// <param name="maxNumberOfDecimalsToConsider">The maximum number of decimal places to consider when calculating the fractional representation. Must be a positive integer.</param>
	/// <returns>A string representing the fractional format of the number. The format includes the whole number part followed by the fractional part (e.g. 3.25 -> "3 1/4")</returns>
	[return: NotNullIfNotNull(nameof(number))]
	public static string? ToFractionString(this decimal number, int maxNumberOfDecimalsToConsider)
	{
		int wholeNumberPart = (int)number;
		decimal decimalNumberPart = number - ToDecimal(wholeNumberPart);
		long denominator = (long)Math.Pow(10, maxNumberOfDecimalsToConsider);
		long numerator = (long)(decimalNumberPart * denominator);
		GreatestCommonDenominator(ref numerator, ref denominator, out long _);
		return $"{wholeNumberPart} {numerator}/{denominator}";
	}

	/// <summary>
	/// Converts a nullable numeric value to a string representation in fractional format reduced to simplest form using greatest common denominator.
	/// </summary>
	/// <param name="number">The nullable value to convert.</param>
	/// <param name="maxNumberOfDecimalsToConsider">The maximum number of decimal places to consider when calculating the fractional representation. Must be a positive integer.</param>
	/// <returns>A string representing the fractional format of the number. The format includes the whole number part followed by the fractional part (e.g. 3.25 -> "3 1/4"). Null if number is null.</returns>
	[return: NotNullIfNotNull(nameof(number))]
	public static string? ToFractionString(this double? number, int maxNumberOfDecimalsToConsider)
	{
		if (number == null)
		{
			return null;
		}

		int wholeNumberPart = (int)number;
		double decimalNumberPart = (double)number - ToDouble(wholeNumberPart);
		long denominator = (long)Math.Pow(10, maxNumberOfDecimalsToConsider);
		long numerator = (long)(decimalNumberPart * denominator);
		GreatestCommonDenominator(ref numerator, ref denominator, out long _);
		return $"{wholeNumberPart} {numerator}/{denominator}";
	}

	/// <summary>
	/// Converts a numeric value to a string representation in fractional format reduced to simplest form using greatest common denominator.
	/// </summary>
	/// <param name="number">The value to convert.</param>
	/// <param name="maxNumberOfDecimalsToConsider">The maximum number of decimal places to consider when calculating the fractional representation. Must be a positive integer.</param>
	/// <returns>A string representing the fractional format of the number. The format includes the whole number part followed by the fractional part (e.g. 3.25 -> "3 1/4")</returns>
	[return: NotNullIfNotNull(nameof(number))]
	public static string? ToFractionString(this double number, int maxNumberOfDecimalsToConsider)
	{
		int wholeNumberPart = (int)number;
		double decimalNumberPart = number - ToDouble(wholeNumberPart);
		long denominator = (long)Math.Pow(10, maxNumberOfDecimalsToConsider);
		long numerator = (long)(decimalNumberPart * denominator);
		GreatestCommonDenominator(ref numerator, ref denominator, out long _);
		return $"{wholeNumberPart} {numerator}/{denominator}";
	}

	private static readonly char[] FractionSplitChars = new[] { ' ', '/' };

	/// <summary>
	/// Converts a string representation of a fraction or decimal value into a decimal value.
	/// </summary>
	/// <remarks>The method supports the following formats: <list type="bullet"> <item> <description>A decimal
	/// value, e.g. "3.14".</description> </item> <item> <description>A fraction in the format "numerator/denominator",
	/// e.g. "1/2".</description> </item> <item> <description>A mixed fraction in the format "whole
	/// numerator/denominator", e.g. "1 1/2".</description> </item> </list> If the input string is null, the method returns null. If the input string is invalid, a FormatException is thrown.
	/// </remarks>
	/// <param name="fractionString">A string containing a decimal value or a fraction in the format "numerator/denominator" or  "whole
	/// numerator/denominator". Can also include a space between the whole number and the fraction.</param>
	/// <returns>A decimal representation of the input string if it is a valid fraction or decimal, otherwise null</returns>
	/// <exception cref="FormatException">Thrown if fractionString is not a valid decimal or fraction format.</exception>
	[return: NotNullIfNotNull(nameof(fractionString))]
	public static decimal? FractionToDecimal(this string? fractionString)
	{
		if (fractionString == null)
		{
			return null;
		}

		if (decimal.TryParse(fractionString, out decimal result))
		{
			return result;
		}

		string[] split = fractionString.Split(FractionSplitChars);

		if (split.Length is 2 or 3 && int.TryParse(split[0], out int numeratorOrWhole) && int.TryParse(split[1], out int denominatorOrNumerator))
		{
			if (split.Length == 2)
			{
				return (decimal)numeratorOrWhole / denominatorOrNumerator;
			}

			if (int.TryParse(split[2], out int denominator))
			{
				return numeratorOrWhole + ((decimal)denominatorOrNumerator / denominator);
			}
		}

		throw new FormatException("Not a valid fraction.");
	}

	/// <summary>
	/// Attempts to convert a fraction represented as a string into its decimal equivalent.
	/// </summary>
	/// <param name="fractionString">The input string containing the fraction to be converted.</param>
	/// <param name="result">Contains the decimal equivalent of the fraction if the conversion was successful, otherwise null.</param>
	/// <returns><see langword="true"/> if the conversion was successful, otherwise false.</returns>
	public static bool TryFractionToDecimal(this string? fractionString, [NotNullWhen(true)] out decimal? result)
	{
		result = null;
		if (fractionString.IsNullOrWhiteSpace())
		{
			return false;
		}

		bool success = true;
		try
		{
			result = fractionString.FractionToDecimal();
		}
		catch (Exception)
		{
			success = false;
		}
		return success;
	}

	/// <summary>
	/// Attempts to convert a fraction represented as a string into its decimal equivalent.
	/// </summary>
	/// <param name="fractionString">The input string containing the fraction to be converted.</param>
	/// <param name="result">Contains the decimal equivalent of the fraction if the conversion was successful, otherwise 0</param>
	/// <returns><see langword="true"/> if the conversion was successful, otherwise false.</returns>
	public static bool TryFractionToDecimal(this string? fractionString, [NotNullWhen(true)] out decimal result)
	{
		result = default;
		if (fractionString.IsNullOrWhiteSpace())
		{
			return false;
		}

		bool success = true;
		try
		{
			result = fractionString.FractionToDecimal() ?? default;
		}
		catch (Exception)
		{
			success = false;
		}
		return success;
	}

	/// <summary>
	/// Attempts to convert the specified string to a decimal value.
	/// </summary>
	/// <param name="inputString">The input string to parse.</param>
	/// <param name="result">When this method returns true, contains the parsed decimal value. When this method returns false, contains null.</param>
	/// <returns><see langword="true"/> if the conversion was successful, otherwise false.</returns>
	public static bool TryStringToDecimal(this string? inputString, [NotNullWhen(true)] out decimal? result)
	{
		result = null;
		if (inputString.IsNullOrWhiteSpace())
		{
			return false;
		}

		bool success;
		try
		{
			//Try reading fraction value first as decimal.TryParse as decimal.TryParse will just give numerator if there is a fraction
			result = inputString.GetOnlyNumbers(true).TryFractionToDecimal(out decimal fractionValue) ? fractionValue :
				decimal.TryParse(inputString.GetOnlyNumbers(), out decimal value) ? value :
				null;

			success = result != null;
		}
		catch (Exception)
		{
			success = false;
		}
		return success;
	}

	/// <summary>
	/// Attempts to convert the specified string to a decimal value.
	/// </summary>
	/// <param name="inputString">The input string to parse.</param>
	/// <param name="result">When this method returns true, contains the parsed decimal value. When this method returns false, contains 0.</param>
	/// <returns><see langword="true"/> if the conversion was successful, otherwise false.</returns>
	public static bool TryStringToDecimal(this string? inputString, [NotNullWhen(true)] out decimal result)
	{
		result = default;
		if (inputString.IsNullOrWhiteSpace())
		{
			return false;
		}

		bool success;
		try
		{
			//Try reading fraction value first as decimal.TryParse as decimal.TryParse will just give numerator if there is a fraction
			result = inputString.GetOnlyNumbers(true).TryFractionToDecimal(out decimal fractionValue) ? fractionValue :
				decimal.TryParse(inputString.GetOnlyNumbers(), out decimal value) ? value :
				default;

			success = result != default;
		}
		catch (Exception)
		{
			success = false;
		}
		return success;
	}

	/// <summary>
	/// Converts a string representation of a fraction or decimal value into a double value.
	/// </summary>
	/// <remarks>The method supports the following formats: <list type="bullet"> <item> <description>A decimal
	/// value, e.g. "3.14".</description> </item> <item> <description>A fraction in the format "numerator/denominator",
	/// e.g. "1/2".</description> </item> <item> <description>A mixed fraction in the format "whole
	/// numerator/denominator", e.g. "1 1/2".</description> </item> </list> If the input string is null, the method returns null. If the input string is invalid, a FormatException is thrown.
	/// </remarks>
	/// <param name="fractionString">A string containing a decimal value or a fraction in the format "numerator/denominator" or  "whole
	/// numerator/denominator". Can also include a space between the whole number and the fraction.</param>
	/// <returns>A double representation of the input string if it is a valid fraction or decimal, otherwise null</returns>
	/// <exception cref="FormatException">Thrown if fractionString is not a valid decimal or fraction format.</exception>
	[return: NotNullIfNotNull(nameof(fractionString))]
	public static double? FractionToDouble(this string? fractionString)
	{
		if (fractionString == null)
		{
			return null;
		}

		if (double.TryParse(fractionString, out double result))
		{
			return result;
		}

		string[] split = fractionString.Split(FractionSplitChars);

		if ((split.Length == 2 || split.Length == 3) && int.TryParse(split[0], out int a) && int.TryParse(split[1], out int b))
		{
			if (split.Length == 2)
			{
				return (double)a / b;
			}

			if (int.TryParse(split[2], out int c))
			{
				return a + ((double)b / c);
			}
		}

		throw new FormatException("Not a valid fraction.");
	}

	/// <summary>
	/// Attempts to convert a fraction represented as a string into its double equivalent.
	/// </summary>
	/// <param name="fractionString">The input string containing the fraction to be converted.</param>
	/// <param name="result">Contains the double equivalent of the fraction if the conversion was successful, otherwise null.</param>
	/// <returns><see langword="true"/> if the conversion was successful, otherwise false.</returns>
	/// Attempts to convert a fraction represented as a string into its double equivalent.
	/// </summary>>
	public static bool TryFractionToDouble(this string? fractionString, [NotNullWhen(true)] out double? result)
	{
		result = null;
		if (fractionString.IsNullOrWhiteSpace())
		{
			return false;
		}

		bool success = true;
		try
		{
			result = fractionString.FractionToDouble();
		}
		catch (Exception)
		{
			success = false;
		}
		return success;
	}

	/// <summary>
	/// Attempts to convert a fraction represented as a string into its double equivalent.
	/// </summary>
	/// <param name="fractionString">The input string containing the fraction to be converted.</param>
	/// <param name="result">Contains the double equivalent of the fraction if the conversion was successful, otherwise 0</param>
	/// <returns><see langword="true"/> if the conversion was successful, otherwise false.</returns>
	public static bool TryFractionToDouble(this string? fractionString, [NotNullWhen(true)] out double result)
	{
		result = default;
		if (fractionString.IsNullOrWhiteSpace())
		{
			return false;
		}

		bool success = true;
		try
		{
			result = fractionString.FractionToDouble() ?? default;
		}
		catch (Exception)
		{
			success = false;
		}
		return success;
	}

	/// <summary>
	/// Attempts to convert the specified string to a double value.
	/// </summary>
	/// <param name="inputString">The input string to parse.</param>
	/// <param name="result">When this method returns true, contains the parsed double value. When this method returns false, contains null.</param>
	/// <returns><see langword="true"/> if the conversion was successful, otherwise false.</returns>
	public static bool TryStringToDouble(this string? inputString, [NotNullWhen(true)] out double? result)
	{
		result = null;
		if (inputString.IsNullOrWhiteSpace())
		{
			return false;
		}

		bool success;
		try
		{
			//Try reading fraction value first as double.TryParse as double.TryParse will just give numerator if there is a fraction
			result = inputString.GetOnlyNumbers(true).TryFractionToDouble(out double fractionValue) ? fractionValue :
				double.TryParse(inputString.GetOnlyNumbers(), out double value) ? value :
				null;

			success = result != null;
		}
		catch (Exception)
		{
			success = false;
		}
		return success;
	}

	/// <summary>
	/// Attempts to convert the specified string to a double value.
	/// </summary>
	/// <param name="inputString">The input string to parse.</param>
	/// <param name="result">When this method returns true, contains the parsed double value. When this method returns false, contains 0.</param>
	/// <returns><see langword="true"/> if the conversion was successful, otherwise false.</returns>
	public static bool TryStringToDouble(this string? inputString, [NotNullWhen(true)] out double result)
	{
		result = default;
		bool success;
		if (inputString.IsNullOrWhiteSpace())
		{
			return false;
		}

		try
		{
			//Try reading fraction value first as double.TryParse as double.TryParse will just give numerator if there is a fraction
			result = inputString.GetOnlyNumbers(true).TryFractionToDouble(out double fractionValue) ? fractionValue :
				double.TryParse(inputString.GetOnlyNumbers(), out double value) ? value :
				default;

			success = result.NotEquals(default, 1e-10m);
		}
		catch (Exception)
		{
			success = false;
		}
		return success;
	}

	/// <summary>
	/// Remove all letters from <paramref name="value"/>.
	/// </summary>
	/// <param name="value">String to remove letters from</param>
	/// <returns><paramref name="value"/> with all letters removed</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? RemoveLetters(this string? value)
	{
		if (value.IsNullOrWhiteSpace())
		{
			return null;
		}

		return RemoveLettersRegex().Replace(value, string.Empty);
	}

	/// <summary>
	/// Removes all numbers from a string, leaving only letters and other non-numeric characters.
	/// </summary>
	/// <param name="value">String to remove all numbers from</param>
	/// <returns>Returns a string containing only the numbers from value, or null if the input is null, empty, or whitespace</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? RemoveNumbers(this string? value)
	{
		if (value.IsNullOrWhiteSpace())
		{
			return null;
		}

		return RemoveNumbersRegex().Replace(value, string.Empty);
	}

	/// <summary>
	/// Gets only the letters and spaces from a string, removing all numbers and other non-letter characters.
	/// </summary>
	/// <param name="value">String to get only letters from</param>
	/// <returns>Returns a string containing only the letters and spaces from value, or null if the input is null, empty, or whitespace</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? GetOnlyLetters(this string? value)
	{
		if (value.IsNullOrWhiteSpace())
		{
			return null;
		}
		return string.Concat(LettersOnlyRegex().Matches(value.Trim())).Trim();
	}

	/// <summary>
	/// Gets only the numbers from a string, removing all letters and other non-numeric characters.
	/// </summary>
	/// <param name="value">String to get only numbers from</param>
	/// <param name="allowFractions">Specifies whether to allow fractions in the numeric output</param>
	/// <returns>Returns a string containing only the numbers from value, or null if the input is null, empty, or whitespace</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? GetOnlyNumbers(this string? value, bool allowFractions = false)
	{
		if (value.IsNullOrWhiteSpace())
		{
			return null;
		}

		return string.Concat(!allowFractions ? NumbersOnlyRegex().Matches(value.Trim()) : NumbersWithFractionsOnlyRegex().Matches(value.Trim())).Trim();
	}

	/// <summary>
	/// Removes all non-alphanumeric characters from the beginning of a string until the first alphanumeric character is reached.
	/// </summary>
	/// <param name="input">The input string to process.</param>
	/// <returns>The processed string with leading non-alphanumeric characters removed.</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static string? RemoveLeadingNonAlphanumeric(this string? input)
	{
		if (input.IsNullOrWhiteSpace())
		{
			return input;
		}

		ReadOnlySpan<char> span = input.AsSpan();
		int index = 0;
		while (index < span.Length && !char.IsLetterOrDigit(span[index]))
		{
			index++;
		}

		return input[index..];
	}

	/// <summary>
	/// Removes all non-alphanumeric characters from the beginning of a string until the first alphanumeric character is reached.
	/// </summary>
	/// <param name="input">The input string to process.</param>
	/// <returns>The processed string with leading non-alphanumeric characters removed.</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static ReadOnlySpan<char> RemoveLeadingNonAlphanumeric(this ReadOnlySpan<char> input)
	{
		if (input.IsEmpty)
		{
			return input;
		}

		int index = 0;
		while (index < input.Length && !char.IsLetterOrDigit(input[index]))
		{
			index++;
		}

		return input[index..];
	}

	/// <summary>
	/// Removes all non-alphanumeric characters from the beginning of a string until the first alphanumeric character is reached.
	/// </summary>
	/// <param name="input">The input string to process.</param>
	/// <returns>The processed string with leading non-alphanumeric characters removed.</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static string? RemoveTrailingNonAlphanumeric(this string? input)
	{
		if (input.IsNullOrWhiteSpace())
		{
			return input;
		}

		ReadOnlySpan<char> span = input.AsSpan();
		int index = span.Length - 1;
		while (index > 0 && !char.IsLetterOrDigit(span[index]))
		{
			index--;
		}

		return input[..(index + 1)];
	}

	/// <summary>
	/// Removes all non-alphanumeric characters from the beginning of a string until the first alphanumeric character is reached.
	/// </summary>
	/// <param name="input">The input string to process.</param>
	/// <returns>The processed string with leading non-alphanumeric characters removed.</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static ReadOnlySpan<char> RemoveTrailingNonAlphanumeric(this ReadOnlySpan<char> input)
	{
		if (input.IsEmpty)
		{
			return input;
		}

		int index = input.Length - 1;
		while (index > 0 && !char.IsLetterOrDigit(input[index]))
		{
			index--;
		}

		return input[..(index + 1)];
	}

	/// <summary>
	/// Removes all non-alphanumeric characters from the beginning and ending of a string until the first alphanumeric character is reached.
	/// </summary>
	/// <param name="input">The input string to process.</param>
	/// <returns>The processed string with leading non-alphanumeric characters removed.</returns>
	[return: NotNullIfNotNull(nameof(input))]
	public static string? TrimOuterNonAlphanumeric(this string? input)
	{
		return input.RemoveLeadingNonAlphanumeric().RemoveTrailingNonAlphanumeric();
	}

	/// <summary>
	/// Counts the occurrences of a specific character in the given string.
	/// </summary>
	/// <param name="input">The string to search within. Can be null or empty.</param>
	/// <param name="charToFind">The character to count occurrences of.</param>
	/// <returns>The number of times the specified character appears in the string. Returns 0 if the input string is null or empty.</returns>
	public static int CountChars(this ReadOnlySpan<char> input, char charToFind)
	{
		if (input.Length == 0)
		{
			return 0;
		}

		int count = 0;
		foreach (char @char in input)
		{
			if (@char == charToFind)
			{
				count++;
			}
		}
		return count;
	}

	/// <summary>
	/// Counts the occurrences of a specific character in the given string.
	/// </summary>
	/// <param name="input">The string to search within. Can be null or empty.</param>
	/// <param name="charToFind">The character to count occurrences of.</param>
	/// <returns>The number of times the specified character appears in the string. Returns 0 if the input string is null or empty.</returns>
	public static int CountChars(this string? input, char charToFind)
	{
		if (input.IsNullOrEmpty())
		{
			return 0;
		}

		return input.AsSpan().CountChars(charToFind);
	}

	/// <summary>
	/// Counts the occurrences of a specific character in the given string.
	/// </summary>
	/// <param name="input">The string to search within. Can be null or empty.</param>
	/// <param name="charToFind">The character to count occurrences of.</param>
	/// <returns>The number of times the specified character appears in the string. Returns 0 if the input string is null or empty.</returns>
	public static int CountChars(this string? input, string charToFind)
	{
		if (charToFind.Length > 1)
		{
			throw new ArgumentException($"{nameof(charToFind)} must be a single character", nameof(charToFind));
		}
		return input.CountChars(charToFind[0]);
	}

	/// <summary>
	/// Checks to see if the input string has a maximum number of occurrences of a specific character or fewer.
	/// </summary>
	/// <param name="input">The string to search within. Can be null or empty.</param>
	/// <param name="charToFind">The character to count occurrences of.</param>
	/// <param name="maxNumberOfChars">Max count of a specific character in input required to return true</param>
	/// <returns><see langword="true"/> if the maximum threshold maxNumberOfChars is not exceeded, otherwise false</returns>
	public static bool HasNoMoreThanNumberOfChars(this ReadOnlySpan<char> input, char charToFind, int maxNumberOfChars)
	{
		if (maxNumberOfChars < 0)
		{
			throw new ArgumentException("maxNumberOfChars must be a number >= 0", nameof(maxNumberOfChars));
		}

		if (input.Length == 0)
		{
			return true;
		}

		int count = 0;
		foreach (char @char in input)
		{
			if (@char == charToFind)
			{
				count++;
				if (count > maxNumberOfChars)
				{
					return false;
				}
			}
		}
		return true;
	}

	/// <summary>
	/// Checks to see if the input string has a maximum number of occurrences of a specific character or fewer.
	/// </summary>
	/// <param name="input">The string to search within. Can be null or empty.</param>
	/// <param name="charToFind">The character to count occurrences of.</param>
	/// <param name="maxNumberOfChars">Max count of a specific character in input required to return true</param>
	/// <returns><see langword="true"/> if the maximum threshold maxNumberOfChars is not exceeded, otherwise false</returns>
	public static bool HasNoMoreThanNumberOfChars(this string? input, char charToFind, int maxNumberOfChars)
	{
		if (maxNumberOfChars < 0)
		{
			throw new ArgumentException("maxNumberOfChars must be a number >= 0", nameof(maxNumberOfChars));
		}
		if (input.IsNullOrEmpty())
		{
			return true;
		}
		return input.AsSpan().HasNoMoreThanNumberOfChars(charToFind, maxNumberOfChars);
	}

	/// <summary>
	/// Checks to see if the input string has a maximum number of occurrences of a specific character or fewer.
	/// </summary>
	/// <param name="input">The string to search within. Can be null or empty.</param>
	/// <param name="charToFind">The character to count occurrences of.</param>
	/// <param name="maxNumberOfChars">Max count of a specific character in input required to return true</param>
	/// <returns><see langword="true"/> if the maximum threshold maxNumberOfChars is not exceeded, otherwise false</returns>
	public static bool HasNoMoreThanNumberOfChars(this string? input, string charToFind, int maxNumberOfChars)
	{
		if (charToFind.Length > 1)
		{
			throw new InvalidDataException("charToFind must be a single character");
		}
		if (maxNumberOfChars < 0)
		{
			throw new ArgumentException("maxNumberOfChars must be a number >= 0", nameof(maxNumberOfChars));
		}
		return input.HasNoMoreThanNumberOfChars(charToFind[0], maxNumberOfChars);
	}

	/// <summary>
	/// Checks to see if the input string has a minimum number of occurrences of a specific character or more.
	/// </summary>
	/// <param name="input">The string to search within. Can be null or empty.</param>
	/// <param name="charToFind">The character to count occurrences of.</param>
	/// <param name="minNumberOfChars">Minimum count of a specific character in input required to return true</param>
	/// <returns><see langword="true"/> if the minimum threshold minNumberOfChars is met or exceeded, otherwise false</returns>
	public static bool HasNoLessThanNumberOfChars(this ReadOnlySpan<char> input, char charToFind, int minNumberOfChars)
	{
		if (minNumberOfChars < 0)
		{
			throw new ArgumentException("minNumberOfChars must be a number >= 0", nameof(minNumberOfChars));
		}

		if (input.Length == 0)
		{
			return minNumberOfChars == 0;
		}

		if (minNumberOfChars == 0)
		{
			return true;
		}

		int count = 0;
		foreach (char @char in input)
		{
			if (@char == charToFind)
			{
				count++;
				if (count == minNumberOfChars)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Checks to see if the input string has a minimum number of occurrences of a specific character or more.
	/// </summary>
	/// <param name="input">The string to search within. Can be null or empty.</param>
	/// <param name="charToFind">The character to count occurrences of.</param>
	/// <param name="minNumberOfChars">Minimum count of a specific character in input required to return true</param>
	/// <returns><see langword="true"/> if the minimum threshold minNumberOfChars is met or exceeded, otherwise false</returns>
	public static bool HasNoLessThanNumberOfChars(this string? input, char charToFind, int minNumberOfChars)
	{
		if (minNumberOfChars < 0)
		{
			throw new ArgumentException("minNumberOfChars must be a number >= 0", nameof(minNumberOfChars));
		}

		if (input.IsNullOrEmpty())
		{
			return minNumberOfChars == 0;
		}

		return input.AsSpan().HasNoLessThanNumberOfChars(charToFind, minNumberOfChars);
	}

	/// <summary>
	/// Checks to see if the input string has a minimum number of occurrences of a specific character or more.
	/// </summary>
	/// <param name="input">The string to search within. Can be null or empty.</param>
	/// <param name="charToFind">The character to count occurrences of.</param>
	/// <param name="minNumberOfChars">Minimum count of a specific character in input required to return true</param>
	/// <returns><see langword="true"/> if the minimum threshold minNumberOfChars is met or exceeded, otherwise false</returns>
	public static bool HasNoLessThanNumberOfChars(this string? input, string charToFind, int minNumberOfChars)
	{
		if (charToFind.Length > 1)
		{
			throw new InvalidDataException("charToFind must be a single character");
		}

		if (minNumberOfChars < 0)
		{
			throw new ArgumentException("minNumberOfChars must be a number >= 0", nameof(minNumberOfChars));
		}
		return input.HasNoLessThanNumberOfChars(charToFind[0], minNumberOfChars);
	}

	public static string SanitizeForLog(this string input)
	{
		return input.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ");
	}
}
