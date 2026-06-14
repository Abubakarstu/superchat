using Domain.Interfaces;
using MediatR;

namespace Application.Commands;

public class EditMessageHandler : IRequestHandler<EditMessageCommand>
{
    private readonly IMessageRepository _messageRepo;
    private readonly IUnitOfWork _uow;

    public EditMessageHandler(IMessageRepository messageRepo, IUnitOfWork uow)
    {
        _messageRepo = messageRepo;
        _uow = uow;
    }

    public async Task Handle(EditMessageCommand request, CancellationToken ct)
    {
        var msg = await _messageRepo.GetByIdAsync(request.MessageId, ct);
        if (msg == null) throw new KeyNotFoundException("Message not found");

        if (string.IsNullOrEmpty(request.NewContent))
            throw new ArgumentException("Content cannot be empty");

        msg.Content = request.NewContent;
        msg.IsEdited = true;
        msg.EditedAt = DateTime.UtcNow;
        _messageRepo.Update(msg);
        await _uow.SaveChangesAsync(ct);
    }
}
