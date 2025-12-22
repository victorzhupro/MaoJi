using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MaoJi.Converters
{
    /// <summary>
    /// 布尔值到可见性的转换器
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // 如果参数为 "Inverse"，则反转逻辑
                if (parameter?.ToString() == "Inverse")
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                var result = visibility == Visibility.Visible;
                if (parameter?.ToString() == "Inverse")
                {
                    return !result;
                }
                return result;
            }
            return false;
        }
    }
}

