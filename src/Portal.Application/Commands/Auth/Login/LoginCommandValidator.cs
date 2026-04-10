using FluentValidation;

namespace Portal.Application.Commands.Auth.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email je obavezan.")
            .EmailAddress().WithMessage("Neispravan format email adrese.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Lozinka je obavezna.");

        RuleFor(x => x.TenantId)
            .NotEmpty();
    }
}
