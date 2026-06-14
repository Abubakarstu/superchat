namespace Application.DTOs;

public class WebWidgetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GreetingText { get; set; }
    public string? PrimaryColor { get; set; }
    public string? Position { get; set; }
    public bool IsActive { get; set; }
    public string? WhatsAppNumber { get; set; }
    public bool EnableBot { get; set; }
}
