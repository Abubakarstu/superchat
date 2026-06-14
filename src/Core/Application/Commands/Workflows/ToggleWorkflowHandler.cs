using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Workflows;

public class ToggleWorkflowHandler : IRequestHandler<ToggleWorkflowCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepo;
    private readonly IUnitOfWork _uow;

    public ToggleWorkflowHandler(IWorkflowRepository workflowRepo, IUnitOfWork uow)
    {
        _workflowRepo = workflowRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(ToggleWorkflowCommand request, CancellationToken ct)
    {
        var wf = await _workflowRepo.GetByIdAsync(request.Id, ct);
        if (wf == null) return false;
        wf.IsActive = request.IsActive;
        wf.UpdatedAt = DateTime.UtcNow;
        _workflowRepo.Update(wf);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
