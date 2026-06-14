using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Contacts;

public class UpdateContactHandler : IRequestHandler<UpdateContactCommand, ContactDto>
{
    private readonly IContactRepository _contactRepo;
    private readonly IUnitOfWork _uow;

    public UpdateContactHandler(IContactRepository contactRepo, IUnitOfWork uow)
    {
        _contactRepo = contactRepo;
        _uow = uow;
    }

    public async Task<ContactDto> Handle(UpdateContactCommand request, CancellationToken ct)
    {
        var contact = await _contactRepo.GetByIdAsync(request.Id, ct);
        if (contact == null) throw new KeyNotFoundException($"Contact {request.Id} not found");

        if (request.Name != null) contact.Name = request.Name;
        if (request.Email != null) contact.Email = request.Email;
        if (request.Company != null) contact.Company = request.Company;
        if (request.Notes != null) contact.Notes = request.Notes;
        if (request.LifecycleStage != null) contact.LifecycleStage = request.LifecycleStage;
        if (request.IsSubscribed.HasValue) contact.IsSubscribed = request.IsSubscribed.Value;

        _contactRepo.Update(contact);
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
            CreatedAt = contact.CreatedAt,
            LastActivityAt = contact.LastActivityAt
        };
    }
}
