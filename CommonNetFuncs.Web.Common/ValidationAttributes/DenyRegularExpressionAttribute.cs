using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]

/// <summary>
/// Validates that a value does NOT match the specified regular expression pattern.
/// This is the inverse of the standard RegularExpression attribute - validation fails if the pattern matches.
/// </summary>
public sealed class DenyRegularExpressionAttribute : ValidationAttribute
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
	/// <param name="pattern">The regular expression pattern to deny. Values matching this pattern will fail validation.</param>
	public DenyRegularExpressionAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string pattern, bool denyOnlyFullMatch = false)
	{
		ArgumentNullException.ThrowIfNull(pattern);
		Pattern = pattern;
		DenyOnlyFullMatch = denyOnlyFullMatch;
		MatchTimeoutInMilliseconds = 2000;
		ErrorMessage = "The field {0} must not match the pattern '{1}'.";
	}

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		SetupRegex();

		// Null or empty values pass validation by default
		if (value is null)
		{
			return ValidationResult.Success;
		}

		string? stringValue = Convert.ToString(value, CultureInfo.CurrentCulture);

		if (string.IsNullOrEmpty(stringValue))
		{
			return ValidationResult.Success;
		}

		if (!DenyOnlyFullMatch)
		{
			// Check if the pattern matches anywhere in the string - if it does, validation fails
			if (Regex!.IsMatch(stringValue))
			{
				string memberName = validationContext.MemberName ?? string.Empty;
				return new ValidationResult(
					FormatErrorMessage(validationContext.DisplayName),
					string.IsNullOrEmpty(memberName) ? null : new[] { memberName }
				);
			}
		}
		else
		{
			// Check if the pattern matches - if it does, validation fails (this is the "deny" logic)
			foreach (ValueMatch m in Regex!.EnumerateMatches(stringValue))
			{
				// We are looking for an exact match, not just a search hit
				if (m.Index == 0 && m.Length == stringValue.Length)
				{
					string memberName = validationContext.MemberName ?? string.Empty;
					return new ValidationResult(
						FormatErrorMessage(validationContext.DisplayName),
						string.IsNullOrEmpty(memberName) ? null : new[] { memberName }
					);
				}
			}
		}

		// If the pattern doesn't match, validation succeeds
		return ValidationResult.Success;
	}

	/// <summary>
	///	Override of <see cref="ValidationAttribute.FormatErrorMessage" />
	/// </summary>
	/// <remarks>This override provides a formatted error message describing the denied pattern</remarks>
	/// <param name="name">The user-visible name to include in the formatted message.</param>
	/// <returns>The localized message to present to the user</returns>
	/// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
	/// <exception cref="ArgumentException"> is thrown if the <see cref="Pattern" /> is not a valid regular expression.</exception>
	public override string FormatErrorMessage(string name)
	{
		SetupRegex();

		return string.IsNullOrEmpty(ErrorMessageString)
			? $"The field {name} must not match the pattern '{Pattern}'."
			: string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Pattern);
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

			Regex = MatchTimeoutInMilliseconds == -1
				? new Regex(Pattern, RegexOptions.Compiled)
				: new Regex(Pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(MatchTimeoutInMilliseconds));
		}
	}
}
