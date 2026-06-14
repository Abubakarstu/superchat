using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Campaigns;

public class GetCampaignsHandler : IRequestHandler<GetCampaignsQuery, IEnumerable<CampaignDto>>
{
    private readonly ICampaignRepository _campaignRepo;

    public GetCampaignsHandler(ICampaignRepository campaignRepo)
    {
        _campaignRepo = campaignRepo;
    }

    public async Task<IEnumerable<CampaignDto>> Handle(GetCampaignsQuery request, CancellationToken ct)
    {
        var campaigns = await _campaignRepo.GetAllAsync(ct);
        return campaigns.Select(c => new CampaignDto
        {
            Id = c.Id,
            Name = c.Name,
            Status = c.Status,
            ScheduledAt = c.ScheduledAt,
            Recurrence = c.Recurrence,
            IsRecurring = c.IsRecurring,
            ChannelType = c.ChannelType,
            CreatedAt = c.CreatedAt,
            TotalRecipients = c.TotalRecipients,
            DeliveredCount = c.DeliveredCount,
            OpenedCount = c.OpenedCount,
            ClickedCount = c.ClickedCount,
            RepliedCount = c.RepliedCount,
            TemplateId = c.TemplateId
        });
    }
}
