using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Core.TypeChecks;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]

/// <summary>
/// Validates that all items in a list do NOT contain any of the specified deny characters.
/// Validation fails if any item contains any character from the deny characters.
/// </summary>
public sealed class ListDenyCharactersAttribute : ValidationAttribute
{
	/// <summary>
	/// Gets the deny characters
	/// </summary>
	public string DenyCharacters { get; }

	/// <summary>
	/// Constructor that accepts the deny characters
	/// </summary>
	/// <param name="denyCharacters">String containing all characters that should be denied. Items containing any of these characters will fail validation.</param>
	public ListDenyCharactersAttribute(string denyCharacters)
	{
		ArgumentNullException.ThrowIfNull(denyCharacters);

		if (denyCharacters.Length == 0)
		{
			throw new ArgumentException("Deny characters cannot be empty", nameof(denyCharacters));
		}

		DenyCharacters = denyCharacters;
		ErrorMessage = "Item at index {0} '{1}' must not contain any of the following characters: '{2}'.";
	}

	/// <summary>
	/// Constructor that accepts the deny characters
	/// </summary>
	/// <param name="denyCharacters">String containing all characters that should be denied. Items containing any of these characters will fail validation.</param>
	public ListDenyCharactersAttribute(char[] denyCharacters)
	{
		ArgumentNullException.ThrowIfNull(denyCharacters);

		if (denyCharacters.Length == 0)
		{
			throw new ArgumentException("Deny characters cannot be empty", nameof(denyCharacters));
		}

		DenyCharacters = new string(denyCharacters);
		ErrorMessage = "Item at index {0} '{1}' must not contain any of the following characters: '{2}'.";
	}

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
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
		throw new InvalidDataException($"${nameof(ListDenyCharactersAttribute)} can only be used on properties that implement IEnumerable");
	}

	private ValidationResult? ValidateEnumerable(IEnumerable<string?> values, string memberName)
	{
		int index = 0;
		ReadOnlySpan<char> denyCharactersSpan = DenyCharacters.AsSpan();

		foreach (string? item in values)
		{
			if (!string.IsNullOrEmpty(item) && item.ContainsAnyCharacter(denyCharactersSpan)) // Null / empty passes automatically
			{
				return new ValidationResult(
					string.Format(CultureInfo.CurrentCulture, ErrorMessageString, index, item.UrlEncodeReadable(), DenyCharacters),
					[memberName]
				);
			}
			index++;
		}
		return null;
	}

	/// <summary>
	///	Override of <see cref="ValidationAttribute.FormatErrorMessage" />
	/// </summary>
	/// <remarks>This override provides a formatted error message describing the denied characters</remarks>
	/// <param name="name">The user-visible name to include in the formatted message.</param>
	/// <returns>The localized message to present to the user</returns>
	public override string FormatErrorMessage(string name)
	{
		return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, "{index}", "{value}", DenyCharacters);
	}
}
