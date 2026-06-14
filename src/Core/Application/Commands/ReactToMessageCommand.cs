using MediatR;

namespace Application.Commands;

public class ReactToMessageCommand : IRequest<Unit>
{
    public Guid MessageId { get; set; }
    public string Emoji { get; set; } = string.Empty;
    public string? SenderJid { get; set; }
    public string? SenderName { get; set; }
}
