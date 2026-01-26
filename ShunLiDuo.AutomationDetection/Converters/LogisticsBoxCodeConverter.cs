using System;
using System.Globalization;
using System.Windows.Data;

namespace ShunLiDuo.AutomationDetection.Converters
{
    /// <summary>
    /// 物流盒编码转换器：去除"物流盒编码"前缀，只显示编码
    /// </summary>
    public class LogisticsBoxCodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            string code = value.ToString();
            
            // 去除"物流盒编码"前缀
            if (code.StartsWith("物流盒编码"))
            {
                return code.Replace("物流盒编码", "").Trim();
            }
            
            return code;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
