using Domain.Entities;

namespace Domain.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByRemoteJidAsync(string remoteJid, CancellationToken cancellationToken = default);
    Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Conversation>> GetActiveAsync(CancellationToken cancellationToken = default);
    void Add(Conversation conversation);
    void Update(Conversation conversation);
    void Delete(Conversation conversation);
}
