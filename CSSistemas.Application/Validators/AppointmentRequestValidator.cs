using CSSistemas.Application.DTOs.Appointment;
using FluentValidation;

namespace CSSistemas.Application.Validators;

public class AppointmentRequestValidator : AbstractValidator<AppointmentRequest>
{
    public AppointmentRequestValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("Negócio é obrigatório.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Serviço é obrigatório.");

        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("Nome do cliente é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres.")
            .MaximumLength(200);

        RuleFor(x => x.ScheduledAt)
            .NotEmpty().WithMessage("Data e hora do agendamento são obrigatórias.")
            .Must(BeFutureOrToday).WithMessage("Data/hora do agendamento deve ser futura ou de hoje.");

        RuleFor(x => x.ClientEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ClientEmail))
            .WithMessage("Email inválido.")
            .MaximumLength(256);

        RuleFor(x => x.ClientPhone)
            .MaximumLength(20);

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }

    private static bool BeFutureOrToday(DateTime value)
    {
        var now = DateTime.UtcNow;
        var date = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return date >= now.AddMinutes(-5); // pequena tolerância
    }
}
