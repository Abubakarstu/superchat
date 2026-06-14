using Application.DTOs;
using MediatR;

namespace Application.Commands.Templates;

public class CreateTemplateCommand : IRequest<TemplateDto>
{
    public Guid WhatsAppAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string Category { get; set; } = "UTILITY";
    public string Body { get; set; } = string.Empty;
    public string? Header { get; set; }
    public string? Footer { get; set; }
    public string? Buttons { get; set; }
}
