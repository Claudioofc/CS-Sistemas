using CSSistemas.Application.DTOs.Business;
using FluentValidation;

namespace CSSistemas.Application.Validators;

public class BusinessRequestValidator : AbstractValidator<BusinessRequest>
{
    public BusinessRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do negócio é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres.")
            .MaximumLength(200);

        RuleFor(x => x.PublicSlug)
            .MaximumLength(100)
            .Matches("^[a-z0-9_-]*$").When(x => !string.IsNullOrEmpty(x.PublicSlug))
            .WithMessage("Slug pode conter apenas letras minúsculas, números, hífen e underscore.");

        RuleFor(x => x.WhatsAppPhone)
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.WhatsAppPhone));
    }
}
