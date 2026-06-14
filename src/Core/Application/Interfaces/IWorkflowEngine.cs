namespace Application.Interfaces;

public interface IWorkflowEngine
{
    Task ExecuteAsync(string eventType, Guid? entityId = null, string? metadata = null);
}
