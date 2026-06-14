using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<SendMessageHandler> _logger;

    public SendMessageHandler(
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        IUnitOfWork unitOfWork,
        IWhatsAppService whatsAppService,
        ILogger<SendMessageHandler> logger)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
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

        var message = new Message
        {
            ConversationId = conversation.Id,
            Content = request.Content,
            Direction = MessageDirection.Outbound,
            Status = MessageStatus.Pending
        };
        _messageRepo.Add(message);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _whatsAppService.SendMessageAsync(request.RemoteJid, request.Content);
            message.Status = MessageStatus.Sent;
            _messageRepo.Update(message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to {RemoteJid}", request.RemoteJid);
            message.Status = MessageStatus.Failed;
            _messageRepo.Update(message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        conversation.LastMessageAt = message.CreatedAt;
        _conversationRepo.Update(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Content = message.Content,
            Direction = message.Direction.ToString(),
            Status = message.Status.ToString(),
            CreatedAt = message.CreatedAt,
            DeliveredAt = message.DeliveredAt,
            MediaUrl = message.MediaUrl,
            MessageType = message.MessageType
        };
    }
}
