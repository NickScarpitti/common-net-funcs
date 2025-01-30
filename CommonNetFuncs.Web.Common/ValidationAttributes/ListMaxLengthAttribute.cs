using System.ComponentModel.DataAnnotations;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]

/// <summary>
/// Validates that all strings in a list do not exceed the specified maximum length
/// </summary>
public sealed class ListMaxLengthAttribute : ValidationAttribute
{
    private readonly int _maxLength;

    public ListMaxLengthAttribute(int maxLength)
    {
        if (maxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Maximum length must be non-negative");
        }

        _maxLength = maxLength;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        int index = 0;
        string memberName = validationContext.MemberName ?? string.Empty;

        // Handle different types that have a Length or Count property
        switch (value)
        {
            case IEnumerable<string?> stringList:
                foreach (string? item in stringList)
                {
                    if (item is not null && item.Length > _maxLength)
                    {
                        return new ValidationResult($"Item at index {index} exceeds the maximum length of {_maxLength}", [memberName]);
                    }
                    index++;
                }
                break;

            case IEnumerable<byte[]?> byteArrayList:
                foreach (byte[]? item in byteArrayList)
                {
                    if (item is not null && item.Length > _maxLength)
                    {
                        return new ValidationResult($"Item at index {index} exceeds the maximum length of {_maxLength}", [memberName]);
                    }
                    index++;
                }
                break;

            case IEnumerable<Array?> arrayList:
                foreach (Array? item in arrayList)
                {
                    if (item is not null && item.Length > _maxLength)
                    {
                        return new ValidationResult($"Item at index {index} exceeds the maximum length of {_maxLength}", [memberName]);
                    }
                    index++;
                }
                break;

            case IEnumerable<ICollection<object>?> collectionList:
                foreach (System.Collections.Generic.ICollection<object>? item in collectionList)
                {
                    if (item is not null && item.Count > _maxLength)
                    {
                        return new ValidationResult($"Item at index {index} exceeds the maximum length of {_maxLength}", [memberName]);
                    }
                    index++;
                }
                break;

            // Handle the case where the value itself is an array or collection
            case Array array:
                if (array.Length > _maxLength)
                {
                    return new ValidationResult($"Collection exceeds the maximum length of {_maxLength}", [memberName]);
                }
                break;

            case ICollection<object> collection:
                if (collection.Count > _maxLength)
                {
                    return new ValidationResult($"Collection exceeds the maximum length of {_maxLength}", [memberName]);
                }
                break;
        }

        return ValidationResult.Success;
    }
}
