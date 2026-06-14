using Application.DTOs;
using MediatR;

namespace Application.Queries.Team;

public class GetAgentsQuery : IRequest<IEnumerable<AgentDto>>
{
}
