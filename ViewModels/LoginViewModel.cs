using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuFu.Services;

namespace HuFu.ViewModels;

public sealed class LoginViewModel : INotifyPropertyChanged
{
    private readonly YunhuApiClient _api = new();

    public event EventHandler? LoginSucceeded;

    private YunhuApiClient.CaptchaData? _captcha;
    public YunhuApiClient.CaptchaData? Captcha
    {
        get => _captcha;
        private set { _captcha = value; OnPropertyChanged(); OnPropertyChanged(nameof(CaptchaBase64)); OnPropertyChanged(nameof(CaptchaId)); }
    }

    public string CaptchaBase64 => Captcha?.B64s ?? string.Empty;
    public string CaptchaId => Captcha?.Id ?? string.Empty;

    private string _imageCaptchaCode = string.Empty;
    public string ImageCaptchaCode
    {
        get => _imageCaptchaCode;
        set { _imageCaptchaCode = value; OnPropertyChanged(); }
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

    private string _emailPassword = string.Empty;
    public string EmailPassword
    {
        get => _emailPassword;
        set { _emailPassword = value; OnPropertyChanged(); }
    }

    private string _mobile = string.Empty;
    public string Mobile
    {
        get => _mobile;
        set { _mobile = value; OnPropertyChanged(); }
    }

    private string _smsCode = string.Empty;
    public string SmsCode
    {
        get => _smsCode;
        set { _smsCode = value; OnPropertyChanged(); }
    }

    private string _deviceId = "winui3-";
    public string DeviceId
    {
        get => _deviceId;
        set { _deviceId = value; OnPropertyChanged(); }
    }

    private string _platform = "windows";
    public string Platform
    {
        get => _platform;
        set { _platform = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public async Task LoginWithEmailAsync()
    {
        await RunLoginAsync(() => _api.LoginWithEmailAsync(Email, EmailPassword, DeviceId, Platform));
    }

    public async Task LoginWithSmsAsync()
    {
        await RunLoginAsync(() => _api.LoginWithSmsAsync(Mobile, SmsCode, DeviceId, Platform));
    }

    public async Task RefreshCaptchaAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            Captcha = await _api.GetCaptchaAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RequestSmsCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(Mobile) || string.IsNullOrWhiteSpace(ImageCaptchaCode))
        {
            ErrorMessage = "请输入手机号和图片验证码";
            return;
        }

        if (Captcha is null)
        {
            ErrorMessage = "请先获取图片验证码";
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            await _api.GetSmsVerificationCodeAsync(Mobile, ImageCaptchaCode, Captcha.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunLoginAsync(Func<Task<string>> loginFunc)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var token = await loginFunc();
            SessionStore.Token = token;
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
