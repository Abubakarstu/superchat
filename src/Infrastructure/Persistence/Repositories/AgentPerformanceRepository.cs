using Domain.Entities.Analytics;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AgentPerformanceRepository : IAgentPerformanceRepository
{
    private readonly AppDbContext _context;
    public AgentPerformanceRepository(AppDbContext context) { _context = context; }

    public async Task<IEnumerable<AgentPerformance>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.AgentPerformances.Where(p => p.Date >= from && p.Date <= to).ToListAsync(ct);

    public void Add(AgentPerformance performance) => _context.AgentPerformances.Add(performance);
}
