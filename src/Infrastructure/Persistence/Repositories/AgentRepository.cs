using Domain.Entities.Collaboration;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly AppDbContext _context;
    public AgentRepository(AppDbContext context) { _context = context; }

    public async Task<Agent?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Agents.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Agents.FirstOrDefaultAsync(a => a.Email == email, ct);

    public async Task<IEnumerable<Agent>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Agents.ToListAsync(ct);

    public async Task<IEnumerable<Agent>> GetByGroupAsync(Guid groupId, CancellationToken ct = default) =>
        await _context.AgentGroups.Where(g => g.Id == groupId).SelectMany(g => g.Agents).ToListAsync(ct);

    public void Add(Agent agent) => _context.Agents.Add(agent);
    public void Update(Agent agent) => _context.Agents.Update(agent);
    public void Delete(Agent agent) => _context.Agents.Remove(agent);
}
