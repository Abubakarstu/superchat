namespace Application.DTOs;

public class CampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public string? Recurrence { get; set; }
    public bool IsRecurring { get; set; }
    public string? ChannelType { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalRecipients { get; set; }
    public int DeliveredCount { get; set; }
    public int OpenedCount { get; set; }
    public int ClickedCount { get; set; }
    public int RepliedCount { get; set; }
    public Guid? TemplateId { get; set; }
}

public class CreateCampaignDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public string? Recurrence { get; set; }
    public Guid? TemplateId { get; set; }
    public string? SegmentFilter { get; set; }
    public string? ChannelType { get; set; }
}
