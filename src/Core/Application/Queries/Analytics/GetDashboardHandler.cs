using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Analytics;

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IConversationRepository _convRepo;
    private readonly IMessageRepository _msgRepo;
    private readonly IContactRepository _contactRepo;
    private readonly ICampaignRepository _campaignRepo;
    private readonly IAnalyticsEventRepository _analyticsRepo;
    private readonly IAgentPerformanceRepository _agentPerfRepo;
    private readonly IConversationMetricRepository _metricRepo;

    public GetDashboardHandler(
        IConversationRepository convRepo,
        IMessageRepository msgRepo,
        IContactRepository contactRepo,
        ICampaignRepository campaignRepo,
        IAnalyticsEventRepository analyticsRepo,
        IAgentPerformanceRepository agentPerfRepo,
        IConversationMetricRepository metricRepo)
    {
        _convRepo = convRepo;
        _msgRepo = msgRepo;
        _contactRepo = contactRepo;
        _campaignRepo = campaignRepo;
        _analyticsRepo = analyticsRepo;
        _agentPerfRepo = agentPerfRepo;
        _metricRepo = metricRepo;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var from = request.From ?? DateTime.UtcNow.AddDays(-30);
        var to = request.To ?? DateTime.UtcNow;

        var conversations = await _convRepo.GetAllAsync(ct);
        var allMessages = new List<Domain.Entities.Message>();
        foreach (var c in conversations)
        {
            var msgs = await _msgRepo.GetByConversationIdAsync(c.Id, ct);
            allMessages.AddRange(msgs);
        }
        var contacts = await _contactRepo.GetAllAsync(ct);
        var campaigns = await _campaignRepo.GetAllAsync(ct);
        var perf = await _agentPerfRepo.GetByDateRangeAsync(from, to, ct);

        return new DashboardDto
        {
            TotalConversations = conversations.Count(),
            ActiveConversations = conversations.Count(c => c.IsActive),
            MessagesToday = allMessages.Count(m => m.CreatedAt.Date == DateTime.UtcNow.Date),
            TotalContacts = contacts.Count(),
            CampaignsSent = campaigns.Count(c => c.Status == "SENT"),
            CampaignsDelivered = campaigns.Sum(c => c.DeliveredCount),
            AgentPerformance = perf.Select(p => new AgentPerformanceDto
            {
                AgentId = p.AgentId,
                ConversationsHandled = p.ConversationsHandled,
                MessagesSent = p.MessagesSent,
                AvgResponseTimeSeconds = p.AvgResponseTimeSeconds
            }).ToList()
        };
    }
}
