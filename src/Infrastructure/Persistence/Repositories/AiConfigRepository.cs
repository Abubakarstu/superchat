using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AiConfigRepository : IAiConfigRepository
{
    private readonly AppDbContext _context;

    public AiConfigRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AiConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AiConfigs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<AiConfig?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiConfigs.FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<AiConfig>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiConfigs.ToListAsync(cancellationToken);
    }

    public void Add(AiConfig config)
    {
        _context.AiConfigs.Add(config);
    }

    public void Update(AiConfig config)
    {
        _context.AiConfigs.Update(config);
    }

    public void Delete(AiConfig config)
    {
        _context.AiConfigs.Remove(config);
    }
}
