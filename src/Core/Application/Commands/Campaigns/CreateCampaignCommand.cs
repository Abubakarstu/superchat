using Application.DTOs;
using MediatR;

namespace Application.Commands.Campaigns;

public class CreateCampaignCommand : IRequest<CampaignDto>
{
    public string Name { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public string? Recurrence { get; set; }
    public Guid? TemplateId { get; set; }
    public string? SegmentFilter { get; set; }
    public string? ChannelType { get; set; } = "whatsapp";
}
