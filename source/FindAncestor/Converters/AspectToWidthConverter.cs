using System;
using System.Globalization;
using System.Windows.Data;

namespace FindAncestor.Converters
{
    public class AspectToWidthConverter : IValueConverter
    {
        // Height × AspectRatio で Width を計算
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height && parameter is double aspect)
            {
                return height * aspect;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}