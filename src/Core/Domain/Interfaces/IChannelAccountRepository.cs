using Domain.Entities.Channels;

namespace Domain.Interfaces;

public interface IChannelAccountRepository
{
    Task<ChannelAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ChannelAccount>> GetByTypeAsync(string channelType, CancellationToken ct = default);
    Task<IEnumerable<ChannelAccount>> GetAllAsync(CancellationToken ct = default);
    void Add(ChannelAccount account);
    void Update(ChannelAccount account);
    void Delete(ChannelAccount account);
}
