namespace Application.Interfaces;

public class IncomingMessage
{
    public string RemoteJid { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaMimeType { get; set; }
    public string MessageType { get; set; } = "text";
    public string? SenderName { get; set; }
}

public class WebhookPayload
{
    public string HttpMethod { get; set; } = "POST";
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> QueryParams { get; set; } = new();
}

public interface IChannelService
{
    string ChannelType { get; }
    Task SendMessageAsync(string channelAccountId, string remoteJid, string message);
    Task<bool> ValidateConnectionAsync(string channelAccountId);
    Task<string> GetChannelInfoAsync(string channelAccountId);
    Task<string> RegisterWebhookAsync(string channelAccountId, string webhookUrl);
    Task UnregisterWebhookAsync(string channelAccountId);
    Task<IncomingMessage?> ParseWebhookAsync(WebhookPayload payload, Dictionary<string, string> config);
}

public interface IChannelServiceFactory
{
    IChannelService? GetService(string channelType);
}
