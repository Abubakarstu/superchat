using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class TagRepository : ITagRepository
{
    private readonly AppDbContext _context;
    public TagRepository(AppDbContext context) { _context = context; }

    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IEnumerable<Tag>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Tags.ToListAsync(ct);

    public void Add(Tag tag) => _context.Tags.Add(tag);
    public void Update(Tag tag) => _context.Tags.Update(tag);
    public void Delete(Tag tag) => _context.Tags.Remove(tag);
}
