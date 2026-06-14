using Domain.Entities.WhatsApp;

namespace Domain.Interfaces;

public interface IWhatsAppAccountRepository
{
    Task<WhatsAppAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<WhatsAppAccount>> GetAllAsync(CancellationToken ct = default);
    void Add(WhatsAppAccount account);
    void Update(WhatsAppAccount account);
    void Delete(WhatsAppAccount account);
}
