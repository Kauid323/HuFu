using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuFu.Services;

namespace HuFu.Pages;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; } = new();

    public ShellPage()
    {
        InitializeComponent();
        NavView.SelectionChanged += NavView_SelectionChanged;

        Loaded += ShellPage_Loaded;

        // default
        ContentFrame.Navigate(typeof(ChatPage));
        NavView.SelectedItem = NavView.MenuItems[0];
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
