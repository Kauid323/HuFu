using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using HuFu.Services;
using Microsoft.UI.Text;
using Windows.System;

namespace HuFu.Controls;

public sealed partial class ChatInputControl : UserControl
{
    public ChatInputControl()
    {
        InitializeComponent();
    }

    public event EventHandler<string>? SendRequested;

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
            var deltaY = _initialPointerY - currentY; // 向上拖拽是减小 Y，所以用初始减当前得到增加的高度
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
        // 点击整个组件区域都聚焦到输入框
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
                e.Handled = true; // 拦截 Enter，防止换行
            }
        }
        else if (shortcut == SettingsService.SendShortcut.CtrlEnter)
        {
            if (e.Key == VirtualKey.Enter && KeyboardState.IsKeyDown(VirtualKey.Control))
            {
                shouldSend = true;
                e.Handled = true; // 拦截 Ctrl+Enter，防止换行
            }
        }

        if (shouldSend)
        {
            ExecuteSend();
        }
    }

    private void ExecuteSend()
    {
        InputBox.Document.GetText(TextGetOptions.UseObjectText, out string text);
        var content = text?.TrimEnd('\r', '\n');

        if (!string.IsNullOrWhiteSpace(content))
        {
            SendRequested?.Invoke(this, content);
            InputBox.Document.SetText(TextSetOptions.None, string.Empty);
            
            // 弹出 Toast (目前使用简单提示)
            ShowToast($"发送预览: {content}");
        }
    }

    private void ShowToast(string message)
    {
        // 简单实现：在控制台输出并在 UI 上显示（之后可以接入真正的 Windows Toast）
        System.Diagnostics.Debug.WriteLine(message);
    }
}

internal static class KeyboardState
{
    public static bool IsKeyDown(VirtualKey key)
    {
        return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }
}
