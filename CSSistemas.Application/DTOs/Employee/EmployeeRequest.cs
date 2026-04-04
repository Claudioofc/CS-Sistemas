namespace CSSistemas.Application.DTOs.Employee;

public record EmployeeRequest(string Name, string? Role = null, bool IsActive = true);
