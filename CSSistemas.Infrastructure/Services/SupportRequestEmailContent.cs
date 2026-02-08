using System.Net;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Conteúdo do e-mail de Fale conosco (DRY entre Resend e SMTP).</summary>
internal static class SupportRequestEmailContent
{
    public const string Subject = "Fale conosco - CS Sistemas (reporte de problema)";

    public static string BuildPlainTextBody(string userName, string userEmail, string message, string? pageUrl)
    {
        var pageBlock = !string.IsNullOrWhiteSpace(pageUrl) ? $"\nPágina onde ocorreu: {pageUrl}\n" : "";
        return $"Um cliente enviou uma mensagem pelo sistema (Fale conosco).\n\nNome: {userName}\nE-mail: {userEmail}{pageBlock}\nMensagem:\n{message}\n\n— CS Sistemas";
    }

    public static string BuildHtmlBody(string userName, string userEmail, string message, string? pageUrl)
    {
        var pageBlock = !string.IsNullOrWhiteSpace(pageUrl)
            ? $"<p><strong>Página onde ocorreu:</strong> <a href=\"{WebUtility.HtmlEncode(pageUrl)}\">{WebUtility.HtmlEncode(pageUrl)}</a></p>"
            : "";
        return $@"
<p>Um cliente enviou uma mensagem pelo sistema (Fale conosco / Reportar problema).</p>
<p><strong>Nome:</strong> {WebUtility.HtmlEncode(userName)}<br/>
<strong>E-mail:</strong> {WebUtility.HtmlEncode(userEmail)}</p>
{pageBlock}
<p><strong>Mensagem:</strong></p>
<p style=""white-space:pre-wrap;"">{WebUtility.HtmlEncode(message)}</p>
<p>— CS Sistemas</p>";
    }
}
