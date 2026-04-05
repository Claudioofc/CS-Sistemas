using CSSistemas.Domain.Entities;

namespace CSSistemas.Application.Interfaces;

public interface ISurveyRepository
{
    Task<bool> HasRespondedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(SurveyResponse response, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SurveyResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
