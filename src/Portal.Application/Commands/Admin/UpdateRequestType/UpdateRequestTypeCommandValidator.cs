using FluentValidation;

namespace Portal.Application.Commands.Admin.UpdateRequestType;

public class UpdateRequestTypeCommandValidator : AbstractValidator<UpdateRequestTypeCommand>
{
    public UpdateRequestTypeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(64)
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Kod smije sadržavati samo mala slova, brojeve i crtice.");

        RuleFor(x => x.NameI18n).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.EstimatedProcessingDays)
            .GreaterThan(0).When(x => x.EstimatedProcessingDays.HasValue);

        RuleForEach(x => x.Fields).ChildRules(field =>
        {
            field.RuleFor(f => f.FieldKey).NotEmpty().MaximumLength(64);
            field.RuleFor(f => f.FieldType).NotEmpty();
            field.RuleFor(f => f.LabelI18n).NotEmpty();
        });

        RuleForEach(x => x.Attachments).ChildRules(att =>
        {
            att.RuleFor(a => a.AttachmentKey).NotEmpty().MaximumLength(64);
            att.RuleFor(a => a.LabelI18n).NotEmpty();
            att.RuleFor(a => a.MaxSizeBytes).GreaterThan(0);
        });
    }
}
