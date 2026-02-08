using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class BusinessHoursRepository : IBusinessHoursRepository
{
    private readonly AppDbContext _context;

    public BusinessHoursRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<BusinessHours>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
        => await _context.BusinessHours
            .AsNoTracking()
            .Where(h => h.BusinessId == businessId)
            .OrderBy(h => h.DayOfWeek)
            .ThenBy(h => h.OpenAtMinutes)
            .ToListAsync(cancellationToken);

    public async Task<List<BusinessHours>> GetByBusinessIdForUpdateAsync(Guid businessId, CancellationToken cancellationToken = default)
        => await _context.BusinessHours
            .Where(h => h.BusinessId == businessId)
            .OrderBy(h => h.DayOfWeek)
            .ToListAsync(cancellationToken);

    public async Task<BusinessHours?> GetByBusinessIdAndDayAsync(Guid businessId, int dayOfWeek, CancellationToken cancellationToken = default)
        => await _context.BusinessHours
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.BusinessId == businessId && h.DayOfWeek == dayOfWeek, cancellationToken);

    public async Task AddAsync(BusinessHours entity, CancellationToken cancellationToken = default)
    {
        await _context.BusinessHours.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BusinessHours entity, CancellationToken cancellationToken = default)
    {
        _context.BusinessHours.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var list = await _context.BusinessHours.Where(h => h.BusinessId == businessId).ToListAsync(cancellationToken);
        foreach (var h in list)
            h.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
