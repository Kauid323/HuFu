using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuFu.Services;
using HuFu.Helpers;
using Microsoft.UI.Xaml;
using System.Linq;
using Msg;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace HuFu.Pages;

public sealed partial class ChatPage : Page
{
    public ChatViewModel ViewModel { get; } = new();
    private MessageScrollHelper? _scrollHelper;

    public ChatPage()
    {
        InitializeComponent();
        Loaded += ChatPage_Loaded;
        Unloaded += ChatPage_Unloaded;
        HuFu.Services.MemoryManager.StartMonitoring();
    }

    private ListView? GetMessageListView()
    {
        if (Content is Grid root)
        {
            // ChatDetailControl is in column 2
            foreach (var child in root.Children)
            {
                if (child is HuFu.Controls.ChatDetailControl detail)
                {
                    return detail.MessageList;
                }
            }
        }
        return null;
    }

    private async void ChatPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _scrollHelper?.Cleanup();
        // 页面卸载时自动保存会话列表到缓存
        await ViewModel.SaveConversationsToCacheAsync();
    }

    private bool _isResizing = false;

    private void Splitter_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast);
    }

    private void Splitter_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isResizing)
        {
            ProtectedCursor = null;
        }
    }

    private void Splitter_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.CapturePointer(e.Pointer);
            _isResizing = true;
        }
    }

    private void Splitter_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isResizing)
        {
            var pointerPosition = e.GetCurrentPoint(this).Position;
            double newWidth = pointerPosition.X;
            
            // 考虑 MinWidth 和 MaxWidth
            if (newWidth >= ConversationColumn.MinWidth && newWidth <= ConversationColumn.MaxWidth)
            {
                ConversationColumn.Width = new GridLength(newWidth);
            }
        }
    }

    private void Splitter_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isResizing && sender is FrameworkElement element)
        {
            element.ReleasePointerCapture(e.Pointer);
            _isResizing = false;
            ProtectedCursor = null;
        }
    }

    private void ChatPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var messageList = GetMessageListView();
        if (messageList is not null)
        {
            _scrollHelper = new MessageScrollHelper(messageList, () => ViewModel.LoadMoreMessagesAsync());
            _scrollHelper.Setup();
        }
        _ = ViewModel.LoadConversationsAsync();
    }

    private ConversationDisplayItem? _rightTappedConversation;

    private void ConversationListView_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element)
        {
            _rightTappedConversation = element.DataContext as ConversationDisplayItem;
        }
    }

    private void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
    {
        if (_rightTappedConversation is not null)
        {
            var window = new ChatDetailWindow(_rightTappedConversation);
            window.Activate();
        }
        _rightTappedConversation = null;
    }
}

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly YunhuApiClient _api = new();

    public string CurrentUserId { get; } = SessionStore.UserId;

    public ChatViewModel()
    {
        UpdateConversationHeader();
    }

    private ConversationDisplayItem? _selectedConversation;
    public ConversationDisplayItem? SelectedConversation
    {
        get => _selectedConversation;
        set
        {
            if (ReferenceEquals(_selectedConversation, value)) return;
            _selectedConversation = value;
            OnPropertyChanged();
            UpdateConversationHeader();
            _ = LoadMessagesForSelectedAsync();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    private bool _isMessageBusy;
    public bool IsMessageBusy
    {
        get => _isMessageBusy;
        set
        {
            _isMessageBusy = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ConversationDisplayItem> Conversations { get; } = new();
    public ObservableCollection<MessageDisplayItem> Messages { get; } = new();

    private string _activeConversationTitle = "";
    public string ActiveConversationTitle
    {
        get => _activeConversationTitle;
        private set { _activeConversationTitle = value; OnPropertyChanged(); }
    }

    private string _activeConversationSubtitle = "";
    public string ActiveConversationSubtitle
    {
        get => _activeConversationSubtitle;
        private set { _activeConversationSubtitle = value; OnPropertyChanged(); }
    }

    private Visibility _emptyHintVisibility = Visibility.Visible;
    public Visibility EmptyHintVisibility
    {
        get => _emptyHintVisibility;
        private set { _emptyHintVisibility = value; OnPropertyChanged(); }
    }

    public async Task LoadConversationsAsync()
    {
        var token = SessionStore.Token;
        if (string.IsNullOrEmpty(token)) return;

        IsBusy = true;
        try
        {
            // 1. 优先尝试从网络加载
            var list = await _api.GetConversationListAsync(token);
            var newList = new List<ConversationDisplayItem>();
            
            foreach (var item in list.Data)
            {
                newList.Add(new ConversationDisplayItem
                {
                    ChatId = item.ChatId,
                    ChatType = item.ChatType,
                    Name = item.Name,
                    LastMessage = item.ChatContent,
                    AvatarUrl = item.AvatarUrl,
                    TimeString = FormatTimestamp(item.TimestampMs)
                });
            }

            // 更新 UI
            Conversations.Clear();
            foreach (var item in newList) Conversations.Add(item);

            if (SelectedConversation is null && Conversations.Count > 0)
            {
                SelectedConversation = Conversations[0];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Network failed, trying local cache: {ex.Message}");
            
            // 2. 网络失败或断网时，才从本地缓存加载
            var cached = await LocalCacheService.LoadConversationsAsync<ConversationDisplayItem>();
            if (cached != null && cached.Count > 0)
            {
                Conversations.Clear();
                foreach (var item in cached) Conversations.Add(item);
                if (SelectedConversation is null) SelectedConversation = Conversations[0];
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveConversationsToCacheAsync()
    {
        if (Conversations.Count > 0)
        {
            await LocalCacheService.SaveConversationsAsync(Conversations.ToList());
        }
    }

    private void UpdateConversationHeader()
    {
        if (SelectedConversation is null)
        {
            ActiveConversationTitle = "";
            ActiveConversationSubtitle = "";
            EmptyHintVisibility = Visibility.Visible;
            return;
        }

        ActiveConversationTitle = SelectedConversation.Name;
        ActiveConversationSubtitle = "TODO: 群聊人数";
        EmptyHintVisibility = Visibility.Collapsed;
    }

    private bool _isLoadingMore;
    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        set { _isLoadingMore = value; OnPropertyChanged(); }
    }

    private string? _oldestMsgId;

    public async Task LoadMoreMessagesAsync()
    {
        if (IsLoadingMore || SelectedConversation is null || string.IsNullOrEmpty(_oldestMsgId)) return;

        var token = SessionStore.Token;
        if (string.IsNullOrEmpty(token)) return;

        IsLoadingMore = true;
        try
        {
            var resp = await _api.GetMessageListAsync(token, SelectedConversation.ChatId, (long)SelectedConversation.ChatType, 30, _oldestMsgId);
            
            if (resp.Msg.Count > 0)
            {
                // 排除掉作为起点的最老的一条消息（API 返回包含该 ID 的消息）
                var newMsgs = resp.Msg.Where(m => m.MsgId != _oldestMsgId).OrderBy(x => x.SendTime).ToList();
                
                if (newMsgs.Count > 0)
                {
                    // 插入到列表开头
                    for (int i = 0; i < newMsgs.Count; i++)
                    {
                        var m = newMsgs[i];
                        Messages.Insert(i, new MessageDisplayItem
                        {
                            MsgId = m.MsgId,
                            SenderName = m.Sender?.Name ?? string.Empty,
                            SenderAvatarUrl = m.Sender?.AvatarUrl ?? string.Empty,
                            Text = m.Content?.Text ?? string.Empty,
                            TimeString = FormatTimestamp((long)m.SendTime),
                            Direction = m.Direction ?? string.Empty,
                            // proto: Msg.direction = "消息位置,左边/右边"。服务端已标注方向，优先使用它判断是否为“我的消息”。
                            // 兼容：若 direction 为空，再回退用 sender.chat_id 与当前用户 id 判断（不同接口/场景字段可能不一致）。
                            IsMine = string.Equals(m.Direction, "right", StringComparison.OrdinalIgnoreCase)
                                    || (string.IsNullOrEmpty(m.Direction)
                                        && !string.IsNullOrEmpty(CurrentUserId)
                                        && string.Equals(m.Sender?.ChatId, CurrentUserId, StringComparison.OrdinalIgnoreCase)),
                            Tags = m.Sender?.Tag?.Select(t => new TagDisplayItem { Text = t.Text, Color = t.Color }).ToList() ?? new(),
                        });
                    }
                    _oldestMsgId = Messages.FirstOrDefault()?.MsgId;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load more messages: {ex.Message}");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    private async Task LoadMessagesForSelectedAsync()
    {
        var token = SessionStore.Token;
        if (string.IsNullOrEmpty(token)) return;
        if (SelectedConversation is null) return;

        IsMessageBusy = true;
        try
        {
            var resp = await _api.GetMessageListAsync(token, SelectedConversation.ChatId, (long)SelectedConversation.ChatType);

            Messages.Clear();

            var sortedMsgs = resp.Msg.OrderBy(x => x.SendTime).ToList();
            foreach (var m in sortedMsgs)
            {
                Messages.Add(new MessageDisplayItem
                {
                    MsgId = m.MsgId,
                    SenderName = m.Sender?.Name ?? string.Empty,
                    SenderAvatarUrl = m.Sender?.AvatarUrl ?? string.Empty,
                    Text = m.Content?.Text ?? string.Empty,
                    TimeString = FormatTimestamp((long)m.SendTime),
                    Direction = m.Direction ?? string.Empty,
                    // proto: Msg.direction = "消息位置,左边/右边"，优先使用它判断是否为“我的消息”。
                    IsMine = string.Equals(m.Direction, "right", StringComparison.OrdinalIgnoreCase)
                            || (string.IsNullOrEmpty(m.Direction)
                                && !string.IsNullOrEmpty(CurrentUserId)
                                && string.Equals(m.Sender?.ChatId, CurrentUserId, StringComparison.OrdinalIgnoreCase)),
                    Tags = m.Sender?.Tag?.Select(t => new TagDisplayItem { Text = t.Text, Color = t.Color }).ToList() ?? new(),
                });
            }
            
            _oldestMsgId = Messages.FirstOrDefault()?.MsgId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load messages: {ex.Message}");
            Messages.Clear();
            _oldestMsgId = null;
        }
        finally
        {
            IsMessageBusy = false;
        }
    }

    private string FormatTimestamp(long ms)
    {
        if (ms <= 0)
        {
            return string.Empty;
        }

        var dt = DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime();
        if (dt.Date == DateTime.Today)
            return dt.ToString("HH:mm");
        if (dt.Date == DateTime.Today.AddDays(-1))
            return "昨天";
        return dt.ToString("MM-dd");
    }

    public async Task<bool> SendMessageAsync(string text)
    {
        if (SelectedConversation is null || string.IsNullOrWhiteSpace(text)) return false;

        var token = SessionStore.Token;
        if (string.IsNullOrEmpty(token)) return false;

        try
        {
            var resp = await _api.SendMessageAsync(token, SelectedConversation.ChatId, (long)SelectedConversation.ChatType, text);
            
            if (resp.Status?.Code == 1)
            {
                System.Diagnostics.Debug.WriteLine("Message sent successfully");
                
                // 发送成功后添加消息到本地列表，而不是重新加载
                var newMsg = new MessageDisplayItem
                {
                    MsgId = Guid.NewGuid().ToString("N"),
                    SenderName = "我", // TODO: 从用户信息获取
                    SenderAvatarUrl = string.Empty,
                    Text = text,
                    TimeString = FormatTimestamp(DateTimeOffset.Now.ToUnixTimeMilliseconds()),
                    Direction = "right",
                    IsMine = true,
                    Tags = new()
                };
                
                Messages.Add(newMsg);
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send message: {resp.Status?.Msg}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending message: {ex.Message}");
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ConversationDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public string TimeString { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public int ChatType { get; set; }
}

public class MessageDisplayItem
{
    public string MsgId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderAvatarUrl { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string TimeString { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public bool IsMine { get; set; }

    public List<TagDisplayItem> Tags { get; set; } = new();
}

public class TagDisplayItem
{
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
