using MediatR;

namespace Application.Commands.Contacts;

public class AddContactTagCommand : IRequest<bool>
{
    public Guid ContactId { get; set; }
    public string TagName { get; set; } = string.Empty;
}
