using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using HuFu.Pages;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace HuFu.Controls;

public sealed partial class ChatDetailControl : UserControl
{
    private bool _isInfoPanelOpen = false;

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

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleInfoPanel();
    }

    private void CloseInfoPanel_Click(object sender, RoutedEventArgs e)
    {
        ToggleInfoPanel();
    }

    private void ToggleInfoPanel()
    {
        _isInfoPanelOpen = !_isInfoPanelOpen;

        if (_isInfoPanelOpen)
        {
            InfoPanel.Visibility = Visibility.Visible;
            
            // 确保有 RenderTransform
            if (InfoPanel.RenderTransform is not TranslateTransform)
            {
                InfoPanel.RenderTransform = new TranslateTransform();
            }
            
            var slideIn = new DoubleAnimation
            {
                From = 320,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            Storyboard.SetTarget(slideIn, InfoPanel.RenderTransform);
            Storyboard.SetTargetProperty(slideIn, "X");
            
            var storyboard = new Storyboard();
            storyboard.Children.Add(slideIn);
            storyboard.Begin();
        }
        else
        {
            var slideOut = new DoubleAnimation
            {
                From = 0,
                To = 320,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            Storyboard.SetTarget(slideOut, InfoPanel.RenderTransform);
            Storyboard.SetTargetProperty(slideOut, "X");
            
            var storyboard = new Storyboard();
            storyboard.Children.Add(slideOut);
            storyboard.Completed += (s, e) => InfoPanel.Visibility = Visibility.Collapsed;
            storyboard.Begin();
        }
    }

    private static readonly HttpClient _httpClient = new(new HttpClientHandler { UseProxy = false });
    private const string Referer = "https://myapp.jwznb.com";

    private async void Image_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string imageUrl || string.IsNullOrEmpty(imageUrl))
            return;

        try
        {
            // 去掉缩略图参数，获取原图 URL
            var originalUrl = imageUrl.Split('?')[0];
            
            // 下载原图到临时文件
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var fileName = $"image_{Guid.NewGuid():N}.jpg";
            var tempFile = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            using var request = new HttpRequestMessage(HttpMethod.Get, originalUrl);
            request.Headers.Referrer = new Uri(Referer);
            
            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await FileIO.WriteBytesAsync(tempFile, bytes);
                
                // 使用系统默认图片查看器打开
                await Launcher.LaunchFileAsync(tempFile);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open image: {ex.Message}");
        }
    }
}
