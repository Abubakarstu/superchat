using Domain.Entities.Automation;

namespace Domain.Interfaces;

public interface IWorkflowRepository
{
    Task<Workflow?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Workflow>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Workflow>> GetActiveByTriggerAsync(string eventType, CancellationToken ct = default);
    void Add(Workflow workflow);
    void Update(Workflow workflow);
    void Delete(Workflow workflow);
}
