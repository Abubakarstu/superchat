using Application.Commands;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace API.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<MessageHub> _hubContext;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IMediator mediator, IHubContext<MessageHub> hubContext, ILogger<WebhookController> logger)
    {
        _mediator = mediator;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("incoming")]
    public async Task<IActionResult> ReceiveMessage([FromBody] WebhookRequest request)
    {
        _logger.LogInformation("Webhook received from {RemoteJid}: {Content}", request.RemoteJid, request.Content);

        var command = new HandleIncomingMessageCommand
        {
            RemoteJid = request.RemoteJid,
            Content = request.Content,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            MessageId = request.MessageId,
            MessageType = request.MessageType
        };

        var messageDto = await _mediator.Send(command);

        await _hubContext.Clients.All.SendAsync("MessageReceived", messageDto);
        await _hubContext.Clients.All.SendAsync("ConversationUpdated", new { messageDto.ConversationId });

        return Ok(new { status = "received", messageId = messageDto.Id });
    }

    [HttpPost("status")]
    public IActionResult StatusUpdate([FromBody] JsonElement body)
    {
        _logger.LogInformation("Status update: {Body}", body.ToString());
        return Ok(new { status = "logged" });
    }
}

public class WebhookRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? MessageId { get; set; }
    public string? MessageType { get; set; }
}
