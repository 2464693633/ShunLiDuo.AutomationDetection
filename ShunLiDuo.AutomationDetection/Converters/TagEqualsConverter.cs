using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShunLiDuo.AutomationDetection.Converters
{
    public class TagEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return false;

            var tag = values[0]?.ToString();
            var currentValue = values[1]?.ToString();

            return tag == currentValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
