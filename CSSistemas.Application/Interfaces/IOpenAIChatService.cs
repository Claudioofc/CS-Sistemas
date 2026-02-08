using CSSistemas.Application.DTOs.WhatsApp;

namespace CSSistemas.Application.Interfaces;

/// <summary>Serviço de IA conversacional (OpenAI) para responder clientes no WhatsApp. A IA recebe nome, duração e preço dos serviços cadastrados e responde APENAS com base nisso (zero invenção).</summary>
public interface IOpenAIChatService
{
    /// <summary>Gera resposta da IA com contexto do negócio (nome, serviços com nome/duração/preço, link de agendamento).</summary>
    Task<string> GetResponseAsync(
        string businessName,
        IReadOnlyList<ServiceInfoForIA> services,
        string? publicSlug,
        string baseBookingUrl,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>Gera resposta e opcionalmente sugere um slot quando há horários disponíveis. Retorna SuggestedSlot se a IA indicar um.</summary>
    Task<WhatsAppIAResponse> GetResponseWithSlotSuggestionAsync(
        string businessName,
        IReadOnlyList<ServiceInfoForIA> services,
        string? publicSlug,
        string baseBookingUrl,
        string userMessage,
        IReadOnlyList<SlotForIA>? availableSlots,
        CancellationToken cancellationToken = default);
}
