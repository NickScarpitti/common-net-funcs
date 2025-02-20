using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Core;

namespace CommonNetFuncs.Web.Common.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AllowedNullableValuesAttribute : ValidationAttribute
{
    private readonly IEnumerable<object> allowedValues;
    private readonly bool allowNull;
    private readonly Type? enumType;

    // Original constructors
    public AllowedNullableValuesAttribute(params object[] allowedValues) : this(true, allowedValues)
    {
    }

    public AllowedNullableValuesAttribute(bool allowNull, params object[] allowedValues)
    {
        this.allowedValues = allowedValues ?? throw new ArgumentNullException(nameof(allowedValues));
        this.allowNull = allowNull;
    }

    // New enum constructor
    public AllowedNullableValuesAttribute(Type enumType) : this(true, enumType)
    {
        if (!enumType.IsEnum)
            throw new ArgumentException("Type must be an enum", nameof(enumType));
    }

    // New enum constructor with null configuration
    public AllowedNullableValuesAttribute(bool allowNull, Type enumType)
    {
        if (!enumType.IsEnum)
        {
            throw new ArgumentException("Type must be an enum", nameof(enumType));
        }

        this.enumType = enumType;
        allowedValues = Enum.GetValues(enumType).Cast<object>();
        this.allowNull = allowNull;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Handle null values based on configuration
        if (value == null)
        {
            return allowNull ? ValidationResult.Success : new ValidationResult($"The field {validationContext.DisplayName} cannot be null.");
        }

        // If this is an enum validation
        if (enumType != null)
        {
            // Handle numeric types that should match enum values
            if (value.GetType().IsNumericType())
            {
                long numericValue = Convert.ToInt64(value);
                IEnumerable<long> enumValues = Enum.GetValues(enumType).Cast<object>().Select(v => Convert.ToInt64(v));

                if (enumValues.Contains(numericValue))
                {
                    return ValidationResult.Success;
                }

                // Create error message showing both enum names and their numeric values
                IEnumerable<string> enumValuesWithNumbers = Enum.GetNames(enumType).Select(name => $"{name} ({Convert.ToInt64(Enum.Parse(enumType, name))})");
                string validValues = string.Join(", ", enumValuesWithNumbers);

                return new ValidationResult($"The field {validationContext.DisplayName} must be one of the following values: {validValues}");
            }

            // Handle direct enum values
            if (!Enum.IsDefined(enumType, value))
            {
                string enumValues = string.Join(", ", Enum.GetNames(enumType));
                return new ValidationResult($"The field {validationContext.DisplayName} must be one of the following values: {enumValues}");
            }
            return ValidationResult.Success;
        }

        // Original validation for non-enum values
        if (allowedValues.Any(v => v?.Equals(value) == true))
        {
            return ValidationResult.Success;
        }

        // Create error message with the list of allowed values
        string allowedValuesList = string.Join(", ", allowedValues.Select(v => v?.ToString() ?? "null"));
        return new ValidationResult($"The field {validationContext.DisplayName} must be one of the following values: {allowedValuesList}");
    }
}
