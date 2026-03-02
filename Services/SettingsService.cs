using Windows.Storage;

namespace HuFu.Services;

public static class SettingsService
{
    private const string SendShortcutKey = "SendShortcut";
    
    public enum SendShortcut
    {
        Enter,
        CtrlEnter
    }

    public static SendShortcut CurrentSendShortcut
    {
        get
        {
            var value = ApplicationData.Current.LocalSettings.Values[SendShortcutKey];
            if (value is int intValue)
            {
                return (SendShortcut)intValue;
            }
            return SendShortcut.Enter; // 默认 Enter
        }
        set
        {
            ApplicationData.Current.LocalSettings.Values[SendShortcutKey] = (int)value;
        }
    }
}
