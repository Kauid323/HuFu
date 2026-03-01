using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using HuFu.Services;

namespace HuFu.Converters;

public class RefererImageConverter : IValueConverter
{
    private static readonly HttpClient _httpClient = new(new HttpClientHandler { UseProxy = false });
    private const string Referer = "https://myapp.jwznb.com";

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string url && !string.IsNullOrEmpty(url))
        {
            var bitmap = new BitmapImage();
            _ = LoadImageAsync(bitmap, url);
            return bitmap;
        }
        return null;
    }

    private static async Task LoadImageAsync(BitmapImage bitmap, string url)
    {
        try
        {
            // 1. 尝试从本地缓存获取
            var cachedFile = await ImageCacheService.GetCachedImageAsync(url);
            if (cachedFile != null)
            {
                using var stream = await cachedFile.OpenReadAsync();
                await bitmap.SetSourceAsync(stream);
                return;
            }

            // 2. 拼接七牛云压缩参数
            var finalUrl = url.Contains("?") ? $"{url}&imageView2/2/w/60/h/60" : $"{url}?imageView2/2/w/60/h/60";

            using var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);
            request.Headers.Referrer = new Uri(Referer);
            
            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                
                // 3. 保存到本地缓存
                await ImageCacheService.SaveImageToCacheAsync(url, bytes);

                // 4. 设置显示
                using var memStream = new InMemoryRandomAccessStream();
                await memStream.WriteAsync(bytes.AsBuffer());
                memStream.Seek(0);
                await bitmap.SetSourceAsync(memStream);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load image with referer and cache: {ex.Message}");
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
