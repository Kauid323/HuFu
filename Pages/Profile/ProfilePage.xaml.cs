using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuFu.Services;

namespace HuFu.Pages;

public sealed partial class ProfilePage : Page
{
    public ProfileViewModel ViewModel { get; } = new();

    public ProfilePage()
    {
        InitializeComponent();
        Loaded += ProfilePage_Loaded;
    }

    private async void ProfilePage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadUserInfoAsync();
    }
}

public class ProfileViewModel : INotifyPropertyChanged
{
    private readonly YunhuApiClient _api = new();

    private string _nickname = "加载中...";
    public string Nickname
    {
        get => _nickname;
        set { _nickname = value; OnPropertyChanged(); }
    }

    private string _avatarUrl = "";
    public string AvatarUrl
    {
        get => _avatarUrl;
        set { _avatarUrl = value; OnPropertyChanged(); }
    }

    private string _uid = "";
    public string Uid
    {
        get => _uid;
        set { _uid = value; OnPropertyChanged(); }
    }

    private string _mobile = "";
    public string Mobile
    {
        get => _mobile;
        set { _mobile = value; OnPropertyChanged(); }
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
                Nickname = userInfo.Data.Name;
                AvatarUrl = userInfo.Data.AvatarUrl;
                Uid = userInfo.Data.Id;
                Mobile = userInfo.Data.Phone;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load user info: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
