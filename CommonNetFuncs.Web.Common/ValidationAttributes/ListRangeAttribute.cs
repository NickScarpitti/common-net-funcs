using System.ComponentModel.DataAnnotations;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]

/// <summary>
/// Validates that all numeric values in a list fall within the specified range
/// </summary>
public sealed class ListRangeAttribute : ValidationAttribute
{
    private readonly double _minimum;
    private readonly double _maximum;

    public ListRangeAttribute(double minimum, double maximum)
    {
        if (minimum > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(minimum), "Minimum value must be less than or equal to maximum value");
        }

        _minimum = minimum;
        _maximum = maximum;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        int index = 0;
        string memberName = validationContext.MemberName ?? string.Empty;

        // Handle different numeric types
        switch (value)
        {
            case IEnumerable<int> intList:
                foreach (int item in intList)
                {
                    if (item < _minimum || item > _maximum)
                    {
                        return new ValidationResult($"Item at index {index} must be between {_minimum} and {_maximum}", [memberName]);
                    }
                    index++;
                }
                break;

            case IEnumerable<double> doubleList:
                foreach (double item in doubleList)
                {
                    if (item < _minimum || item > _maximum)
                    {
                        return new ValidationResult($"Item at index {index} must be between {_minimum} and {_maximum}", [memberName]);
                    }
                    index++;
                }
                break;

            case IEnumerable<decimal> decimalList:
                decimal min = (decimal)_minimum;
                decimal max = (decimal)_maximum;
                foreach (decimal item in decimalList)
                {
                    if (item < min || item > max)
                    {
                        return new ValidationResult($"Item at index {index} must be between {_minimum} and {_maximum}", [memberName]);
                    }
                    index++;
                }
                break;

                // Add more numeric types as needed
        }

        return ValidationResult.Success;
    }
}
