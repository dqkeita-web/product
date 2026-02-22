using FindAncestor.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace FindAncestor.Converters
{
    public class AspectRatioToIsCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;

            var selected = value as AspectRatioItem;
            var item = parameter as AspectRatioItem;
            if (selected == null || item == null) return false;

            return Math.Abs(selected.Value - item.Value) < 0.0001;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value && parameter is AspectRatioItem item)
                return item;
            return Binding.DoNothing;
        }
    }
}