using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Core.TypeChecks;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]

/// <summary>
/// Validates that all items in a list do NOT match the specified regular expression pattern.
/// Validation fails if any item matches the pattern.
/// </summary>
public sealed class ListDenyRegularExpressionAttribute : ValidationAttribute
{
	/// <summary>
	///	Gets or sets the timeout to use when matching the regular expression pattern (in milliseconds)
	///	(-1 means never timeout).
	/// </summary>
	public int MatchTimeoutInMilliseconds { get; set; }

	/// <summary>
	/// Gets the timeout to use when matching the regular expression pattern
	/// </summary>
	public TimeSpan MatchTimeout => TimeSpan.FromMilliseconds(MatchTimeoutInMilliseconds);

	/// <summary>
	/// Gets the regular expression pattern to use
	/// </summary>
	public string Pattern { get; }

	/// <summary>
	/// Validation mode: if true, only deny full matches of the pattern; if false, deny any match within the string
	/// </summary>
	public bool DenyOnlyFullMatch { get; set; }

	private Regex? Regex { get; set; }

	/// <summary>
	/// Constructor that accepts the regular expression pattern
	/// </summary>
	/// <param name="pattern">The regular expression pattern to deny. Items matching this pattern will fail validation.</param>
	/// <param name="denyOnlyFullMatch">If true, only deny items that fully match the pattern; if false, deny items containing any match.</param>
	public ListDenyRegularExpressionAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string pattern, bool denyOnlyFullMatch = false)
	{
		ArgumentNullException.ThrowIfNull(pattern);
		Pattern = pattern;
		DenyOnlyFullMatch = denyOnlyFullMatch;
		MatchTimeoutInMilliseconds = 2000;
		ErrorMessage = "Item at index {0} '{1}' must not match the pattern '{2}'.";
	}

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		SetupRegex();

		if (value is null)
		{
			return ValidationResult.Success;
		}

		string memberName = validationContext.MemberName ?? string.Empty;

		// Handle different types of collections
		if (value is IEnumerable<string?> or IEnumerable<string>)
		{
			ValidationResult? result = ValidateEnumerable((IEnumerable<string?>)value, memberName);
			return result ?? ValidationResult.Success;
		}
		else if (value.GetType().IsEnumerable())
		{
			ValidationResult? result = ValidateEnumerable(((IEnumerable)value).Cast<object?>().Select(x => Convert.ToString(x, CultureInfo.CurrentCulture)), memberName);
			return result ?? ValidationResult.Success;
		}
		throw new InvalidDataException($"${nameof(ListDenyRegularExpressionAttribute)} can only be used on properties that implement IEnumerable");
	}

	private ValidationResult? ValidateEnumerable(IEnumerable<string?> values, string memberName)
	{
		int index = 0;
		foreach (string? item in values)
		{
			if (!string.IsNullOrEmpty(item)) // Null / empty passes automatically
			{
				bool failsValidation = false;

				if (!DenyOnlyFullMatch)
				{
					// Check if the pattern matches anywhere in the string - if it does, validation fails
					failsValidation = Regex!.IsMatch(item);
				}
				else
				{
					// Check if the pattern is a full match of the entire string
					foreach (ValueMatch m in Regex!.EnumerateMatches(item))
					{
						// We are looking for an exact match, not just a search hit
						if (m.Index == 0 && m.Length == item.Length)
						{
							failsValidation = true;
							break;
						}
					}
				}

				if (failsValidation)
				{
					return new ValidationResult(
						string.Format(CultureInfo.CurrentCulture, ErrorMessageString, index, item.UrlEncodeReadable(), Pattern),
						[memberName]
					);
				}
			}
			index++;
		}
		return null;
	}

	/// <summary>
	///	Override of <see cref="ValidationAttribute.FormatErrorMessage" />
	/// </summary>
	/// <remarks>This override provides a formatted error message describing the pattern</remarks>
	/// <param name="name">The user-visible name to include in the formatted message.</param>
	/// <returns>The localized message to present to the user</returns>
	/// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
	/// <exception cref="ArgumentException"> is thrown if the <see cref="Pattern" /> is not a valid regular expression.</exception>
	public override string FormatErrorMessage(string name)
	{
		SetupRegex();

		return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, "{index}", "{value}", Pattern);
	}

	/// <summary>
	/// Sets up the <see cref="Regex" /> property from the <see cref="Pattern" /> property.
	/// </summary>
	/// <exception cref="ArgumentException"> is thrown if the current <see cref="Pattern" /> cannot be parsed</exception>
	/// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
	/// <exception cref="ArgumentOutOfRangeException"> thrown if <see cref="MatchTimeoutInMilliseconds" /> is negative (except -1),
	/// zero or greater than approximately 24 days </exception>
	[MemberNotNull(nameof(Regex))]
	private void SetupRegex()
	{
		// Compile the regex for better performance when used multiple times
		if (Regex == null)
		{
			if (string.IsNullOrEmpty(Pattern))
			{
				throw new InvalidOperationException("Regex pattern cannot be null or empty");
			}

			Regex = MatchTimeoutInMilliseconds == -1 ? new Regex(Pattern, RegexOptions.Compiled) : new Regex(Pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(MatchTimeoutInMilliseconds));
		}
	}
}
