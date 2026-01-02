using System.ComponentModel.DataAnnotations;
using System.Globalization;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]

/// <summary>
/// Validates that a value does NOT contain any of the specified blacklisted characters.
/// Validation fails if the value contains any character from the blacklist.
/// </summary>
public sealed class DenyCharactersAttribute : ValidationAttribute
{
	/// <summary>
	/// Gets the deny characters
	/// </summary>
	public string DenyCharacters { get; }

	/// <summary>
	/// Constructor that accepts the deny characters
	/// </summary>
	/// <param name="denyCharacters">String containing all characters that should be denied. Values containing any of these characters will fail validation.</param>
	public DenyCharactersAttribute(string denyCharacters)
	{
		ArgumentNullException.ThrowIfNull(denyCharacters);

		if (denyCharacters.Length == 0)
		{
			throw new ArgumentException("Deny characters cannot be empty", nameof(denyCharacters));
		}

		DenyCharacters = denyCharacters;
		ErrorMessage = "The field {0} must not contain any of the following characters: '{1}'.";
	}

	/// <summary>
	/// Constructor that accepts the deny characters
	/// </summary>
	/// <param name="denyCharacters">String containing all characters that should be denied. Values containing any of these characters will fail validation.</param>
	public DenyCharactersAttribute(char[] denyCharacters)
	{
		ArgumentNullException.ThrowIfNull(denyCharacters);

		if (denyCharacters.Length == 0)
		{
			throw new ArgumentException("Deny characters cannot be empty", nameof(denyCharacters));
		}

		DenyCharacters = new string(denyCharacters);
		ErrorMessage = "The field {0} must not contain any of the following characters: '{1}'.";
	}

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
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

		// Check if the string contains any deny characters
		if (stringValue.ContainsAnyCharacter(DenyCharacters.AsSpan()))
		{
			string memberName = validationContext.MemberName ?? string.Empty;
			return new ValidationResult(
				FormatErrorMessage(validationContext.DisplayName),
				string.IsNullOrEmpty(memberName) ? null : new[] { memberName }
			);
		}

		// If no deny characters found, validation succeeds
		return ValidationResult.Success;
	}

	/// <summary>
	///	Override of <see cref="ValidationAttribute.FormatErrorMessage" />
	/// </summary>
	/// <remarks>This override provides a formatted error message describing the denied characters</remarks>
	/// <param name="name">The user-visible name to include in the formatted message.</param>
	/// <returns>The localized message to present to the user</returns>
	public override string FormatErrorMessage(string name)
	{
		return string.IsNullOrEmpty(ErrorMessageString)
			? $"The field {name} must not contain any of the following characters: '{DenyCharacters}'."
			: string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, DenyCharacters);
	}
}
