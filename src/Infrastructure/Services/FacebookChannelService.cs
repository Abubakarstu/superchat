using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class FacebookChannelService : IChannelService
{
    public string ChannelType => "facebook";

    private readonly HttpClient _http;
    private readonly ILogger<FacebookChannelService> _logger;

    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _sendHistory = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastUserMessage = new();

    private const int MaxMessagesPerDay = 200;
    private const int MinIntervalSeconds = 2;
    private const int WindowHours = 24;

    public FacebookChannelService(HttpClient http, ILogger<FacebookChannelService> logger)
    {
        _http = http;
        _logger = logger;
    }

    private string? GetAccessToken(Dictionary<string, string> config) =>
        config.TryGetValue("accessToken", out var t) ? t : null;

    private string? GetPageId(Dictionary<string, string> config) =>
        config.TryGetValue("pageId", out var p) ? p : null;

    private string? GetAppSecret(Dictionary<string, string> config) =>
        config.TryGetValue("appSecret", out var s) ? s : null;

    private Dictionary<string, string> ParseConfig(string channelAccountId)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(channelAccountId) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task SendMessageAsync(string channelAccountId, string remoteJid, string message)
    {
        var config = ParseConfig(channelAccountId);
        var token = GetAccessToken(config);
        var pageId = GetPageId(config);

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(pageId))
            throw new InvalidOperationException("Facebook channel not configured: missing pageId or accessToken");

        var convKey = $"{channelAccountId}:{remoteJid}";

        if (!CanSend(convKey))
            throw new InvalidOperationException("Rate limit exceeded or outside 24h messaging window. Wait or let the user message first.");

        var url = $"https://graph.facebook.com/v21.0/me/messages?access_token={token}";
        var payload = new
        {
            recipient = new { id = remoteJid },
            message = new { text = message },
            messaging_type = "RESPONSE"
        };

        var response = await _http.PostAsJsonAsync(url, payload);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Facebook send failed ({Status}): {Body}", response.StatusCode, body);

            if ((int)response.StatusCode == 429)
                throw new InvalidOperationException("Rate limited by Meta. Try again later.");

            if ((int)response.StatusCode == 403 && body.Contains("error"))
            {
                var err = JsonSerializer.Deserialize<MetaErrorResponse>(body);
                var msg = err?.Error?.Message ?? "";
                if (msg.Contains("page not allowed", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("restricted", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("ban", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("disabled", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogCritical("FACEBOOK BAN DETECTED: {Msg}", msg);
                    throw new InvalidOperationException($"Facebook access restricted: {msg}. Check your Page permissions.");
                }
            }

            response.EnsureSuccessStatusCode();
        }

        RecordSend(convKey);
    }

    public async Task<bool> ValidateConnectionAsync(string channelAccountId)
    {
        var config = ParseConfig(channelAccountId);
        var token = GetAccessToken(config);
        var pageId = GetPageId(config);

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(pageId))
            return false;

        try
        {
            var response = await _http.GetAsync($"https://graph.facebook.com/v21.0/{pageId}?access_token={token}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Facebook validate failed: {Body}", body);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Facebook validate exception");
            return false;
        }
    }

    public async Task<string> GetChannelInfoAsync(string channelAccountId)
    {
        var config = ParseConfig(channelAccountId);
        var token = GetAccessToken(config);
        var pageId = GetPageId(config);

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(pageId))
            return "unknown";

        try
        {
            var response = await _http.GetAsync($"https://graph.facebook.com/v21.0/{pageId}?fields=name,username&access_token={token}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<FacebookPageInfo>();
                return json?.Name ?? json?.Username ?? pageId;
            }
        }
        catch { }
        return pageId;
    }

    public async Task<string> RegisterWebhookAsync(string channelAccountId, string webhookUrl)
    {
        var config = ParseConfig(channelAccountId);
        var token = GetAccessToken(config);
        var pageId = GetPageId(config);
        var appSecret = GetAppSecret(config);

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(pageId) || string.IsNullOrEmpty(appSecret))
            throw new InvalidOperationException("Facebook channel not configured: missing pageId, accessToken, or appSecret");

        var verifyToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower();

        var subscribeUrl = $"https://graph.facebook.com/v21.0/{pageId}/subscribed_apps?access_token={token}&subscribed_fields=messages,messaging_postbacks,message_deliveries,message_reads";
        var response = await _http.PostAsync(subscribeUrl, null);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Facebook webhook subscribe failed: {Body}", body);
            throw new InvalidOperationException($"Failed to subscribe to Page webhooks: {body}");
        }

        return verifyToken;
    }

    public async Task UnregisterWebhookAsync(string channelAccountId)
    {
        var config = ParseConfig(channelAccountId);
        var token = GetAccessToken(config);
        var pageId = GetPageId(config);

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(pageId))
            return;

        var url = $"https://graph.facebook.com/v21.0/{pageId}/subscribed_apps?access_token={token}";
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Facebook webhook unsubscribe failed: {Body}", responseBody);
        }
    }

    public async Task<IncomingMessage?> ParseWebhookAsync(WebhookPayload payload, Dictionary<string, string> config)
    {
        if (payload.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var mode = payload.QueryParams.GetValueOrDefault("hub.mode", "");
            var token = payload.QueryParams.GetValueOrDefault("hub.verify_token", "");
            var challenge = payload.QueryParams.GetValueOrDefault("hub.challenge", "");

            if (mode == "subscribe" && !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(challenge))
            {
                var expected = config.GetValueOrDefault("verifyToken", "");
                if (token == expected)
                {
                    throw new WebhookChallengeException(challenge);
                }
            }
            return null;
        }

        var body = payload.Body;
        var appSecret = config.GetValueOrDefault("appSecret", "");
        if (!string.IsNullOrEmpty(appSecret))
        {
            var signature = payload.Headers.GetValueOrDefault("X-Hub-Signature-256", "");
            if (!VerifySignature(body, appSecret, signature))
            {
                _logger.LogWarning("Facebook webhook signature mismatch — possible spoofed request");
                return null;
            }
        }

        try
        {
            var fbPayload = JsonSerializer.Deserialize<FacebookWebhookPayload>(body);
            if (fbPayload?.Entry == null || fbPayload.Entry.Count == 0)
                return null;

            var entry = fbPayload.Entry[0];
            var messaging = entry.Messaging;
            if (messaging == null || messaging.Count == 0)
            {
                var changes = entry.Changes;
                if (changes != null && changes.Count > 0)
                {
                    foreach (var change in changes)
                    {
                        if (change.Field == "messages" && change.Value?.Messages != null)
                        {
                            foreach (var msg in change.Value.Messages)
                            {
                                var from = msg.From?.Id ?? "";
                                var text = msg.Text ?? "";
                                var mediaUrl = GetMediaUrl(msg);

                                var sender = msg.From?.Name ?? "";
                                if (long.TryParse(msg.TimestampMs, out var tsLong))
                                {
                                    var msgTime = DateTimeOffset.FromUnixTimeMilliseconds(tsLong).UtcDateTime;
                                    _lastUserMessage[from] = msgTime;
                                }

                                return new IncomingMessage
                                {
                                    RemoteJid = from,
                                    Content = text,
                                    MediaUrl = mediaUrl,
                                    SenderName = sender,
                                    MessageType = !string.IsNullOrEmpty(mediaUrl) ? "image" : "text"
                                };
                            }
                        }
                    }
                }
                return null;
            }

            var messageData = messaging[0];
            var senderId = messageData.Sender?.Id ?? "";
            var messageText = messageData.Message?.Text ?? "";
            var attachments = messageData.Message?.Attachments;

            var incomingMediaUrl = "";
            if (attachments != null && attachments.Count > 0)
            {
                var att = attachments[0];
                if (att.Type == "image" || att.Type == "audio" || att.Type == "video" || att.Type == "file")
                {
                    incomingMediaUrl = att.Payload?.Url ?? "";
                }
            }

            var senderName = messageData.Sender?.Name ?? messageData.Sender?.Id ?? "";

            if (!string.IsNullOrEmpty(senderId))
            {
                var msgTime = DateTime.UtcNow;
                if (messageData.Timestamp.HasValue)
                {
                    msgTime = DateTimeOffset.FromUnixTimeSeconds(messageData.Timestamp.Value).UtcDateTime;
                }
                _lastUserMessage[senderId] = msgTime;
            }

            if (string.IsNullOrEmpty(senderId))
                return null;

            return new IncomingMessage
            {
                RemoteJid = senderId,
                Content = messageText,
                MediaUrl = incomingMediaUrl,
                MediaMimeType = attachments?.FirstOrDefault()?.Payload?.MimeType,
                SenderName = senderName ?? "",
                MessageType = !string.IsNullOrEmpty(incomingMediaUrl) ? attachments![0].Type : "text"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Facebook webhook JSON");
            return null;
        }
    }

    private static string? GetMediaUrl(FacebookMessageItem msg)
    {
        if (msg.Attachments?.Count > 0)
            return msg.Attachments[0].Payload?.Url;
        return null;
    }

    private static bool VerifySignature(string body, string appSecret, string signature)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        var prefix = "sha256=";
        if (!signature.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var expectedSig = signature[prefix.Length..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var actualSig = Convert.ToHexString(hash).ToLower();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSig),
            Encoding.UTF8.GetBytes(actualSig));
    }

    private bool CanSend(string convKey)
    {
        var history = _sendHistory.GetOrAdd(convKey, _ => new Queue<DateTime>());

        lock (history)
        {
            var now = DateTime.UtcNow;
            var dayAgo = now.AddHours(-24);

            while (history.Count > 0 && history.Peek() < dayAgo)
                history.Dequeue();

            if (history.Count >= MaxMessagesPerDay)
                return false;

            if (history.Count > 0)
            {
                var last = history.Last();
                if ((now - last).TotalSeconds < MinIntervalSeconds)
                    return false;
            }
        }

        if (_lastUserMessage.TryGetValue(convKey, out var lastMsg))
        {
            if ((DateTime.UtcNow - lastMsg).TotalHours > WindowHours)
                return false;
        }

        return true;
    }

    private void RecordSend(string convKey)
    {
        var history = _sendHistory.GetOrAdd(convKey, _ => new Queue<DateTime>());
        lock (history)
        {
            history.Enqueue(DateTime.UtcNow);
        }
    }
}

public class WebhookChallengeException : Exception
{
    public string Challenge { get; }
    public WebhookChallengeException(string challenge) : base("Webhook verification challenge")
    {
        Challenge = challenge;
    }
}

public class FacebookPageInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
}

public class MetaErrorResponse
{
    public MetaError? Error { get; set; }
}

public class MetaError
{
    public string? Message { get; set; }
    public int Code { get; set; }
    public int? ErrorSubcode { get; set; }
    public string? Type { get; set; }
    public string? FbTraceId { get; set; }
}

public class FacebookWebhookPayload
{
    public string? Object { get; set; }
    public List<FacebookEntry>? Entry { get; set; }
}

public class FacebookEntry
{
    public string? Id { get; set; }
    public long? Time { get; set; }
    public List<FacebookMessaging>? Messaging { get; set; }
    public List<FacebookChange>? Changes { get; set; }
}

public class FacebookChange
{
    public string? Field { get; set; }
    public FacebookChangeValue? Value { get; set; }
}

public class FacebookChangeValue
{
    public string? PageId { get; set; }
    public List<FacebookMessageItem>? Messages { get; set; }
}

public class FacebookMessageItem
{
    public string? Mid { get; set; }
    public string? Text { get; set; }
    public string? TimestampMs { get; set; }
    public FacebookMessageFrom? From { get; set; }
    public List<FacebookAttachment>? Attachments { get; set; }
}

public class FacebookMessageFrom
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class FacebookMessaging
{
    public FacebookSender? Sender { get; set; }
    public FacebookRecipient? Recipient { get; set; }
    public long? Timestamp { get; set; }
    public FacebookMessageData? Message { get; set; }
    public FacebookPostback? Postback { get; set; }
}

public class FacebookSender
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class FacebookRecipient
{
    public string? Id { get; set; }
}

public class FacebookMessageData
{
    public string? Mid { get; set; }
    public string? Text { get; set; }
    public long? Seq { get; set; }
    public bool? IsEcho { get; set; }
    public string? AppId { get; set; }
    public string? Metadata { get; set; }
    public List<FacebookAttachment>? Attachments { get; set; }
}

public class FacebookAttachment
{
    public string? Type { get; set; }
    public FacebookPayload? Payload { get; set; }
}

public class FacebookPayload
{
    public string? Url { get; set; }
    public string? MimeType { get; set; }
    public string? StickerId { get; set; }
}

public class FacebookPostback
{
    public string? Title { get; set; }
    public string? Payload { get; set; }
    public string? Referral { get; set; }
}
