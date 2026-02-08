using CSSistemas.Application.DTOs.Service;
using FluentValidation;

namespace CSSistemas.Application.Validators;

public class ServiceRequestValidator : AbstractValidator<ServiceRequest>
{
    public ServiceRequestValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("Negócio é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do serviço é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres.")
            .MaximumLength(200);

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duração deve ser maior que zero.")
            .LessThanOrEqualTo(480).WithMessage("Duração máxima é 480 minutos (8 horas).");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).When(x => x.Price.HasValue)
            .WithMessage("Preço não pode ser negativo.");
    }
}
