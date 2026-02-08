namespace CSSistemas.Application.DTOs.Business;

/// <summary>Horário de um dia. Se null, o dia está fechado.</summary>
public record BusinessHoursItemResponse(int DayOfWeek, int? OpenAtMinutes, int? CloseAtMinutes);
