using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

public class HandleIncomingMessageHandler : IRequestHandler<HandleIncomingMessageCommand, MessageDto>
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IAiConfigRepository _aiConfigRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiService _aiService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<HandleIncomingMessageHandler> _logger;

    public HandleIncomingMessageHandler(
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        IAiConfigRepository aiConfigRepo,
        IUnitOfWork unitOfWork,
        IAiService aiService,
        IWhatsAppService whatsAppService,
        ILogger<HandleIncomingMessageHandler> logger)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _aiConfigRepo = aiConfigRepo;
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<MessageDto> Handle(HandleIncomingMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepo.GetByRemoteJidAsync(request.RemoteJid, cancellationToken);
        if (conversation == null)
        {
            conversation = new Conversation
            {
                RemoteJid = request.RemoteJid,
                ContactName = request.ContactName ?? request.RemoteJid,
                ContactPhone = request.ContactPhone
            };
            _conversationRepo.Add(conversation);
        }

        var incomingMessage = new Message
        {
            ConversationId = conversation.Id,
            Content = request.Content,
            Direction = MessageDirection.Inbound,
            Status = MessageStatus.Delivered,
            MessageType = request.MessageType ?? "text"
        };
        _messageRepo.Add(incomingMessage);
        conversation.LastMessageAt = incomingMessage.CreatedAt;
        _conversationRepo.Update(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                var aiConfig = await _aiConfigRepo.GetActiveAsync(cancellationToken);
                var reply = await _aiService.GenerateReplyAsync(
                    request.Content,
                    aiConfig?.SystemPrompt ?? "You are a helpful WhatsApp assistant.",
                    aiConfig?.Model ?? "claude-sonnet-4-20250514",
                    aiConfig?.Temperature ?? 0.7,
                    aiConfig?.MaxTokens ?? 1024);

                var replyMessage = new Message
                {
                    ConversationId = conversation.Id,
                    Content = reply,
                    Direction = MessageDirection.Outbound,
                    Status = MessageStatus.Pending
                };
                _messageRepo.Add(replyMessage);

                await _whatsAppService.SendMessageAsync(request.RemoteJid, reply);
                replyMessage.Status = MessageStatus.Sent;
                _messageRepo.Update(replyMessage);
                conversation.LastMessageAt = replyMessage.CreatedAt;
                _conversationRepo.Update(conversation);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process AI reply for {RemoteJid}", request.RemoteJid);
            }
        }, cancellationToken);

        return new MessageDto
        {
            Id = incomingMessage.Id,
            ConversationId = incomingMessage.ConversationId,
            Content = incomingMessage.Content,
            Direction = incomingMessage.Direction.ToString(),
            Status = incomingMessage.Status.ToString(),
            CreatedAt = incomingMessage.CreatedAt,
            DeliveredAt = incomingMessage.DeliveredAt,
            MediaUrl = incomingMessage.MediaUrl,
            MessageType = incomingMessage.MessageType
        };
    }
}
