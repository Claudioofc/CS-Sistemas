using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class SurveyRepository : ISurveyRepository
{
    private readonly AppDbContext _db;

    public SurveyRepository(AppDbContext db) => _db = db;

    public Task<bool> HasRespondedAsync(Guid userId, CancellationToken cancellationToken = default)
        => _db.SurveyResponses.AnyAsync(r => r.UserId == userId, cancellationToken);

    public async Task AddAsync(SurveyResponse response, CancellationToken cancellationToken = default)
    {
        _db.SurveyResponses.Add(response);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SurveyResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.SurveyResponses
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
}
