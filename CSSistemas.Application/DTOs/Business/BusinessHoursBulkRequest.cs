namespace CSSistemas.Application.DTOs.Business;

/// <summary>Lista de 7 itens (0=Domingo a 6=Sábado) para atualizar horários de funcionamento.</summary>
public record BusinessHoursBulkRequest(List<BusinessHoursItemRequest> Items);
