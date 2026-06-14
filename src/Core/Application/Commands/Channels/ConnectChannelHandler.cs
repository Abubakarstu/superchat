using Application.DTOs;
using Application.Interfaces;
using Domain.Entities.Channels;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Channels;

public class ConnectChannelHandler : IRequestHandler<ConnectChannelCommand, ChannelAccountDto>
{
    private readonly IChannelAccountRepository _channelRepo;
    private readonly IUnitOfWork _uow;
    private readonly IChannelServiceFactory _factory;
    private readonly ILogger<ConnectChannelHandler> _logger;

    public ConnectChannelHandler(
        IChannelAccountRepository channelRepo,
        IUnitOfWork uow,
        IChannelServiceFactory factory,
        ILogger<ConnectChannelHandler> logger)
    {
        _channelRepo = channelRepo;
        _uow = uow;
        _factory = factory;
        _logger = logger;
    }

    public async Task<ChannelAccountDto> Handle(ConnectChannelCommand request, CancellationToken ct)
    {
        var account = new ChannelAccount
        {
            ChannelType = request.ChannelType,
            Name = request.Name,
            AccountId = request.AccountId,
            AccessToken = request.AccessToken,
            IsConnected = true
        };

        var service = _factory.GetService(request.ChannelType);
        if (service != null)
        {
            var validated = await service.ValidateConnectionAsync(request.AccessToken ?? "");
            account.IsConnected = validated;

            if (validated)
            {
                try
                {
                    var info = await service.GetChannelInfoAsync(request.AccessToken ?? "");
                    if (!string.IsNullOrEmpty(info) && info != "unknown" && info != request.ChannelType)
                        account.Name = info;
                }
                catch { }

                try
                {
                    var baseUrl = "https://your-domain.com";
                    var webhookSecret = await service.RegisterWebhookAsync(request.AccessToken ?? "", $"{baseUrl}/api/webhooks/meta/{account.Id}");
                    account.WebhookSecret = webhookSecret;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Webhook registration failed for {Type}", request.ChannelType);
                }
            }
        }

        _channelRepo.Add(account);
        await _uow.SaveChangesAsync(ct);

        return new ChannelAccountDto
        {
            Id = account.Id,
            ChannelType = account.ChannelType,
            Name = account.Name,
            IsConnected = account.IsConnected,
            CreatedAt = account.CreatedAt
        };
    }
}
