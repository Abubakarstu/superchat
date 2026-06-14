using Application.DTOs;
using MediatR;

namespace Application.Queries.Templates;

public class GetTemplatesQuery : IRequest<IEnumerable<TemplateDto>>
{
    public Guid? AccountId { get; set; }
}
