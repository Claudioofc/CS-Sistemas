using System.Globalization;

namespace CSSistemas.Application.Helpers;

/// <summary>Fuso horário Brasil e formatação de data/hora para exibição (e-mails, notificações). Único ponto de definição (DRY).</summary>
public static class BrazilTimeHelper
{
    private static readonly TimeZoneInfo BrazilTz = GetBrazilTimeZone();

    /// <summary>Obtém o fuso de Brasília. Tenta America/Sao_Paulo (Linux/Mac) e fallback para E. South America Standard Time (Windows).</summary>
    public static TimeZoneInfo GetBrazilTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }

    /// <summary>Converte UTC para horário Brasil e formata para exibição (ex.: "02/02/2025 às 14:30").</summary>
    public static string FormatUtcToBrazilDateTime(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, BrazilTz);
        return local.ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.GetCultureInfo("pt-BR"));
    }

    /// <summary>Converte data/hora não-UTC para UTC assumindo que o valor está no fuso Brasil.</summary>
    public static DateTime ConvertBrazilToUtc(DateTime localOrUnspecified)
    {
        if (localOrUnspecified.Kind == DateTimeKind.Utc)
            return localOrUnspecified;
        var asUnspecified = DateTime.SpecifyKind(localOrUnspecified, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(asUnspecified, BrazilTz);
    }
}
