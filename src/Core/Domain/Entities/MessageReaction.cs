namespace Domain.Entities;

public class MessageReaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public string Emoji { get; set; } = string.Empty;
    public string? SenderJid { get; set; }
    public string? SenderName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Message Message { get; set; } = null!;
}
