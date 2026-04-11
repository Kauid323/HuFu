using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using HuFu.Pages;
using HuFu.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace HuFu.Controls;

public sealed partial class ChatDetailControl : UserControl
{
    private bool _isInfoPanelOpen = false;
    private ContentDialog? _currentDialog = null;

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

    private async void VoiceRoomButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedConversation == null)
            return;

        var chatId = ViewModel.SelectedConversation.ChatId;
        var token = SessionStore.Token;
        
        if (string.IsNullOrEmpty(token))
            return;

        try
        {
            var api = new YunhuApiClient();
            var response = await api.GetGroupVoiceRoomsAsync(token, chatId);

            if (response.Code != 1 || response.Data == null)
            {
                await ShowErrorDialogAsync("获取语音房间失败", response.Msg ?? "未知错误");
                return;
            }

            await ShowVoiceRoomListDialogAsync(response.Data.Rooms);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync("获取语音房间失败", ex.Message);
        }
    }

    private async Task ShowVoiceRoomListDialogAsync(YunhuApiClient.VoiceRoomInfo[] rooms)
    {
        var dialog = new ContentDialog
        {
            Title = "语音房间列表",
            CloseButtonText = "关闭",
            XamlRoot = this.XamlRoot
        };

        _currentDialog = dialog;

        if (rooms.Length == 0)
        {
            dialog.Content = new TextBlock
            {
                Text = "当前没有活跃的语音房间",
                Margin = new Thickness(0, 12, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }
        else
        {
            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 400,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var stackPanel = new StackPanel
            {
                Spacing = 8
            };

            foreach (var room in rooms)
            {
                var roomCard = CreateRoomCard(room);
                stackPanel.Children.Add(roomCard);
            }

            scrollViewer.Content = stackPanel;
            dialog.Content = scrollViewer;
        }

        await dialog.ShowAsync();
        _currentDialog = null;
    }

    private async void JoinVoiceRoom_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not YunhuApiClient.VoiceRoomInfo room)
            return;

        var token = SessionStore.Token;
        if (string.IsNullOrEmpty(token))
            return;

        // 先关闭房间列表对话框
        _currentDialog?.Hide();

        try
        {
            button.IsEnabled = false;
            button.Content = "加入中...";

            // TODO: 实现 LiveKit 语音房间功能
            // var voiceService = new VoiceRoomService();
            // await voiceService.JoinRoomAsync(token, room.RoomId, room.ChatId);

            await ShowErrorDialogAsync("提示", "语音房间功能开发中");
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync("加入失败", ex.Message);
        }
        finally
        {
            button.IsEnabled = true;
            button.Content = "加入";
        }
    }

    private Border CreateRoomCard(YunhuApiClient.VoiceRoomInfo room)
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 4)
        };

        var grid = new Grid
        {
            ColumnSpacing = 12
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // 头像
        var avatar = new Microsoft.UI.Xaml.Controls.PersonPicture
        {
            Width = 48,
            Height = 48,
            DisplayName = room.Nickname,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(avatar, 0);
        grid.Children.Add(avatar);

        // 信息栏
        var infoStack = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        var titleText = new TextBlock
        {
            Text = room.Title,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        infoStack.Children.Add(titleText);

        var creatorText = new TextBlock
        {
            Text = $"创建者: {room.Nickname}",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
        };
        infoStack.Children.Add(creatorText);

        var countText = new TextBlock
        {
            Text = $"参与人数: {room.Count}",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
        };
        infoStack.Children.Add(countText);

        var timeText = new TextBlock
        {
            Text = $"创建时间: {DateTimeOffset.FromUnixTimeSeconds(room.CreateTime).ToLocalTime():yyyy-MM-dd HH:mm:ss}",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
        };
        infoStack.Children.Add(timeText);

        Grid.SetColumn(infoStack, 1);
        grid.Children.Add(infoStack);

        // 加入按钮
        var joinButton = new Button
        {
            Content = "加入",
            VerticalAlignment = VerticalAlignment.Center,
            Tag = room
        };
        joinButton.Click += JoinVoiceRoom_Click;
        Grid.SetColumn(joinButton, 2);
        grid.Children.Add(joinButton);

        border.Child = grid;
        return border;
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
