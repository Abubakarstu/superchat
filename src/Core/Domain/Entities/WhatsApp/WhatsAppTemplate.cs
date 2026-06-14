namespace Domain.Entities.WhatsApp;

public class WhatsAppTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WhatsAppAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string Category { get; set; } = "UTILITY";
    public string Status { get; set; } = "PENDING";
    public string? RejectionReason { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? Header { get; set; }
    public string? Footer { get; set; }
    public string? Buttons { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public string? TemplateId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public WhatsAppAccount Account { get; set; } = null!;
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
}
