using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.Channels;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IChannelServiceFactory _factory;
    private readonly IChannelAccountRepository _channelRepo;
    private readonly IConversationRepository _convRepo;
    private readonly IMessageRepository _msgRepo;
    private readonly IHubContext<MessageHub> _hub;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WebhookController> _logger;
    private readonly IWhatsAppService _whatsApp;

    public WebhookController(
        IChannelServiceFactory factory,
        IChannelAccountRepository channelRepo,
        IConversationRepository convRepo,
        IMessageRepository msgRepo,
        IHubContext<MessageHub> hub,
        IUnitOfWork uow,
        ILogger<WebhookController> logger,
        IWhatsAppService whatsApp)
    {
        _factory = factory;
        _channelRepo = channelRepo;
        _convRepo = convRepo;
        _msgRepo = msgRepo;
        _hub = hub;
        _uow = uow;
        _logger = logger;
        _whatsApp = whatsApp;
    }

    [HttpPost("incoming")]
    public async Task<IActionResult> IncomingMessage([FromBody] IncomingWebhookRequest request)
    {
        var remoteJid = request.RemoteJid ?? "";
        if (string.IsNullOrEmpty(remoteJid))
            return BadRequest("remoteJid required");

        var conv = await _convRepo.GetByRemoteJidAsync(remoteJid);
        if (conv == null)
        {
            conv = new Conversation
            {
                RemoteJid = remoteJid,
                ContactName = request.ContactName ?? remoteJid,
                ContactPhone = request.ContactPhone,
                ChannelType = "whatsapp",
                Status = "open",
                IsRead = false
            };
            _convRepo.Add(conv);
            await _uow.SaveChangesAsync();
        }
        else
        {
            conv.IsRead = false;
            conv.Status = "open";
            conv.LastMessageAt = DateTime.UtcNow;
            conv.ContactName = request.ContactName ?? conv.ContactName;
            _convRepo.Update(conv);
        }

        var message = new Message
        {
            ConversationId = conv.Id,
            Content = request.Content ?? "",
            Direction = MessageDirection.Inbound,
            Status = MessageStatus.Delivered,
            MediaUrl = request.MediaUrl,
            MessageType = request.MessageType ?? "text",
            FileName = request.FileName,
            MimeType = request.MimeType,
            ProviderMessageId = request.MessageId,
            CreatedAt = DateTime.UtcNow
        };
        _msgRepo.Add(message);
        await _uow.SaveChangesAsync();

        try
        {
            await _hub.Clients.Group(conv.Id.ToString()).SendAsync("NewMessage", new
            {
                id = message.Id.ToString(),
                conversationId = conv.Id.ToString(),
                content = message.Content,
                direction = "Inbound",
                status = "Delivered",
                mediaUrl = message.MediaUrl,
                messageType = message.MessageType,
                fileName = message.FileName,
                mimeType = message.MimeType,
                providerMessageId = message.ProviderMessageId,
                createdAt = message.CreatedAt,
                contactName = conv.ContactName,
                remoteJid = conv.RemoteJid
            });

            await _hub.Clients.All.SendAsync("ConversationUpdated", new
            {
                id = conv.Id.ToString(),
                remoteJid = conv.RemoteJid,
                contactName = conv.ContactName,
                lastMessageAt = conv.LastMessageAt,
                channelType = conv.ChannelType,
                status = conv.Status,
                isRead = conv.IsRead
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed");
        }

        return Ok();
    }

    private async Task<WebhookPayload> BuildPayload()
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        var headers = new Dictionary<string, string>();
        foreach (var h in Request.Headers)
        {
            headers[h.Key] = h.Value.ToString();
        }

        var queryParams = new Dictionary<string, string>();
        foreach (var q in Request.Query)
        {
            queryParams[q.Key] = q.Value.ToString();
        }

        return new WebhookPayload
        {
            HttpMethod = Request.Method,
            Body = body,
            Headers = headers,
            QueryParams = queryParams
        };
    }

    [HttpPost("meta/{accountId}")]
    [HttpGet("meta/{accountId}")]
    public async Task<IActionResult> MetaWebhook(Guid accountId)
    {
        var account = await _channelRepo.GetByIdAsync(accountId);
        if (account == null)
            return NotFound("Channel account not found");

        var service = _factory.GetService(account.ChannelType);
        if (service == null)
            return BadRequest($"No service for channel type: {account.ChannelType}");

        var config = ParseAccountConfig(account);
        config["verifyToken"] = account.WebhookSecret ?? "";

        var payload = await BuildPayload();

        try
        {
            var incoming = await service.ParseWebhookAsync(payload, config);
            if (incoming == null)
                return Ok();

            await RouteIncomingMessage(account, incoming);
            return Ok("EVENT_RECEIVED");
        }
        catch (WebhookChallengeException ex)
        {
            return Content(ex.Challenge, "text/plain");
        }
    }

    [HttpPost("generic/{channelType}/{accountId}")]
    public async Task<IActionResult> GenericWebhook(string channelType, Guid accountId)
    {
        var account = await _channelRepo.GetByIdAsync(accountId);
        if (account == null)
            return NotFound("Channel account not found");

        var service = _factory.GetService(channelType);
        if (service == null)
            return BadRequest($"No service for channel type: {channelType}");

        var config = ParseAccountConfig(account);
        var payload = await BuildPayload();

        try
        {
            var incoming = await service.ParseWebhookAsync(payload, config);
            if (incoming == null)
                return Ok();

            await RouteIncomingMessage(account, incoming);
            return Ok("EVENT_RECEIVED");
        }
        catch (WebhookChallengeException ex)
        {
            return Content(ex.Challenge, "text/plain");
        }
    }

    private async Task RouteIncomingMessage(ChannelAccount account, IncomingMessage incoming)
    {
        var remoteJid = incoming.RemoteJid;

        var conv = await _convRepo.GetByRemoteJidAsync(remoteJid);

        if (conv == null)
        {
            conv = new Conversation
            {
                RemoteJid = remoteJid,
                ContactName = incoming.SenderName ?? remoteJid,
                ChannelType = account.ChannelType,
                ChannelAccountId = account.Id,
                Status = "open",
                IsRead = false
            };
            _convRepo.Add(conv);
            await _uow.SaveChangesAsync();
        }
        else
        {
            conv.IsRead = false;
            conv.Status = "open";
            conv.LastMessageAt = DateTime.UtcNow;
            _convRepo.Update(conv);
        }

        var message = new Message
        {
            ConversationId = conv.Id,
            Content = incoming.Content,
            Direction = MessageDirection.Inbound,
            Status = MessageStatus.Delivered,
            MediaUrl = incoming.MediaUrl,
            MimeType = incoming.MediaMimeType,
            MessageType = incoming.MessageType,
            CreatedAt = DateTime.UtcNow
        };
        _msgRepo.Add(message);
        await _uow.SaveChangesAsync();

        try
        {
            await _hub.Clients.Group(conv.Id.ToString()).SendAsync("NewMessage", new
            {
                id = message.Id.ToString(),
                conversationId = conv.Id.ToString(),
                content = message.Content,
                direction = "Inbound",
                status = "Delivered",
                mediaUrl = message.MediaUrl,
                mimeType = message.MimeType,
                messageType = message.MessageType,
                createdAt = message.CreatedAt,
                contactName = conv.ContactName,
                remoteJid = conv.RemoteJid,
                channelType = account.ChannelType
            });

            await _hub.Clients.All.SendAsync("ConversationUpdated", new
            {
                id = conv.Id.ToString(),
                remoteJid = conv.RemoteJid,
                contactName = conv.ContactName,
                lastMessageAt = conv.LastMessageAt,
                channelType = account.ChannelType,
                status = conv.Status,
                isRead = conv.IsRead
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed");
        }
    }

    private static Dictionary<string, string> ParseAccountConfig(ChannelAccount account)
    {
        try
        {
            if (!string.IsNullOrEmpty(account.AccessToken))
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(account.AccessToken);
                if (parsed != null) return parsed;
            }
        }
        catch { }

        return new Dictionary<string, string>
        {
            ["accessToken"] = account.AccessToken ?? "",
            ["accountId"] = account.AccountId ?? ""
        };
    }
}

public class IncomingWebhookRequest
{
    public string? RemoteJid { get; set; }
    public string? Content { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? MessageId { get; set; }
    public string? MessageType { get; set; }
    public string? MediaUrl { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
}
