using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Channels;

public class GetChannelsHandler : IRequestHandler<GetChannelsQuery, IEnumerable<ChannelAccountDto>>
{
    private readonly IChannelAccountRepository _channelRepo;

    public GetChannelsHandler(IChannelAccountRepository channelRepo)
    {
        _channelRepo = channelRepo;
    }

    public async Task<IEnumerable<ChannelAccountDto>> Handle(GetChannelsQuery request, CancellationToken ct)
    {
        var channels = await _channelRepo.GetAllAsync(ct);
        return channels.Select(c => new ChannelAccountDto
        {
            Id = c.Id, ChannelType = c.ChannelType,
            Name = c.Name, IsConnected = c.IsConnected, CreatedAt = c.CreatedAt
        });
    }
}
