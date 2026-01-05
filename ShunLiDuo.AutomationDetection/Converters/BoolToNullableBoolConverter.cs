using System;
using System.Globalization;
using System.Windows.Data;

namespace ShunLiDuo.AutomationDetection.Converters
{
    public class BoolToNullableBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            bool isSelected = values[0] is bool selected && selected;
            bool isIndeterminate = values[1] is bool indeterminate && indeterminate;

            if (isIndeterminate)
                return null; // 返回 null 表示 Indeterminate 状态

            return isSelected;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // 这个转换器主要用于显示，不需要 ConvertBack
            return new object[] { value is bool b && b, false };
        }
    }
}

