using Application.DTOs;
using MediatR;

namespace Application.Commands.Contacts;

public class CreateContactCommand : IRequest<ContactDto>
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = "whatsapp";
    public string? LifecycleStage { get; set; }
}
