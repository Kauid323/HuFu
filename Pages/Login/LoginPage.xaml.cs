using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HuFu.ViewModels;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Navigation;

namespace HuFu.Pages;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel { get; } = new();

    public LoginPage()
    {
        InitializeComponent();

        ViewModel.LoginSucceeded += ViewModel_LoginSucceeded;

        Loaded += LoginPage_Loaded;
    }

    private void ViewModel_LoginSucceeded(object? sender, EventArgs e)
    {
        Frame.Navigate(typeof(ShellPage));
    }

    private async void LoginPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= LoginPage_Loaded;
        await ViewModel.RefreshCaptchaAsync();
        await UpdateCaptchaImageAsync();
    }

    private void EmailPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            ViewModel.EmailPassword = pb.Password;
        }
    }

    private async void RefreshCaptcha_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshCaptchaAsync();
        await UpdateCaptchaImageAsync();
    }

    private async void RequestSmsCode_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RequestSmsCodeAsync();
    }

    private Task UpdateCaptchaImageAsync()
    {
        try
        {
            var b64 = ViewModel.CaptchaBase64;
            if (string.IsNullOrWhiteSpace(b64))
            {
                CaptchaImage.Source = null;
                return Task.CompletedTask;
            }

            // Android side uses substringAfter(",") which implies possible data URI prefix.
            var commaIndex = b64.IndexOf(',');
            if (commaIndex >= 0)
            {
                b64 = b64[(commaIndex + 1)..];
            }

            var bytes = Convert.FromBase64String(b64);

            var bitmap = new BitmapImage();
            using var ms = new MemoryStream(bytes);
            bitmap.SetSource(ms.AsRandomAccessStream());
            CaptchaImage.Source = bitmap;
        }
        catch
        {
            CaptchaImage.Source = null;
        }

        return Task.CompletedTask;
    }

    private async void EmailLogin_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoginWithEmailAsync();
    }

    private async void SmsLogin_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoginWithSmsAsync();
    }
}
