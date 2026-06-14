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
    private readonly IChannelServiceFactory _channelFactory;
    private readonly ILogger<SendMessageHandler> _logger;

    public SendMessageHandler(
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        IUnitOfWork unitOfWork,
        IWhatsAppService whatsAppService,
        IChannelServiceFactory channelFactory,
        ILogger<SendMessageHandler> logger)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
        _channelFactory = channelFactory;
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

        var isMedia = !string.IsNullOrEmpty(request.MessageType) && request.MessageType != "text";

        var message = new Message
        {
            ConversationId = conversation.Id,
            Content = request.Content,
            Direction = MessageDirection.Outbound,
            Status = MessageStatus.Pending,
            MessageType = request.MessageType ?? (isMedia ? request.MessageType : "text"),
            MediaUrl = request.MediaUrl,
            FileName = request.FileName,
            MimeType = request.MimeType,
            ReplyToId = request.ReplyToId
        };
        _messageRepo.Add(message);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            if (conversation.ChannelType == "whatsapp")
            {
                if (isMedia && !string.IsNullOrEmpty(request.MediaUrl))
                {
                    await _whatsAppService.SendMediaAsync(new SendMediaRequest
                    {
                        RemoteJid = request.RemoteJid,
                        MediaUrl = request.MediaUrl,
                        MediaType = request.MessageType ?? "document",
                        Caption = request.Content,
                        FileName = request.FileName ?? "file",
                        MimeType = request.MimeType
                    });
                }
                else
                {
                    await _whatsAppService.SendMessageAsync(request.RemoteJid, request.Content);
                }
            }
            else
            {
                var channelService = _channelFactory.GetService(conversation.ChannelType);
                if (channelService != null && conversation.ChannelAccountId.HasValue)
                {
                    await channelService.SendMessageAsync(
                        conversation.ChannelAccountId.ToString()!,
                        conversation.RemoteJid,
                        request.Content);
                }
            }
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
            ReadAt = message.ReadAt,
            MediaUrl = message.MediaUrl,
            MessageType = message.MessageType,
            FileName = message.FileName,
            FileSize = message.FileSize,
            MimeType = message.MimeType,
            ReplyToId = message.ReplyToId,
            IsEdited = message.IsEdited,
            EditedAt = message.EditedAt
        };
    }
}
