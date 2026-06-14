using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HandleIncomingMessageHandler> _logger;

    public HandleIncomingMessageHandler(
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        IAiConfigRepository aiConfigRepo,
        IUnitOfWork unitOfWork,
        IAiService aiService,
        IWhatsAppService whatsAppService,
        IServiceScopeFactory scopeFactory,
        ILogger<HandleIncomingMessageHandler> logger)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _aiConfigRepo = aiConfigRepo;
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _whatsAppService = whatsAppService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<MessageDto> Handle(HandleIncomingMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepo.GetByRemoteJidAsync(request.RemoteJid, cancellationToken);
        var isNew = false;
        if (conversation == null)
        {
            conversation = new Conversation
            {
                RemoteJid = request.RemoteJid,
                ContactName = request.ContactName ?? request.RemoteJid,
                ContactPhone = request.ContactPhone
            };
            _conversationRepo.Add(conversation);
            isNew = true;
        }

        var incomingMessage = new Message
        {
            ConversationId = conversation.Id,
            Content = request.Content,
            Direction = MessageDirection.Inbound,
            Status = MessageStatus.Delivered,
            MessageType = request.MessageType ?? "text",
            MediaUrl = request.MediaUrl,
            FileName = request.FileName,
            MimeType = request.MimeType
        };
        _messageRepo.Add(incomingMessage);
        conversation.LastMessageAt = incomingMessage.CreatedAt;
        if (!isNew) _conversationRepo.Update(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var isMedia = request.MessageType != "text" && !string.IsNullOrEmpty(request.MessageType);
        if (!isMedia)
        {
            var remoteJid = request.RemoteJid;
            var content = request.Content;
            var conversationId = conversation.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var aiConfigRepo = scope.ServiceProvider.GetRequiredService<IAiConfigRepository>();
                    var messageRepo = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
                    var conversationRepo = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();
                    var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

                    var aiConfig = await aiConfigRepo.GetActiveAsync();
                    var reply = await aiService.GenerateReplyAsync(
                        content,
                        aiConfig?.SystemPrompt ?? "You are a helpful WhatsApp assistant.",
                        aiConfig?.Model ?? "llama3.2",
                        aiConfig?.Temperature ?? 0.7,
                        aiConfig?.MaxTokens ?? 1024,
                        aiConfig?.OllamaBaseUrl);

                    var replyMessage = new Message
                    {
                        ConversationId = conversationId,
                        Content = reply,
                        Direction = MessageDirection.Outbound,
                        Status = MessageStatus.Pending
                    };
                    messageRepo.Add(replyMessage);

                    await whatsAppService.SendMessageAsync(remoteJid, reply);
                    replyMessage.Status = MessageStatus.Sent;
                    messageRepo.Update(replyMessage);

                    var conv = await conversationRepo.GetByIdAsync(conversationId);
                    if (conv != null)
                    {
                        conv.LastMessageAt = replyMessage.CreatedAt;
                        conversationRepo.Update(conv);
                    }

                    await uow.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process AI reply for {RemoteJid}", remoteJid);
                }
            });
        }

        return new MessageDto
        {
            Id = incomingMessage.Id,
            ConversationId = incomingMessage.ConversationId,
            Content = incomingMessage.Content,
            Direction = incomingMessage.Direction.ToString(),
            Status = incomingMessage.Status.ToString(),
            CreatedAt = incomingMessage.CreatedAt,
            DeliveredAt = incomingMessage.DeliveredAt,
            ReadAt = incomingMessage.ReadAt,
            MediaUrl = incomingMessage.MediaUrl ?? request.MediaUrl,
            MessageType = incomingMessage.MessageType,
            FileName = incomingMessage.FileName ?? request.FileName,
            MimeType = incomingMessage.MimeType ?? request.MimeType
        };
    }
}
