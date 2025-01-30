using System.ComponentModel.DataAnnotations;

namespace CommonNetFuncs.Web.Common.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]

    /// <summary>
    /// Validates that all strings in a list do not exceed the specified length
    /// </summary>
    public sealed class ListStringLengthAttribute : ValidationAttribute
    {
        private readonly int _maxLength;
        private readonly int _minLength;

        public ListStringLengthAttribute(int maximumLength)
        {
            _maxLength = maximumLength;
            _minLength = 0;
        }

        public ListStringLengthAttribute(int minimumLength, int maximumLength)
        {
            if (minimumLength > maximumLength)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumLength), "Minimum length must be less than or equal to maximum length");
            }

            _minLength = minimumLength;
            _maxLength = maximumLength;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            if (value is not IEnumerable<string?> list)
            {
                return ValidationResult.Success;
            }

            int index = 0;
            foreach (string? item in list)
            {
                if (item is null)
                {
                    index++;
                    continue;
                }

                int length = item.Length;
                if (length < _minLength || length > _maxLength)
                {
                    return new ValidationResult($"Item at index {index} must be between {_minLength} and {_maxLength} characters", [validationContext.MemberName ?? string.Empty]);
                }
                index++;
            }

            return ValidationResult.Success;
        }
    }
}
