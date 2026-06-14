using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands;

public class ReactToMessageHandler : IRequestHandler<ReactToMessageCommand, Unit>
{
    private readonly IMessageRepository _messageRepo;
    private readonly IMessageReactionRepository _reactionRepo;
    private readonly IUnitOfWork _uow;

    public ReactToMessageHandler(IMessageRepository messageRepo, IMessageReactionRepository reactionRepo, IUnitOfWork uow)
    {
        _messageRepo = messageRepo;
        _reactionRepo = reactionRepo;
        _uow = uow;
    }

    public async Task<Unit> Handle(ReactToMessageCommand request, CancellationToken ct)
    {
        var msg = await _messageRepo.GetByIdAsync(request.MessageId, ct);
        if (msg == null) throw new KeyNotFoundException("Message not found");

        var existing = msg.Reactions.FirstOrDefault(r => r.SenderJid == request.SenderJid && r.Emoji == request.Emoji);
        if (existing != null)
        {
            _reactionRepo.Delete(existing);
        }
        else
        {
            var reaction = new MessageReaction
            {
                MessageId = request.MessageId,
                Emoji = request.Emoji,
                SenderJid = request.SenderJid,
                SenderName = request.SenderName
            };
            _reactionRepo.Add(reaction);
        }

        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
