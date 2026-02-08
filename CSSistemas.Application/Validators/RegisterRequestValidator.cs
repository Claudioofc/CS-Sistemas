using System.Linq;
using CSSistemas.Application.DTOs.Auth;
using FluentValidation;

namespace CSSistemas.Application.Validators;

/// <summary>Validação de registro — apenas no backend (DRY).</summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres.")
            .MaximumLength(200);

        RuleFor(x => x.DocumentType)
            .Must(dt => dt == 0 || dt == 1).WithMessage("Selecione CPF (0) ou CNPJ (1).");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("CPF ou CNPJ é obrigatório.");
        RuleFor(x => x.DocumentNumber)
            .Must((req, doc) =>
            {
                var digits = doc == null ? "" : new string(doc.Where(char.IsDigit).ToArray());
                return req.DocumentType == 0 ? digits.Length == 11 : digits.Length == 14;
            })
            .WithMessage("CPF deve ter 11 dígitos ou CNPJ 14 dígitos.");
    }
}
