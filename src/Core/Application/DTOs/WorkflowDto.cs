namespace Application.DTOs;

public class WorkflowDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkflowTriggerDto> Triggers { get; set; } = new();
    public List<WorkflowActionDto> Actions { get; set; } = new();
}

public class WorkflowTriggerDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public int Order { get; set; }
}

public class WorkflowActionDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Configuration { get; set; } = string.Empty;
    public int Order { get; set; }
    public int? DelayMinutes { get; set; }
}
