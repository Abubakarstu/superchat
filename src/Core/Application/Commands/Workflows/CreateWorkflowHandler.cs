using Application.DTOs;
using Domain.Entities.Automation;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Workflows;

public class CreateWorkflowHandler : IRequestHandler<CreateWorkflowCommand, WorkflowDto>
{
    private readonly IWorkflowRepository _workflowRepo;
    private readonly IUnitOfWork _uow;

    public CreateWorkflowHandler(IWorkflowRepository workflowRepo, IUnitOfWork uow)
    {
        _workflowRepo = workflowRepo;
        _uow = uow;
    }

    public async Task<WorkflowDto> Handle(CreateWorkflowCommand request, CancellationToken ct)
    {
        var workflow = new Workflow
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        int ord = 0;
        foreach (var t in request.Triggers)
        {
            workflow.Triggers.Add(new WorkflowTrigger
            {
                EventType = t.EventType,
                Condition = t.Condition,
                Order = ord++
            });
        }

        ord = 0;
        foreach (var a in request.Actions)
        {
            workflow.Actions.Add(new WorkflowAction
            {
                ActionType = a.ActionType,
                Configuration = a.Configuration,
                DelayMinutes = a.DelayMinutes,
                Order = ord++
            });
        }

        _workflowRepo.Add(workflow);
        await _uow.SaveChangesAsync(ct);

        return new WorkflowDto
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description,
            IsActive = workflow.IsActive,
            CreatedAt = workflow.CreatedAt,
            Triggers = workflow.Triggers.Select(t => new WorkflowTriggerDto
            {
                Id = t.Id, EventType = t.EventType, Condition = t.Condition, Order = t.Order
            }).ToList(),
            Actions = workflow.Actions.Select(a => new WorkflowActionDto
            {
                Id = a.Id, ActionType = a.ActionType, Configuration = a.Configuration,
                Order = a.Order, DelayMinutes = a.DelayMinutes
            }).ToList()
        };
    }
}
