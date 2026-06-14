using Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers;

[ApiController]
[Route("api/presence")]
public class PresenceController : ControllerBase
{
    private readonly IConversationRepository _convRepo;
    private readonly IUnitOfWork _uow;
    private readonly IHubContext<MessageHub> _hub;

    public PresenceController(IConversationRepository convRepo, IUnitOfWork uow, IHubContext<MessageHub> hub)
    {
        _convRepo = convRepo;
        _uow = uow;
        _hub = hub;
    }

    [HttpPost("typing")]
    public async Task<IActionResult> Typing([FromBody] TypingRequest request)
    {
        var conv = await _convRepo.GetByRemoteJidAsync(request.RemoteJid);
        if (conv != null)
        {
            conv.IsTyping = request.IsTyping;
            _convRepo.Update(conv);
            await _uow.SaveChangesAsync();
        }
        await _hub.Clients.All.SendAsync("ContactTyping", new { remoteJid = request.RemoteJid, isTyping = request.IsTyping });
        return Ok();
    }

    [HttpPost("online")]
    public async Task<IActionResult> Online([FromBody] OnlineRequest request)
    {
        var conv = await _convRepo.GetByRemoteJidAsync(request.RemoteJid);
        if (conv != null)
        {
            conv.IsOnline = request.IsOnline;
            if (!request.IsOnline) conv.LastSeenAt = DateTime.UtcNow;
            _convRepo.Update(conv);
            await _uow.SaveChangesAsync();
        }
        await _hub.Clients.All.SendAsync("ContactOnline", new { remoteJid = request.RemoteJid, isOnline = request.IsOnline, lastSeenAt = conv?.LastSeenAt });
        return Ok();
    }
}

public class TypingRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public bool IsTyping { get; set; }
}

public class OnlineRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}
