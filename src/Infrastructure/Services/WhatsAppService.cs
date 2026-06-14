using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

    public async Task SendMediaAsync(SendMediaRequest request)
    {
        var payload = new { remoteJid = request.RemoteJid, mediaUrl = request.MediaUrl, mediaType = request.MediaType, caption = request.Caption ?? "", fileName = request.FileName ?? "file", mimeType = request.MimeType ?? "" };
        var response = await _httpClient.PostAsJsonAsync("/send-media", payload);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Media sent to {RemoteJid} type={MediaType}", request.RemoteJid, request.MediaType);
    }

    public async Task SendTemplateAsync(SendTemplateRequest request)
    {
        var payload = new { remoteJid = request.RemoteJid, templateName = request.TemplateName ?? "", language = request.Language ?? "en", body = request.Body, header = request.Header ?? "", footer = request.Footer ?? "", buttons = request.Buttons ?? "", contentType = request.ContentType ?? "twilio/text", typesJson = request.TypesJson ?? "" };
        var response = await _httpClient.PostAsJsonAsync("/send-template", payload);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Template sent to {RemoteJid}", request.RemoteJid);
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
        catch { return false; }
    }

    public async Task<byte[]?> GetProfilePictureAsync(string jid)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/profile-picture?jid={Uri.EscapeDataString(jid)}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch { return null; }
    }

    public async Task SendReactionAsync(SendReactionRequest request)
    {
        var payload = new { remoteJid = request.RemoteJid, messageId = request.MessageId, emoji = request.Emoji, remove = request.Remove };
        var response = await _httpClient.PostAsJsonAsync("/send-reaction", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReadReceiptsAsync(ReadReceiptsRequest request)
    {
        var payload = new { remoteJid = request.RemoteJid, messageIds = request.MessageIds };
        var response = await _httpClient.PostAsJsonAsync("/read-receipts", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task EditMessageAsync(EditMessageRequest request)
    {
        var payload = new { remoteJid = request.RemoteJid, messageId = request.MessageId, newText = request.NewText };
        var response = await _httpClient.PostAsJsonAsync("/edit-message", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteMessageAsync(DeleteMessageRequest request)
    {
        var payload = new { remoteJid = request.RemoteJid, messageId = request.MessageId, forEveryone = request.ForEveryone };
        var response = await _httpClient.PostAsJsonAsync("/delete-message", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendContactAsync(SendContactRequest request)
    {
        var payload = new { remoteJid = request.RemoteJid, contactName = request.ContactName, contactPhone = request.ContactPhone };
        var response = await _httpClient.PostAsJsonAsync("/send-contact", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendPollAsync(SendPollRequest request)
    {
        var payload = new { remoteJid = request.RemoteJid, pollName = request.PollName, options = request.Options, selectableCount = request.SelectableCount };
        var response = await _httpClient.PostAsJsonAsync("/send-poll", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendStatusAsync(SendStatusRequest request)
    {
        var payload = new { text = request.Text ?? "", mediaUrl = request.MediaUrl ?? "", mediaType = request.MediaType ?? "" };
        var response = await _httpClient.PostAsJsonAsync("/send-status", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task<GroupMetadataResult?> GetGroupMetadataAsync(string groupJid)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/group-metadata?groupJid={Uri.EscapeDataString(groupJid)}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<GroupMetadataResult>();
        }
        catch { return null; }
    }

    public async Task CreateGroupAsync(GroupCreateRequest request)
    {
        var payload = new { subject = request.Subject, participants = request.Participants };
        var response = await _httpClient.PostAsJsonAsync("/group-create", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task GroupParticipantsUpdateAsync(GroupParticipantsRequest request)
    {
        var payload = new { groupJid = request.GroupJid, participants = request.Participants, action = request.Action };
        var response = await _httpClient.PostAsJsonAsync("/group-participants", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task GroupUpdateAsync(GroupUpdateRequest request)
    {
        var payload = new { groupJid = request.GroupJid, subject = request.Subject, description = request.Description, setting = request.Setting, ephemeralDuration = request.EphemeralDuration };
        var response = await _httpClient.PostAsJsonAsync("/group-update", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task GroupLeaveAsync(string groupJid)
    {
        var payload = new { groupJid };
        var response = await _httpClient.PostAsJsonAsync("/group-leave", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task BlockContactAsync(BlockContactRequest request)
    {
        var payload = new { remoteJid = request.RemoteJid, block = request.Block };
        var response = await _httpClient.PostAsJsonAsync("/block-contact", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateProfileAsync(UpdateProfileRequest request)
    {
        var payload = new { name = request.Name ?? "", status = request.Status ?? "", profilePictureUrl = request.ProfilePictureUrl ?? "" };
        var response = await _httpClient.PostAsJsonAsync("/update-profile", payload);
        response.EnsureSuccessStatusCode();
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
