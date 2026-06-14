using Application.DTOs;
using MediatR;

namespace Application.Commands.Channels;

public class ConnectChannelCommand : IRequest<ChannelAccountDto>
{
    public string ChannelType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string? AccessToken { get; set; }
}
