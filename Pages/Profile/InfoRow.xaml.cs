using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace HuFu.Pages;

public sealed partial class InfoRow : UserControl
{
    public InfoRow()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(InfoRow), new PropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(InfoRow), new PropertyMetadata(string.Empty));

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty SubValueProperty = DependencyProperty.Register(
        nameof(SubValue), typeof(string), typeof(InfoRow), new PropertyMetadata(string.Empty));

    public string SubValue
    {
        get => (string)GetValue(SubValueProperty);
        set => SetValue(SubValueProperty, value);
    }

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(string), typeof(InfoRow), new PropertyMetadata(string.Empty));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IsCopyableProperty = DependencyProperty.Register(
        nameof(IsCopyable), typeof(bool), typeof(InfoRow), new PropertyMetadata(false));

    public bool IsCopyable
    {
        get => (bool)GetValue(IsCopyableProperty);
        set => SetValue(IsCopyableProperty, value);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Value))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(Value);
            Clipboard.SetContent(dataPackage);
        }
    }
}
