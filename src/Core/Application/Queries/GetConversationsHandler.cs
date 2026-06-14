using Application.DTOs;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public class GetConversationsHandler : IRequestHandler<GetConversationsQuery, IEnumerable<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly ILogger<GetConversationsHandler> _logger;

    public GetConversationsHandler(
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        ILogger<GetConversationsHandler> logger)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversations = request.ActiveOnly == true
            ? await _conversationRepo.GetActiveAsync(cancellationToken)
            : await _conversationRepo.GetAllAsync(cancellationToken);

        var result = new List<ConversationDto>();
        foreach (var conv in conversations)
        {
            var messages = (await _messageRepo.GetByConversationIdAsync(conv.Id, cancellationToken)).ToList();
            result.Add(new ConversationDto
            {
                Id = conv.Id,
                RemoteJid = conv.RemoteJid,
                ContactName = conv.ContactName,
                ContactPhone = conv.ContactPhone,
                ChannelType = conv.ChannelType,
                AvatarUrl = conv.Contact?.AvatarUrl,
                IsOnline = conv.IsOnline,
                IsTyping = conv.IsTyping,
                LastSeenAt = conv.LastSeenAt,
                CreatedAt = conv.CreatedAt,
                LastMessageAt = conv.LastMessageAt,
                MessageCount = messages.Count,
                LastMessage = messages.LastOrDefault()?.Content
            });
        }

        return result.OrderByDescending(c => c.LastMessageAt);
    }
}
