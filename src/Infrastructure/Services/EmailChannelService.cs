using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;

namespace Infrastructure.Services;

public class EmailChannelService : IChannelService
{
    public string ChannelType => "email";

    private readonly ILogger<EmailChannelService> _logger;

    public EmailChannelService(ILogger<EmailChannelService> logger)
    {
        _logger = logger;
    }

    public async Task SendMessageAsync(string channelAccountId, string remoteJid, string message)
    {
        try
        {
            var parts = channelAccountId.Split('|');
            var smtpHost = parts[0];
            var smtpPort = int.Parse(parts[1]);
            var username = parts[2];
            var password = parts[3];

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var mail = new MailMessage(username, remoteJid, "Superchat Message", message);
            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", remoteJid);
            throw;
        }
    }

    public Task<bool> ValidateConnectionAsync(string channelAccountId) => Task.FromResult(true);

    public Task<string> GetChannelInfoAsync(string channelAccountId) => Task.FromResult("email");

    public Task<string> RegisterWebhookAsync(string channelAccountId, string webhookUrl)
    {
        return Task.FromResult("");
    }

    public Task UnregisterWebhookAsync(string channelAccountId)
    {
        return Task.CompletedTask;
    }

    public Task<IncomingMessage?> ParseWebhookAsync(WebhookPayload payload, Dictionary<string, string> config)
    {
        return Task.FromResult<IncomingMessage?>(null);
    }
}
