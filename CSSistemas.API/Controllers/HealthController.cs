using CSSistemas.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    /// <summary>Verifica conexão com PostgreSQL.</summary>
    [HttpGet("db")]
    [ProducesResponseType(typeof(HealthDbResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthDbResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Db(CancellationToken cancellationToken)
    {
        try
        {
            var conectado = await _db.Database.CanConnectAsync(cancellationToken);
            return Ok(new HealthDbResponse(conectado, "PostgreSQL conectado com sucesso."));
        }
        catch (Exception ex)
        {
            return StatusCode(503, new HealthDbResponse(false, ex.Message));
        }
    }
}

public record HealthDbResponse(bool Conectado, string Mensagem);
