using Domain.Entities.Channels;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ChannelAccountRepository : IChannelAccountRepository
{
    private readonly AppDbContext _context;
    public ChannelAccountRepository(AppDbContext context) { _context = context; }

    public async Task<ChannelAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.ChannelAccounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<ChannelAccount>> GetByTypeAsync(string channelType, CancellationToken ct = default) =>
        await _context.ChannelAccounts.Where(a => a.ChannelType == channelType).ToListAsync(ct);

    public async Task<IEnumerable<ChannelAccount>> GetAllAsync(CancellationToken ct = default) =>
        await _context.ChannelAccounts.ToListAsync(ct);

    public void Add(ChannelAccount account) => _context.ChannelAccounts.Add(account);
    public void Update(ChannelAccount account) => _context.ChannelAccounts.Update(account);
    public void Delete(ChannelAccount account) => _context.ChannelAccounts.Remove(account);
}
