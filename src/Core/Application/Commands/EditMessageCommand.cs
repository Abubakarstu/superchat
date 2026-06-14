using MediatR;

namespace Application.Commands;

public class EditMessageCommand : IRequest
{
    public Guid MessageId { get; set; }
    public string NewContent { get; set; } = string.Empty;
}
