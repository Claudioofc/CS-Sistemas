using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.DTOs.WhatsApp;
using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Serviço de IA conversacional (OpenAI) para responder clientes no WhatsApp.</summary>
public class OpenAIChatService : IOpenAIChatService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAISettings _settings;
    private readonly ILogger<OpenAIChatService> _logger;

    public OpenAIChatService(HttpClient httpClient, IOptions<OpenAISettings> settings, ILogger<OpenAIChatService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public async Task<string> GetResponseAsync(
        string businessName,
        IReadOnlyList<ServiceInfoForIA> services,
        string? publicSlug,
        string baseBookingUrl,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return GetFallbackResponse(businessName, publicSlug, baseBookingUrl);
        }

        var servicesBlock = BuildServicesBlockForPrompt(services);
        var linkText = !string.IsNullOrWhiteSpace(publicSlug)
            ? $"{baseBookingUrl.TrimEnd('/')}/agendar/{publicSlug}"
            : "o link de agendamento (configure na área do profissional)";

        var systemContent = $@"Você é o assistente virtual do negócio {businessName}.
Responda de forma breve, amigável e profissional, em português.

SERVIÇOS CADASTRADOS (use APENAS estes dados para responder sobre preços, duração ou nomes):
{servicesBlock}

REGRAS OBRIGATÓRIAS:
- Para preço, duração ou nome de serviço: responda SOMENTE com base na lista acima. Se o serviço ou valor NÃO estiver na lista, diga: ""O valor deve ser confirmado diretamente com o profissional."" ou ""Esse serviço não está disponível para informação automática. Entre em contato com o estabelecimento.""
- Para agendar: oriente o cliente a acessar: {linkText}.
- Não invente horários, preços ou serviços. Zero invenção.";

        var payload = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = systemContent },
                new { role = "user", content = userMessage }
            },
            max_tokens = 300
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI API error: {StatusCode}", response.StatusCode);
                return GetFallbackResponse(businessName, publicSlug, baseBookingUrl);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
                return GetFallbackResponse(businessName, publicSlug, baseBookingUrl);
            var message = choices[0].GetProperty("message").GetProperty("content").GetString();
            return string.IsNullOrWhiteSpace(message) ? GetFallbackResponse(businessName, publicSlug, baseBookingUrl) : message.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI request failed");
            return GetFallbackResponse(businessName, publicSlug, baseBookingUrl);
        }
    }

    public async Task<WhatsAppIAResponse> GetResponseWithSlotSuggestionAsync(
        string businessName,
        IReadOnlyList<ServiceInfoForIA> services,
        string? publicSlug,
        string baseBookingUrl,
        string userMessage,
        IReadOnlyList<SlotForIA>? availableSlots,
        CancellationToken cancellationToken = default)
    {
        if (availableSlots == null || availableSlots.Count == 0)
        {
            var text = await GetResponseAsync(businessName, services, publicSlug, baseBookingUrl, userMessage, cancellationToken);
            return new WhatsAppIAResponse(text, null);
        }

        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return new WhatsAppIAResponse(GetFallbackResponse(businessName, publicSlug, baseBookingUrl), null);
        }

        var servicesBlock = BuildServicesBlockForPrompt(services);
        var linkText = !string.IsNullOrWhiteSpace(publicSlug)
            ? $"{baseBookingUrl.TrimEnd('/')}/agendar/{publicSlug}"
            : "o link de agendamento (configure na área do profissional)";

        var slotsBlock = string.Join("\n", availableSlots.Select(s =>
            $"- {s.ServiceName}: horário UTC {s.ScheduledAtUtc:yyyy-MM-ddTHH:mm:ss}Z (serviceId={s.ServiceId};scheduledAtUtc={s.ScheduledAtUtc:yyyy-MM-ddTHH:mm:ss}Z)"));

        var systemContent = $@"Você é o assistente virtual do negócio {businessName}.
Responda de forma breve, amigável e profissional, em português.

SERVIÇOS CADASTRADOS (use APENAS estes dados para preço/duração/nome):
{servicesBlock}

REGRAS: Para preço ou serviço não listado, diga para confirmar com o profissional. Para agendar: {linkText}.

Horários disponíveis para sugestão (use exatamente o formato ao sugerir):
{slotsBlock}
Se o cliente quiser agendar e você for sugerir um horário, escolha UM da lista acima e no FINAL da sua resposta adicione EXATAMENTE uma linha: SUGGESTED_SLOT:serviceId=<guid>;scheduledAtUtc=<yyyy-MM-ddTHH:mm:ss>Z
Não invente horários fora da lista. Se não for sugerir agendamento, não adicione a linha SUGGESTED_SLOT.";

        var payload = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = systemContent },
                new { role = "user", content = userMessage }
            },
            max_tokens = 350
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI API error: {StatusCode}", response.StatusCode);
                return new WhatsAppIAResponse(GetFallbackResponse(businessName, publicSlug, baseBookingUrl), null);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
                return new WhatsAppIAResponse(GetFallbackResponse(businessName, publicSlug, baseBookingUrl), null);
            var rawMessage = choices[0].GetProperty("message").GetProperty("content").GetString();
            var message = string.IsNullOrWhiteSpace(rawMessage) ? GetFallbackResponse(businessName, publicSlug, baseBookingUrl) : rawMessage.Trim();

            var suggested = TryParseSuggestedSlot(message, availableSlots);
            var cleanMessage = suggested != null ? RemoveSuggestedSlotLine(message) : message;
            return new WhatsAppIAResponse(cleanMessage.Trim(), suggested);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI request failed");
            return new WhatsAppIAResponse(GetFallbackResponse(businessName, publicSlug, baseBookingUrl), null);
        }
    }

    private static SuggestedSlotDto? TryParseSuggestedSlot(string message, IReadOnlyList<SlotForIA> availableSlots)
    {
        var match = Regex.Match(message, @"SUGGESTED_SLOT:\s*serviceId=([a-fA-F0-9-]+);scheduledAtUtc=([^\s\r\n]+)", RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        if (!Guid.TryParse(match.Groups[1].Value, out var serviceId)) return null;
        if (!DateTime.TryParse(match.Groups[2].Value.Trim(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var scheduledAt)) return null;
        var slot = availableSlots.FirstOrDefault(s => s.ServiceId == serviceId && Math.Abs((s.ScheduledAtUtc - scheduledAt).TotalSeconds) < 1);
        return slot != null ? new SuggestedSlotDto(slot.ServiceId, slot.ScheduledAtUtc) : null;
    }

    private static string RemoveSuggestedSlotLine(string message)
    {
        return Regex.Replace(message, @"\s*SUGGESTED_SLOT:[^\r\n]*", "", RegexOptions.IgnoreCase).Trim();
    }

    /// <summary>Monta o bloco de serviços para o prompt (nome, duração, preço). Funciona para qualquer nicho.</summary>
    private static string BuildServicesBlockForPrompt(IReadOnlyList<ServiceInfoForIA> services)
    {
        if (services == null || services.Count == 0)
            return "- Nenhum serviço cadastrado. Se perguntarem preço ou serviço, diga para confirmar com o profissional.";
        var lines = services.Select(s =>
        {
            var preco = s.Price.HasValue ? $"R$ {s.Price.Value:N2}" : "a confirmar com o profissional";
            return $"- {s.Name}: duração {s.DurationMinutes} min, preço {preco}";
        });
        return string.Join("\n", lines);
    }

    private static string GetFallbackResponse(string businessName, string? publicSlug, string baseBookingUrl)
    {
        var link = !string.IsNullOrWhiteSpace(publicSlug)
            ? $"{baseBookingUrl.TrimEnd('/')}/agendar/{publicSlug}"
            : null;
        if (link != null)
            return $"Olá! Sou o assistente de {businessName}. Para agendar, acesse: {link}. Posso ajudar em algo mais?";
        return $"Olá! Sou o assistente de {businessName}. Entre em contato com o estabelecimento para agendar. Posso ajudar em algo mais?";
    }
}
