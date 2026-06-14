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
