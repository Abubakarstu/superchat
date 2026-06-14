using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Contacts;

public class GetContactsHandler : IRequestHandler<GetContactsQuery, IEnumerable<ContactDto>>
{
    private readonly IContactRepository _contactRepo;
    private readonly ITagRepository _tagRepo;

    public GetContactsHandler(IContactRepository contactRepo, ITagRepository tagRepo)
    {
        _contactRepo = contactRepo;
        _tagRepo = tagRepo;
    }

    public async Task<IEnumerable<ContactDto>> Handle(GetContactsQuery request, CancellationToken ct)
    {
        var contacts = await _contactRepo.GetAllAsync(ct);

        if (!string.IsNullOrEmpty(request.Tag))
        {
            var tags = await _tagRepo.GetAllAsync(ct);
            var tag = tags.FirstOrDefault(t => t.Name == request.Tag);
            if (tag != null)
                contacts = await _contactRepo.GetByTagAsync(tag.Id, ct);
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            var q = request.Search.ToLower();
            contacts = contacts.Where(c =>
                c.Name.ToLower().Contains(q) ||
                (c.Phone?.Contains(q) ?? false) ||
                (c.Email?.ToLower().Contains(q) ?? false));
        }

        return contacts.Select(c => new ContactDto
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            Company = c.Company,
            Notes = c.Notes,
            Source = c.Source,
            LifecycleStage = c.LifecycleStage,
            IsSubscribed = c.IsSubscribed,
            CreatedAt = c.CreatedAt,
            LastActivityAt = c.LastActivityAt,
            Tags = c.ContactTags.Select(ct => ct.Tag.Name).ToList()
        });
    }
}
