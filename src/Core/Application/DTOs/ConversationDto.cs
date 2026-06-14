namespace Application.DTOs;

public class ConversationDto
{
    public Guid Id { get; set; }
    public string RemoteJid { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
    public string? LastMessage { get; set; }
}
