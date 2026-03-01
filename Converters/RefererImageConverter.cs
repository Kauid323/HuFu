using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;

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
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Referrer = new Uri(Referer);
            
            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var memStream = new InMemoryRandomAccessStream();
                await stream.CopyToAsync(memStream.AsStreamForWrite());
                memStream.Seek(0);
                await bitmap.SetSourceAsync(memStream);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load image with referer: {ex.Message}");
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
