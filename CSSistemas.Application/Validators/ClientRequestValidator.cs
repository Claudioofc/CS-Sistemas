using CSSistemas.Application.DTOs.Client;
using FluentValidation;

namespace CSSistemas.Application.Validators;

public class ClientRequestValidator : AbstractValidator<ClientRequest>
{
    public ClientRequestValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("Negócio é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do cliente é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres.")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email inválido.");
        RuleFor(x => x.Email)
            .MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
