namespace Application.DTOs;

public class TemplateDto
{
    public Guid Id { get; set; }
    public Guid WhatsAppAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? Header { get; set; }
    public string? Footer { get; set; }
    public string? Buttons { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string Category { get; set; } = "UTILITY";
    public string Body { get; set; } = string.Empty;
    public string? Header { get; set; }
    public string? Footer { get; set; }
    public string? Buttons { get; set; }
}
