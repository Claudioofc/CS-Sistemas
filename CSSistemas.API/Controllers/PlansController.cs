using CSSistemas.Application.DTOs.Plan;
using CSSistemas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly IPlanRepository _repository;

    public PlansController(IPlanRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Lista planos ativos (Mensal, 6 meses, 1 ano) para exibir na tela de assinatura.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var plans = await _repository.GetActiveAsync(cancellationToken);
        var response = plans.Select(p => new PlanResponse(
            p.Id,
            p.Name,
            p.Price,
            p.BillingIntervalMonths,
            p.Features,
            p.IsActive,
            p.CreatedAt
        )).ToList();
        return Ok(response);
    }
}
