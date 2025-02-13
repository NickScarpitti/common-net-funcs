using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using CommonNetFuncs.Core;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]

/// <summary>
/// Validates that all strings in a list do not exceed the specified maximum length
/// </summary>
public sealed class ListMaxLengthAttribute : ValidationAttribute
{
    private const string DefaultErrorMessageString = "Value cannot be longer than {0} characters long";
    private const int MaxAllowableLength = -1;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MaxLengthAttribute" /> class.
    /// </summary>
    /// <param name="length">
    ///     The maximum allowable length of collection/string data.
    ///     Value must be greater than zero.
    /// </param>
    [RequiresUnreferencedCode(CountPropertyHelper.RequiresUnreferencedCodeMessage)]
    public ListMaxLengthAttribute(int length) : base(() => DefaultErrorMessageString)
    {
        Length = length;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MaxLengthAttribute" /> class.
    ///     The maximum allowable length supported by the database will be used.
    /// </summary>
    [RequiresUnreferencedCode(CountPropertyHelper.RequiresUnreferencedCodeMessage)]
    public ListMaxLengthAttribute() : base(() => DefaultErrorMessageString)
    {
        Length = MaxAllowableLength;
    }

    /// <summary>
    ///     Gets the maximum allowable length of the collection/string data.
    /// </summary>
    public int Length { get; }

    /// <summary>
    ///     Applies formatting to a specified error message. (Overrides <see cref="ValidationAttribute.FormatErrorMessage" />)
    /// </summary>
    /// <param name="name">The name to include in the formatted string.</param>
    /// <returns>A localized string to describe the maximum acceptable length.</returns>
    public override string FormatErrorMessage(string name) =>
        // An error occurred, so we know the value is greater than the maximum if it was specified
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Length);

    /// <summary>
    ///     Checks that Length has a legal value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Length is zero or less than negative one.</exception>
    private void EnsureLegalLengths()
    {
        if (Length == 0 || Length < -1)
        {
            throw new InvalidOperationException("Length cannot be zero or less than negative one");
        }
    }

    internal static class CountPropertyHelper
    {
        internal const string RequiresUnreferencedCodeMessage = "Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.";

        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public static bool TryGetCount(object value, out int count)
        {
            Debug.Assert(value != null);

            if (value is ICollection collection)
            {
                count = collection.Count;
                return true;
            }

            PropertyInfo? property = value.GetType().GetRuntimeProperty("Count");
            if (property?.CanRead == true && property.PropertyType == typeof(int))
            {
                count = (int)property.GetValue(value)!;
                return true;
            }

            count = -1;
            return false;
        }
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Check the lengths for legality
        EnsureLegalLengths();

        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not ICollection collection)
        {
            throw new InvalidDataException("ListMaxLengthAttribute must be used on a collection");
        }

        int index = 0;
        string memberName = validationContext.MemberName ?? string.Empty;

        foreach (string? item in collection)
        {
            int length;
            if (value is string str)
            {
                length = str.Length;
            }
            else if (!CountPropertyHelper.TryGetCount(value, out length))
            {
                throw new InvalidCastException($"Invalid value type {value.GetType()}");
            }

            if (MaxAllowableLength != Length && length > Length)
            {
                return new ValidationResult($"Item at index {index} '{item.UrlEncodeReadable()}' exceeds the maximum length of {MaxAllowableLength}", [memberName]);
            }
            index++;
        }

        return ValidationResult.Success;
    }
}
