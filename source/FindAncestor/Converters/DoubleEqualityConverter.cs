using System;
using System.Globalization;
using System.Windows.Data;

namespace FindAncestor.Converters
{
    public class DoubleEqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return Math.Abs(System.Convert.ToDouble(value) - System.Convert.ToDouble(parameter)) < 0.0001;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return System.Convert.ToDouble(parameter);
            return Binding.DoNothing;
        }
    }
}