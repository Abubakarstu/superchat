using Domain.Entities;

namespace Domain.Interfaces;

public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Contact?> GetByPhoneAsync(string phone, CancellationToken ct = default);
    Task<IEnumerable<Contact>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Contact>> GetByTagAsync(Guid tagId, CancellationToken ct = default);
    void Add(Contact contact);
    void Update(Contact contact);
    void Delete(Contact contact);
}
