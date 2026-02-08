using CSSistemas.Application.DTOs.Business;
using FluentValidation;

namespace CSSistemas.Application.Validators;

public class BusinessHoursBulkRequestValidator : AbstractValidator<BusinessHoursBulkRequest>
{
    public BusinessHoursBulkRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Lista de horários é obrigatória.")
            .Must(items => items != null && items.Count == 7).WithMessage("Informe os 7 dias da semana (0 a 6).");

        When(x => x.Items != null && x.Items.Count == 7, () =>
        {
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.DayOfWeek)
                    .InclusiveBetween(0, 6).WithMessage("DayOfWeek deve ser 0 (Domingo) a 6 (Sábado).");
                item.RuleFor(x => x.OpenAtMinutes)
                    .InclusiveBetween(0, 1439).When(x => x.OpenAtMinutes.HasValue)
                    .WithMessage("OpenAtMinutes deve ser 0 a 1439 (minutos desde meia-noite).");
                item.RuleFor(x => x.CloseAtMinutes)
                    .InclusiveBetween(0, 1439).When(x => x.CloseAtMinutes.HasValue)
                    .WithMessage("CloseAtMinutes deve ser 0 a 1439.");
                item.RuleFor(x => x)
                    .Must(x => !x.OpenAtMinutes.HasValue && !x.CloseAtMinutes.HasValue ||
                              x.OpenAtMinutes.HasValue && x.CloseAtMinutes.HasValue && x.OpenAtMinutes!.Value < x.CloseAtMinutes!.Value)
                    .WithMessage("Abertura deve ser anterior ao fechamento, ou ambos nulos (dia fechado).");
            });
        });
    }
}
