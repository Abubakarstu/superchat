namespace Domain.Entities.WebWidget;

public class WebWidget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Default Widget";
    public string? GreetingText { get; set; } = "Hi! How can we help you?";
    public string? PrimaryColor { get; set; } = "#075e54";
    public string? Position { get; set; } = "right";
    public bool IsActive { get; set; } = true;
    public string? WhatsAppNumber { get; set; }
    public string? FallbackEmail { get; set; }
    public bool EnableBot { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
