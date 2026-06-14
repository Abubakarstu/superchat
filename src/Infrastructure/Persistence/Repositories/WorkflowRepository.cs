using Domain.Entities.Automation;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly AppDbContext _context;
    public WorkflowRepository(AppDbContext context) { _context = context; }

    public async Task<Workflow?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Workflows.Include(w => w.Triggers).Include(w => w.Actions).FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IEnumerable<Workflow>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Workflows.Include(w => w.Triggers).Include(w => w.Actions).ToListAsync(ct);

    public async Task<IEnumerable<Workflow>> GetActiveByTriggerAsync(string eventType, CancellationToken ct = default) =>
        await _context.Workflows.Include(w => w.Triggers).Include(w => w.Actions)
            .Where(w => w.IsActive && w.Triggers.Any(t => t.EventType == eventType)).ToListAsync(ct);

    public void Add(Workflow workflow) => _context.Workflows.Add(workflow);
    public void Update(Workflow workflow) => _context.Workflows.Update(workflow);
    public void Delete(Workflow workflow) => _context.Workflows.Remove(workflow);
}
