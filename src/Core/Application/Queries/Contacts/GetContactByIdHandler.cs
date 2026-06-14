using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Contacts;

public class GetContactByIdHandler : IRequestHandler<GetContactByIdQuery, ContactDto?>
{
    private readonly IContactRepository _contactRepo;

    public GetContactByIdHandler(IContactRepository contactRepo)
    {
        _contactRepo = contactRepo;
    }

    public async Task<ContactDto?> Handle(GetContactByIdQuery request, CancellationToken ct)
    {
        var c = await _contactRepo.GetByIdAsync(request.Id, ct);
        if (c == null) return null;

        return new ContactDto
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
        };
    }
}
