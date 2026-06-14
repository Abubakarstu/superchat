using MediatR;

namespace Application.Commands;

public class DeleteMessageCommand : IRequest
{
    public Guid MessageId { get; set; }
    public bool ForEveryone { get; set; }
}
