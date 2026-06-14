using Application.Commands;
using Application.DTOs;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator)
    {
        _mediator = mediator;
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
            Content = request.Content,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone
        };

        var message = await _mediator.Send(command);
        return Ok(message);
    }
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
}
