using Application.DTOs;
using MediatR;

namespace Application.Commands;

public class SendTemplateCommand : IRequest<MessageDto>
{
    public string RemoteJid { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public string? Header { get; set; }
    public string? Footer { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContentType { get; set; }
    public string? TypesJson { get; set; }
}
