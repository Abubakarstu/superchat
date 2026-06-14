using Domain.Entities.WhatsApp;

namespace Domain.Interfaces;

public interface IWhatsAppTemplateRepository
{
    Task<WhatsAppTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<WhatsAppTemplate>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
    Task<IEnumerable<WhatsAppTemplate>> GetAllAsync(CancellationToken ct = default);
    void Add(WhatsAppTemplate template);
    void Update(WhatsAppTemplate template);
    void Delete(WhatsAppTemplate template);
}
