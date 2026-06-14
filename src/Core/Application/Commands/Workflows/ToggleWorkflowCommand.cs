using MediatR;

namespace Application.Commands.Workflows;

public class ToggleWorkflowCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}
