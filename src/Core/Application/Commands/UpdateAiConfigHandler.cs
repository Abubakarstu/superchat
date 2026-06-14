using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands;

public class UpdateAiConfigHandler : IRequestHandler<UpdateAiConfigCommand, AiConfigDto>
{
    private readonly IAiConfigRepository _aiConfigRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAiConfigHandler(IAiConfigRepository aiConfigRepo, IUnitOfWork unitOfWork)
    {
        _aiConfigRepo = aiConfigRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<AiConfigDto> Handle(UpdateAiConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await _aiConfigRepo.GetByIdAsync(request.Id, cancellationToken);
        if (config == null)
            throw new KeyNotFoundException($"AI config with id {request.Id} not found.");

        config.Name = request.Name;
        config.SystemPrompt = request.SystemPrompt;
        config.Provider = request.Provider;
        config.Model = request.Model;
        config.Temperature = request.Temperature;
        config.MaxTokens = request.MaxTokens;
        config.OllamaBaseUrl = request.OllamaBaseUrl;
        config.IsActive = request.IsActive;
        config.UpdatedAt = DateTime.UtcNow;

        _aiConfigRepo.Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AiConfigDto
        {
            Id = config.Id,
            Name = config.Name,
            SystemPrompt = config.SystemPrompt,
            Provider = config.Provider,
            Model = config.Model,
            Temperature = config.Temperature,
            MaxTokens = config.MaxTokens,
            OllamaBaseUrl = config.OllamaBaseUrl,
            IsActive = config.IsActive
        };
    }
}
