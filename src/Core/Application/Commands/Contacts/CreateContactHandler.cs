using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Contacts;

public class CreateContactHandler : IRequestHandler<CreateContactCommand, ContactDto>
{
    private readonly IContactRepository _contactRepo;
    private readonly IUnitOfWork _uow;

    public CreateContactHandler(IContactRepository contactRepo, IUnitOfWork uow)
    {
        _contactRepo = contactRepo;
        _uow = uow;
    }

    public async Task<ContactDto> Handle(CreateContactCommand request, CancellationToken ct)
    {
        var existing = request.Phone != null ? await _contactRepo.GetByPhoneAsync(request.Phone, ct) : null;
        if (existing != null)
        {
            return new ContactDto
            {
                Id = existing.Id,
                Name = existing.Name,
                Phone = existing.Phone,
                Email = existing.Email,
                Company = existing.Company,
                Notes = existing.Notes,
                Source = existing.Source,
                LifecycleStage = existing.LifecycleStage,
                IsSubscribed = existing.IsSubscribed,
                CreatedAt = existing.CreatedAt,
                LastActivityAt = existing.LastActivityAt
            };
        }

        var contact = new Contact
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Company = request.Company,
            Notes = request.Notes,
            Source = request.Source,
            LifecycleStage = request.LifecycleStage
        };
        _contactRepo.Add(contact);
        await _uow.SaveChangesAsync(ct);

        return new ContactDto
        {
            Id = contact.Id,
            Name = contact.Name,
            Phone = contact.Phone,
            Email = contact.Email,
            Company = contact.Company,
            Notes = contact.Notes,
            Source = contact.Source,
            LifecycleStage = contact.LifecycleStage,
            IsSubscribed = contact.IsSubscribed,
            CreatedAt = contact.CreatedAt
        };
    }
}
