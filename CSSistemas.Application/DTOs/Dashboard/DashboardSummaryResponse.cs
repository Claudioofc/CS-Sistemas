namespace CSSistemas.Application.DTOs.Dashboard;

/// <summary>Resumo do dashboard para um negócio.</summary>
public record DashboardSummaryResponse(
    int ProximosAgendamentosCount,
    int ClientesHojeCount,
    int FaltasCount,
    decimal GanhosDoMes,
    IReadOnlyList<AgendaItemDto> AgendaDoDia,
    IReadOnlyList<ProximoAgendamentoDto> ProximosAgendamentos);

public record AgendaItemDto(string Hora, string Servico, string Cliente);

public record ProximoAgendamentoDto(string Data, string Hora, string Cliente, string Servico);
