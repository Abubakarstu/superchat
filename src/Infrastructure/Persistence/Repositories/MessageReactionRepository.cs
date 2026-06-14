using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Persistence.Repositories;

public class MessageReactionRepository : IMessageReactionRepository
{
    private readonly AppDbContext _context;

    public MessageReactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(MessageReaction reaction)
    {
        _context.MessageReactions.Add(reaction);
    }

    public void Delete(MessageReaction reaction)
    {
        _context.MessageReactions.Remove(reaction);
    }
}
