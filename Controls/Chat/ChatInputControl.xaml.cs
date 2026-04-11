using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using HuFu.Services;
using HuFu.Pages;
using Microsoft.UI.Text;
using Windows.System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

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
    private bool _isEmojiPickerOpen = false;

    private void EmojiButton_Click(object sender, RoutedEventArgs e)
    {
        _isEmojiPickerOpen = !_isEmojiPickerOpen;
        EmojiPickerPopup.Visibility = _isEmojiPickerOpen ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CloseEmojiPicker_Click(object sender, RoutedEventArgs e)
    {
        _isEmojiPickerOpen = false;
        EmojiPickerPopup.Visibility = Visibility.Collapsed;
    }

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
        if (e.Key == VirtualKey.V && KeyboardState.IsKeyDown(VirtualKey.Control))
        {
            var data = Clipboard.GetContent();
            if (data.Contains(StandardDataFormats.Bitmap))
            {
                e.Handled = true;
                _ = HandleClipboardImageAsync(data);
                return;
            }
        }

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

    private async void ImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || ViewModel.SelectedConversation is null)
            return;

        var window = (Application.Current as App)?.MainWindow;
        if (window is null)
            return;

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".gif");
        picker.FileTypeFilter.Add(".webp");

        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        var buffer = await FileIO.ReadBufferAsync(file);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0)
            return;

        var (preview, mimeType, _) = await BuildPreviewAsync(bytes);

        var dialog = new ContentDialog
        {
            Title = "发送图片",
            Content = preview,
            PrimaryButtonText = "发送",
            CloseButtonText = "取消",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.SendImageAsync(bytes, file.Name, file.ContentType ?? mimeType);
        }
    }

    private async Task HandleClipboardImageAsync(DataPackageView data)
    {
        if (ViewModel is null || ViewModel.SelectedConversation is null) return;

        try
        {
            var bitmap = await data.GetBitmapAsync();
            if (bitmap is null) return;

            using var stream = await bitmap.OpenReadAsync();
            var bytes = await ReadAllBytesAsync(stream);
            if (bytes.Length == 0) return;

            var (previewImage, mimeType, fileName) = await BuildPreviewAsync(bytes);

            var dialog = new ContentDialog
            {
                Title = "发送图片",
                Content = previewImage,
                PrimaryButtonText = "发送",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.SendImageAsync(bytes, fileName, mimeType);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard image failed: {ex.Message}");
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(IRandomAccessStream stream)
    {
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        var size = (uint)stream.Size;
        await reader.LoadAsync(size);
        var bytes = new byte[size];
        reader.ReadBytes(bytes);
        return bytes;
    }

    private static async Task<(UIElement preview, string? mimeType, string fileName)> BuildPreviewAsync(byte[] bytes)
    {
        string? mimeType = null;
        string fileName = "clipboard.png";

        try
        {
            using var infoStream = new InMemoryRandomAccessStream();
            await infoStream.WriteAsync(bytes.AsBuffer());
            infoStream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(infoStream);
            mimeType = decoder.DecoderInformation.MimeTypes.FirstOrDefault();
            var ext = decoder.DecoderInformation.FileExtensions.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ext))
            {
                fileName = $"clipboard.{ext.TrimStart('.')}";
            }
        }
        catch
        {
        }

        var image = new Image
        {
            MaxWidth = 320,
            MaxHeight = 320,
            Stretch = Stretch.Uniform
        };

        using var previewStream = new InMemoryRandomAccessStream();
        await previewStream.WriteAsync(bytes.AsBuffer());
        previewStream.Seek(0);
        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(previewStream);
        image.Source = bitmap;

        return (image, mimeType, fileName);
    }
}

internal static class KeyboardState
{
    public static bool IsKeyDown(VirtualKey key)
    {
        return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }
}
