using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Workflows;

public class GetWorkflowsHandler : IRequestHandler<GetWorkflowsQuery, IEnumerable<WorkflowDto>>
{
    private readonly IWorkflowRepository _workflowRepo;

    public GetWorkflowsHandler(IWorkflowRepository workflowRepo)
    {
        _workflowRepo = workflowRepo;
    }

    public async Task<IEnumerable<WorkflowDto>> Handle(GetWorkflowsQuery request, CancellationToken ct)
    {
        var workflows = await _workflowRepo.GetAllAsync(ct);
        return workflows.Select(w => new WorkflowDto
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            IsActive = w.IsActive,
            CreatedAt = w.CreatedAt,
            Triggers = w.Triggers.Select(t => new WorkflowTriggerDto
            {
                Id = t.Id, EventType = t.EventType, Condition = t.Condition, Order = t.Order
            }).ToList(),
            Actions = w.Actions.Select(a => new WorkflowActionDto
            {
                Id = a.Id, ActionType = a.ActionType, Configuration = a.Configuration,
                Order = a.Order, DelayMinutes = a.DelayMinutes
            }).ToList()
        });
    }
}
