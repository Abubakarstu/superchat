using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace API;

public class MessageHub : Hub
{
    private readonly IConversationRepository _convRepo;
    private readonly IUnitOfWork _uow;

    public MessageHub(IConversationRepository convRepo, IUnitOfWork uow)
    {
        _convRepo = convRepo;
        _uow = uow;
    }

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
    }

    public async Task Typing(string remoteJid, bool isTyping)
    {
        var conv = await _convRepo.GetByRemoteJidAsync(remoteJid);
        if (conv != null)
        {
            conv.IsTyping = isTyping;
            _convRepo.Update(conv);
            await _uow.SaveChangesAsync();
        }
        await Clients.All.SendAsync("ContactTyping", new { remoteJid, isTyping });
    }

    public async Task Online(string remoteJid, bool isOnline)
    {
        var conv = await _convRepo.GetByRemoteJidAsync(remoteJid);
        if (conv != null)
        {
            conv.IsOnline = isOnline;
            if (!isOnline) conv.LastSeenAt = DateTime.UtcNow;
            _convRepo.Update(conv);
            await _uow.SaveChangesAsync();
        }
        await Clients.All.SendAsync("ContactOnline", new { remoteJid, isOnline, lastSeenAt = conv?.LastSeenAt });
    }
}
