namespace Application.Interfaces;

public interface IChannelService
{
    string ChannelType { get; }
    Task SendMessageAsync(string channelAccountId, string remoteJid, string message);
    Task<bool> ValidateConnectionAsync(string channelAccountId);
    Task<string> GetChannelInfoAsync(string channelAccountId);
}

public interface IChannelServiceFactory
{
    IChannelService? GetService(string channelType);
}
