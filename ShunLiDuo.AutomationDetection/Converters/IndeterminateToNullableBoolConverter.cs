using System;
using System.Globalization;
using System.Windows.Data;

namespace ShunLiDuo.AutomationDetection.Converters
{
    public class IndeterminateToNullableBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 这个转换器主要用于显示，实际的三态由事件处理
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false; // null 表示不确定状态
            if (value is bool boolValue)
                return boolValue;
            return false;
        }
    }
}
