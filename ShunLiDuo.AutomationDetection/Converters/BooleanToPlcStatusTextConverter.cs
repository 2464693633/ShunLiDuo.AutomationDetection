using System;
using System.Globalization;
using System.Windows.Data;

namespace ShunLiDuo.AutomationDetection.Converters
{
    public class BooleanToPlcStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected ? "PLC已连接" : "PLC未连接";
            }
            return "PLC未连接";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

