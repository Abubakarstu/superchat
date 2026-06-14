using Application.DTOs;
using MediatR;

namespace Application.Commands.Workflows;

public class CreateWorkflowCommand : IRequest<WorkflowDto>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<WorkflowTriggerDto> Triggers { get; set; } = new();
    public List<WorkflowActionDto> Actions { get; set; } = new();
}
