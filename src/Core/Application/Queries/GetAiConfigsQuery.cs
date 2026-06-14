using Application.DTOs;
using MediatR;

namespace Application.Queries;

public class GetAiConfigsQuery : IRequest<IEnumerable<AiConfigDto>>
{
}
