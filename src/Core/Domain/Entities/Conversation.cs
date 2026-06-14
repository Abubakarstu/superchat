namespace Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RemoteJid { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
