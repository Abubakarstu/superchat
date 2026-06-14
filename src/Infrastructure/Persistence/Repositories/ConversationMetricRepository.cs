using Domain.Entities.Analytics;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ConversationMetricRepository : IConversationMetricRepository
{
    private readonly AppDbContext _context;
    public ConversationMetricRepository(AppDbContext context) { _context = context; }

    public async Task<IEnumerable<ConversationMetric>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.ConversationMetrics.Where(m => m.Date >= from && m.Date <= to).ToListAsync(ct);

    public void Add(ConversationMetric metric) => _context.ConversationMetrics.Add(metric);
}
