namespace CSSistemas.Application.DTOs.Employee;

public record EmployeeResponse(Guid Id, string Name, string? Role, bool IsActive);
