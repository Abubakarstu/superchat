using Domain.Entities.Integrations;

namespace Domain.Interfaces;

public interface IIntegrationRepository
{
    Task<Integration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Integration>> GetByProviderAsync(string provider, CancellationToken ct = default);
    Task<IEnumerable<Integration>> GetAllAsync(CancellationToken ct = default);
    void Add(Integration integration);
    void Update(Integration integration);
    void Delete(Integration integration);
}
