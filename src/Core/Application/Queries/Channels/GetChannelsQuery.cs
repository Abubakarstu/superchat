using Application.DTOs;
using MediatR;

namespace Application.Queries.Channels;

public class GetChannelsQuery : IRequest<IEnumerable<ChannelAccountDto>>
{
}
