using CSSistemas.Application.DTOs.SystemMessage;
using FluentValidation;

namespace CSSistemas.Application.Validators;

public class SystemMessageRequestValidator : AbstractValidator<SystemMessageRequest>
{
    public SystemMessageRequestValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("Negócio é obrigatório.");

        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Chave do template é obrigatória.")
            .MinimumLength(2).WithMessage("Chave deve ter no mínimo 2 caracteres.")
            .MaximumLength(100)
            .Matches(@"^[a-z0-9_]+$").WithMessage("Chave pode conter apenas letras minúsculas, números e underscore.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Título é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.Body)
            .NotNull();
        RuleFor(x => x.Body)
            .MaximumLength(4000).When(x => x.Body != null);
    }
}
