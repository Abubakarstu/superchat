namespace Domain.Entities.Automation;

public class Workflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<WorkflowTrigger> Triggers { get; set; } = new List<WorkflowTrigger>();
    public ICollection<WorkflowAction> Actions { get; set; } = new List<WorkflowAction>();
}

public class WorkflowTrigger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public int Order { get; set; }
    public Workflow Workflow { get; set; } = null!;
}

public class WorkflowAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Configuration { get; set; } = string.Empty;
    public int Order { get; set; }
    public int? DelayMinutes { get; set; }
    public Workflow Workflow { get; set; } = null!;
}
