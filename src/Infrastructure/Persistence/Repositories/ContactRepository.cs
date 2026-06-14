using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly AppDbContext _context;
    public ContactRepository(AppDbContext context) { _context = context; }

    public async Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Contacts.Include(c => c.ContactTags).ThenInclude(ct => ct.Tag).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Contact?> GetByPhoneAsync(string phone, CancellationToken ct = default) =>
        await _context.Contacts.Include(c => c.ContactTags).ThenInclude(ct => ct.Tag).FirstOrDefaultAsync(c => c.Phone == phone, ct);

    public async Task<IEnumerable<Contact>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Contacts.Include(c => c.ContactTags).ThenInclude(ct => ct.Tag).OrderByDescending(c => c.LastActivityAt).ToListAsync(ct);

    public async Task<IEnumerable<Contact>> GetByTagAsync(Guid tagId, CancellationToken ct = default) =>
        await _context.Contacts.Include(c => c.ContactTags).ThenInclude(ct => ct.Tag)
            .Where(c => c.ContactTags.Any(ct => ct.TagId == tagId)).ToListAsync(ct);

    public void Add(Contact contact) => _context.Contacts.Add(contact);
    public void Update(Contact contact) => _context.Contacts.Update(contact);
    public void Delete(Contact contact) => _context.Contacts.Remove(contact);
}
