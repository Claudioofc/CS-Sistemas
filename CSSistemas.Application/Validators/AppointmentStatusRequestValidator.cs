using CSSistemas.Application.DTOs.Appointment;
using CSSistemas.Domain.Enums;
using FluentValidation;

namespace CSSistemas.Application.Validators;

/// <summary>Validação do request de alteração de status (justificativa opcional ao cancelar).</summary>
public class AppointmentStatusRequestValidator : AbstractValidator<AppointmentStatusRequest>
{
    public const int CancellationReasonMaxLength = 500;

    public AppointmentStatusRequestValidator()
    {
        RuleFor(x => x.CancellationReason)
            .MaximumLength(CancellationReasonMaxLength)
            .When(x => x.Status == AppointmentStatus.Cancelled && !string.IsNullOrEmpty(x.CancellationReason))
            .WithMessage($"O motivo do cancelamento deve ter no máximo {CancellationReasonMaxLength} caracteres.");
    }
}
