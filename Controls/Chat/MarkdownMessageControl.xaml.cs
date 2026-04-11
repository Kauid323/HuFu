using HuFu.Helpers.Markdown;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace HuFu.Controls;

public sealed partial class MarkdownMessageControl : UserControl
{
    private readonly WinUiMarkdownRenderer _renderer = new();

    public MarkdownMessageControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(
            nameof(MarkdownText),
            typeof(string),
            typeof(MarkdownMessageControl),
            new PropertyMetadata(string.Empty, OnRenderPropertyChanged));

    public bool IsMine
    {
        get => (bool)GetValue(IsMineProperty);
        set => SetValue(IsMineProperty, value);
    }

    public static readonly DependencyProperty IsMineProperty =
        DependencyProperty.Register(
            nameof(IsMine),
            typeof(bool),
            typeof(MarkdownMessageControl),
            new PropertyMetadata(false, OnRenderPropertyChanged));

    private static void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownMessageControl control)
        {
            control.Render();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Render();
    }

    private void Render()
    {
        var options = BuildOptions();
        MarkdownBlock.Foreground = options.Foreground;
        _renderer.Render(MarkdownBlock, MarkdownText ?? string.Empty, options);
    }

    private MarkdownRenderOptions BuildOptions()
    {
        var foreground = GetResourceBrush(IsMine
            ? "SystemControlForegroundAltHighBrush"
            : "SystemControlForegroundBaseHighBrush") ?? new SolidColorBrush(Colors.Black);

        var linkForeground = GetResourceBrush(IsMine
            ? "SystemControlForegroundAltHighBrush"
            : "SystemControlHyperlinkTextBrush") ?? new SolidColorBrush(Colors.DodgerBlue);

        var quoteForeground = GetResourceBrush(IsMine
            ? "SystemControlForegroundAltMediumBrush"
            : "SystemControlForegroundBaseMediumBrush") ?? new SolidColorBrush(Colors.Gray);

        var borderBrush = GetResourceBrush(IsMine
            ? "SystemControlForegroundAltMediumBrush"
            : "SystemControlForegroundBaseMediumBrush") ?? new SolidColorBrush(Colors.LightGray);

        var codeInlineBg = new SolidColorBrush(IsMine
            ? Windows.UI.Color.FromArgb(30, 255, 255, 255)
            : Windows.UI.Color.FromArgb(20, 0, 0, 0));

        var codeBlockBg = new SolidColorBrush(IsMine
            ? Windows.UI.Color.FromArgb(20, 255, 255, 255)
            : Windows.UI.Color.FromArgb(14, 0, 0, 0));

        return new MarkdownRenderOptions
        {
            Foreground = foreground,
            LinkForeground = linkForeground,
            QuoteForeground = quoteForeground,
            QuoteBorderBrush = borderBrush,
            SeparatorBrush = borderBrush,
            TableBorderBrush = borderBrush,
            CodeInlineBackground = codeInlineBg,
            CodeBlockBackground = codeBlockBg,
            MaxImageWidth = 300
        };
    }

    private static Brush? GetResourceBrush(string key)
    {
        if (Application.Current?.Resources?.TryGetValue(key, out var value) == true)
        {
            return value as Brush;
        }
        return null;
    }
}
