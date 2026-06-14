using Domain.Entities;

namespace Domain.Interfaces;

public interface IAiConfigRepository
{
    Task<AiConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AiConfig?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AiConfig>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(AiConfig config);
    void Update(AiConfig config);
    void Delete(AiConfig config);
}
