using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Serviço em background que roda a cada 30 minutos e envia lembretes de agendamento 24h antes do horário marcado.</summary>
public sealed class AppointmentReminderService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(IServiceProvider services, ILogger<AppointmentReminderService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar lembretes de agendamento.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var appointmentRepo = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        var baseUrl = config["BaseBookingUrl"]?.TrimEnd('/') ?? "";

        var now = DateTime.UtcNow;
        // Janela: agendamentos que iniciam entre 23h e 25h a partir de agora
        var from = now.AddHours(23);
        var to = now.AddHours(25);

        var appointments = await appointmentRepo.GetUpcomingWithoutReminderAsync(from, to, cancellationToken);

        foreach (var a in appointments)
        {
            try
            {
                var businessName = a.Business?.Name ?? "Estabelecimento";
                var serviceName = a.Service?.Name ?? "Serviço";
                var scheduledAtFormatted = a.ScheduledAt.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");
                var cancelLink = string.IsNullOrWhiteSpace(a.CancelToken)
                    ? ""
                    : $"{baseUrl}/cancelar/{a.CancelToken}";
                var clientEmail = a.ClientEmail!;

                await emailSender.SendAppointmentReminderAsync(clientEmail, a.ClientName, scheduledAtFormatted, serviceName, businessName, cancelLink, cancellationToken);

                if (!string.IsNullOrWhiteSpace(a.ClientPhone))
                {
                    var msg = $"Olá, {a.ClientName}! Lembrete: você tem um agendamento em {businessName} amanhã às {a.ScheduledAt.ToLocalTime():HH:mm} ({serviceName}).";
                    if (!string.IsNullOrWhiteSpace(cancelLink))
                        msg += $" Para cancelar: {cancelLink}";
                    await whatsApp.SendTextAsync(a.ClientPhone, msg, cancellationToken);
                }

                a.MarkReminderSent();
                await appointmentRepo.UpdateAsync(a, cancellationToken);
                _logger.LogInformation("Lembrete enviado para agendamento {Id} ({Email})", a.Id, clientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar lembrete para agendamento {Id}", a.Id);
            }
        }
    }
}
