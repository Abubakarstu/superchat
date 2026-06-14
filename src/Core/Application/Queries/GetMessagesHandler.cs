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
            ReadAt = m.ReadAt,
            MediaUrl = m.MediaUrl,
            MessageType = m.MessageType,
            FileName = m.FileName,
            FileSize = m.FileSize,
            MimeType = m.MimeType,
            ReplyToId = m.ReplyToId,
            IsEdited = m.IsEdited,
            EditedAt = m.EditedAt,
            IsDeletedForMe = m.IsDeletedForMe,
            Reactions = m.Reactions.Select(r => new ReactionDto
            {
                Id = r.Id,
                MessageId = r.MessageId,
                Emoji = r.Emoji,
                SenderJid = r.SenderJid,
                SenderName = r.SenderName,
                CreatedAt = r.CreatedAt
            }).ToList(),
            ReplyTo = m.ReplyTo != null ? new MessageDto
            {
                Id = m.ReplyTo.Id,
                Content = m.ReplyTo.Content,
                MessageType = m.ReplyTo.MessageType,
                MediaUrl = m.ReplyTo.MediaUrl,
                FileName = m.ReplyTo.FileName,
                Direction = m.ReplyTo.Direction.ToString(),
                CreatedAt = m.ReplyTo.CreatedAt
            } : null
        });
    }
}
