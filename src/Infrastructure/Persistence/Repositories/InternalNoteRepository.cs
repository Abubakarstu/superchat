using Domain.Entities.Collaboration;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class InternalNoteRepository : IInternalNoteRepository
{
    private readonly AppDbContext _context;
    public InternalNoteRepository(AppDbContext context) { _context = context; }

    public async Task<InternalNote?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.InternalNotes.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IEnumerable<InternalNote>> GetByConversationIdAsync(Guid conversationId, CancellationToken ct = default) =>
        await _context.InternalNotes.Where(n => n.ConversationId == conversationId).OrderByDescending(n => n.CreatedAt).ToListAsync(ct);

    public void Add(InternalNote note) => _context.InternalNotes.Add(note);
    public void Update(InternalNote note) => _context.InternalNotes.Update(note);
    public void Delete(InternalNote note) => _context.InternalNotes.Remove(note);
}
