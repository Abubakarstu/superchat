using Domain.Entities.WhatsApp;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class WhatsAppAccountRepository : IWhatsAppAccountRepository
{
    private readonly AppDbContext _context;
    public WhatsAppAccountRepository(AppDbContext context) { _context = context; }

    public async Task<WhatsAppAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.WhatsAppAccounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<WhatsAppAccount>> GetAllAsync(CancellationToken ct = default) =>
        await _context.WhatsAppAccounts.ToListAsync(ct);

    public void Add(WhatsAppAccount account) => _context.WhatsAppAccounts.Add(account);
    public void Update(WhatsAppAccount account) => _context.WhatsAppAccounts.Update(account);
    public void Delete(WhatsAppAccount account) => _context.WhatsAppAccounts.Remove(account);
}
