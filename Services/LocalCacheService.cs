using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HuFu.Services;

public static class LocalCacheService
{
    private const string ConversationCacheFile = "conversations_cache.dat";
    private const string ProtectionDescriptor = "LOCAL=user";

    public static async Task SaveConversationsAsync<T>(IEnumerable<T> conversations)
    {
        try
        {
            var json = JsonSerializer.Serialize(conversations);
            var buffer = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8);
            
            var provider = new DataProtectionProvider(ProtectionDescriptor);
            var protectedBuffer = await provider.ProtectAsync(buffer);

            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(ConversationCacheFile, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBufferAsync(file, protectedBuffer);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to cache conversations: {ex.Message}");
        }
    }

    public static async Task<List<T>?> LoadConversationsAsync<T>()
    {
        try
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(ConversationCacheFile);
            var protectedBuffer = await FileIO.ReadBufferAsync(file);

            var provider = new DataProtectionProvider();
            var unprotectedBuffer = await provider.UnprotectAsync(protectedBuffer);

            var json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, unprotectedBuffer);
            return JsonSerializer.Deserialize<List<T>>(json);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load conversation cache: {ex.Message}");
            return null;
        }
    }
}
