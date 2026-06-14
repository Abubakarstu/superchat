using Domain.Entities.WebWidget;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class WebWidgetRepository : IWebWidgetRepository
{
    private readonly AppDbContext _context;
    public WebWidgetRepository(AppDbContext context) { _context = context; }

    public async Task<WebWidget?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.WebWidgets.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IEnumerable<WebWidget>> GetAllAsync(CancellationToken ct = default) =>
        await _context.WebWidgets.ToListAsync(ct);

    public void Add(WebWidget widget) => _context.WebWidgets.Add(widget);
    public void Update(WebWidget widget) => _context.WebWidgets.Update(widget);
    public void Delete(WebWidget widget) => _context.WebWidgets.Remove(widget);
}
