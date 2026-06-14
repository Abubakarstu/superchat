using MediatR;

namespace Application.Commands;

public class MarkMessageReadCommand : IRequest<Unit>
{
    public Guid MessageId { get; set; }
}
