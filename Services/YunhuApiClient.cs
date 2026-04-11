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
using Group;

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

    public async Task<send_message> SendMessageAsync(string token, string chatId, long chatType, string text)
    {
        var msgId = Guid.NewGuid().ToString("N"); // 生成不带杠号的消息ID

        var req = new send_message_send
        {
            MsgId = msgId,
            ChatId = chatId,
            ChatType = chatType,
            ContentType = 1, // 1-文本消息
            Content = new send_message_send.Types.Content
            {
                Text = text
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/msg/send-message");
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
        return send_message.Parser.ParseFrom(bytes);
    }

    public async Task<send_message> SendImageMessageAsync(string token, string chatId, long chatType, ImageUploadResult upload)
    {
        var msgId = Guid.NewGuid().ToString("N");

        var req = new send_message_send
        {
            MsgId = msgId,
            ChatId = chatId,
            ChatType = chatType,
            ContentType = 2, // 2-图片
            Content = new send_message_send.Types.Content
            {
                Image = upload.FileKey
            },
            Media = new send_message_send.Types.Media
            {
                FileKey = upload.FileKey,
                FileKey2 = upload.FileKey,
                FileHash = upload.FileHash ?? string.Empty,
                FileType = upload.FileType ?? string.Empty,
                ImageHeight = upload.ImageHeight,
                ImageWidth = upload.ImageWidth,
                FileSize = upload.FileSize,
                FileSuffix = upload.FileSuffix ?? string.Empty
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/msg/send-message");
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
        return send_message.Parser.ParseFrom(bytes);
    }

    public async Task<info> GetGroupInfoAsync(string token, string groupId)
    {
        var req = new info_send
        {
            GroupId = groupId
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/group/info");
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
        return info.Parser.ParseFrom(bytes);
    }

    public async Task<VoiceRoomListResponse> GetGroupVoiceRoomsAsync(string token, string groupId)
    {
        var req = new { groupId };
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/group/live-room");
        request.Headers.Add("token", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(req, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}");
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<VoiceRoomListResponse>(responseText, JsonOptions);
        if (result is null)
        {
            throw new InvalidOperationException($"failed to parse response: {responseText}");
        }

        return result;
    }

    public async Task<VoiceJoinTokenResponse> GetVoiceJoinTokenAsync(string token, string roomId, string chatId)
    {
        var req = new { roomId, chatId };
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/live/add");
        request.Headers.Add("token", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(req, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}");
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<VoiceJoinTokenResponse>(responseText, JsonOptions);
        if (result is null)
        {
            throw new InvalidOperationException($"failed to parse response: {responseText}");
        }

        return result;
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

    public sealed class VoiceRoomListResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("data")]
        public VoiceRoomListData? Data { get; set; }
    }

    public sealed class VoiceRoomListData
    {
        [JsonPropertyName("rooms")]
        public VoiceRoomInfo[] Rooms { get; set; } = Array.Empty<VoiceRoomInfo>();
    }

    public sealed class VoiceRoomInfo
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("roomId")]
        public string RoomId { get; set; } = string.Empty;

        [JsonPropertyName("chatId")]
        public string ChatId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("chatType")]
        public int ChatType { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("createBy")]
        public string CreateBy { get; set; } = string.Empty;

        [JsonPropertyName("createTime")]
        public long CreateTime { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("avatarUrl")]
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public sealed class VoiceJoinTokenResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("data")]
        public VoiceJoinTokenData? Data { get; set; }
    }

    public sealed class VoiceJoinTokenData
    {
        [JsonPropertyName("joinToken")]
        public string JoinToken { get; set; } = string.Empty;
    }

    public async Task<RecommendPostsResponse> GetRecommendPostsAsync(string token, int size = 20, int page = 1)
    {
        var req = new { size, page };
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/community/posts/post-list-recommend");
        request.Headers.Add("token", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(req, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}");
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RecommendPostsResponse>(responseText, JsonOptions);
        if (result is null)
        {
            throw new InvalidOperationException($"failed to parse response: {responseText}");
        }

        return result;
    }

    public async Task<PostDetailResponse> GetPostDetailAsync(string token, int postId)
    {
        var req = new { id = postId };
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/community/posts/post-detail");
        request.Headers.Add("token", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(req, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}");
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PostDetailResponse>(responseText, JsonOptions);
        if (result is null)
        {
            throw new InvalidOperationException($"failed to parse response: {responseText}");
        }

        return result;
    }

    public sealed class RecommendPostsResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("data")]
        public RecommendPostsData? Data { get; set; }
    }

    public sealed class RecommendPostsData
    {
        [JsonPropertyName("posts")]
        public PostInfo[] Posts { get; set; } = Array.Empty<PostInfo>();

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public sealed class PostInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("baId")]
        public int BaId { get; set; }

        [JsonPropertyName("senderId")]
        public string SenderId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public int ContentType { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("createTime")]
        public long CreateTime { get; set; }

        [JsonPropertyName("likeNum")]
        public int LikeNum { get; set; }

        [JsonPropertyName("commentNum")]
        public int CommentNum { get; set; }

        [JsonPropertyName("collectNum")]
        public int CollectNum { get; set; }

        [JsonPropertyName("amountNum")]
        public double AmountNum { get; set; }

        [JsonPropertyName("senderNickname")]
        public string SenderNickname { get; set; } = string.Empty;

        [JsonPropertyName("senderAvatar")]
        public string SenderAvatar { get; set; } = string.Empty;

        [JsonPropertyName("isLiked")]
        public string IsLiked { get; set; } = "0";

        [JsonPropertyName("isCollected")]
        public int IsCollected { get; set; }

        [JsonPropertyName("isVip")]
        public int IsVip { get; set; }
    }

    public sealed class PostDetailResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("data")]
        public PostDetailData? Data { get; set; }
    }

    public sealed class PostDetailData
    {
        [JsonPropertyName("post")]
        public PostDetailInfo? Post { get; set; }

        [JsonPropertyName("isAdmin")]
        public int IsAdmin { get; set; }
    }

    public sealed class PostDetailInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("baId")]
        public int BaId { get; set; }

        [JsonPropertyName("senderId")]
        public string SenderId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public int ContentType { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("createTime")]
        public long CreateTime { get; set; }

        [JsonPropertyName("likeNum")]
        public int LikeNum { get; set; }

        [JsonPropertyName("commentNum")]
        public int CommentNum { get; set; }

        [JsonPropertyName("collectNum")]
        public int CollectNum { get; set; }

        [JsonPropertyName("amountNum")]
        public double AmountNum { get; set; }

        [JsonPropertyName("senderNickname")]
        public string SenderNickname { get; set; } = string.Empty;

        [JsonPropertyName("senderAvatar")]
        public string SenderAvatar { get; set; } = string.Empty;

        [JsonPropertyName("createTimeText")]
        public string CreateTimeText { get; set; } = string.Empty;

        [JsonPropertyName("isLiked")]
        public int IsLiked { get; set; }

        [JsonPropertyName("isCollected")]
        public int IsCollected { get; set; }

        [JsonPropertyName("isReward")]
        public int IsReward { get; set; }

        [JsonPropertyName("isVip")]
        public int IsVip { get; set; }
    }
}
