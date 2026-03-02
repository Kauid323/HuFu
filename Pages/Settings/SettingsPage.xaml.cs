using Microsoft.UI.Xaml.Controls;
using HuFu.Services;
using System.Linq;

namespace HuFu.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var current = SettingsService.CurrentSendShortcut;
        var item = ShortcutComboBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => i.Tag?.ToString() == current.ToString());
        if (item != null)
        {
            ShortcutComboBox.SelectedItem = item;
        }
    }

    private void ShortcutComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ShortcutComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (System.Enum.TryParse<SettingsService.SendShortcut>(tag, out var result))
            {
                SettingsService.CurrentSendShortcut = result;
            }
        }
    }
}
