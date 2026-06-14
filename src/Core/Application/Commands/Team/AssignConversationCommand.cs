using MediatR;

namespace Application.Commands.Team;

public class AssignConversationCommand : IRequest<bool>
{
    public Guid ConversationId { get; set; }
    public Guid AgentId { get; set; }
}
