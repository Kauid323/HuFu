using Windows.Security.Credentials;
using System.Linq;

namespace HuFu.Services;

public static class SessionStore
{
    private const string ResourceName = "HuFu_Auth";
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

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public static void Logout()
    {
        Token = null;
        UserId = null;
    }
}
