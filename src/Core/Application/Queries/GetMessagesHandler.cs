using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries;

public class GetMessagesHandler : IRequestHandler<GetMessagesQuery, IEnumerable<MessageDto>>
{
    private readonly IMessageRepository _messageRepo;

    public GetMessagesHandler(IMessageRepository messageRepo)
    {
        _messageRepo = messageRepo;
    }

    public async Task<IEnumerable<MessageDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _messageRepo.GetByConversationIdAsync(request.ConversationId, cancellationToken);
        return messages.OrderBy(m => m.CreatedAt).Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Content = m.Content,
            Direction = m.Direction.ToString(),
            Status = m.Status.ToString(),
            CreatedAt = m.CreatedAt,
            DeliveredAt = m.DeliveredAt,
            MediaUrl = m.MediaUrl,
            MessageType = m.MessageType
        });
    }
}
