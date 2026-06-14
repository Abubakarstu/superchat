using Application.DTOs;
using MediatR;

namespace Application.Commands.Templates;

public class UpdateTemplateCommand : IRequest<TemplateDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string Category { get; set; } = "UTILITY";
    public string Body { get; set; } = string.Empty;
    public string? Header { get; set; }
    public string? Footer { get; set; }
    public string? Buttons { get; set; }
    public string? ContentType { get; set; }
    public string? TypesJson { get; set; }
}
