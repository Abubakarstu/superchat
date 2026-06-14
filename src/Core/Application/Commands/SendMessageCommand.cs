using Application.DTOs;
using MediatR;

namespace Application.Commands;

public class SendMessageCommand : IRequest<MessageDto>
{
    public string RemoteJid { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
}
