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
    private readonly IEmployeeServicePriceRepository _priceRepo;

    public EmployeesController(
        IEmployeeRepository employeeRepo,
        IBusinessRepository businessRepo,
        IEmployeeServicePriceRepository priceRepo)
    {
        _employeeRepo = employeeRepo;
        _businessRepo = businessRepo;
        _priceRepo = priceRepo;
    }

    private async Task<bool> BelongsToUserAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return false;
        var business = await _businessRepo.GetByIdAsync(businessId, cancellationToken);
        return business != null && business.UserId == userId.Value;
    }

    private static EmployeeResponse ToResponse(Employee e, ILookup<Guid, EmployeeServicePriceDto> pricesLookup) =>
        new(e.Id, e.Name, e.Role, e.IsActive, pricesLookup[e.Id].ToList());

    /// <summary>Lista funcionários do negócio com seus preços por serviço.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid businessId, CancellationToken cancellationToken)
    {
        if (!await BelongsToUserAsync(businessId, cancellationToken)) return Forbid();
        var employees = await _employeeRepo.GetByBusinessIdAsync(businessId, onlyActive: false, cancellationToken);
        var prices = await _priceRepo.GetByEmployeeIdsAsync(employees.Select(e => e.Id), cancellationToken);
        var lookup = prices.ToLookup(p => p.EmployeeId, p => new EmployeeServicePriceDto(p.ServiceId, p.Price));
        return Ok(employees.Select(e => ToResponse(e, lookup)));
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
        return CreatedAtAction(nameof(GetAll), new { businessId }, new EmployeeResponse(employee.Id, employee.Name, employee.Role, employee.IsActive, new List<EmployeeServicePriceDto>()));
    }

    /// <summary>Atualiza nome, cargo e status do funcionário.</summary>
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

        var prices = await _priceRepo.GetByEmployeeIdsAsync(new[] { id }, cancellationToken);
        var priceDtos = prices.Select(p => new EmployeeServicePriceDto(p.ServiceId, p.Price)).ToList();
        return Ok(new EmployeeResponse(employee.Id, employee.Name, employee.Role, employee.IsActive, priceDtos));
    }

    /// <summary>Define preços personalizados do funcionário por serviço. Substitui todos os preços existentes.</summary>
    [HttpPut("{id}/prices")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrices(Guid businessId, Guid id, [FromBody] List<EmployeeServicePriceRequest> request, CancellationToken cancellationToken)
    {
        if (!await BelongsToUserAsync(businessId, cancellationToken)) return Forbid();
        var employee = await _employeeRepo.GetByIdAndBusinessIdAsync(id, businessId, cancellationToken);
        if (employee == null) throw CommException.NotFound("Funcionário não encontrado.");

        var prices = (request ?? new())
            .Where(p => p.Price >= 0)
            .Select(p => (p.ServiceId, p.Price));

        await _priceRepo.ReplaceAllForEmployeeAsync(id, prices, cancellationToken);
        return NoContent();
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

public record EmployeeServicePriceRequest(Guid ServiceId, decimal Price);
