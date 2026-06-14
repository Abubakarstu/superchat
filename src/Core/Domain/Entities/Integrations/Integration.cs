namespace Domain.Entities.Integrations;

public class Integration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? ConfigJson { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncAt { get; set; }
}
