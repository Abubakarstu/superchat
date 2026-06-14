using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _context;

    public ConversationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.Include(c => c.Contact).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetByRemoteJidAsync(string remoteJid, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.Include(c => c.Contact).FirstOrDefaultAsync(c => c.RemoteJid == remoteJid, cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.Include(c => c.Contact).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.Include(c => c.Contact).Where(c => c.IsActive).ToListAsync(cancellationToken);
    }

    public void Add(Conversation conversation)
    {
        _context.Conversations.Add(conversation);
    }

    public void Update(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
    }

    public void Delete(Conversation conversation)
    {
        _context.Conversations.Remove(conversation);
    }
}
