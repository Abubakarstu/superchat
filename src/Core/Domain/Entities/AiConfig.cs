namespace Domain.Entities;

public class AiConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Default";
    public string SystemPrompt { get; set; } = "You are a helpful WhatsApp assistant.";
    public string Provider { get; set; } = "ollama";
    public string? Model { get; set; } = "llama3.2";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1024;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
