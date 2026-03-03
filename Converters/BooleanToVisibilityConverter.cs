using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace HuFu.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool boolValue = value is bool b && b;
        
        // 如果 parameter 是 "False"，则逻辑取反
        if (parameter is string paramStr && string.Equals(paramStr, "False", StringComparison.OrdinalIgnoreCase))
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
