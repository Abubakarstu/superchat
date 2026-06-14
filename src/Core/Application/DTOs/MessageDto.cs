namespace Application.DTOs;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? MediaUrl { get; set; }
    public string? MessageType { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public Guid? ReplyToId { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeletedForMe { get; set; }
    public List<ReactionDto> Reactions { get; set; } = new();
    public MessageDto? ReplyTo { get; set; }
}

public class ReactionDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Emoji { get; set; } = string.Empty;
    public string? SenderJid { get; set; }
    public string? SenderName { get; set; }
    public DateTime CreatedAt { get; set; }
}
