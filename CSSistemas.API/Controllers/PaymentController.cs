using System.Text.Json;
using System.Text.Json.Serialization;
using CSSistemas.API.Extensions;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly PaymentSettings _payment;
    private readonly IPlanRepository _planRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;

    public PaymentController(
        Microsoft.Extensions.Options.IOptions<PaymentSettings> payment,
        IPlanRepository planRepository,
        IUserRepository userRepository,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env)
    {
        _payment = payment.Value;
        _planRepository = planRepository;
        _userRepository = userRepository;
        _httpClientFactory = httpClientFactory;
        _env = env;
    }

    private static bool HasValidMercadoPagoToken(string token)
    {
        return token.Length > 20
            && !token.Contains("ACCESS_TOKEN_AQUI")
            && !token.Contains("COLE_SEU");
    }

    /// <summary>Indica se o pagamento com cartão (Mercado Pago) está habilitado.</summary>
    [HttpGet("card")]
    [ProducesResponseType(typeof(CardEnabledResponse), StatusCodes.Status200OK)]
    public IActionResult GetCardEnabled()
    {
        var token = _payment.MercadoPago.AccessToken ?? "";
        var enabled = _payment.Card.Enabled && HasValidMercadoPagoToken(token);
        return Ok(new CardEnabledResponse(enabled));
    }

    /// <summary>Indica se o PIX (Mercado Pago Orders API) está habilitado (mesmo token do cartão).</summary>
    [HttpGet("pix-enabled")]
    [ProducesResponseType(typeof(PixEnabledResponse), StatusCodes.Status200OK)]
    public IActionResult GetPixEnabled()
    {
        var token = _payment.MercadoPago.AccessToken ?? "";
        return Ok(new PixEnabledResponse(HasValidMercadoPagoToken(token)));
    }

    /// <summary>Cria ordem PIX no Mercado Pago (Orders API) com valor do plano. Retorna QR code (base64) e código copia-e-cola. Ao escanear, o valor já vem preenchido.</summary>
    [HttpPost("pix")]
    [ProducesResponseType(typeof(PixOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CreatePixOrder([FromBody] CreatePixRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var token = _payment.MercadoPago.AccessToken ?? "";
        if (!HasValidMercadoPagoToken(token))
            throw CommException.BadRequest("PIX não configurado. Configure Payment:MercadoPago:AccessToken no appsettings.");

        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan == null || !plan.IsActive)
            throw CommException.BadRequest("Plano inválido ou inativo.");

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        // No sandbox do Mercado Pago o e-mail do pagador deve conter @testuser.com
        var payerEmail = _env.IsDevelopment()
            ? "test@testuser.com"
            : (user?.Email ?? "pagador@cssistemas.com.br");

        // Orders API v1: external_reference máx 64 chars. Formato: userId (32) + planId (32), sem separador.
        var totalAmount = plan.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var externalRef = $"{userId:N}{plan.Id:N}";
        var orderPayload = new
        {
            type = "online",
            external_reference = externalRef,
            total_amount = totalAmount,
            payer = new { email = payerEmail, first_name = "Cliente" },
            transactions = new
            {
                payments = new[]
                {
                    new
                    {
                        amount = totalAmount,
                        payment_method = new { id = "pix", type = "bank_transfer" }
                    }
                }
            }
        };

        using var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        // Obrigatório na Orders API: evita criar a mesma ordem duas vezes em caso de retry.
        http.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString("N"));
        var json = JsonSerializer.Serialize(orderPayload);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await http.PostAsync("https://api.mercadopago.com/v1/orders", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Resposta ao cliente via CommException (mensagem única).
            throw CommException.BadGateway("Erro ao criar ordem PIX no Mercado Pago. Verifique o Access Token e as credenciais.");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;
        var orderId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        string? qrCode = null;
        string? qrCodeBase64 = null;
        if (root.TryGetProperty("transactions", out var transactions) &&
            transactions.TryGetProperty("payments", out var payments) &&
            payments.GetArrayLength() > 0)
        {
            var firstPayment = payments[0];
            if (firstPayment.TryGetProperty("payment_method", out var pm))
            {
                if (pm.TryGetProperty("qr_code", out var qr)) qrCode = qr.GetString();
                if (pm.TryGetProperty("qr_code_base64", out var qr64)) qrCodeBase64 = qr64.GetString();
            }
        }

        if (string.IsNullOrEmpty(orderId))
            throw CommException.BadGateway("Resposta inválida do Mercado Pago (sem id da ordem).");

        return Ok(new PixOrderResponse(orderId, qrCode ?? "", qrCodeBase64 ?? ""));
    }

    /// <summary>Cria preferência de checkout no Mercado Pago e retorna a URL para redirecionar o usuário. Cartão aceita todas as bandeiras (Visa, Mastercard, Elo, Hipercard, etc.) via Checkout Pro.</summary>
    [HttpPost("mercado-pago/checkout")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CreateMercadoPagoCheckout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        var token = _payment.MercadoPago.AccessToken ?? "";
        if (!_payment.Card.Enabled || !HasValidMercadoPagoToken(token))
            throw CommException.BadRequest("Pagamento com cartão não está configurado. Configure Payment:MercadoPago:AccessToken no appsettings ou em CONFIGURAR-CARTAO.md.");

        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan == null || !plan.IsActive)
            throw CommException.BadRequest("Plano inválido ou inativo.");

        var mp = _payment.MercadoPago;
        string successUrl = mp.SuccessUrl ?? "";
        string failureUrl = mp.FailureUrl ?? "";
        string pendingUrl = mp.PendingUrl ?? mp.FailureUrl ?? "";

        // Se o frontend enviou a origem atual (returnBaseUrl), usa para montar as back_urls e o "Voltar à loja" do Mercado Pago redirecionar para a mesma URL (http/https e porta) em que o usuário está.
        var baseUrl = request.ReturnBaseUrl?.Trim();
        if (!string.IsNullOrEmpty(baseUrl) && (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            var baseTrim = baseUrl.TrimEnd('/');
            successUrl = $"{baseTrim}/planos?status=success";
            failureUrl = $"{baseTrim}/planos?status=failure";
            pendingUrl = $"{baseTrim}/planos";
        }
        else if (string.IsNullOrWhiteSpace(successUrl) || string.IsNullOrWhiteSpace(failureUrl))
            throw CommException.BadRequest("URLs de retorno do Mercado Pago não configuradas.");

        var preference = new MercadoPagoPreferenceRequest
        {
            Items = new List<MercadoPagoItem>
            {
                new MercadoPagoItem
                {
                    Id = plan.Id.ToString(),
                    Title = plan.Name,
                    Quantity = 1,
                    UnitPrice = plan.Price,
                    CurrencyId = "BRL"
                }
            },
            BackUrls = new MercadoPagoBackUrls
            {
                Success = successUrl,
                Failure = failureUrl,
                Pending = pendingUrl
            },
            AutoReturn = "approved",
            ExternalReference = $"{userId}_{plan.Id}"
        };

        using var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("Authorization", "Bearer " + mp.AccessToken);
        var json = JsonSerializer.Serialize(preference, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await http.PostAsync("https://api.mercadopago.com/checkout/preferences", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CommException.BadGateway("Erro ao criar checkout no Mercado Pago. Verifique o Access Token e as credenciais.");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var prefResponse = JsonSerializer.Deserialize<MercadoPagoPreferenceResponse>(responseJson);
        if (string.IsNullOrWhiteSpace(prefResponse?.InitPoint))
            throw CommException.BadGateway("Resposta inválida do Mercado Pago.");

        return Ok(new CheckoutResponse(prefResponse.InitPoint));
    }
}

/// <summary>Indica se o pagamento com cartão está habilitado.</summary>
public record CardEnabledResponse(bool Enabled);

/// <summary>Indica se o PIX está habilitado (mesmo Access Token do cartão).</summary>
public record PixEnabledResponse(bool Enabled);

/// <summary>Request para criar ordem PIX: plano escolhido (valor já sai no QR).</summary>
public record CreatePixRequest(Guid PlanId);

/// <summary>Resposta da ordem PIX: id da ordem, código copia-e-cola e QR em base64 para exibir imagem.</summary>
public record PixOrderResponse(string OrderId, string QrCode, string QrCodeBase64);

/// <summary>Request para criar checkout (plano escolhido). ReturnBaseUrl opcional: origem do frontend (ex.: https://localhost:5173) para o link "Voltar à loja" do Mercado Pago redirecionar corretamente.</summary>
public record CheckoutRequest(Guid PlanId, string? ReturnBaseUrl = null);

/// <summary>URL para redirecionar o usuário ao checkout do Mercado Pago.</summary>
public record CheckoutResponse(string InitPoint);

// DTOs para a API do Mercado Pago (Checkout Preferences) — nomes em snake_case
internal class MercadoPagoPreferenceRequest
{
    [JsonPropertyName("items")]
    public List<MercadoPagoItem> Items { get; set; } = new();
    [JsonPropertyName("back_urls")]
    public MercadoPagoBackUrls BackUrls { get; set; } = new();
    [JsonPropertyName("auto_return")]
    public string? AutoReturn { get; set; }
    [JsonPropertyName("external_reference")]
    public string? ExternalReference { get; set; }
}

internal class MercadoPagoItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }
    [JsonPropertyName("currency_id")]
    public string? CurrencyId { get; set; }
}

internal class MercadoPagoBackUrls
{
    [JsonPropertyName("success")]
    public string? Success { get; set; }
    [JsonPropertyName("failure")]
    public string? Failure { get; set; }
    [JsonPropertyName("pending")]
    public string? Pending { get; set; }
}

internal class MercadoPagoPreferenceResponse
{
    public string? Id { get; set; }
    [JsonPropertyName("init_point")]
    public string? InitPoint { get; set; }
}
