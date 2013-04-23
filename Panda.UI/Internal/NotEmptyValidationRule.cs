using System.Globalization;
using System.Windows.Controls;

namespace Panda.UI.Internal
{
    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value == null)
                return new ValidationResult(false, "is null");

            var raw = value.ToString();
            if(string.IsNullOrWhiteSpace(raw))
                return new ValidationResult(false,"is blank");

            return ValidationResult.ValidResult;
        }
    }
}