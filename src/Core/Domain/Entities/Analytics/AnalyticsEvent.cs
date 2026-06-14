namespace Domain.Entities.Analytics;

public class AnalyticsEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? AgentId { get; set; }
    public string? Metadata { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ConversationMetric
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public DateTime Date { get; set; }
    public int MessageCount { get; set; }
    public double FirstResponseTimeSeconds { get; set; }
    public double ResolutionTimeSeconds { get; set; }
    public bool IsResolved { get; set; }
    public Guid? AssignedAgentId { get; set; }
}

public class AgentPerformance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public DateTime Date { get; set; }
    public int ConversationsHandled { get; set; }
    public int MessagesSent { get; set; }
    public double AvgResponseTimeSeconds { get; set; }
    public double AvgResolutionTimeSeconds { get; set; }
    public int ResolvedCount { get; set; }
    public int EscalatedCount { get; set; }
}
