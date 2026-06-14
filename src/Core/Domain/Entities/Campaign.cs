using Domain.Entities.WhatsApp;

namespace Domain.Entities;

public class Campaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT";
    public DateTime? ScheduledAt { get; set; }
    public string? Recurrence { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? TemplateId { get; set; }
    public string? SegmentFilter { get; set; }
    public string? ChannelType { get; set; } = "whatsapp";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public int TotalRecipients { get; set; }
    public int DeliveredCount { get; set; }
    public int OpenedCount { get; set; }
    public int ClickedCount { get; set; }
    public int RepliedCount { get; set; }
    public int UnsubscribeCount { get; set; }
    public WhatsAppTemplate? Template { get; set; }
    public ICollection<CampaignRecipient> Recipients { get; set; } = new List<CampaignRecipient>();
}

public class CampaignRecipient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public Guid ContactId { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? RepliedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
}
