namespace Domain.Entities.Channels;

public class ChannelAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ChannelType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string? AccessToken { get; set; }
    public string? WebhookSecret { get; set; }
    public bool IsConnected { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
