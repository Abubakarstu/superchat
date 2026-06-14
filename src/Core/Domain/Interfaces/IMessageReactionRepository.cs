using Domain.Entities;

namespace Domain.Interfaces;

public interface IMessageReactionRepository
{
    void Add(MessageReaction reaction);
    void Delete(MessageReaction reaction);
}
