using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HuFu.Services;

public sealed class ImageUploadService
{
    private const string DefaultUploadHost = "upload-z2.qiniup.com";
    private const string DefaultImageBaseUrl = "https://chat-img.jwznb.com/";
    private const string DefaultBucket = "chat68";

    private readonly HttpClient _httpClient;

    public ImageUploadService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { UseProxy = false });
    }

    public async Task<ImageUploadResult> UploadImageAsync(string token, byte[] bytes, string? fileName, string? contentType)
    {
        if (bytes.Length == 0)
            throw new InvalidOperationException("empty image");

        var (width, height, mimeFromDecoder, extFromDecoder) = await TryDecodeImageInfoAsync(bytes);
        var mimeType = !string.IsNullOrWhiteSpace(contentType) ? contentType! : (mimeFromDecoder ?? "image/png");
        var ext = GuessExt(fileName, mimeType, extFromDecoder);
        var key = $"{Md5Hex(bytes)}.{ext}";

        var uploadToken = await GetQiniuUploadTokenAsync(token);
        var host = NormalizeHost(await QueryUploadHostAsync(uploadToken, DefaultBucket));
        var uploadUrl = $"https://{host}";
        var imageBase = await GetImageBaseUrlAsync(token);

        string respText;
        try
        {
            respText = await UploadOnceAsync(uploadUrl, uploadToken, key, bytes, mimeType);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("no such domain", StringComparison.OrdinalIgnoreCase))
        {
            respText = await UploadOnceAsync($"https://{DefaultUploadHost}", uploadToken, key, bytes, mimeType);
        }
        catch (Exception ex) when (ex.Message.Contains("no such domain", StringComparison.OrdinalIgnoreCase))
        {
            respText = await UploadOnceAsync($"https://{DefaultUploadHost}", uploadToken, key, bytes, mimeType);
        }

        var (fileKey, fileHash) = ParseQiniuUploadResponse(respText, key);

        return new ImageUploadResult
        {
            FileKey = fileKey,
            FileHash = fileHash,
            FileType = mimeType,
            FileSize = bytes.LongLength,
            ImageHeight = height,
            ImageWidth = width,
            FileSuffix = ext,
            ImageUrl = $"{imageBase}{fileKey}"
        };
    }

    private async Task<string> GetQiniuUploadTokenAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://chat-go.jwzhd.com/v1/misc/qiniu-token");
        request.Headers.TryAddWithoutValidation("token", token);
        using var response = await _httpClient.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"qiniu-token http error: {(int)response.StatusCode} {text}");
        }

        using var doc = JsonDocument.Parse(text);
        if (!doc.RootElement.TryGetProperty("code", out var codeEl) || codeEl.GetInt32() != 1)
        {
            throw new InvalidOperationException($"qiniu-token api error: {text}");
        }

        if (!doc.RootElement.TryGetProperty("data", out var dataEl) || !dataEl.TryGetProperty("token", out var tokEl))
        {
            throw new InvalidOperationException($"qiniu-token missing token: {text}");
        }

        var tok = tokEl.GetString();
        if (string.IsNullOrWhiteSpace(tok))
        {
            throw new InvalidOperationException($"qiniu-token missing token: {text}");
        }
        return tok;
    }

    private async Task<string> GetImageBaseUrlAsync(string token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://chat-go.jwzhd.com/v1/misc/configure-distribution");
            request.Headers.TryAddWithoutValidation("token", token);
            using var response = await _httpClient.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return DefaultImageBaseUrl;
            }

            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("code", out var codeEl) || codeEl.GetInt32() != 1)
            {
                return DefaultImageBaseUrl;
            }
            if (!doc.RootElement.TryGetProperty("data", out var dataEl))
            {
                return DefaultImageBaseUrl;
            }
            if (!dataEl.TryGetProperty("imageUrl", out var imgEl))
            {
                return DefaultImageBaseUrl;
            }
            var url = imgEl.GetString();
            return string.IsNullOrWhiteSpace(url) ? DefaultImageBaseUrl : EnsureTrailingSlash(url);
        }
        catch
        {
            return DefaultImageBaseUrl;
        }
    }

    private async Task<string> QueryUploadHostAsync(string uploadToken, string bucket)
    {
        var ak = uploadToken.Split(':')[0];
        var url = $"https://api.qiniu.com/v4/query?ak={Uri.EscapeDataString(ak)}&bucket={Uri.EscapeDataString(bucket)}";
        try
        {
            using var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return DefaultUploadHost;
            var payload = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(payload);

            if (!doc.RootElement.TryGetProperty("hosts", out var hostsEl) || hostsEl.ValueKind != JsonValueKind.Array || hostsEl.GetArrayLength() == 0)
                return DefaultUploadHost;

            var first = hostsEl[0];
            if (!first.TryGetProperty("up", out var upEl)) return DefaultUploadHost;
            if (!upEl.TryGetProperty("domains", out var domainsEl) || domainsEl.ValueKind != JsonValueKind.Array || domainsEl.GetArrayLength() == 0)
                return DefaultUploadHost;

            var d = domainsEl[0].GetString();
            return string.IsNullOrWhiteSpace(d) ? DefaultUploadHost : d;
        }
        catch
        {
            return DefaultUploadHost;
        }
    }

    private static string NormalizeHost(string domainOrUrl)
    {
        var s = (domainOrUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(s)) return DefaultUploadHost;

        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(s, UriKind.Absolute, out var u) && !string.IsNullOrWhiteSpace(u.Host))
                return u.Host;
        }

        var slash = s.IndexOf('/');
        if (slash >= 0) s = s[..slash];
        return string.IsNullOrWhiteSpace(s) ? DefaultUploadHost : s;
    }

    private async Task<string> UploadOnceAsync(string uploadUrl, string uploadToken, string key, byte[] bytes, string mimeType)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(uploadToken), "token");
        content.Add(new StringContent(key), "key");

        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        content.Add(fileContent, "file", key);

        using var req = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        req.Content = content;
        req.Headers.TryAddWithoutValidation("user-agent", "QiniuDart");
        req.Headers.TryAddWithoutValidation("accept-encoding", "gzip");

        using var resp = await _httpClient.SendAsync(req);
        var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"qiniu upload failed: {(int)resp.StatusCode} {text} (uploadUrl={uploadUrl})");
        }
        return text;
    }

    private static (string key, string hash) ParseQiniuUploadResponse(string responseJson, string fallbackKey)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var key = doc.RootElement.TryGetProperty("key", out var keyEl) ? keyEl.GetString() : fallbackKey;
            var hash = doc.RootElement.TryGetProperty("hash", out var hashEl) ? hashEl.GetString() : string.Empty;
            return (string.IsNullOrWhiteSpace(key) ? fallbackKey : key!, hash ?? string.Empty);
        }
        catch
        {
            return (fallbackKey, string.Empty);
        }
    }

    private static string GuessExt(string? fileName, string mimeType, string? extFromDecoder)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty).TrimStart('.').ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(ext)) return ext;

        if (!string.IsNullOrWhiteSpace(extFromDecoder)) return extFromDecoder!;

        var mt = (mimeType ?? string.Empty).ToLowerInvariant();
        if (mt.Contains("png")) return "png";
        if (mt.Contains("jpeg") || mt.Contains("jpg")) return "jpg";
        if (mt.Contains("gif")) return "gif";
        if (mt.Contains("webp")) return "webp";
        return "bin";
    }

    private static string Md5Hex(byte[] bytes)
    {
        var hash = MD5.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static async Task<(long width, long height, string? mime, string? ext)> TryDecodeImageInfoAsync(byte[] bytes)
    {
        try
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var mime = decoder.DecoderInformation.MimeTypes.FirstOrDefault();
            var ext = decoder.DecoderInformation.FileExtensions.FirstOrDefault()?.TrimStart('.');
            return (decoder.PixelWidth, decoder.PixelHeight, mime, ext);
        }
        catch
        {
            return (0, 0, null, null);
        }
    }

    private static string EnsureTrailingSlash(string url)
    {
        return url.EndsWith('/') ? url : $"{url}/";
    }
}

public sealed class ImageUploadResult
{
    public string FileKey { get; init; } = string.Empty;
    public string FileHash { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public long ImageHeight { get; init; }
    public long ImageWidth { get; init; }
    public string FileSuffix { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
}
