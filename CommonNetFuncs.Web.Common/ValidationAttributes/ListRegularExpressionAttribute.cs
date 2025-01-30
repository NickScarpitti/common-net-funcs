using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]

/// <summary>
/// Validates that all items in a list match the specified regular expression pattern
/// </summary>
public sealed class ListRegularExpressionAttribute : ValidationAttribute
{
    private readonly Regex _regex;

    public ListRegularExpressionAttribute(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        // Compile the regex for better performance when used multiple times
        _regex = new Regex(pattern, RegexOptions.Compiled);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        int index = 0;
        string memberName = validationContext.MemberName ?? string.Empty;

        // Handle different types of collections
        switch (value)
        {
            case IEnumerable<string?> stringList:
                foreach (string? item in stringList)
                {
                    if (item is not null && !_regex.IsMatch(item))
                    {
                        return new ValidationResult($"Item at index {index} does not match the required pattern", [memberName]);
                    }
                    index++;
                }
                break;

            case IEnumerable<int> intList:
                foreach (int item in intList)
                {
                    if (!_regex.IsMatch(item.ToString()))
                    {
                        return new ValidationResult($"Item at index {index} does not match the required pattern", new[] { memberName });
                    }
                    index++;
                }
                break;

            case IEnumerable<double> doubleList:
                foreach (double item in doubleList)
                {
                    if (!_regex.IsMatch(item.ToString()))
                    {
                        return new ValidationResult($"Item at index {index} does not match the required pattern", [memberName]);
                    }
                    index++;
                }
                break;

                // Add more numeric types as needed
        }

        return ValidationResult.Success;
    }
}
