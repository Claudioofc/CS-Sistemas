using System.Net;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Conteúdo do e-mail de boas-vindas ao novo usuário (DRY entre Resend e SMTP).</summary>
internal static class WelcomeEmailContent
{
    public const string Subject = "Bem-vindo ao CS Sistemas";

    public static string BuildPlainTextBody(string userName)
    {
        return $"Olá, {userName}!\n\nBem-vindo ao CS Sistemas. Agora você pode configurar sua empresa, cadastrar serviços e horários e compartilhar o link de agendamento com seus clientes.\n\nAcesse o painel e, se tiver dúvidas, use o menu \"Fale conosco\".\n\n— Equipe CS Sistemas";
    }

    public static string BuildHtmlBody(string userName)
    {
        var safeName = WebUtility.HtmlEncode(userName);
        return $@"
<p>Olá, {safeName}!</p>
<p>Bem-vindo ao <strong>CS Sistemas</strong>. Agora você pode configurar sua empresa, cadastrar serviços e horários e compartilhar o link de agendamento com seus clientes.</p>
<p>Acesse o painel e, se tiver dúvidas, use o menu <strong>Fale conosco</strong>.</p>
<p>— Equipe CS Sistemas</p>";
    }
}
