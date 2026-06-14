using Domain.Entities;

namespace Domain.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    void Add(Message message);
    void Update(Message message);
    void Delete(Message message);
}
