using Domain.Entities;

namespace Domain.Interfaces;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Campaign>> GetAllAsync(CancellationToken ct = default);
    void Add(Campaign campaign);
    void Update(Campaign campaign);
    void Delete(Campaign campaign);
}
