using CSSistemas.API.Extensions;
using CSSistemas.Application.DTOs.Employee;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/business/{businessId}/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IBusinessRepository _businessRepo;

    public EmployeesController(IEmployeeRepository employeeRepo, IBusinessRepository businessRepo)
    {
        _employeeRepo = employeeRepo;
        _businessRepo = businessRepo;
    }

    private async Task<bool> BelongsToUserAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return false;
        var business = await _businessRepo.GetByIdAsync(businessId, cancellationToken);
        return business != null && business.UserId == userId.Value;
    }

    /// <summary>Lista funcionários do negócio.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid businessId, CancellationToken cancellationToken)
    {
        if (!await BelongsToUserAsync(businessId, cancellationToken)) return Forbid();
        var employees = await _employeeRepo.GetByBusinessIdAsync(businessId, onlyActive: false, cancellationToken);
        return Ok(employees.Select(e => new EmployeeResponse(e.Id, e.Name, e.Role, e.IsActive)));
    }

    /// <summary>Cria um funcionário no negócio.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(Guid businessId, [FromBody] EmployeeRequest request, CancellationToken cancellationToken)
    {
        if (!await BelongsToUserAsync(businessId, cancellationToken)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Name))
            throw CommException.BadRequest("O nome é obrigatório.");

        var employee = Employee.Create(businessId, request.Name, request.Role);
        await _employeeRepo.AddAsync(employee, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { businessId }, new EmployeeResponse(employee.Id, employee.Name, employee.Role, employee.IsActive));
    }

    /// <summary>Atualiza um funcionário.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid businessId, Guid id, [FromBody] EmployeeRequest request, CancellationToken cancellationToken)
    {
        if (!await BelongsToUserAsync(businessId, cancellationToken)) return Forbid();
        var employee = await _employeeRepo.GetByIdAndBusinessIdAsync(id, businessId, cancellationToken);
        if (employee == null) throw CommException.NotFound("Funcionário não encontrado.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw CommException.BadRequest("O nome é obrigatório.");

        employee.Update(request.Name, request.Role, request.IsActive);
        await _employeeRepo.UpdateAsync(employee, cancellationToken);
        return Ok(new EmployeeResponse(employee.Id, employee.Name, employee.Role, employee.IsActive));
    }

    /// <summary>Remove um funcionário (soft delete).</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        if (!await BelongsToUserAsync(businessId, cancellationToken)) return Forbid();
        var deleted = await _employeeRepo.SoftDeleteAsync(id, businessId, cancellationToken);
        if (!deleted) throw CommException.NotFound("Funcionário não encontrado.");
        return NoContent();
    }
}
