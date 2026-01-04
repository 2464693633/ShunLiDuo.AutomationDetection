using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShunLiDuo.AutomationDetection.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string currentView = value.ToString();
            string targetView = parameter.ToString();

            return currentView == targetView ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

