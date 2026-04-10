using FluentValidation;

namespace Portal.Application.Commands.Auth.VerifyEmail;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token je obavezan.");
    }
}
