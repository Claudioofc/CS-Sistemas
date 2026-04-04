using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

/// <summary>Webhook para notificações do Mercado Pago (Orders API). Evento: Order. Ao receber, consulta a ordem e, se paga, concede assinatura Premium.</summary>
[ApiController]
[Route("api/webhook")]
[AllowAnonymous]
public class MercadoPagoWebhookController : ControllerBase
{
    private readonly PaymentSettings _payment;
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    public MercadoPagoWebhookController(
        Microsoft.Extensions.Options.IOptions<PaymentSettings> payment,
        IPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository,
        IHttpClientFactory httpClientFactory)
    {
        _payment = payment.Value;
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>Recebe notificação de ordem (order.updated etc.). Responde 200 rápido e processa em seguida: GET order, se status paid → criar assinatura a partir de external_reference (userId_planId).</summary>
    [HttpPost("mercadopago")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MercadoPagoOrder([FromBody] MercadoPagoWebhookPayload payload, CancellationToken cancellationToken)
    {
        // Valida assinatura HMAC-SHA256 se WebhookSecret estiver configurado
        var webhookSecret = _payment.MercadoPago.WebhookSecret;
        if (!string.IsNullOrWhiteSpace(webhookSecret))
        {
            if (!ValidateSignature(Request.Headers, payload?.Data?.Id, webhookSecret))
                return Ok(); // Retorna 200 para não revelar que a assinatura falhou
        }

        // Resposta imediata para o MP não reenviar
        if (payload?.Data?.Id == null)
            return Ok();

        var orderId = payload.Data.Id;
        var token = _payment.MercadoPago.AccessToken ?? "";
        if (string.IsNullOrWhiteSpace(token))
            return Ok();

        // GET /v1/orders/{id} para obter status e external_reference
        using var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        var response = await http.GetAsync($"https://api.mercadopago.com/v1/orders/{orderId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Ok();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;
        var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
        var externalRef = root.TryGetProperty("external_reference", out var refProp) ? refProp.GetString() : null;

        // Só conceder assinatura se ordem está paga e temos referência userId_planId
        if (string.IsNullOrEmpty(externalRef) || !string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
            return Ok();

        // external_reference = userId (32 hex) + planId (32 hex), sem separador
        if (externalRef.Length != 64 || !Guid.TryParse(externalRef.AsSpan(0, 32), out var userId) || !Guid.TryParse(externalRef.AsSpan(32, 32), out var planId))
            return Ok();

        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan == null)
            return Ok();

        var subscription = Subscription.CreateFromPlan(userId, plan.BillingIntervalMonths);
        await _subscriptionRepository.AddAsync(subscription, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Valida a assinatura HMAC-SHA256 do Mercado Pago.
    /// Header x-signature: ts=TIMESTAMP,v1=HASH
    /// Header x-request-id: REQUEST_ID
    /// Mensagem assinada: id:DATA_ID;request-id:REQUEST_ID;ts:TIMESTAMP
    /// </summary>
    private static bool ValidateSignature(IHeaderDictionary headers, string? dataId, string secret)
    {
        try
        {
            var xSignature = headers["x-signature"].FirstOrDefault();
            var xRequestId = headers["x-request-id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(xSignature) || string.IsNullOrWhiteSpace(dataId))
                return false;

            // Parse ts e v1 do header x-signature
            string? ts = null, v1 = null;
            foreach (var part in xSignature.Split(','))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2)
                {
                    if (kv[0].Trim() == "ts") ts = kv[1].Trim();
                    else if (kv[0].Trim() == "v1") v1 = kv[1].Trim();
                }
            }
            if (string.IsNullOrWhiteSpace(ts) || string.IsNullOrWhiteSpace(v1))
                return false;

            var manifest = $"id:{dataId};request-id:{xRequestId ?? ""};ts:{ts}";
            var key = Encoding.UTF8.GetBytes(secret);
            var data = Encoding.UTF8.GetBytes(manifest);
            var hash = Convert.ToHexString(HMACSHA256.HashData(key, data)).ToLowerInvariant();
            return hash == v1.ToLowerInvariant();
        }
        catch
        {
            return false;
        }
    }
}

public class MercadoPagoWebhookPayload
{
    public string? Action { get; set; }
    public string? Type { get; set; }
    public MercadoPagoWebhookData? Data { get; set; }
}

public class MercadoPagoWebhookData
{
    public string? Id { get; set; }
}
