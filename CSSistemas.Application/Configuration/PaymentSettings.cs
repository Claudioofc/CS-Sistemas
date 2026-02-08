namespace CSSistemas.Application.Configuration;

/// <summary>Configuração de pagamentos (PIX e cartão). Preenchido via appsettings na API.</summary>
public class PaymentSettings
{
    public const string SectionName = "Payment";

    public PixSettings Pix { get; set; } = new();
    public CardSettings Card { get; set; } = new();
    public MercadoPagoSettings MercadoPago { get; set; } = new();
}

public class PixSettings
{
    /// <summary>Chave PIX (e-mail, CPF, CNPJ ou telefone).</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Tipo da chave: email, cpf, cnpj, phone, random.</summary>
    public string KeyType { get; set; } = "email";
    /// <summary>Nome completo do recebedor (como está na conta PIX). Máx. 25 caracteres no BRCode.</summary>
    public string MerchantName { get; set; } = string.Empty;
    /// <summary>Cidade do recebedor (obrigatório no BRCode). Máx. 15 caracteres.</summary>
    public string MerchantCity { get; set; } = string.Empty;
}

public class CardSettings
{
    /// <summary>Pagamento com cartão habilitado (Mercado Pago).</summary>
    public bool Enabled { get; set; }
}

/// <summary>Configuração do Mercado Pago (Checkout Pro para cartão).</summary>
public class MercadoPagoSettings
{
    /// <summary>Access Token do Mercado Pago (credenciais > produção ou teste). Nunca exponha no frontend.</summary>
    public string AccessToken { get; set; } = string.Empty;
    /// <summary>URL de retorno após pagamento aprovado (ex: https://seusite.com/planos?status=success).</summary>
    public string SuccessUrl { get; set; } = string.Empty;
    /// <summary>URL de retorno quando o usuário cancela ou falha (ex: https://seusite.com/planos?status=failure).</summary>
    public string FailureUrl { get; set; } = string.Empty;
    /// <summary>URL de retorno quando o usuário clica em "voltar" (ex: https://seusite.com/planos).</summary>
    public string PendingUrl { get; set; } = string.Empty;
}
