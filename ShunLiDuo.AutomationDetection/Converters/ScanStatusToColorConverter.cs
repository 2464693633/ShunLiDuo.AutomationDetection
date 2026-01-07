using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShunLiDuo.AutomationDetection.Converters
{
    public class ScanStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "匹配成功":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 绿色
                    case "匹配失败":
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 红色
                    case "等待扫码":
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // 灰色
                    case "未选择规则":
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 橙色
                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // 默认灰色
                }
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // 默认灰色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

