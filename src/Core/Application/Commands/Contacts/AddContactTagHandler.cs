using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Contacts;

public class AddContactTagHandler : IRequestHandler<AddContactTagCommand, bool>
{
    private readonly IContactRepository _contactRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IUnitOfWork _uow;

    public AddContactTagHandler(IContactRepository contactRepo, ITagRepository tagRepo, IUnitOfWork uow)
    {
        _contactRepo = contactRepo;
        _tagRepo = tagRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(AddContactTagCommand request, CancellationToken ct)
    {
        var contact = await _contactRepo.GetByIdAsync(request.ContactId, ct);
        if (contact == null) return false;

        var tags = await _tagRepo.GetAllAsync(ct);
        var tag = tags.FirstOrDefault(t => t.Name == request.TagName);
        if (tag == null)
        {
            tag = new Tag { Name = request.TagName };
            _tagRepo.Add(tag);
        }

        if (!contact.ContactTags.Any(ctg => ctg.TagId == tag.Id))
        {
            contact.ContactTags.Add(new ContactTag { ContactId = contact.Id, TagId = tag.Id });
            _contactRepo.Update(contact);
            await _uow.SaveChangesAsync(ct);
        }

        return true;
    }
}
