using Application.DTOs;
using MediatR;

namespace Application.Commands.Team;

public class CreateAgentCommand : IRequest<AgentDto>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "agent";
}
