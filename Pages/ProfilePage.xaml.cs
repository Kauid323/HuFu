using Microsoft.UI.Xaml.Controls;
using HuFu.Services;

namespace HuFu.Pages;

public sealed partial class ProfilePage : Page
{
    public string TokenText => $"Token: {SessionStore.Token}";

    public ProfilePage()
    {
        InitializeComponent();
    }
}
