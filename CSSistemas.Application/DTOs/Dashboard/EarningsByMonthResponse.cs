namespace CSSistemas.Application.DTOs.Dashboard;

/// <summary>Ganhos de um mês (para gráfico ou lista).</summary>
public record EarningsByMonthDto(int Year, int Month, string MonthLabel, decimal Total);

/// <summary>Resposta do endpoint de ganhos por mês (últimos N meses).</summary>
public record EarningsByMonthResponse(IReadOnlyList<EarningsByMonthDto> Months);

/// <summary>Um item do detalhe de ganhos (agendamento concluído com serviço e valor).</summary>
public record EarningsDetailItemDto(DateTime ScheduledAt, string ClientName, string ServiceName, decimal Price);

/// <summary>Resposta do endpoint de detalhe de ganhos (lista do que gerou os ganhos).</summary>
public record EarningsDetailResponse(IReadOnlyList<EarningsDetailItemDto> Items);
