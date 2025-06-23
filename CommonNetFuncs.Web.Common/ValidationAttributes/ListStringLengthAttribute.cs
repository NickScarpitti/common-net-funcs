using System.ComponentModel.DataAnnotations;
using System.Globalization;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]

/// <summary>
/// Validates that all strings in a list do not exceed the specified length
/// </summary>
public sealed class ListStringLengthAttribute(int maximumLength) : ValidationAttribute
{
    public int MaximumLength
    {
        get;
    } = maximumLength >= 0 ? maximumLength : throw new ArgumentOutOfRangeException(nameof(maximumLength), "Maximum length must be >= 0");

    private int _minimumLength;

    public int MinimumLength
    {
        get => _minimumLength;
        set => _minimumLength = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(MinimumLength), "Minimum length must be >= 0");
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        EnsureLegalLengths();

        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not System.Collections.ICollection collection)
        {
            throw new InvalidDataException("ListStringLengthAttribute must be used on a collection");
        }

        //if (value is not IEnumerable<string?> list)
        //{
        //    return ValidationResult.Success;
        //}

        int index = 0;
        foreach (string? item in collection)
        {
            if (item is null)
            {
                index++;
                continue;
            }

            int length = item.Length;
            if (length < MinimumLength || length > MaximumLength)
            {
                return new ValidationResult($"Item at index {index} '{item.UrlEncodeReadable()}' must be between {MinimumLength} and {MaximumLength} characters", [validationContext.MemberName ?? string.Empty]);
            }
            index++;
        }

        return ValidationResult.Success;
    }

    /// <summary>
    ///     Override of <see cref="ValidationAttribute.FormatErrorMessage" />
    /// </summary>
    /// <param name="name">The name to include in the formatted string</param>
    /// <returns>A localized string to describe the maximum acceptable length</returns>
    /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
    public override string FormatErrorMessage(string name)
    {
        EnsureLegalLengths();

        bool useErrorMessageWithMinimum = MinimumLength != 0 && ErrorMessage == null;
        string errorMessage = useErrorMessageWithMinimum
            ? "Minimum length {1} must be less than or equal to maximum length {2}"
            : "Maximum length {1} cannot be negative";

        // it's ok to pass in the minLength even for the error message without a {2} param since string.Format will just
        // ignore extra arguments
        return string.Format(CultureInfo.CurrentCulture, errorMessage, name, MaximumLength, MinimumLength);
    }

    /// <summary>
    ///     Checks that MinimumLength and MaximumLength have legal values.  Throws InvalidOperationException if not.
    /// </summary>
    private void EnsureLegalLengths()
    {
        if (MaximumLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumLength), "Maximum length cannot be negative");
        }

        if (MinimumLength > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumLength), "Minimum length must be less than or equal to maximum length");
        }
    }
}
