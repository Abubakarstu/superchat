namespace Application.DTOs;

public class ContactDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? LifecycleStage { get; set; }
    public bool IsSubscribed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class UpdateContactDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
    public string? LifecycleStage { get; set; }
    public bool? IsSubscribed { get; set; }
}
