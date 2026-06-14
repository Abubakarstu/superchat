using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Infrastructure.Services;

public class TelegramChannelService : IChannelService
{
    public string ChannelType => "telegram";

    private readonly HttpClient _http;
    private readonly ILogger<TelegramChannelService> _logger;

    public TelegramChannelService(HttpClient http, ILogger<TelegramChannelService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task SendMessageAsync(string channelAccountId, string remoteJid, string message)
    {
        var botToken = channelAccountId;
        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
        var payload = new { chat_id = remoteJid, text = message };
        var response = await _http.PostAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> ValidateConnectionAsync(string channelAccountId)
    {
        try
        {
            var botToken = channelAccountId;
            var response = await _http.GetAsync($"https://api.telegram.org/bot{botToken}/getMe");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<string> GetChannelInfoAsync(string channelAccountId)
    {
        var botToken = channelAccountId;
        var response = await _http.GetAsync($"https://api.telegram.org/bot{botToken}/getMe");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadFromJsonAsync<TelegramUserResponse>();
            return json?.Result?.Username ?? "unknown";
        }
        return "unknown";
    }

    public async Task<string> RegisterWebhookAsync(string channelAccountId, string webhookUrl)
    {
        var botToken = channelAccountId;
        var secret = Guid.NewGuid().ToString("N");
        var url = $"https://api.telegram.org/bot{botToken}/setWebhook?url={webhookUrl}&secret_token={secret}";
        var response = await _http.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            _logger.LogError("Telegram webhook register failed: {Body}", body);
        return secret;
    }

    public async Task UnregisterWebhookAsync(string channelAccountId)
    {
        var botToken = channelAccountId;
        await _http.GetAsync($"https://api.telegram.org/bot{botToken}/deleteWebhook");
    }

    public async Task<IncomingMessage?> ParseWebhookAsync(WebhookPayload payload, Dictionary<string, string> config)
    {
        if (payload.HttpMethod != "POST")
            return null;

        var body = payload.Body;

        try
        {
            var update = JsonSerializer.Deserialize<TelegramUpdate>(body);
            if (update?.Message == null)
                return null;

            var text = update.Message.Text ?? "";
            var chatId = update.Message.Chat?.Id.ToString() ?? "";
            var senderName = $"{update.Message.From?.FirstName ?? ""} {update.Message.From?.LastName ?? ""}".Trim();
            if (string.IsNullOrEmpty(senderName)) senderName = update.Message.From?.Username ?? chatId;

            var mediaUrl = "";
            var msgType = "text";
            if (update.Message.Photo is { Count: > 0 })
            {
                var photo = update.Message.Photo.Last();
                mediaUrl = photo.FileId;
                msgType = "image";
            }
            else if (!string.IsNullOrEmpty(update.Message.Document?.FileId))
            {
                mediaUrl = update.Message.Document.FileId;
                msgType = "document";
            }

            return new IncomingMessage
            {
                RemoteJid = chatId,
                Content = text,
                MediaUrl = mediaUrl,
                SenderName = senderName,
                MessageType = msgType
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Telegram webhook");
            return null;
        }
    }
}

public class TelegramUpdate
{
    public long? UpdateId { get; set; }
    public TelegramMessage? Message { get; set; }
}

public class TelegramMessage
{
    public long? MessageId { get; set; }
    public TelegramFrom? From { get; set; }
    public TelegramChat? Chat { get; set; }
    public long? Date { get; set; }
    public string? Text { get; set; }
    public List<TelegramPhoto>? Photo { get; set; }
    public TelegramDocument? Document { get; set; }
}

public class TelegramFrom
{
    public long? Id { get; set; }
    public bool? IsBot { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
}

public class TelegramChat
{
    public long? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Type { get; set; }
}

public class TelegramPhoto
{
    public string? FileId { get; set; }
    public int? FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public class TelegramDocument
{
    public string? FileId { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public int? FileSize { get; set; }
}

public class TelegramUserResponse
{
    public TelegramUser? Result { get; set; }
}

public class TelegramUser
{
    public long Id { get; set; }
    public string? Username { get; set; }
}
