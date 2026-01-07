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
            // 处理从 UI 到数据源的转换
            // 当用户点击复选框时，更新 IsSelected
            // IsIndeterminate 由 PermissionItem 的逻辑自动管理
            if (value == null)
            {
                // null 表示 Indeterminate 状态
                // 对于 Indeterminate 状态，不更新 IsSelected，让事件处理程序处理
                // 返回 DependencyProperty.UnsetValue 表示不更新该绑定
                return new object[] { System.Windows.DependencyProperty.UnsetValue, System.Windows.DependencyProperty.UnsetValue };
            }
            
            if (value is bool boolValue)
            {
                // 返回新的 IsSelected 值，IsIndeterminate 不更新（返回 UnsetValue）
                return new object[] { boolValue, System.Windows.DependencyProperty.UnsetValue };
            }
            
            return new object[] { System.Windows.DependencyProperty.UnsetValue, System.Windows.DependencyProperty.UnsetValue };
        }
    }
}

