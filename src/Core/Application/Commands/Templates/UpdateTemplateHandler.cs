using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Templates;

public class UpdateTemplateHandler : IRequestHandler<UpdateTemplateCommand, TemplateDto>
{
    private readonly IWhatsAppTemplateRepository _templateRepo;
    private readonly IUnitOfWork _uow;

    public UpdateTemplateHandler(IWhatsAppTemplateRepository templateRepo, IUnitOfWork uow)
    {
        _templateRepo = templateRepo;
        _uow = uow;
    }

    public async Task<TemplateDto> Handle(UpdateTemplateCommand request, CancellationToken ct)
    {
        var template = await _templateRepo.GetByIdAsync(request.Id, ct);
        if (template == null)
            throw new KeyNotFoundException("Template not found");

        template.Name = request.Name;
        template.Language = request.Language;
        template.Category = request.Category;
        template.Body = request.Body;
        template.Header = request.Header;
        template.Footer = request.Footer;
        template.Buttons = request.Buttons;
        template.ContentType = request.ContentType ?? template.ContentType;
        template.TypesJson = request.TypesJson ?? template.TypesJson;

        _templateRepo.Update(template);
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
