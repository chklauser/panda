using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Panda.UI.Internal
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses",Justification = "False positive. Instantiated from WPF/XAML")]
    class CommandLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var raw = (value ?? "").ToString();
            raw = raw.Trim();
            if (raw.Length == 0)
                return DependencyProperty.UnsetValue;

            // domain type to string
            var prefixToMark = parameter as String;
            int insertionIndex = 0;
            if (prefixToMark != null)
            {
                insertionIndex = prefixToMark.IndexOf(prefixToMark,StringComparison.Ordinal);
                if (insertionIndex < 0)
                    insertionIndex = 0;
            }

            return raw.Insert(insertionIndex, "_");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // string to domain type
            var raw = (value ?? "").ToString();
            raw = raw.Trim();
            if (raw.Length == 0)
                return DependencyProperty.UnsetValue;

            // domain type to string
            var prefixToMark = parameter as String;
            int insertionIndex = 0;
            if (prefixToMark != null)
            {
                insertionIndex = prefixToMark.IndexOf(prefixToMark, StringComparison.Ordinal);
                if (insertionIndex < 0)
                    insertionIndex = 0;
            }

            return raw.Remove(insertionIndex-1, 1);
            // DependencyProperty.UnsetValue signals error
        }
    }
}
