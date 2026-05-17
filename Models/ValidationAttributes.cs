using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HealthInsuranceManagement.Models
{
    public sealed class AdultDateAttribute : ValidationAttribute
    {
        public int MinimumAge { get; init; } = 18;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not DateTime date)
                return ValidationResult.Success;

            var today = DateTime.Today;
            if (date == default)
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.");

            if (date.Date > today)
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} cannot be in the future.");

            var age = today.Year - date.Year;
            if (date.Date > today.AddYears(-age))
                age--;

            return age >= MinimumAge
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must be at least {MinimumAge} years old.");
        }
    }

    public sealed class NotFutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return ValidationResult.Success;

            var date = value is DateTime dateTime ? dateTime.Date : (DateTime?)null;

            if (date.HasValue && date.Value > DateTime.Today)
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} cannot be in the future.");

            return ValidationResult.Success;
        }
    }

    public sealed class StrongPasswordAttribute : ValidationAttribute
    {
        private static readonly Regex StrongPasswordRegex = new(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$", RegexOptions.Compiled);

        public override bool IsValid(object? value)
        {
            return value is string password && StrongPasswordRegex.IsMatch(password);
        }

        public override string FormatErrorMessage(string name)
        {
            return ErrorMessage ?? $"{name} must be at least 8 characters and include a letter, a number, and a symbol.";
        }
    }
}
