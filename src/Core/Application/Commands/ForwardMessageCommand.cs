using Application.DTOs;
using MediatR;

namespace Application.Commands;

public class ForwardMessageCommand : IRequest<MessageDto>
{
    public Guid MessageId { get; set; }
    public string TargetRemoteJid { get; set; } = string.Empty;
    public string? TargetContactName { get; set; }
    public string? TargetContactPhone { get; set; }
}
