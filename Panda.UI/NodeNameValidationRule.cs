using System.Globalization;
using System.Windows.Controls;

namespace Panda.UI
{
    public class NodeNameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (VirtualFileSystem.IsLegalNodeName((string) value))
            {
                return new ValidationResult(true, null);
            }
            else
            {
                var msg = string.Format("Node name must not contain a /");
                return new ValidationResult(false, msg);
            }
        }
    }
}
