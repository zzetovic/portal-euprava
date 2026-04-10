using FluentValidation;

namespace Portal.Application.Commands.Auth.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token je obavezan.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova lozinka je obavezna.")
            .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 znakova.")
            .MaximumLength(128);
    }
}
