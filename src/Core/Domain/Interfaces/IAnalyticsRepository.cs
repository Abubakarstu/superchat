using Domain.Entities.Analytics;

namespace Domain.Interfaces;

public interface IAnalyticsEventRepository
{
    void Add(AnalyticsEvent evt);
    Task<IEnumerable<AnalyticsEvent>> GetByTypeAsync(string eventType, DateTime from, DateTime to, CancellationToken ct = default);
}

public interface IConversationMetricRepository
{
    Task<IEnumerable<ConversationMetric>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    void Add(ConversationMetric metric);
}

public interface IAgentPerformanceRepository
{
    Task<IEnumerable<AgentPerformance>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    void Add(AgentPerformance performance);
}
