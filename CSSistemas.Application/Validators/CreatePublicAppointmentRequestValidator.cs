using CSSistemas.Application.DTOs.PublicBooking;
using FluentValidation;

namespace CSSistemas.Application.Validators;

public class CreatePublicAppointmentRequestValidator : AbstractValidator<CreatePublicAppointmentRequest>
{
    public CreatePublicAppointmentRequestValidator()
    {
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
            .WithMessage("Email inválido.");
        RuleFor(x => x.ClientEmail)
            .MaximumLength(256).When(x => !string.IsNullOrEmpty(x.ClientEmail));

        RuleFor(x => x.ClientPhone)
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.ClientPhone));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Notes));
    }

    private static bool BeFutureOrToday(DateTime d)
    {
        var utc = d.Kind == DateTimeKind.Utc ? d : DateTime.SpecifyKind(d, DateTimeKind.Utc);
        return utc >= DateTime.UtcNow.AddMinutes(-5);
    }
}
