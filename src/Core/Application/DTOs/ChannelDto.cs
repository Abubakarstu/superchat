namespace Application.DTOs;

public class ChannelAccountDto
{
    public Guid Id { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public DateTime CreatedAt { get; set; }
}
