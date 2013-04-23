using System;
using System.Globalization;
using System.Security.Policy;
using System.Windows.Controls;

namespace Panda.UI.Internal
{
    public class UrlValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return Validate(value);
        }

        public static ValidationResult Validate(object value)
        {
            if (value == null)
                return new ValidationResult(false, "Is null");

            var raw = value.ToString();
            if (String.IsNullOrWhiteSpace(raw))
                return new ValidationResult(false, "Is empty.");

            if (!Uri.IsWellFormedUriString(raw, UriKind.Absolute))
                return new ValidationResult(false, "Is not a valid URL.");
            else
                return ValidationResult.ValidResult;
        }
    }
}