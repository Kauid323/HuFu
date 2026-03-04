using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace HuFu.Converters;

public class ContentTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ulong contentType && parameter is string targetTypeStr && ulong.TryParse(targetTypeStr, out var expectedType))
        {
            return contentType == expectedType ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
