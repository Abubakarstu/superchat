using Domain.Entities.Collaboration;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Team;

public class AssignConversationHandler : IRequestHandler<AssignConversationCommand, bool>
{
    private readonly IConversationRepository _convRepo;
    private readonly IAgentRepository _agentRepo;
    private readonly IUnitOfWork _uow;

    public AssignConversationHandler(IConversationRepository convRepo, IAgentRepository agentRepo, IUnitOfWork uow)
    {
        _convRepo = convRepo;
        _agentRepo = agentRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(AssignConversationCommand request, CancellationToken ct)
    {
        var conv = await _convRepo.GetByIdAsync(request.ConversationId, ct);
        var agent = await _agentRepo.GetByIdAsync(request.AgentId, ct);
        if (conv == null || agent == null) return false;

        conv.Assignments.Add(new ConversationAssignment
        {
            ConversationId = conv.Id,
            AgentId = agent.Id,
            Type = "assigned"
        });
        _convRepo.Update(conv);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
