using Application.Commands;
using FluentValidation;

namespace Application.Validators;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.RemoteJid)
            .NotEmpty().WithMessage("RemoteJid is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required.")
            .MaximumLength(4096).WithMessage("Message content cannot exceed 4096 characters.");
    }
}
