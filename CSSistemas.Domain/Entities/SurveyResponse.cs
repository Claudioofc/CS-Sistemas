namespace CSSistemas.Domain.Entities;

public class SurveyResponse
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int Score { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected SurveyResponse() { }

    public static SurveyResponse Create(Guid userId, int score, string? comment)
    {
        if (score < 0 || score > 10)
            throw new ArgumentOutOfRangeException(nameof(score), "Score deve ser entre 0 e 10.");
        return new SurveyResponse
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Score = score,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
