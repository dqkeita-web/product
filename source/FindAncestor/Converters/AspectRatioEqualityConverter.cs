using System;
using System.Globalization;
using System.Windows.Data;
using FindAncestor.Models;
using FindAncestor.ViewModels;

namespace FindAncestor.Converters
{
    public class AspectRatioEqualityConverter : IValueConverter
    {
        // value = SelectedAspectRatio, parameter = AspectRatioItem
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AspectRatioItem selected && parameter is AspectRatioItem item)
                return Math.Abs(selected.Value - item.Value) < 0.0001;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value && parameter is AspectRatioItem item)
                return item;
            return Binding.DoNothing;
        }
    }
}