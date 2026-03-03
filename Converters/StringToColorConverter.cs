using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HuFu.Converters;

public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string colorStr || string.IsNullOrWhiteSpace(colorStr))
        {
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        try
        {
            // 尝试解析十六进制颜色（如 "#FF0000" 或 "#FFFF0000"）
            if (colorStr.StartsWith("#"))
            {
                var hex = colorStr.TrimStart('#');
                
                // 补全 alpha 通道
                if (hex.Length == 6)
                {
                    hex = "FF" + hex;
                }
                else if (hex.Length == 8)
                {
                    // 已经包含 alpha
                }
                else
                {
                    return new SolidColorBrush(Microsoft.UI.Colors.Gray);
                }

                var r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                var a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);

                return new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }

            // 尝试解析颜色名称（如 "Red", "Blue"）
            var colorProperty = typeof(Microsoft.UI.Colors).GetProperty(colorStr, 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase);
            
            if (colorProperty != null)
            {
                var color = (Color)colorProperty.GetValue(null)!;
                return new SolidColorBrush(color);
            }

            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }
        catch
        {
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
