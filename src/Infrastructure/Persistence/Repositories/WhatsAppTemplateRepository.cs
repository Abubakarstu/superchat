using Domain.Entities.WhatsApp;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class WhatsAppTemplateRepository : IWhatsAppTemplateRepository
{
    private readonly AppDbContext _context;
    public WhatsAppTemplateRepository(AppDbContext context) { _context = context; }

    public async Task<WhatsAppTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.WhatsAppTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IEnumerable<WhatsAppTemplate>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default) =>
        await _context.WhatsAppTemplates.Where(t => t.WhatsAppAccountId == accountId).ToListAsync(ct);

    public async Task<IEnumerable<WhatsAppTemplate>> GetAllAsync(CancellationToken ct = default) =>
        await _context.WhatsAppTemplates.ToListAsync(ct);

    public void Add(WhatsAppTemplate template) => _context.WhatsAppTemplates.Add(template);
    public void Update(WhatsAppTemplate template) => _context.WhatsAppTemplates.Update(template);
    public void Delete(WhatsAppTemplate template) => _context.WhatsAppTemplates.Remove(template);
}
