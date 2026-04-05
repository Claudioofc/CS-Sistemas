namespace CSSistemas.Application.DTOs.Employee;

public record EmployeeServicePriceDto(Guid ServiceId, decimal Price);

public record EmployeeResponse(Guid Id, string Name, string? Role, bool IsActive, IReadOnlyList<EmployeeServicePriceDto>? ServicePrices = null);
