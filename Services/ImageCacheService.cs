using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace HuFu.Services;

public static class ImageCacheService
{
    private static readonly string CacheFolderName = "ImageCache";

    public static async Task<StorageFile?> GetCachedImageAsync(string url)
    {
        try
        {
            var fileName = GetMd5Hash(url);
            var cacheFolder = await GetCacheFolderAsync();
            var item = await cacheFolder.TryGetItemAsync(fileName);
            return item as StorageFile;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking image cache: {ex.Message}");
            return null;
        }
    }

    public static async Task SaveImageToCacheAsync(string url, byte[] data)
    {
        try
        {
            var fileName = GetMd5Hash(url);
            var cacheFolder = await GetCacheFolderAsync();
            var file = await cacheFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using var stream = await file.OpenStreamForWriteAsync();
            await stream.WriteAsync(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving image to cache: {ex.Message}");
        }
    }

    private static async Task<StorageFolder> GetCacheFolderAsync()
    {
        return await ApplicationData.Current.LocalFolder.CreateFolderAsync(CacheFolderName, CreationCollisionOption.OpenIfExists);
    }

    private static string GetMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}
