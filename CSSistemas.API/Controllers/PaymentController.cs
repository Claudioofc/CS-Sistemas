using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSSistemas.API.Extensions;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

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

    public PaymentController(
        Microsoft.Extensions.Options.IOptions<PaymentSettings> payment,
        IPlanRepository planRepository,
        IUserRepository userRepository,
        IHttpClientFactory httpClientFactory)
    {
        _payment = payment.Value;
        _planRepository = planRepository;
        _userRepository = userRepository;
        _httpClientFactory = httpClientFactory;
    }

    private static bool HasValidMercadoPagoToken(string token)
    {
        return token.Length > 20
            && !token.Contains("ACCESS_TOKEN_AQUI")
            && !token.Contains("COLE_SEU");
    }

    private bool HasStaticPixConfigured()
    {
        var pix = _payment.Pix;
        return !string.IsNullOrWhiteSpace(pix.Key)
            && !string.IsNullOrWhiteSpace(pix.MerchantName)
            && !string.IsNullOrWhiteSpace(pix.MerchantCity);
    }

    /// <summary>Gera o BRCode (copia e cola) para PIX estático com valor fixo.</summary>
    private static string GeneratePixBrCode(string pixKey, string merchantName, string merchantCity, decimal amount, string txid)
    {
        merchantName = merchantName.Length > 25 ? merchantName[..25] : merchantName;
        merchantCity = merchantCity.Length > 15 ? merchantCity[..15] : merchantCity;
        if (txid.Length > 25) txid = txid[..25];

        static string Tlv(string tag, string value) => $"{tag}{value.Length:D2}{value}";

        var gui = Tlv("00", "br.gov.bcb.pix");
        var key = Tlv("01", pixKey);
        var mai = Tlv("26", gui + key);
        var adft = Tlv("62", Tlv("05", txid));
        var amountStr = amount.ToString("F2", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.Append(Tlv("00", "01"));
        sb.Append(mai);
        sb.Append(Tlv("52", "0000"));
        sb.Append(Tlv("53", "986"));
        sb.Append(Tlv("54", amountStr));
        sb.Append(Tlv("58", "BR"));
        sb.Append(Tlv("59", merchantName));
        sb.Append(Tlv("60", merchantCity));
        sb.Append(adft);
        sb.Append("6304");

        var crc = ComputeCrc16(sb.ToString());
        sb.Append(crc.ToString("X4"));
        return sb.ToString();
    }

    private static ushort ComputeCrc16(string input)
    {
        ushort crc = 0xFFFF;
        foreach (var c in input)
        {
            crc ^= (ushort)(c << 8);
            for (var i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc;
    }

    private static string GenerateQrCodeBase64(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        var bytes = qrCode.GetGraphic(10);
        return Convert.ToBase64String(bytes);
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

    /// <summary>Indica se o PIX está habilitado (Mercado Pago ou PIX estático configurado).</summary>
    [HttpGet("pix-enabled")]
    [ProducesResponseType(typeof(PixEnabledResponse), StatusCodes.Status200OK)]
    public IActionResult GetPixEnabled()
    {
        var token = _payment.MercadoPago.AccessToken ?? "";
        var enabled = HasValidMercadoPagoToken(token) || HasStaticPixConfigured();
        return Ok(new PixEnabledResponse(enabled));
    }

    /// <summary>Cria ordem PIX: usa Mercado Pago se configurado, caso contrário gera QR Code estático com a chave PIX da configuração.</summary>
    [HttpPost("pix")]
    [ProducesResponseType(typeof(PixOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CreatePixOrder([FromBody] CreatePixRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan == null || !plan.IsActive)
            throw CommException.BadRequest("Plano inválido ou inativo.");

        var mpToken = _payment.MercadoPago.AccessToken ?? "";

        // --- PIX estático tem precedência quando a chave está configurada ---
        if (!HasStaticPixConfigured() && HasValidMercadoPagoToken(mpToken))
        {
            var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
            var payerEmail = user?.Email ?? "pagador@cssistemas.com.br";
            var totalAmount = plan.Price.ToString("F2", CultureInfo.InvariantCulture);
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
                        new { amount = totalAmount, payment_method = new { id = "pix", type = "bank_transfer" } }
                    }
                }
            };
            using var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("Authorization", "Bearer " + mpToken);
            http.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString("N"));
            var json = JsonSerializer.Serialize(orderPayload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync("https://api.mercadopago.com/v1/orders", content, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw CommException.BadGateway($"Erro ao criar ordem PIX no Mercado Pago. Detalhe: {(int)response.StatusCode} - {responseJson}");
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            var orderId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            if (string.IsNullOrEmpty(orderId))
                throw CommException.BadGateway("Resposta inválida do Mercado Pago (sem id da ordem).");
            string? qrCode = null, qrCodeBase64 = null;
            if (root.TryGetProperty("transactions", out var transactions) &&
                transactions.TryGetProperty("payments", out var payments) &&
                payments.GetArrayLength() > 0)
            {
                var pm = payments[0].TryGetProperty("payment_method", out var pmProp) ? pmProp : (JsonElement?)null;
                if (pm.HasValue)
                {
                    if (pm.Value.TryGetProperty("qr_code", out var qr)) qrCode = qr.GetString();
                    if (pm.Value.TryGetProperty("qr_code_base64", out var qr64)) qrCodeBase64 = qr64.GetString();
                }
            }
            return Ok(new PixOrderResponse(orderId, qrCode ?? "", qrCodeBase64 ?? ""));
        }

        // --- PIX estático (sem Mercado Pago) ---
        if (!HasStaticPixConfigured())
            throw CommException.BadRequest("PIX não configurado. Configure Payment:Pix ou Payment:MercadoPago:AccessToken.");

        var pix = _payment.Pix;
        var txid = Guid.NewGuid().ToString("N")[..25];
        var brCode = GeneratePixBrCode(pix.Key, pix.MerchantName, pix.MerchantCity, plan.Price, txid);
        var qrBase64 = GenerateQrCodeBase64(brCode);
        return Ok(new PixOrderResponse($"STATIC-{txid}", brCode, qrBase64));
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
            ExternalReference = $"{userId:N}{plan.Id:N}"
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
