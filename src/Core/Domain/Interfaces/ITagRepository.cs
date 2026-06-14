using Domain.Entities;

namespace Domain.Interfaces;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Tag>> GetAllAsync(CancellationToken ct = default);
    void Add(Tag tag);
    void Update(Tag tag);
    void Delete(Tag tag);
}
