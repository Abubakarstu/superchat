using Application.DTOs;
using Domain.Entities.WhatsApp;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Templates;

public class CreateTemplateHandler : IRequestHandler<CreateTemplateCommand, TemplateDto>
{
    private readonly IWhatsAppTemplateRepository _templateRepo;
    private readonly IUnitOfWork _uow;

    public CreateTemplateHandler(IWhatsAppTemplateRepository templateRepo, IUnitOfWork uow)
    {
        _templateRepo = templateRepo;
        _uow = uow;
    }

    public async Task<TemplateDto> Handle(CreateTemplateCommand request, CancellationToken ct)
    {
        var template = new WhatsAppTemplate
        {
            WhatsAppAccountId = request.WhatsAppAccountId,
            Name = request.Name,
            Language = request.Language,
            Category = request.Category,
            Body = request.Body,
            Header = request.Header,
            Footer = request.Footer,
            Buttons = request.Buttons,
            ContentType = request.ContentType,
            TypesJson = request.TypesJson,
            Status = "PENDING"
        };
        _templateRepo.Add(template);
        await _uow.SaveChangesAsync(ct);

        return new TemplateDto
        {
            Id = template.Id,
            WhatsAppAccountId = template.WhatsAppAccountId,
            Name = template.Name,
            Language = template.Language,
            Category = template.Category,
            Status = template.Status,
            Body = template.Body,
            Header = template.Header,
            Footer = template.Footer,
            Buttons = template.Buttons,
            ContentType = template.ContentType,
            TypesJson = template.TypesJson,
            CreatedAt = template.CreatedAt
        };
    }
}
