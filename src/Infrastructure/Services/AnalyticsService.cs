using Application.Interfaces;
using Domain.Entities.Analytics;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsEventRepository _eventRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(IAnalyticsEventRepository eventRepo, IUnitOfWork uow, ILogger<AnalyticsService> logger)
    {
        _eventRepo = eventRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task TrackEventAsync(string eventType, string? entityType = null, Guid? entityId = null, Guid? agentId = null, string? metadata = null)
    {
        try
        {
            var evt = new AnalyticsEvent
            {
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                AgentId = agentId,
                Metadata = metadata
            };
            _eventRepo.Add(evt);
            await _uow.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track analytics event {EventType}", eventType);
        }
    }
}
