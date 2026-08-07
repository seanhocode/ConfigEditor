using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConfigEditor.Wpf.Converters;

/// <summary>
/// 將 null 值轉為 Collapsed，非 null 轉為 Visible
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 轉換值為 Visibility
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// 反向轉換（不支援）
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
