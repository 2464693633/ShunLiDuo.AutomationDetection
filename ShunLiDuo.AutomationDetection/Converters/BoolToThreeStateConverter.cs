using System;
using System.Globalization;
using System.Windows.Data;

namespace ShunLiDuo.AutomationDetection.Converters
{
    public class BoolToThreeStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果值可以转换为bool且为true，返回true以启用三态
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

