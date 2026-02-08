using CSSistemas.Application.DTOs.WhatsApp;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Helpers;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

/// <summary>Webhook para receber mensagens do WhatsApp. Formato genérico: to = nosso número, from = número do cliente, text = mensagem.</summary>
[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private static readonly string[] ConfirmationTriggers = { "sim", "confirmar", "confirmo", "quero confirmar", "pode ser", "confirmado" };

    private readonly IBusinessRepository _businessRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IAvailabilityService _availabilityService;
    private readonly IOpenAIChatService _openAIChatService;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly IPendingWhatsAppSlotStore _pendingSlotStore;
    private readonly IConfiguration _configuration;

    public WebhooksController(
        IBusinessRepository businessRepo,
        IServiceRepository serviceRepo,
        IAppointmentRepository appointmentRepo,
        IAvailabilityService availabilityService,
        IOpenAIChatService openAIChatService,
        IWhatsAppSender whatsAppSender,
        IPendingWhatsAppSlotStore pendingSlotStore,
        IConfiguration configuration)
    {
        _businessRepo = businessRepo;
        _serviceRepo = serviceRepo;
        _appointmentRepo = appointmentRepo;
        _availabilityService = availabilityService;
        _openAIChatService = openAIChatService;
        _whatsAppSender = whatsAppSender;
        _pendingSlotStore = pendingSlotStore;
        _configuration = configuration;
    }

    /// <summary>Recebe mensagem do WhatsApp (to = número do negócio, from = cliente, text = mensagem). Responde com IA e envia via WhatsApp. Se o cliente confirmar um slot pendente, cria o agendamento.</summary>
    [HttpPost("whatsapp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WhatsApp([FromBody] WhatsAppWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        if (payload?.To == null || payload?.From == null || payload?.Text == null)
            throw CommException.BadRequest("Payload inválido. Envie to, from e text.");

        var toNormalized = new string(payload.To.Where(char.IsDigit).ToArray()).Trim();
        var business = await _businessRepo.GetByWhatsAppPhoneAsync(toNormalized, cancellationToken);
        if (business == null)
            return Ok(new { received = true, message = "Número não vinculado a nenhum negócio." });

        var fromNormalized = new string(payload.From.Where(char.IsDigit).ToArray()).Trim();
        var text = payload.Text.Trim();
        var isConfirmation = ConfirmationTriggers.Contains(text, StringComparer.OrdinalIgnoreCase);

        var pendingSlot = await _pendingSlotStore.TryGetAndRemoveAsync(fromNormalized, cancellationToken);
        if (isConfirmation && pendingSlot != null)
        {
            var service = await _serviceRepo.GetByIdAndBusinessIdAsync(pendingSlot.ServiceId, pendingSlot.BusinessId, cancellationToken);
            if (service != null && !service.IsDeleted)
            {
                var hasConflict = await _appointmentRepo.HasConflictAsync(pendingSlot.BusinessId, pendingSlot.ScheduledAtUtc, service.DurationMinutes, null, cancellationToken);
                if (!hasConflict)
                {
                    var appointment = Appointment.Create(
                        pendingSlot.BusinessId,
                        pendingSlot.ServiceId,
                        "Cliente WhatsApp",
                        pendingSlot.ScheduledAtUtc,
                        pendingSlot.ClientPhone,
                        null,
                        null);
                    await _appointmentRepo.AddAsync(appointment, cancellationToken);
                    var scheduledFormatted = BrazilTimeHelper.FormatUtcToBrazilDateTime(pendingSlot.ScheduledAtUtc);
                    var reply = $"Agendamento realizado! Data/hora: {scheduledFormatted} (horário de Brasília). Até lá!";
                    await _whatsAppSender.SendTextAsync(fromNormalized, reply, cancellationToken);
                    return Ok(new { received = true, replied = true, appointmentCreated = true });
                }
            }
        }

        var services = await _serviceRepo.GetByBusinessIdAsync(business.Id, onlyActive: true, cancellationToken);
        var servicesForIA = services.Select(s => new ServiceInfoForIA(s.Name, s.DurationMinutes, s.Price)).ToList();
        var baseBookingUrl = _configuration["BaseBookingUrl"] ?? "https://app.cssistemas.com";

        var wantsToBook = text.Contains("agendar", StringComparison.OrdinalIgnoreCase) || text.Contains("marcar", StringComparison.OrdinalIgnoreCase) || text.Contains("horário", StringComparison.OrdinalIgnoreCase);
        var availableSlots = new List<SlotForIA>();
        if (wantsToBook && services.Count > 0)
        {
            var tomorrowUtc = DateTime.UtcNow.AddDays(1);
            var tomorrowBrazil = TimeZoneInfo.ConvertTimeFromUtc(tomorrowUtc, BrazilTimeHelper.GetBrazilTimeZone()).Date;
            foreach (var svc in services.Take(3))
            {
                var slots = await _availabilityService.GetAvailableSlotsAsync(business.Id, svc.Id, tomorrowBrazil, 30, cancellationToken);
                foreach (var slotUtc in slots.Take(3))
                    availableSlots.Add(new SlotForIA(svc.Id, svc.Name, slotUtc));
            }
        }

        var response = await _openAIChatService.GetResponseWithSlotSuggestionAsync(
            business.Name,
            servicesForIA,
            business.PublicSlug,
            baseBookingUrl,
            text,
            availableSlots.Count > 0 ? availableSlots : null,
            cancellationToken);

        if (response.SuggestedSlot != null)
            await _pendingSlotStore.SetAsync(fromNormalized, new PendingSlotData(business.Id, response.SuggestedSlot.ServiceId, response.SuggestedSlot.ScheduledAtUtc, fromNormalized), TimeSpan.FromHours(1), cancellationToken);

        await _whatsAppSender.SendTextAsync(fromNormalized, response.Message, cancellationToken);

        return Ok(new { received = true, replied = true });
    }
}

/// <summary>Payload genérico do webhook WhatsApp (adaptar ao Z-API/Twilio/Meta).</summary>
public class WhatsAppWebhookPayload
{
    /// <summary>Número que recebeu a mensagem (nosso número / negócio).</summary>
    public string? To { get; set; }
    /// <summary>Número de quem enviou (cliente).</summary>
    public string? From { get; set; }
    /// <summary>Texto da mensagem.</summary>
    public string? Text { get; set; }
}
