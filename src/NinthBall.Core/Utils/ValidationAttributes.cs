
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Utils
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
    public class ValidateNestedAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // 'Required' is not my concern.
            if (value == null) return ValidationResult.Success;

            var results = new List<ValidationResult>();
            var parentName = validationContext.DisplayName;

            // Handle collections
            if (value is IEnumerable enumerable && value is not string)
            {
                var index = 0;
                foreach (var element in enumerable)
                {
                    if (element == null)
                    {
                        index++;
                        continue;
                    }
                    else
                    {
                        var elementContext = new ValidationContext(element);
                        var elementResults = new List<ValidationResult>();

                        if (!Validator.TryValidateObject(element, elementContext, elementResults, true))
                            foreach (var r in elementResults)
                                results.Add(new ValidationResult($"{parentName}[{index}]: {r.ErrorMessage}", r.MemberNames));

                        index++;
                    }
                }
            }
            else
            {
                // Handle single nested object
                var nestedContext = new ValidationContext(value);
                Validator.TryValidateObject(value, nestedContext, results, true);

            }

            if (results.Count == 0)
                return ValidationResult.Success;

            return new ValidationResult(
                string.Join(Environment.NewLine, results.Select(r => r.ErrorMessage))
            );
        }
    }
}
