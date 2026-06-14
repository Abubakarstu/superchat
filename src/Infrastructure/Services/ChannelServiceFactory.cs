using Application.Interfaces;

namespace Infrastructure.Services;

public class ChannelServiceFactory : IChannelServiceFactory
{
    private readonly IEnumerable<IChannelService> _services;

    public ChannelServiceFactory(IEnumerable<IChannelService> services)
    {
        _services = services;
    }

    public IChannelService? GetService(string channelType)
    {
        return _services.FirstOrDefault(s =>
            s.ChannelType.Equals(channelType, StringComparison.OrdinalIgnoreCase));
    }
}
