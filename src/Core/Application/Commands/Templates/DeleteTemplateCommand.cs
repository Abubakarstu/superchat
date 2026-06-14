using MediatR;

namespace Application.Commands.Templates;

public class DeleteTemplateCommand : IRequest
{
    public Guid Id { get; set; }
}
