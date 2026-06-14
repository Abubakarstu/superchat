using Application.DTOs;
using Domain.Entities.Collaboration;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Team;

public class CreateAgentHandler : IRequestHandler<CreateAgentCommand, AgentDto>
{
    private readonly IAgentRepository _agentRepo;
    private readonly IUnitOfWork _uow;

    public CreateAgentHandler(IAgentRepository agentRepo, IUnitOfWork uow)
    {
        _agentRepo = agentRepo;
        _uow = uow;
    }

    public async Task<AgentDto> Handle(CreateAgentCommand request, CancellationToken ct)
    {
        var agent = new Agent { Name = request.Name, Email = request.Email, Role = request.Role };
        _agentRepo.Add(agent);
        await _uow.SaveChangesAsync(ct);
        return new AgentDto
        {
            Id = agent.Id, Name = agent.Name, Email = agent.Email,
            Role = agent.Role, Status = agent.Status, CreatedAt = agent.CreatedAt
        };
    }
}
