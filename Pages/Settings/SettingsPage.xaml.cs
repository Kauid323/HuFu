using Microsoft.UI.Xaml.Controls;
using HuFu.Services;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HuFu.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; } = new();

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
        
        NavigationItemsList.DragItemsCompleted += NavigationItemsList_DragItemsCompleted;
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

        ViewModel.LoadNavigationItems();
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

    private void NavigationItemsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        ViewModel.SaveNavigationItems();
    }

    private void ResetNavigation_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SettingsService.ResetNavigationItems();
        ViewModel.LoadNavigationItems();
    }
}

public class SettingsPageViewModel : INotifyPropertyChanged
{
    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = new();

    public void LoadNavigationItems()
    {
        NavigationItems.Clear();
        var items = SettingsService.GetNavigationItems();
        foreach (var item in items)
        {
            NavigationItems.Add(new NavigationItemViewModel(this)
            {
                Tag = item.Tag,
                Title = item.Title,
                Icon = item.Icon,
                IsVisible = item.IsVisible,
                Order = item.Order
            });
        }
    }

    public void SaveNavigationItems()
    {
        var items = NavigationItems.Select((item, index) => new SettingsService.NavigationItemConfig
        {
            Tag = item.Tag,
            Title = item.Title,
            Icon = item.Icon,
            IsVisible = item.IsVisible,
            Order = index
        }).ToList();

        SettingsService.SaveNavigationItems(items);
        System.Diagnostics.Debug.WriteLine($"Saved {items.Count} navigation items");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class NavigationItemViewModel : INotifyPropertyChanged
{
    private readonly SettingsPageViewModel? _parent;

    public NavigationItemViewModel(SettingsPageViewModel? parent = null)
    {
        _parent = parent;
    }

    public string Tag { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged();
                // 自动保存
                _parent?.SaveNavigationItems();
            }
        }
    }

    public int Order { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
