using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Team;

public class GetAgentsHandler : IRequestHandler<GetAgentsQuery, IEnumerable<AgentDto>>
{
    private readonly IAgentRepository _agentRepo;

    public GetAgentsHandler(IAgentRepository agentRepo)
    {
        _agentRepo = agentRepo;
    }

    public async Task<IEnumerable<AgentDto>> Handle(GetAgentsQuery request, CancellationToken ct)
    {
        var agents = await _agentRepo.GetAllAsync(ct);
        return agents.Select(a => new AgentDto
        {
            Id = a.Id, Name = a.Name, Email = a.Email,
            Role = a.Role, Status = a.Status, AvatarUrl = a.AvatarUrl, CreatedAt = a.CreatedAt
        });
    }
}
