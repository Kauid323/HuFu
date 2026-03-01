using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Conversation;
using Google.Protobuf;
using Msg;
using User;

namespace HuFu.Services;

public sealed class YunhuApiClient
{
    private static readonly Uri BaseUri = new("https://chat-go.jwzhd.com");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public YunhuApiClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { UseProxy = false });
        _httpClient.BaseAddress = BaseUri;
    }

    public async Task<string> LoginWithEmailAsync(string email, string password, string deviceId, string platform)
    {
        var req = new
        {
            email,
            password,
            deviceId,
            platform,
        };

        var resp = await PostJsonAsync<YunhuTokenResponse>("/v1/user/email-login", req);
        if (resp.Code != 1)
        {
            throw new InvalidOperationException(resp.Msg ?? "login failed");
        }

        var token = resp.Data?.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("login succeeded but token is empty");
        }

        return token;
    }

    public async Task<CaptchaData> GetCaptchaAsync()
    {
        var resp = await PostJsonAsync<CaptchaResponse>("/v1/user/captcha", null);
        if (resp.Code != 1 || resp.Data is null)
        {
            throw new InvalidOperationException($"get captcha failed: code={resp.Code}, msg={resp.Msg}");
        }

        return resp.Data;
    }

    public async Task GetSmsVerificationCodeAsync(string mobile, string captchaCode, string captchaId)
    {
        var req = new SmsCaptchaRequest
        {
            Mobile = mobile,
            Code = captchaCode,
            Id = captchaId,
        };

        var resp = await PostJsonAsync<SimpleStatusResponse>("/v1/verification/get-verification-code", req);
        if (resp.Code != 1)
        {
            throw new InvalidOperationException($"get sms verification code failed: code={resp.Code}, msg={resp.Msg}");
        }
    }

    public async Task<string> LoginWithSmsAsync(string mobile, string captcha, string deviceId, string platform)
    {
        var req = new
        {
            mobile,
            captcha,
            deviceId,
            platform,
        };

        var resp = await PostJsonAsync<YunhuTokenResponse>("/v1/user/verification-login", req);
        if (resp.Code != 1)
        {
            throw new InvalidOperationException($"login failed: code={resp.Code}, msg={resp.Msg}");
        }

        var token = resp.Data?.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("login succeeded but token is empty");
        }

        return token;
    }

    public async Task<ConversationList> GetConversationListAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/conversation/list");
        request.Headers.Add("token", token);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));

        request.Content = null;

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        return ConversationList.Parser.ParseFrom(bytes);
    }

    public async Task<list_message> GetMessageListAsync(string token, string chatId, long chatType, long msgCount = 30, string? msgId = null)
    {
        var req = new list_message_send
        {
            MsgCount = msgCount,
            ChatType = chatType,
            ChatId = chatId,
        };
        if (!string.IsNullOrWhiteSpace(msgId))
        {
            req.MsgId = msgId;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/msg/list-message");
        request.Headers.Add("token", token);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
        request.Content = new ByteArrayContent(req.ToByteArray());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        return list_message.Parser.ParseFrom(bytes);
    }

    public async Task<UserInfo> GetUserInfoAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/user/info");
        request.Headers.Add("token", token);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        return UserInfo.Parser.ParseFrom(bytes);
    }

    private async Task<T> PostJsonAsync<T>(string path, object? body)
    {
        HttpContent? content = null;
        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.PostAsync(path, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {responseText}");
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("empty response body");
        }

        var obj = JsonSerializer.Deserialize<T>(responseText, JsonOptions);
        if (obj is null)
        {
            throw new InvalidOperationException($"failed to parse response: {responseText}");
        }

        return obj;
    }

    private sealed class YunhuTokenResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("data")]
        public YunhuTokenData? Data { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }
    }

    private sealed class YunhuTokenData
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    public sealed class CaptchaResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("data")]
        public CaptchaData? Data { get; set; }
    }

    public sealed class CaptchaData
    {
        [JsonPropertyName("b64s")]
        public string B64s { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private sealed class SmsCaptchaRequest
    {
        [JsonPropertyName("mobile")]
        public string Mobile { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private sealed class SimpleStatusResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }
    }
}
