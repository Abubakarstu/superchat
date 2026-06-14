using Domain.Entities.Integrations;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class IntegrationRepository : IIntegrationRepository
{
    private readonly AppDbContext _context;
    public IntegrationRepository(AppDbContext context) { _context = context; }

    public async Task<Integration?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Integrations.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IEnumerable<Integration>> GetByProviderAsync(string provider, CancellationToken ct = default) =>
        await _context.Integrations.Where(i => i.Provider == provider).ToListAsync(ct);

    public async Task<IEnumerable<Integration>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Integrations.ToListAsync(ct);

    public void Add(Integration integration) => _context.Integrations.Add(integration);
    public void Update(Integration integration) => _context.Integrations.Update(integration);
    public void Delete(Integration integration) => _context.Integrations.Remove(integration);
}
