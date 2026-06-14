using Application.Commands;
using Application.DTOs;
using Application.Interfaces;
using Application.Queries;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<MessageHub> _hub;
    private readonly IWhatsAppService _whatsApp;
    private readonly IConversationRepository _convRepo;
    private readonly IMessageRepository _msgRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IMediator mediator,
        IHubContext<MessageHub> hub,
        IWhatsAppService whatsApp,
        IConversationRepository convRepo,
        IMessageRepository msgRepo,
        IUnitOfWork uow,
        ILogger<ConversationsController> logger)
    {
        _mediator = mediator;
        _hub = hub;
        _whatsApp = whatsApp;
        _convRepo = convRepo;
        _msgRepo = msgRepo;
        _uow = uow;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConversationDto>>> GetAll([FromQuery] bool? activeOnly)
    {
        var query = new GetConversationsQuery { ActiveOnly = activeOnly };
        var conversations = await _mediator.Send(query);
        return Ok(conversations);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(Guid id)
    {
        var query = new GetMessagesQuery { ConversationId = id };
        var messages = await _mediator.Send(query);
        return Ok(messages);
    }

    [HttpPost("{remoteJid}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(string remoteJid, [FromBody] SendMessageRequest request)
    {
        var command = new SendMessageCommand
        {
            RemoteJid = remoteJid,
            Content = request.Content ?? "",
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            MessageType = request.MessageType,
            MediaUrl = request.MediaUrl,
            FileName = request.FileName,
            MimeType = request.MimeType,
            ReplyToId = request.ReplyToId
        };

        var message = await _mediator.Send(command);
        await _hub.Clients.All.SendAsync("MessageReceived", message);
        await _hub.Clients.All.SendAsync("ConversationUpdated", new { message.ConversationId });
        return Ok(message);
    }

    [HttpPost("send-contact")]
    public async Task<IActionResult> SendContact([FromBody] SendContactRequest request)
    {
        try
        {
            await _whatsApp.SendContactAsync(request);
            // Store as a message in the conversation
            var conv = await _convRepo.GetByRemoteJidAsync(request.RemoteJid);
            if (conv == null)
                return Ok();
            var msg = new Message
            {
                ConversationId = conv.Id,
                Content = $"👤 Contact: {request.ContactName} ({request.ContactPhone})",
                Direction = MessageDirection.Outbound,
                Status = MessageStatus.Sent,
                MessageType = "contact",
                CreatedAt = DateTime.UtcNow
            };
            _msgRepo.Add(msg);
            await _uow.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("MessageReceived", new
            {
                id = msg.Id.ToString(), conversationId = conv.Id.ToString(),
                content = msg.Content, direction = "Outbound", messageType = "contact"
            });
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send contact failed");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("send-poll")]
    public async Task<IActionResult> SendPoll([FromBody] SendPollRequest request)
    {
        try
        {
            await _whatsApp.SendPollAsync(request);
            var conv = await _convRepo.GetByRemoteJidAsync(request.RemoteJid);
            if (conv == null)
                return Ok();
            var msg = new Message
            {
                ConversationId = conv.Id,
                Content = $"📊 Poll: {request.PollName} [{string.Join(", ", request.Options)}]",
                Direction = MessageDirection.Outbound,
                Status = MessageStatus.Sent,
                MessageType = "poll",
                CreatedAt = DateTime.UtcNow
            };
            _msgRepo.Add(msg);
            await _uow.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("MessageReceived", new
            {
                id = msg.Id.ToString(), conversationId = conv.Id.ToString(),
                content = msg.Content, direction = "Outbound", messageType = "poll"
            });
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send poll failed");
            return StatusCode(500, ex.Message);
        }
    }
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? MessageType { get; set; }
    public string? MediaUrl { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public Guid? ReplyToId { get; set; }
}
