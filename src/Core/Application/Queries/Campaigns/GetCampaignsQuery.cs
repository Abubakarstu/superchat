using Application.DTOs;
using MediatR;

namespace Application.Queries.Campaigns;

public class GetCampaignsQuery : IRequest<IEnumerable<CampaignDto>>
{
}
