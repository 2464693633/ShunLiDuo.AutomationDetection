using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShunLiDuo.AutomationDetection.Converters
{
    public class StringToVisibilityConverterForError : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            string str = value.ToString();
            return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

