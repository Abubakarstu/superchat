namespace Application.Interfaces;

public interface IAnalyticsService
{
    Task TrackEventAsync(string eventType, string? entityType = null, Guid? entityId = null, Guid? agentId = null, string? metadata = null);
}
