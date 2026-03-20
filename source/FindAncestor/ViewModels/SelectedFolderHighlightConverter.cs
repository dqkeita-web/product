using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FindAncestor.ViewModels;

public class SelectedFolderHighlightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return Visibility.Collapsed;

        if (values[0] == null || values[1] == null)
            return Visibility.Collapsed;

        return values[0].Equals(values[1])
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}