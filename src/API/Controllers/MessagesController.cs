using Application.Commands;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<MessageHub> _hub;
    private readonly IWhatsAppService _whatsApp;

    public MessagesController(IMediator mediator, IHubContext<MessageHub> hub, IWhatsAppService whatsApp)
    {
        _mediator = mediator;
        _hub = hub;
        _whatsApp = whatsApp;
    }

    [HttpPost("{id:guid}/react")]
    public async Task<IActionResult> React(Guid id, [FromBody] ReactRequest request)
    {
        await _mediator.Send(new ReactToMessageCommand
        {
            MessageId = id,
            Emoji = request.Emoji,
            SenderJid = request.SenderJid ?? "system",
            SenderName = request.SenderName ?? "You"
        });
        // Send reaction to WhatsApp
        if (!string.IsNullOrEmpty(request.RemoteJid) && !string.IsNullOrEmpty(request.MessageKeyId))
        {
            _ = _whatsApp.SendReactionAsync(new SendReactionRequest
            {
                RemoteJid = request.RemoteJid,
                MessageId = request.MessageKeyId,
                Emoji = request.Emoji,
                Remove = string.IsNullOrEmpty(request.Emoji)
            });
        }
        await _hub.Clients.All.SendAsync("MessageReacted", new { messageId = id, emoji = request.Emoji, senderJid = request.SenderJid ?? "system" });
        return Ok();
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, [FromBody] ReadRequest request)
    {
        await _mediator.Send(new MarkMessageReadCommand { MessageId = id });
        if (!string.IsNullOrEmpty(request.RemoteJid) && request.MessageIds != null && request.MessageIds.Count > 0)
        {
            _ = _whatsApp.ReadReceiptsAsync(new ReadReceiptsRequest
            {
                RemoteJid = request.RemoteJid,
                MessageIds = request.MessageIds
            });
        }
        await _hub.Clients.All.SendAsync("MessageRead", new { messageId = id });
        return Ok();
    }

    [HttpPost("{id:guid}/forward")]
    public async Task<IActionResult> Forward(Guid id, [FromBody] ForwardRequest request)
    {
        var msg = await _mediator.Send(new ForwardMessageCommand
        {
            MessageId = id,
            TargetRemoteJid = request.TargetRemoteJid,
            TargetContactName = request.TargetContactName,
            TargetContactPhone = request.TargetContactPhone
        });
        await _hub.Clients.All.SendAsync("MessageReceived", msg);
        return Ok(msg);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, [FromBody] EditRequest request)
    {
        await _mediator.Send(new EditMessageCommand { MessageId = id, NewContent = request.Content });
        // Send edit to WhatsApp
        if (!string.IsNullOrEmpty(request.RemoteJid) && !string.IsNullOrEmpty(request.MessageKeyId))
        {
            _ = _whatsApp.EditMessageAsync(new EditMessageRequest
            {
                RemoteJid = request.RemoteJid,
                MessageId = request.MessageKeyId,
                NewText = request.Content
            });
        }
        await _hub.Clients.All.SendAsync("MessageEdited", new { messageId = id, content = request.Content });
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] bool forEveryone = false, [FromQuery] string? remoteJid = null, [FromQuery] string? messageKeyId = null)
    {
        await _mediator.Send(new DeleteMessageCommand { MessageId = id, ForEveryone = forEveryone });
        if (forEveryone && !string.IsNullOrEmpty(remoteJid) && !string.IsNullOrEmpty(messageKeyId))
        {
            _ = _whatsApp.DeleteMessageAsync(new DeleteMessageRequest
            {
                RemoteJid = remoteJid,
                MessageId = messageKeyId,
                ForEveryone = true
            });
        }
        await _hub.Clients.All.SendAsync("MessageDeleted", new { messageId = id, forEveryone });
        return Ok();
    }
}

public class ReactRequest
{
    public string Emoji { get; set; } = string.Empty;
    public string? SenderJid { get; set; }
    public string? SenderName { get; set; }
    public string? RemoteJid { get; set; }
    public string? MessageKeyId { get; set; }
}

public class ReadRequest
{
    public string? RemoteJid { get; set; }
    public List<string>? MessageIds { get; set; }
}

public class ForwardRequest
{
    public string TargetRemoteJid { get; set; } = string.Empty;
    public string? TargetContactName { get; set; }
    public string? TargetContactPhone { get; set; }
}

public class EditRequest
{
    public string Content { get; set; } = string.Empty;
    public string? RemoteJid { get; set; }
    public string? MessageKeyId { get; set; }
}
