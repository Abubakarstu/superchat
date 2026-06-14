using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

public class ForwardMessageHandler : IRequestHandler<ForwardMessageCommand, MessageDto>
{
    private readonly IMessageRepository _messageRepo;
    private readonly IConversationRepository _conversationRepo;
    private readonly IUnitOfWork _uow;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<ForwardMessageHandler> _logger;

    public ForwardMessageHandler(
        IMessageRepository messageRepo,
        IConversationRepository conversationRepo,
        IUnitOfWork uow,
        IWhatsAppService whatsAppService,
        ILogger<ForwardMessageHandler> logger)
    {
        _messageRepo = messageRepo;
        _conversationRepo = conversationRepo;
        _uow = uow;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<MessageDto> Handle(ForwardMessageCommand request, CancellationToken ct)
    {
        var original = await _messageRepo.GetByIdAsync(request.MessageId, ct);
        if (original == null) throw new KeyNotFoundException("Original message not found");

        var targetConv = await _conversationRepo.GetByRemoteJidAsync(request.TargetRemoteJid, ct);
        if (targetConv == null)
        {
            targetConv = new Conversation
            {
                RemoteJid = request.TargetRemoteJid,
                ContactName = request.TargetContactName ?? request.TargetRemoteJid,
                ContactPhone = request.TargetContactPhone,
                ChannelType = original.Conversation?.ChannelType ?? "whatsapp"
            };
            _conversationRepo.Add(targetConv);
        }

        var prefix = "Forwarded from " + (original.Conversation?.ContactName ?? "unknown") + ":\n\n";
        var content = prefix + original.Content;

        var forwarded = new Message
        {
            ConversationId = targetConv.Id,
            Content = content,
            Direction = MessageDirection.Outbound,
            Status = MessageStatus.Pending,
            MessageType = original.MessageType,
            MediaUrl = original.MediaUrl,
            FileName = original.FileName,
            MimeType = original.MimeType
        };
        _messageRepo.Add(forwarded);
        await _uow.SaveChangesAsync(ct);

        try
        {
            if (!string.IsNullOrEmpty(original.MediaUrl))
            {
                await _whatsAppService.SendMediaAsync(new SendMediaRequest
                {
                    RemoteJid = request.TargetRemoteJid,
                    MediaUrl = original.MediaUrl,
                    MediaType = original.MessageType ?? "document",
                    Caption = content,
                    FileName = original.FileName ?? "file"
                });
            }
            else
            {
                await _whatsAppService.SendMessageAsync(request.TargetRemoteJid, content);
            }
            forwarded.Status = MessageStatus.Sent;
            _messageRepo.Update(forwarded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forward failed");
            forwarded.Status = MessageStatus.Failed;
            _messageRepo.Update(forwarded);
        }

        targetConv.LastMessageAt = forwarded.CreatedAt;
        _conversationRepo.Update(targetConv);
        await _uow.SaveChangesAsync(ct);

        return new MessageDto
        {
            Id = forwarded.Id,
            ConversationId = forwarded.ConversationId,
            Content = forwarded.Content,
            Direction = "Outbound",
            Status = forwarded.Status.ToString(),
            CreatedAt = forwarded.CreatedAt,
            MessageType = forwarded.MessageType,
            MediaUrl = forwarded.MediaUrl,
            FileName = forwarded.FileName
        };
    }
}
