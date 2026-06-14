using Application.DTOs;
using MediatR;

namespace Application.Commands;

public class UpdateAiConfigCommand : IRequest<AiConfigDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? Model { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public bool IsActive { get; set; }
}
