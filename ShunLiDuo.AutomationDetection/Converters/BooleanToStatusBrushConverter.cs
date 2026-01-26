using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShunLiDuo.AutomationDetection.Converters
{
    public class BooleanToStatusBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush TrueBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#52C41A")); // Success Green
        private static readonly SolidColorBrush FalseBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4F")); // Error Red

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueBrush : FalseBrush;
            }
            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
