using Domain.Interfaces;
using MediatR;

namespace Application.Commands;

public class DeleteMessageHandler : IRequestHandler<DeleteMessageCommand>
{
    private readonly IMessageRepository _messageRepo;
    private readonly IUnitOfWork _uow;

    public DeleteMessageHandler(IMessageRepository messageRepo, IUnitOfWork uow)
    {
        _messageRepo = messageRepo;
        _uow = uow;
    }

    public async Task Handle(DeleteMessageCommand request, CancellationToken ct)
    {
        var msg = await _messageRepo.GetByIdAsync(request.MessageId, ct);
        if (msg == null) throw new KeyNotFoundException("Message not found");

        if (request.ForEveryone)
        {
            _messageRepo.Delete(msg);
        }
        else
        {
            msg.IsDeletedForMe = true;
            msg.DeletedAt = DateTime.UtcNow;
            _messageRepo.Update(msg);
        }
        await _uow.SaveChangesAsync(ct);
    }
}
