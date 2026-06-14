namespace Application.DTOs;

public class DashboardDto
{
    public int TotalConversations { get; set; }
    public int ActiveConversations { get; set; }
    public int MessagesToday { get; set; }
    public double AvgFirstResponseTimeSeconds { get; set; }
    public double AvgResolutionTimeSeconds { get; set; }
    public int TotalContacts { get; set; }
    public int CampaignsSent { get; set; }
    public int CampaignsDelivered { get; set; }
    public List<AgentPerformanceDto> AgentPerformance { get; set; } = new();
    public List<ConversationTrendDto> ConversationTrend { get; set; } = new();
}

public class AgentPerformanceDto
{
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int ConversationsHandled { get; set; }
    public int MessagesSent { get; set; }
    public double AvgResponseTimeSeconds { get; set; }
}

public class ConversationTrendDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
