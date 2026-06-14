using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries;

public class GetAiConfigsHandler : IRequestHandler<GetAiConfigsQuery, IEnumerable<AiConfigDto>>
{
    private readonly IAiConfigRepository _aiConfigRepo;

    public GetAiConfigsHandler(IAiConfigRepository aiConfigRepo)
    {
        _aiConfigRepo = aiConfigRepo;
    }

    public async Task<IEnumerable<AiConfigDto>> Handle(GetAiConfigsQuery request, CancellationToken cancellationToken)
    {
        var configs = await _aiConfigRepo.GetAllAsync(cancellationToken);
        return configs.Select(c => new AiConfigDto
        {
            Id = c.Id,
            Name = c.Name,
            SystemPrompt = c.SystemPrompt,
            Provider = c.Provider,
            Model = c.Model,
            Temperature = c.Temperature,
            MaxTokens = c.MaxTokens,
            IsActive = c.IsActive
        });
    }
}
