namespace CSSistemas.Application.Interfaces;

/// <summary>Envio de e-mail (redefinição de senha, confirmação/cancelamento de agendamento, etc.). Implementação na Infrastructure.</summary>
public interface IEmailSender
{
    /// <summary>Envia e-mail com link para redefinir senha.</summary>
    Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default);

    /// <summary>Confirmação de agendamento público com link para cancelar.</summary>
    Task SendAppointmentConfirmationAsync(string toEmail, string clientName, string scheduledAtFormatted, string serviceName, string businessName, string cancelLink, CancellationToken cancellationToken = default);

    /// <summary>Aviso de que o profissional cancelou o agendamento. Justificativa opcional.</summary>
    Task SendAppointmentCancelledByProfessionalAsync(string toEmail, string clientName, string scheduledAtFormatted, string businessName, string? cancellationReason = null, CancellationToken cancellationToken = default);

    /// <summary>Aviso para o admin: novo usuário se cadastrou no sistema.</summary>
    Task SendNewUserRegisteredAsync(string toEmail, string newUserName, string newUserEmail, CancellationToken cancellationToken = default);

    /// <summary>Mensagem de suporte: cliente reporta problema/erro; envia para o e-mail do admin.</summary>
    Task SendSupportRequestAsync(string toEmail, string userName, string userEmail, string message, string? pageUrl = null, byte[]? attachment = null, string? attachmentFileName = null, CancellationToken cancellationToken = default);
}
