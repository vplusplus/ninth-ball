using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NinthBall.Core
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MinAttribute(double Min) : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // 'Required' is not my concern.
            if (value == null) return ValidationResult.Success;

            double dblValue = 0.0;
            try { dblValue = Convert.ToDouble(value); } catch { return ValidationResult.Success; }
            return dblValue < Min ? new ValidationResult($"{validationContext.MemberName} must be >= {Min:F1}") : ValidationResult.Success;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MaxAttribute(double Max) : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // 'Required' is not my concern.
            if (value == null) return ValidationResult.Success;

            double dblValue = 0.0;
            try { dblValue = Convert.ToDouble(value); } catch { return ValidationResult.Success; }
            return dblValue > Max ? new ValidationResult($"{validationContext.MemberName} must be <= {Max:F1}") : ValidationResult.Success;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class FileExistsAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // 'Required' is not my concern.
            if (value == null) return ValidationResult.Success;

            return value is not string fileName ? ValidationResult.Success
                : File.Exists(fileName) ? ValidationResult.Success
                : new ValidationResult($"{validationContext.MemberName} | File not found | {Path.GetFileName(fileName)}");
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ValidateNestedAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // 'Required' is not my concern.
            if (value == null) return ValidationResult.Success;

            var innerResults = new List<ValidationResult>();
            var innerContext = new ValidationContext(value, serviceProvider: null, items: null);
            var innerIsValid = Validator.TryValidateObject(value, innerContext, innerResults, validateAllProperties: true);

            if (!innerIsValid)
            {
                var buffer = new StringBuilder();
                var parentName = validationContext.DisplayName;

                foreach (var inner in innerResults)
                {
                    var mssgWithPrefix = $"{parentName}: {inner.ErrorMessage}";
                    buffer.AppendLine(mssgWithPrefix);
                }

                return new ValidationResult(buffer.ToString());
            }

            return ValidationResult.Success;
        }
    }
}
