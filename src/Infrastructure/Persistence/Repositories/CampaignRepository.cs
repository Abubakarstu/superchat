using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CampaignRepository : ICampaignRepository
{
    private readonly AppDbContext _context;
    public CampaignRepository(AppDbContext context) { _context = context; }

    public async Task<Campaign?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Campaigns.Include(c => c.Recipients).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<Campaign>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Campaigns.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

    public void Add(Campaign campaign) => _context.Campaigns.Add(campaign);
    public void Update(Campaign campaign) => _context.Campaigns.Update(campaign);
    public void Delete(Campaign campaign) => _context.Campaigns.Remove(campaign);
}
