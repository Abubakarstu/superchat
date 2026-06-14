using Domain.Interfaces;
using MediatR;

namespace Application.Commands;

public class MarkMessageReadHandler : IRequestHandler<MarkMessageReadCommand, Unit>
{
    private readonly IMessageRepository _messageRepo;
    private readonly IUnitOfWork _uow;

    public MarkMessageReadHandler(IMessageRepository messageRepo, IUnitOfWork uow)
    {
        _messageRepo = messageRepo;
        _uow = uow;
    }

    public async Task<Unit> Handle(MarkMessageReadCommand request, CancellationToken ct)
    {
        var msg = await _messageRepo.GetByIdAsync(request.MessageId, ct);
        if (msg == null) return Unit.Value;
        msg.ReadAt = DateTime.UtcNow;
        msg.Status = Domain.Enums.MessageStatus.Read;
        _messageRepo.Update(msg);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
