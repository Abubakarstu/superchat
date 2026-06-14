using Domain.Entities.Collaboration;

namespace Domain.Interfaces;

public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<Agent>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Agent>> GetByGroupAsync(Guid groupId, CancellationToken ct = default);
    void Add(Agent agent);
    void Update(Agent agent);
    void Delete(Agent agent);
}

public interface IAgentGroupRepository
{
    Task<AgentGroup?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<AgentGroup>> GetAllAsync(CancellationToken ct = default);
    void Add(AgentGroup group);
    void Update(AgentGroup group);
    void Delete(AgentGroup group);
}

public interface IInternalNoteRepository
{
    Task<InternalNote?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<InternalNote>> GetByConversationIdAsync(Guid conversationId, CancellationToken ct = default);
    void Add(InternalNote note);
    void Update(InternalNote note);
    void Delete(InternalNote note);
}
