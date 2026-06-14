namespace Domain.Entities.Collaboration;

public class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "agent";
    public string Status { get; set; } = "offline";
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ConversationAssignment> Assignments { get; set; } = new List<ConversationAssignment>();
    public ICollection<InternalNote> Notes { get; set; } = new List<InternalNote>();
    public ICollection<AgentGroup> AgentGroups { get; set; } = new List<AgentGroup>();
}

public class ConversationAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid AgentId { get; set; }
    public string Type { get; set; } = "assigned";
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public Agent Agent { get; set; } = null!;
}

public class InternalNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid AgentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Conversation Conversation { get; set; } = null!;
    public Agent Agent { get; set; } = null!;
}

public class AgentGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
}
