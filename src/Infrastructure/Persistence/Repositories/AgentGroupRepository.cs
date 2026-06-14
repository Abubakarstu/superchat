using Domain.Entities.Collaboration;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AgentGroupRepository : IAgentGroupRepository
{
    private readonly AppDbContext _context;
    public AgentGroupRepository(AppDbContext context) { _context = context; }

    public async Task<AgentGroup?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.AgentGroups.Include(g => g.Agents).FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<IEnumerable<AgentGroup>> GetAllAsync(CancellationToken ct = default) =>
        await _context.AgentGroups.Include(g => g.Agents).ToListAsync(ct);

    public void Add(AgentGroup group) => _context.AgentGroups.Add(group);
    public void Update(AgentGroup group) => _context.AgentGroups.Update(group);
    public void Delete(AgentGroup group) => _context.AgentGroups.Remove(group);
}
