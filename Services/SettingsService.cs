using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HuFu.Services;

public static class SettingsService
{
    private const string SendShortcutKey = "SendShortcut";
    private const string NavigationItemsKey = "NavigationItems";
    
    public enum SendShortcut
    {
        Enter,
        CtrlEnter
    }

    public static SendShortcut CurrentSendShortcut
    {
        get
        {
            var value = SessionStore.GetSetting(SendShortcutKey);
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var intValue))
            {
                return (SendShortcut)intValue;
            }
            return SendShortcut.Enter; // 默认 Enter
        }
        set
        {
            SessionStore.SaveSetting(SendShortcutKey, ((int)value).ToString());
        }
    }

    public class NavigationItemConfig
    {
        public string Tag { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public int Order { get; set; }
    }

    private static readonly List<NavigationItemConfig> DefaultNavigationItems = new()
    {
        new() { Tag = "conversation", Title = "会话", Icon = "Message", IsVisible = true, Order = 0 },
        new() { Tag = "community", Title = "社群", Icon = "People", IsVisible = true, Order = 1 },
        new() { Tag = "contacts", Title = "联系人", Icon = "Contact", IsVisible = true, Order = 2 },
        new() { Tag = "discover", Title = "发现", Icon = "Find", IsVisible = true, Order = 3 },
    };

    // 导航配置变更事件
    public static event System.EventHandler? NavigationConfigChanged;

    public static List<NavigationItemConfig> GetNavigationItems()
    {
        var json = SessionStore.GetSetting(NavigationItemsKey);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<NavigationItemConfig>>(json);
                if (items != null && items.Count > 0)
                {
                    return items.OrderBy(x => x.Order).ToList();
                }
            }
            catch
            {
                // 解析失败，返回默认值
            }
        }
        return new List<NavigationItemConfig>(DefaultNavigationItems);
    }

    public static void SaveNavigationItems(List<NavigationItemConfig> items)
    {
        var json = JsonSerializer.Serialize(items);
        SessionStore.SaveSetting(NavigationItemsKey, json);
        
        // 触发变更事件
        NavigationConfigChanged?.Invoke(null, System.EventArgs.Empty);
    }

    public static void ResetNavigationItems()
    {
        SessionStore.RemoveSetting(NavigationItemsKey);
        
        // 触发变更事件
        NavigationConfigChanged?.Invoke(null, System.EventArgs.Empty);
    }
}
