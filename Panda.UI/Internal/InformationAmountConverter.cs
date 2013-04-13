using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Panda.UI.Internal
{
    public class InformationAmountConverter : IValueConverter
    {
        public const int SiBinaryMultiplier = 1024;
        public const int SiMultiplier = 1000;
        public static readonly string[] BinarySuffixes = new[] {"KiB","MiB","GiB","TiB"};
        public static readonly string[] DecimalSuffixes = new[] { "KB", "MB", "GB", "TB" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // domain type to string
            if (value == null)
                return DependencyProperty.UnsetValue;

            var numBytes = System.Convert.ToInt64(value, culture);

            var displayNum = (double) numBytes;
            var suffix = "";
            for (var nextSuffixIndex = 0;displayNum > SiBinaryMultiplier && nextSuffixIndex < BinarySuffixes.Length;nextSuffixIndex++)
            {
                suffix = BinarySuffixes[nextSuffixIndex];
                displayNum /= SiBinaryMultiplier;
            }

            return String.Format(culture, "{0} {1}", displayNum, suffix);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // string to domain type
            if (value == null)
                return null;

            var raw = value.ToString();

            // check binary suffixes
            var actualMultiplier = _parseMultiplier(BinarySuffixes, SiBinaryMultiplier, ref raw);

            if (actualMultiplier == 1)
            {
                //also check for decimal suffixes
                actualMultiplier = _parseMultiplier(DecimalSuffixes, SiMultiplier, ref raw);
            }

            double displayNumber;
            try
            {
                displayNumber = System.Convert.ToDouble(raw, culture);
            }
            catch (FormatException e)
            {
                Trace.WriteLine("Conversion of raw value failed. " + e.Message);
                return null;
            }

            var exact = displayNumber*actualMultiplier;
            if (exact > Int64.MaxValue)
            {
                exact = Int64.MaxValue;
            }

            return (long?) exact;
        }

        private static int _parseMultiplier(IEnumerable<string> suffixes, int suffixMultiplier, ref string raw)
        {
            var candidateMultiplier = 1;
            foreach (var suffix in suffixes)
            {
                candidateMultiplier *= suffixMultiplier;
                var idx = raw.LastIndexOf(suffix, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    raw = raw.Remove(idx, suffix.Length);
                    return candidateMultiplier;
                }
            }
            return 1;
        }
    }
}
