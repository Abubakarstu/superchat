using MediatR;

namespace Application.Commands.Campaigns;

public class SendCampaignCommand : IRequest<bool>
{
    public Guid CampaignId { get; set; }
}
