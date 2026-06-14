using Domain.Entities.Channels;
using Domain.Entities.Collaboration;

namespace Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RemoteJid { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string ChannelType { get; set; } = "whatsapp";
    public string Status { get; set; } = "open";
    public string Priority { get; set; } = "normal";
    public bool IsRead { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? ChannelAccountId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsOnline { get; set; }
    public bool IsTyping { get; set; }
    public DateTime? LastSeenAt { get; set; }

    public Contact? Contact { get; set; }
    public ChannelAccount? ChannelAccount { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ConversationAssignment> Assignments { get; set; } = new List<ConversationAssignment>();
    public ICollection<InternalNote> Notes { get; set; } = new List<InternalNote>();
}
