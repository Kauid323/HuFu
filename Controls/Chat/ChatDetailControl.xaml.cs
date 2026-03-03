using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HuFu.Pages;

namespace HuFu.Controls;

public sealed partial class ChatDetailControl : UserControl
{
    public ChatDetailControl()
    {
        InitializeComponent();
    }

    public ChatViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty) as ChatViewModel;
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(ChatViewModel),
        typeof(ChatDetailControl),
        new PropertyMetadata(null));

    public ListView MessageList => MessageListView;
}
