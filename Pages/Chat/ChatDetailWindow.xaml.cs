using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HuFu.Helpers;
using System;
using Windows.Graphics;

namespace HuFu.Pages;

public sealed partial class ChatDetailWindow : Window
{
    public ChatViewModel ViewModel { get; }
    private MessageScrollHelper? _scrollHelper;

    public ChatDetailWindow(ConversationDisplayItem conversation)
    {
        InitializeComponent();
        
        ViewModel = new ChatViewModel();
        ViewModel.SelectedConversation = conversation;
        
        Title = conversation.Name;
        
        SetupTitleBar();
        SetWindowSize();
        
        ChatDetail.Loaded += ChatDetail_Loaded;
        Closed += ChatDetailWindow_Closed;
    }

    private void ChatDetail_Loaded(object sender, RoutedEventArgs e)
    {
        var messageList = ChatDetail.MessageList;
        if (messageList is not null)
        {
            _scrollHelper = new MessageScrollHelper(messageList, () => ViewModel.LoadMoreMessagesAsync());
            _scrollHelper.Setup();
        }
    }

    private void SetupTitleBar()
    {
        try
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                var titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    titleBar.ButtonBackgroundColor = Colors.Transparent;
                    titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    titleBar.ButtonForegroundColor = Colors.White;
                    titleBar.ButtonInactiveForegroundColor = Colors.Gray;
                }
                else
                {
                    titleBar.ButtonBackgroundColor = Colors.Transparent;
                    titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    titleBar.ButtonForegroundColor = Colors.Black;
                    titleBar.ButtonInactiveForegroundColor = Colors.Gray;
                }
            }
        }
        catch
        {
            // Fallback if title bar customization fails
        }
    }

    private void SetWindowSize()
    {
        try
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                appWindow.Resize(new SizeInt32(800, 600));
            }
        }
        catch
        {
            // Fallback to default size
        }
    }

    private void ChatDetailWindow_Closed(object sender, WindowEventArgs args)
    {
        _scrollHelper?.Cleanup();
    }
}
