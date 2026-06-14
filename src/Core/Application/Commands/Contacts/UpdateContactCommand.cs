using Application.DTOs;
using MediatR;

namespace Application.Commands.Contacts;

public class UpdateContactCommand : IRequest<ContactDto>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
    public string? LifecycleStage { get; set; }
    public bool? IsSubscribed { get; set; }
}
