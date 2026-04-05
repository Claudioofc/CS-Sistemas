using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using CSSistemas.API.Extensions;
using CSSistemas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SurveyController : ControllerBase
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly IUserRepository _userRepo;
    private readonly IBusinessRepository _businessRepo;
    private readonly IAppointmentRepository _appointmentRepo;

    public SurveyController(
        ISurveyRepository surveyRepo,
        IUserRepository userRepo,
        IBusinessRepository businessRepo,
        IAppointmentRepository appointmentRepo)
    {
        _surveyRepo = surveyRepo;
        _userRepo = userRepo;
        _businessRepo = businessRepo;
        _appointmentRepo = appointmentRepo;
    }

    /// <summary>Verifica se o usuário deve ver o modal de pesquisa de satisfação.</summary>
    [HttpGet("eligibility")]
    [ProducesResponseType(typeof(SurveyEligibilityResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEligibility(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _userRepo.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null || user.IsAdmin)
            return Ok(new SurveyEligibilityResponse(false));

        // Conta mínima de 7 dias
        if ((DateTime.UtcNow - user.CreatedAt).TotalDays < 7)
            return Ok(new SurveyEligibilityResponse(false));

        // Já respondeu
        if (await _surveyRepo.HasRespondedAsync(userId.Value, cancellationToken))
            return Ok(new SurveyEligibilityResponse(false));

        // Dispensou 2 ou mais vezes
        if (user.SurveyDismissals >= 2)
            return Ok(new SurveyEligibilityResponse(false));

        // Tem pelo menos 1 agendamento em qualquer negócio
        var businesses = await _businessRepo.GetByUserIdAsync(userId.Value, cancellationToken);
        var hasAppointment = false;
        foreach (var business in businesses)
        {
            var appts = await _appointmentRepo.GetByBusinessIdAsync(business.Id, cancellationToken: cancellationToken);
            if (appts.Count > 0) { hasAppointment = true; break; }
        }
        if (!hasAppointment)
            return Ok(new SurveyEligibilityResponse(false));

        return Ok(new SurveyEligibilityResponse(true));
    }

    /// <summary>Salva a resposta da pesquisa de satisfação (NPS 0–10).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] SurveySubmitRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        if (request.Score < 0 || request.Score > 10)
            throw CommException.BadRequest("Score deve ser entre 0 e 10.");

        var response = SurveyResponse.Create(userId.Value, request.Score, request.Comment);
        await _surveyRepo.AddAsync(response, cancellationToken);
        return NoContent();
    }

    /// <summary>Registra que o usuário dispensou o modal sem responder.</summary>
    [HttpPost("dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Dismiss(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var user = await _userRepo.GetByIdForUpdateAsync(userId.Value, cancellationToken);
        if (user == null) return Unauthorized();
        user.IncrementSurveyDismissals();
        await _userRepo.UpdateAsync(user, cancellationToken);
        return NoContent();
    }

    /// <summary>Retorna todas as respostas (apenas admin).</summary>
    [HttpGet("results")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<SurveyResultItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResults(CancellationToken cancellationToken)
    {
        var responses = await _surveyRepo.GetAllAsync(cancellationToken);
        var result = responses.Select(r => new SurveyResultItem(r.Id, r.UserId, r.Score, r.Comment, r.CreatedAt));
        return Ok(result);
    }
}

public record SurveyEligibilityResponse(bool Eligible);
public record SurveySubmitRequest(int Score, string? Comment);
public record SurveyResultItem(Guid Id, Guid UserId, int Score, string? Comment, DateTime CreatedAt);
