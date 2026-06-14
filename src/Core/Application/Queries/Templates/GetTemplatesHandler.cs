using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Templates;

public class GetTemplatesHandler : IRequestHandler<GetTemplatesQuery, IEnumerable<TemplateDto>>
{
    private readonly IWhatsAppTemplateRepository _templateRepo;

    public GetTemplatesHandler(IWhatsAppTemplateRepository templateRepo)
    {
        _templateRepo = templateRepo;
    }

    public async Task<IEnumerable<TemplateDto>> Handle(GetTemplatesQuery request, CancellationToken ct)
    {
        var templates = request.AccountId.HasValue
            ? await _templateRepo.GetByAccountIdAsync(request.AccountId.Value, ct)
            : await _templateRepo.GetAllAsync(ct);

        return templates.Select(t => new TemplateDto
        {
            Id = t.Id,
            WhatsAppAccountId = t.WhatsAppAccountId,
            Name = t.Name,
            Language = t.Language,
            Category = t.Category,
            Status = t.Status,
            RejectionReason = t.RejectionReason,
            Body = t.Body,
            Header = t.Header,
            Footer = t.Footer,
            Buttons = t.Buttons,
            CreatedAt = t.CreatedAt
        });
    }
}
