using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuFu.Services;

namespace HuFu.Pages;

public sealed partial class ChatPage : Page
{
    public ChatViewModel ViewModel { get; } = new();

    public ChatPage()
    {
        InitializeComponent();
        Loaded += ChatPage_Loaded;
    }

    private async void ChatPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadConversationsAsync();
    }
}

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly YunhuApiClient _api = new();

    private ConversationDisplayItem? _selectedConversation;
    public ConversationDisplayItem? SelectedConversation
    {
        get => _selectedConversation;
        set { _selectedConversation = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ConversationDisplayItem> Conversations { get; } = new();

    public async Task LoadConversationsAsync()
    {
        var token = SessionStore.Token;
        if (string.IsNullOrEmpty(token)) return;

        IsBusy = true;
        try
        {
            var list = await _api.GetConversationListAsync(token);
            Conversations.Clear();
            
            foreach (var item in list.Data)
            {
                Conversations.Add(new ConversationDisplayItem
                {
                    ChatId = item.ChatId,
                    Name = item.Name,
                    LastMessage = item.ChatContent,
                    AvatarUrl = item.AvatarUrl,
                    TimeString = FormatTimestamp(item.TimestampMs)
                });
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
}
