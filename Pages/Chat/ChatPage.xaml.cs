using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuFu.Services;
using Microsoft.UI.Xaml;
using System.Linq;
using Msg;

namespace HuFu.Pages;

public sealed partial class ChatPage : Page
{
    public ChatViewModel ViewModel { get; } = new();

    public ChatPage()
    {
        InitializeComponent();
        Loaded += ChatPage_Loaded;
        HuFu.Services.MemoryManager.StartMonitoring();
    }

    private ScrollViewer? GetScrollViewer(DependencyObject element)
    {
        if (element is ScrollViewer sv) return sv;
        for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);
            var result = GetScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }

    private void OnMessageListScroll(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            // 只有当滚动停止且处于顶部时触发
            if (!e.IsIntermediate && sv.VerticalOffset < 1.0)
            {
                System.Diagnostics.Debug.WriteLine("Top reached, triggering LoadMoreMessagesAsync");
                _ = ViewModel.LoadMoreMessagesAsync();
            }
        }
    }

    private void ChatPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // 尝试获取 ScrollViewer
        var scrollViewer = GetScrollViewer(MessageListView);
        if (scrollViewer != null)
        {
            scrollViewer.ViewChanged += OnMessageListScroll;
            System.Diagnostics.Debug.WriteLine("ScrollViewer found and event attached.");
        }
        else
        {
            // 如果加载时还没生成，监听 LayoutUpdated 再次尝试
            MessageListView.LayoutUpdated += MessageListView_LayoutUpdated;
            System.Diagnostics.Debug.WriteLine("ScrollViewer NOT found initially, waiting for LayoutUpdated.");
        }
        _ = ViewModel.LoadConversationsAsync();
    }

    private void MessageListView_LayoutUpdated(object? sender, object e)
    {
        var scrollViewer = GetScrollViewer(MessageListView);
        if (scrollViewer != null)
        {
            MessageListView.LayoutUpdated -= MessageListView_LayoutUpdated;
            scrollViewer.ViewChanged += OnMessageListScroll;
            System.Diagnostics.Debug.WriteLine("ScrollViewer found in LayoutUpdated and event attached.");
        }
    }
}

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly YunhuApiClient _api = new();

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
        // 1. 先从本地缓存加载，实现秒开
        var cached = await LocalCacheService.LoadConversationsAsync<ConversationDisplayItem>();
        if (cached != null && cached.Count > 0)
        {
            Conversations.Clear();
            foreach (var item in cached) Conversations.Add(item);
            if (SelectedConversation is null) SelectedConversation = Conversations[0];
        }

        var token = SessionStore.Token;
        if (string.IsNullOrEmpty(token)) return;

        IsBusy = true;
        try
        {
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

            // 2. 更新 UI 并保存到加密缓存
            Conversations.Clear();
            foreach (var item in newList) Conversations.Add(item);
            await LocalCacheService.SaveConversationsAsync(newList);

            if (SelectedConversation is null && Conversations.Count > 0)
            {
                SelectedConversation = Conversations[0];
            }
        }
        catch (Exception ex)
        {
            // TODO: Handle error
            System.Diagnostics.Debug.WriteLine($"Failed to load conversations: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
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
    public string SenderName { get; set; } = string.Empty;
    public string SenderAvatarUrl { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string TimeString { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
}
