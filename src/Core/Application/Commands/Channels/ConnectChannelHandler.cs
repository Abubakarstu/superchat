using Application.DTOs;
using Domain.Entities.Channels;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Channels;

public class ConnectChannelHandler : IRequestHandler<ConnectChannelCommand, ChannelAccountDto>
{
    private readonly IChannelAccountRepository _channelRepo;
    private readonly IUnitOfWork _uow;

    public ConnectChannelHandler(IChannelAccountRepository channelRepo, IUnitOfWork uow)
    {
        _channelRepo = channelRepo;
        _uow = uow;
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
        _channelRepo.Add(account);
        await _uow.SaveChangesAsync(ct);

        return new ChannelAccountDto
        {
            Id = account.Id, ChannelType = account.ChannelType,
            Name = account.Name, IsConnected = account.IsConnected, CreatedAt = account.CreatedAt
        };
    }
}
