namespace CSSistemas.Application.DTOs.Business;

/// <summary>Um dia da semana: DayOfWeek 0-6. Se OpenAtMinutes e CloseAtMinutes forem null, o dia é fechado.</summary>
public record BusinessHoursItemRequest(int DayOfWeek, int? OpenAtMinutes, int? CloseAtMinutes);
