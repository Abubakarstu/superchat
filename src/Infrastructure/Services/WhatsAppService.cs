using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient httpClient, IOptions<BaileysOptions> options, ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
        _logger = logger;
    }

    public async Task SendMessageAsync(string remoteJid, string message)
    {
        var payload = new SendMessageRequest { RemoteJid = remoteJid, Message = message };
        var response = await _httpClient.PostAsJsonAsync("/send-message", payload);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Message sent to {RemoteJid}", remoteJid);
    }

    public async Task<string> GetQrCodeAsync()
    {
        var response = await _httpClient.GetAsync("/qr");
        response.EnsureSuccessStatusCode();
        var qrResponse = await response.Content.ReadFromJsonAsync<QrResponse>();
        return qrResponse?.Qr ?? string.Empty;
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class BaileysOptions
{
    public string BaseUrl { get; set; } = "http://localhost:3001";
}

public class SendMessageRequest
{
    [JsonPropertyName("remoteJid")]
    public string RemoteJid { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class QrResponse
{
    [JsonPropertyName("qr")]
    public string? Qr { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
