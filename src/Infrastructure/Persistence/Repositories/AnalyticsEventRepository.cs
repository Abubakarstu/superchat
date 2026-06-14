using Domain.Entities.Analytics;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AnalyticsEventRepository : IAnalyticsEventRepository
{
    private readonly AppDbContext _context;
    public AnalyticsEventRepository(AppDbContext context) { _context = context; }

    public void Add(AnalyticsEvent evt) => _context.AnalyticsEvents.Add(evt);

    public async Task<IEnumerable<AnalyticsEvent>> GetByTypeAsync(string eventType, DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.AnalyticsEvents.Where(e => e.EventType == eventType && e.Timestamp >= from && e.Timestamp <= to).ToListAsync(ct);
}
