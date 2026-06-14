using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Campaigns;

public class CreateCampaignHandler : IRequestHandler<CreateCampaignCommand, CampaignDto>
{
    private readonly ICampaignRepository _campaignRepo;
    private readonly IUnitOfWork _uow;

    public CreateCampaignHandler(ICampaignRepository campaignRepo, IUnitOfWork uow)
    {
        _campaignRepo = campaignRepo;
        _uow = uow;
    }

    public async Task<CampaignDto> Handle(CreateCampaignCommand request, CancellationToken ct)
    {
        var campaign = new Campaign
        {
            Name = request.Name,
            ScheduledAt = request.ScheduledAt,
            Recurrence = request.Recurrence,
            TemplateId = request.TemplateId,
            SegmentFilter = request.SegmentFilter,
            ChannelType = request.ChannelType,
            Status = request.ScheduledAt.HasValue ? "SCHEDULED" : "DRAFT"
        };
        _campaignRepo.Add(campaign);
        await _uow.SaveChangesAsync(ct);

        return new CampaignDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Status = campaign.Status,
            ScheduledAt = campaign.ScheduledAt,
            Recurrence = campaign.Recurrence,
            ChannelType = campaign.ChannelType,
            CreatedAt = campaign.CreatedAt,
            TemplateId = campaign.TemplateId
        };
    }
}
