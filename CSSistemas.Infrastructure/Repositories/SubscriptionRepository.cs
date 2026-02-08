using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _context;

    public SubscriptionRepository(AppDbContext context) => _context = context;

    public async Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.EndsAt >= now)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsAnyByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Subscriptions.AnyAsync(s => s.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Subscription>> GetActiveByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0) return Array.Empty<Subscription>();
        var now = DateTime.UtcNow;
        var subs = await _context.Subscriptions
            .AsNoTracking()
            .Where(s => idList.Contains(s.UserId) && s.EndsAt >= now)
            .OrderByDescending(s => s.EndsAt)
            .ToListAsync(cancellationToken);
        return subs.GroupBy(s => s.UserId).Select(g => g.First()).ToList();
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
