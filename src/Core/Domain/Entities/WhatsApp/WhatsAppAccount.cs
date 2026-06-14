namespace Domain.Entities.WhatsApp;

public class WhatsAppAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PhoneNumberId { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string WabaId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Location { get; set; }
    public bool IsVerified { get; set; }
    public bool IsConnected { get; set; }
    public string WebhookSecret { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastVerifiedAt { get; set; }
    public ICollection<WhatsAppTemplate> Templates { get; set; } = new List<WhatsAppTemplate>();
}
