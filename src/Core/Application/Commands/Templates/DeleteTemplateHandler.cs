using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Templates;

public class DeleteTemplateHandler : IRequestHandler<DeleteTemplateCommand>
{
    private readonly IWhatsAppTemplateRepository _templateRepo;
    private readonly IUnitOfWork _uow;

    public DeleteTemplateHandler(IWhatsAppTemplateRepository templateRepo, IUnitOfWork uow)
    {
        _templateRepo = templateRepo;
        _uow = uow;
    }

    public async Task Handle(DeleteTemplateCommand request, CancellationToken ct)
    {
        var template = await _templateRepo.GetByIdAsync(request.Id, ct);
        if (template == null)
            throw new KeyNotFoundException("Template not found");

        _templateRepo.Delete(template);
        await _uow.SaveChangesAsync(ct);
    }
}
