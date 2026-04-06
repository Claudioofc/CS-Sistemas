using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
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

    public async Task<IReadOnlyList<Subscription>> GetPremiumSubscriptionsOrderedByStartedAtAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.SubscriptionType == SubscriptionType.Monthly)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithExternalOrderIdAsync(string externalOrderId, CancellationToken cancellationToken = default)
        => await _context.Subscriptions.AnyAsync(s => s.ExternalOrderId == externalOrderId, cancellationToken);

    public async Task<IReadOnlyList<Subscription>> GetExpiringForWarningAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddDays(daysBeforeExpiry - 0.5);
        var windowEnd = now.AddDays(daysBeforeExpiry + 0.5);

        return await _context.Subscriptions
            .Include(s => s.User)
            .Where(s => s.SubscriptionType == SubscriptionType.Monthly
                     && s.EndsAt >= windowStart
                     && s.EndsAt <= windowEnd
                     && (daysBeforeExpiry == 7 ? s.ExpiryWarning7DaySentAt == null : s.ExpiryWarning1DaySentAt == null))
            .ToListAsync(cancellationToken);
    }
}
