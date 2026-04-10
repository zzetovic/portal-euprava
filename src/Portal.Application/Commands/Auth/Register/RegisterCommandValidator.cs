using FluentValidation;

namespace Portal.Application.Commands.Auth.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email je obavezan.")
            .EmailAddress().WithMessage("Neispravan format email adrese.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Lozinka je obavezna.")
            .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 znakova.")
            .MaximumLength(128);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ime je obavezno.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Prezime je obavezno.")
            .MaximumLength(100);

        RuleFor(x => x.Oib)
            .Length(11).When(x => x.Oib is not null)
            .Matches(@"^\d{11}$").When(x => x.Oib is not null)
            .WithMessage("OIB mora sadržavati točno 11 znamenki.");

        RuleFor(x => x.Phone)
            .MaximumLength(32).When(x => x.Phone is not null);

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant je obavezan.");
    }
}
