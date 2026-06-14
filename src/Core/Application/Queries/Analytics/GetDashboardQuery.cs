using Application.DTOs;
using MediatR;

namespace Application.Queries.Analytics;

public class GetDashboardQuery : IRequest<DashboardDto>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
