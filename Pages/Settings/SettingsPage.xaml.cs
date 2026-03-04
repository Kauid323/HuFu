using Microsoft.UI.Xaml.Controls;
using HuFu.Services;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Threading.Tasks;
using WinRT;
using Microsoft.UI.Xaml;

namespace HuFu.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; } = new();
    private HashSet<VirtualKey> _pressedKeys = new();
    private string _customShortcut = string.Empty;
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
        _isInitializing = false;
        
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
        
        // 检查并修复旧的图标数据（如果是文字则重置）
        var needsReset = ViewModel.NavigationItems.Any(item => 
            item.Icon.Length > 2 || // Unicode 字符通常是 1-2 个字符
            item.Icon.Contains("Find") || 
            item.Icon.Contains("People") || 
            item.Icon.Contains("Contact") || 
            item.Icon.Contains("Message"));
        
        if (needsReset)
        {
            SettingsService.ResetNavigationItems();
            ViewModel.LoadNavigationItems();
        }
        
        ViewModel.UpdateShortcutDisplay();
    }

    private async void ShortcutComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 避免初始化时触发
        if (_isInitializing) return;
        
        if (ShortcutComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (tag == "Custom")
            {
                // 显示自定义快捷键对话框
                _pressedKeys.Clear();
                _customShortcut = string.Empty;
                
                // 创建对话框内容
                var keysTextBlock = new TextBlock
                {
                    Text = "等待输入...",
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 20, 0, 20)
                };
                
                var stackPanel = new StackPanel
                {
                    Spacing = 12
                };
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = "请按下您想要设置的快捷键组合",
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                });
                stackPanel.Children.Add(keysTextBlock);
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = "提示：可以使用 Ctrl、Shift、Alt 等修饰键组合其他按键",
                    FontSize = 12,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"],
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                });
                
                var dialog = new ContentDialog
                {
                    Title = "自定义快捷键",
                    Content = stackPanel,
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary
                };
                
                // 设置 XamlRoot - 从 ShortcutComboBox 获取
                dialog.XamlRoot = ShortcutComboBox.XamlRoot;
                
                // 添加键盘事件监听
                KeyEventHandler keyDownHandler = (s, args) =>
                {
                    args.Handled = true;
                    var key = args.Key;
                    
                    // 添加所有按键到集合
                    _pressedKeys.Add(key);
                    UpdateKeyDisplay(keysTextBlock);
                };
                
                KeyEventHandler keyUpHandler = (s, args) =>
                {
                    args.Handled = true;
                    _pressedKeys.Remove(args.Key);
                };
                
                dialog.KeyDown += keyDownHandler;
                dialog.KeyUp += keyUpHandler;
                
                var result = await dialog.ShowAsync();
                
                // 移除键盘事件监听
                dialog.KeyDown -= keyDownHandler;
                dialog.KeyUp -= keyUpHandler;
                
                if (result != ContentDialogResult.Primary || string.IsNullOrEmpty(_customShortcut))
                {
                    // 用户取消或未设置，恢复之前的选择
                    LoadSettings();
                }
                else
                {
                    // 保存自定义快捷键
                    SettingsService.CurrentSendShortcut = SettingsService.SendShortcut.Custom;
                    SettingsService.CustomShortcutValue = _customShortcut;
                    ViewModel.CurrentShortcutDisplay = _customShortcut;
                }
            }
            else if (System.Enum.TryParse<SettingsService.SendShortcut>(tag, out var result))
            {
                SettingsService.CurrentSendShortcut = result;
                ViewModel.UpdateShortcutDisplay();
            }
        }
    }

    private void UpdateKeyDisplay(TextBlock textBlock)
    {
        if (_pressedKeys.Count == 0)
        {
            textBlock.Text = "等待输入...";
            _customShortcut = string.Empty;
            return;
        }

        var keys = new List<string>();
        
        // 检查修饰键
        if (_pressedKeys.Contains(VirtualKey.Control))
            keys.Add("Ctrl");
        if (_pressedKeys.Contains(VirtualKey.Shift))
            keys.Add("Shift");
        if (_pressedKeys.Contains(VirtualKey.Menu))
            keys.Add("Alt");
        if (_pressedKeys.Contains(VirtualKey.LeftWindows) || _pressedKeys.Contains(VirtualKey.RightWindows))
            keys.Add("Win");
        
        // 添加其他按键
        foreach (var key in _pressedKeys)
        {
            if (key != VirtualKey.Control && key != VirtualKey.Shift && 
                key != VirtualKey.Menu && key != VirtualKey.LeftWindows && 
                key != VirtualKey.RightWindows)
            {
                keys.Add(GetKeyName(key));
            }
        }
        
        _customShortcut = string.Join(" + ", keys);
        textBlock.Text = _customShortcut;
    }

    private string GetKeyName(VirtualKey key)
    {
        // 特殊按键映射
        var specialKeys = new Dictionary<VirtualKey, string>
        {
            { VirtualKey.Control, "Ctrl" },
            { VirtualKey.Shift, "Shift" },
            { VirtualKey.Menu, "Alt" },
            { VirtualKey.LeftWindows, "Win" },
            { VirtualKey.RightWindows, "Win" },
            { VirtualKey.Enter, "Enter" },
            { VirtualKey.Space, "Space" },
            { VirtualKey.Tab, "Tab" },
            { VirtualKey.Escape, "Esc" },
            { VirtualKey.Back, "Backspace" },
            { VirtualKey.Delete, "Delete" },
            { VirtualKey.Insert, "Insert" },
            { VirtualKey.Home, "Home" },
            { VirtualKey.End, "End" },
            { VirtualKey.PageUp, "PageUp" },
            { VirtualKey.PageDown, "PageDown" },
            { VirtualKey.Left, "Left" },
            { VirtualKey.Right, "Right" },
            { VirtualKey.Up, "Up" },
            { VirtualKey.Down, "Down" }
        };
        
        if (specialKeys.ContainsKey(key))
            return specialKeys[key];
        
        // F1-F12
        if (key >= VirtualKey.F1 && key <= VirtualKey.F12)
            return $"F{(int)key - (int)VirtualKey.F1 + 1}";
        
        // 数字和字母
        if ((key >= VirtualKey.Number0 && key <= VirtualKey.Number9) ||
            (key >= VirtualKey.A && key <= VirtualKey.Z))
            return ((char)key).ToString();
        
        // 其他按键直接返回名称
        return key.ToString();
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

    private async void OpenImageCache_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var cacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            var imageFolder = await cacheFolder.CreateFolderAsync("Images", Windows.Storage.CreationCollisionOption.OpenIfExists);
            await Windows.System.Launcher.LaunchFolderAsync(imageFolder);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open image cache folder: {ex.Message}");
        }
    }
}

public class SettingsPageViewModel : INotifyPropertyChanged
{
    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = new();

    private string _appVersion = "1.0.0";
    public string AppVersion
    {
        get => _appVersion;
        set
        {
            if (_appVersion != value)
            {
                _appVersion = value;
                OnPropertyChanged();
            }
        }
    }

    private int _visibleItemsCount;
    public int VisibleItemsCount
    {
        get => _visibleItemsCount;
        set
        {
            if (_visibleItemsCount != value)
            {
                _visibleItemsCount = value;
                OnPropertyChanged();
            }
        }
    }

    private string _currentShortcutDisplay = "Enter";
    public string CurrentShortcutDisplay
    {
        get => _currentShortcutDisplay;
        set
        {
            if (_currentShortcutDisplay != value)
            {
                _currentShortcutDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    public void LoadNavigationItems()
    {
        NavigationItems.Clear();
        var items = SettingsService.GetNavigationItems();
        foreach (var item in items)
        {
            var viewModel = new NavigationItemViewModel(this)
            {
                Tag = item.Tag,
                Title = item.Title,
                Icon = item.Icon,
                IsVisible = item.IsVisible,
                Order = item.Order
            };
            viewModel.PropertyChanged += NavigationItem_PropertyChanged;
            NavigationItems.Add(viewModel);
        }
        UpdateVisibleItemsCount();
    }

    private void NavigationItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NavigationItemViewModel.IsVisible))
        {
            UpdateVisibleItemsCount();
        }
    }

    private void UpdateVisibleItemsCount()
    {
        VisibleItemsCount = NavigationItems.Count(item => item.IsVisible);
    }

    public void UpdateShortcutDisplay()
    {
        var current = SettingsService.CurrentSendShortcut;
        CurrentShortcutDisplay = current switch
        {
            SettingsService.SendShortcut.Enter => "Enter",
            SettingsService.SendShortcut.CtrlEnter => "Ctrl+Enter",
            SettingsService.SendShortcut.Custom => SettingsService.CustomShortcutValue,
            _ => "Enter"
        };
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
