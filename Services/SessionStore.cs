using Windows.Security.Credentials;
using System.Linq;

namespace HuFu.Services;

public static class SessionStore
{
    private const string ResourceName = "HuFu_Auth";
    private const string SettingsResourceName = "HuFu_Settings";
    private const string UserName = "DefaultUser";
    private static readonly PasswordVault Vault = new();

    private static string? _token;
    private static string? _userId;

    public static string? Token
    {
        get
        {
            if (_token != null) return _token;
            try
            {
                var credentials = Vault.RetrieveAll().Where(c => c.Resource == ResourceName).ToList();
                var tokenCred = credentials.FirstOrDefault(c => c.UserName == "Token");
                if (tokenCred != null)
                {
                    tokenCred.RetrievePassword();
                    _token = tokenCred.Password;
                }
            }
            catch { }
            return _token;
        }
        set
        {
            _token = value;
            SaveCredential("Token", _token);
        }
    }

    public static string? UserId
    {
        get
        {
            if (_userId != null) return _userId;
            try
            {
                var credentials = Vault.RetrieveAll().Where(c => c.Resource == ResourceName).ToList();
                var userCred = credentials.FirstOrDefault(c => c.UserName == "UserId");
                if (userCred != null)
                {
                    userCred.RetrievePassword();
                    _userId = userCred.Password;
                }
            }
            catch { }
            return _userId;
        }
        set
        {
            _userId = value;
            SaveCredential("UserId", _userId);
        }
    }

    private static void SaveCredential(string userName, string? password)
    {
        try
        {
            // Remove existing for this specific userName
            var existing = Vault.RetrieveAll().Where(c => c.Resource == ResourceName && c.UserName == userName).ToList();
            foreach (var c in existing)
            {
                Vault.Remove(c);
            }

            if (!string.IsNullOrEmpty(password))
            {
                Vault.Add(new PasswordCredential(ResourceName, userName, password));
            }
        }
        catch { }
    }

    /// <summary>
    /// 保存加密的设置数据
    /// </summary>
    public static void SaveSetting(string key, string? value)
    {
        try
        {
            var existing = Vault.RetrieveAll().Where(c => c.Resource == SettingsResourceName && c.UserName == key).ToList();
            foreach (var c in existing)
            {
                Vault.Remove(c);
            }

            if (!string.IsNullOrEmpty(value))
            {
                Vault.Add(new PasswordCredential(SettingsResourceName, key, value));
            }
        }
        catch { }
    }

    /// <summary>
    /// 读取加密的设置数据
    /// </summary>
    public static string? GetSetting(string key)
    {
        try
        {
            var credentials = Vault.RetrieveAll().Where(c => c.Resource == SettingsResourceName).ToList();
            var cred = credentials.FirstOrDefault(c => c.UserName == key);
            if (cred != null)
            {
                cred.RetrievePassword();
                return cred.Password;
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// 删除设置数据
    /// </summary>
    public static void RemoveSetting(string key)
    {
        try
        {
            var existing = Vault.RetrieveAll().Where(c => c.Resource == SettingsResourceName && c.UserName == key).ToList();
            foreach (var c in existing)
            {
                Vault.Remove(c);
            }
        }
        catch { }
    }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public static void Logout()
    {
        Token = null;
        UserId = null;
    }
}
