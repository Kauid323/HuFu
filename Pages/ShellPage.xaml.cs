using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using HuFu.Services;

namespace HuFu.Pages;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; } = new();

    public ShellPage()
    {
        InitializeComponent();
        
        LoadNavigationItems();
        
        NavView.SelectionChanged += NavView_SelectionChanged;
        Loaded += ShellPage_Loaded;
        
        // 监听 Frame 导航事件
        ContentFrame.Navigated += ContentFrame_Navigated;
        
        // 监听导航配置变更事件
        SettingsService.NavigationConfigChanged += OnNavigationConfigChanged;

        // default
        ContentFrame.Navigate(typeof(ChatPage));
        if (NavView.MenuItems.Count > 0)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    private void OnNavigationConfigChanged(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("ShellPage: Navigation config changed event received");
        
        // 在 UI 线程上刷新
        DispatcherQueue.TryEnqueue(() =>
        {
            LoadNavigationItems();
        });
    }

    private Type? _previousPageType;

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        // 如果从设置页返回，刷新导航项
        if (_previousPageType == typeof(SettingsPage) && e.SourcePageType != typeof(SettingsPage))
        {
            LoadNavigationItems();
            
            // 重新选中当前页
            var currentTag = e.SourcePageType.Name switch
            {
                nameof(ChatPage) => "conversation",
                nameof(CommunityPage) => "community",
                nameof(ContactsPage) => "contacts",
                nameof(DiscoverPage) => "discover",
                nameof(ProfilePage) => "profile",
                _ => null
            };

            if (currentTag != null)
            {
                var item = NavView.MenuItems.Cast<NavigationViewItem>()
                    .FirstOrDefault(x => x.Tag?.ToString() == currentTag);
                if (item != null)
                {
                    NavView.SelectedItem = item;
                }
            }
        }

        _previousPageType = e.SourcePageType;
    }

    private void LoadNavigationItems()
    {
        NavView.MenuItems.Clear();
        
        var items = SettingsService.GetNavigationItems()
            .Where(x => x.IsVisible)
            .OrderBy(x => x.Order);

        foreach (var item in items)
        {
            var navItem = new NavigationViewItem
            {
                Content = item.Title,
                Tag = item.Tag
            };

            // 设置图标
            if (!string.IsNullOrEmpty(item.Icon))
            {
                // 使用 FontIcon 显示 Unicode 图标
                navItem.Icon = new FontIcon { Glyph = item.Icon };
            }

            NavView.MenuItems.Add(navItem);
        }
        
        System.Diagnostics.Debug.WriteLine($"Loaded {NavView.MenuItems.Count} navigation items");
    }

    public void RefreshNavigation()
    {
        System.Diagnostics.Debug.WriteLine("ShellPage: RefreshNavigation called");
        LoadNavigationItems();
    }

    private async void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadUserInfoAsync();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case "conversation":
                    ContentFrame.Navigate(typeof(ChatPage));
                    break;
                case "community":
                    ContentFrame.Navigate(typeof(CommunityPage));
                    break;
                case "contacts":
                    ContentFrame.Navigate(typeof(ContactsPage));
                    break;
                case "discover":
                    ContentFrame.Navigate(typeof(DiscoverPage));
                    break;
                case "profile":
                    ContentFrame.Navigate(typeof(ProfilePage));
                    break;
                case "settings":
                    ContentFrame.Navigate(typeof(SettingsPage));
                    break;
            }
        }
    }
}

public class ShellViewModel : INotifyPropertyChanged
{
    private readonly YunhuApiClient _api = new();

    private string _avatarUrl = "";
    public string AvatarUrl
    {
        get => _avatarUrl;
        set { _avatarUrl = value; OnPropertyChanged(); }
    }

    public async Task LoadUserInfoAsync()
    {
        var token = SessionStore.Token;
        if (string.IsNullOrEmpty(token)) return;

        try
        {
            var userInfo = await _api.GetUserInfoAsync(token);
            if (userInfo?.Data != null)
            {
                AvatarUrl = userInfo.Data.AvatarUrl;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load shell user info: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
