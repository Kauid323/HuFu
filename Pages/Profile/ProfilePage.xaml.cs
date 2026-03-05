using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuFu.Services;
using Microsoft.UI.Xaml;

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

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "退出登录",
            Content = "确定要退出登录吗？",
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // 清除登录信息
            SessionStore.Token = string.Empty;
            SessionStore.UserId = string.Empty;
            
            // 导航到登录页
            if (this.Frame != null)
            {
                // 先导航到根 Frame
                var rootFrame = Window.Current?.Content as Frame;
                if (rootFrame != null)
                {
                    rootFrame.Navigate(typeof(LoginPage));
                }
                else
                {
                    // 如果找不到根 Frame，尝试从当前 Frame 导航
                    this.Frame.Navigate(typeof(LoginPage));
                }
            }
        }
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

        private string _email = "";
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _coin = "0";
        public string Coin
        {
            get => _coin;
            set { _coin = value; OnPropertyChanged(); }
        }

        private string _vipStatus = "未开通";
        public string VipStatus
        {
            get => _vipStatus;
            set { _vipStatus = value; OnPropertyChanged(); }
        }

        private string _vipExpiry = "";
        public string VipExpiry
        {
            get => _vipExpiry;
            set { _vipExpiry = value; OnPropertyChanged(); }
        }

        private string _invitationCode = "";
        public string InvitationCode
        {
            get => _invitationCode;
            set { _invitationCode = value; OnPropertyChanged(); }
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
                    var data = userInfo.Data;
                    Nickname = data.Name;
                    AvatarUrl = data.AvatarUrl;
                    Uid = data.Id;
                    SessionStore.UserId = data.Id;
                    Mobile = string.IsNullOrEmpty(data.Phone) ? "未绑定" : data.Phone;
                    Email = string.IsNullOrEmpty(data.Email) ? "未绑定" : data.Email;
                    Coin = data.Coin.ToString("F2");
                    
                    if (data.IsVip == 1)
                    {
                        VipStatus = "尊贵会员";
                        var expiryDate = DateTimeOffset.FromUnixTimeMilliseconds(data.VipExpiredTime).ToLocalTime();
                        VipExpiry = expiryDate.ToString("yyyy-MM-dd 到期");
                    }
                    else
                    {
                        VipStatus = "普通用户";
                        VipExpiry = "";
                    }

                    InvitationCode = data.InvitationCode;
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
