using Windows.Security.Credentials;
using System.Linq;

namespace HuFu.Services;

public static class SessionStore
{
    private const string ResourceName = "HuFu_Auth";
    private const string UserName = "DefaultUser";
    private static readonly PasswordVault Vault = new();

    private static string? _token;
    public static string? Token
    {
        get
        {
            if (_token != null) return _token;
            try
            {
                var credential = Vault.RetrieveAll().FirstOrDefault(c => c.Resource == ResourceName);
                if (credential != null)
                {
                    credential.RetrievePassword();
                    _token = credential.Password;
                }
            }
            catch { }
            return _token;
        }
        set
        {
            _token = value;
            try
            {
                // Clear existing
                foreach (var c in Vault.RetrieveAll().Where(c => c.Resource == ResourceName))
                {
                    Vault.Remove(c);
                }

                if (!string.IsNullOrEmpty(_token))
                {
                    Vault.Add(new PasswordCredential(ResourceName, UserName, _token));
                }
            }
            catch { }
        }
    }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public static void Logout()
    {
        Token = null;
    }
}
