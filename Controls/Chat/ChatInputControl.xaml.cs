using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using HuFu.Services;
using HuFu.Pages;
using Microsoft.UI.Text;
using Windows.System;

namespace HuFu.Controls;

public sealed partial class ChatInputControl : UserControl
{
    public ChatInputControl()
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
        typeof(ChatInputControl),
        new PropertyMetadata(null));

    private bool _isResizing = false;
    private double _initialPointerY;
    private double _initialHeight;

    private void ResizeHandle_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth);
    }

    private void ResizeHandle_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizing)
        {
            ProtectedCursor = null;
        }
    }

    private void ResizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.CapturePointer(e.Pointer);
            _isResizing = true;
            _initialPointerY = e.GetCurrentPoint(null).Position.Y;
            _initialHeight = RootGrid.ActualHeight;
        }
    }

    private void ResizeHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isResizing)
        {
            var currentY = e.GetCurrentPoint(null).Position.Y;
            var deltaY = _initialPointerY - currentY;
            var newHeight = _initialHeight + deltaY;

            if (newHeight >= RootGrid.MinHeight && newHeight <= RootGrid.MaxHeight)
            {
                RootGrid.Height = newHeight;
            }
        }
    }

    private void ResizeHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isResizing && sender is FrameworkElement element)
        {
            element.ReleasePointerCapture(e.Pointer);
            _isResizing = false;
            ProtectedCursor = null;
        }
    }

    private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        InputBox.Focus(FocusState.Programmatic);
    }

    private void InputBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var shortcut = SettingsService.CurrentSendShortcut;
        bool shouldSend = false;

        if (shortcut == SettingsService.SendShortcut.Enter)
        {
            if (e.Key == VirtualKey.Enter && !KeyboardState.IsKeyDown(VirtualKey.Control) && !KeyboardState.IsKeyDown(VirtualKey.Shift))
            {
                shouldSend = true;
                e.Handled = true;
            }
        }
        else if (shortcut == SettingsService.SendShortcut.CtrlEnter)
        {
            if (e.Key == VirtualKey.Enter && KeyboardState.IsKeyDown(VirtualKey.Control))
            {
                shouldSend = true;
                e.Handled = true;
            }
        }

        if (shouldSend)
        {
            _ = ExecuteSendAsync();
        }
    }

    private async System.Threading.Tasks.Task ExecuteSendAsync()
    {
        if (ViewModel is null) return;

        InputBox.Document.GetText(TextGetOptions.UseObjectText, out string text);
        var content = text?.TrimEnd('\r', '\n');

        if (!string.IsNullOrWhiteSpace(content))
        {
            var success = await ViewModel.SendMessageAsync(content);
            
            if (success)
            {
                InputBox.Document.SetText(TextSetOptions.None, string.Empty);
                System.Diagnostics.Debug.WriteLine($"消息发送成功: {content}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"消息发送失败: {content}");
            }
        }
    }
}

internal static class KeyboardState
{
    public static bool IsKeyDown(VirtualKey key)
    {
        return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }
}