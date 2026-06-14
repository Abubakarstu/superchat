namespace Domain.Entities;

public class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = "whatsapp";
    public string? LifecycleStage { get; set; } = "lead";
    public bool IsSubscribed { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityAt { get; set; }
    public string? CustomFields { get; set; }
    public ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<CampaignRecipient> CampaignRecipients { get; set; } = new List<CampaignRecipient>();
}

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6c757d";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
}

public class ContactTag
{
    public Guid ContactId { get; set; }
    public Guid TagId { get; set; }
    public Contact Contact { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
