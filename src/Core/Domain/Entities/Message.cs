using Domain.Enums;

namespace Domain.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageDirection Direction { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public string? MediaUrl { get; set; }
    public string? MessageType { get; set; } = "text";

    public Conversation Conversation { get; set; } = null!;
}
