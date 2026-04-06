using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class BusinessRepository : IBusinessRepository
{
    private readonly AppDbContext _context;

    public BusinessRepository(AppDbContext context) => _context = context;

    public async Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<Business?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);

    public async Task<Business?> GetByIdAndUserIdForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Business>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Businesses
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Business>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Businesses
            .AsNoTracking()
            .Include(b => b.User)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

    public async Task<Business?> GetByPublicSlugAsync(string publicSlug, CancellationToken cancellationToken = default)
        => await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.PublicSlug == publicSlug.Trim().ToLowerInvariant(), cancellationToken);

    public async Task AddAsync(Business business, CancellationToken cancellationToken = default)
    {
        await _context.Businesses.AddAsync(business, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Business business, CancellationToken cancellationToken = default)
    {
        _context.Businesses.Update(business);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var business = await GetByIdAndUserIdForUpdateAsync(id, userId, cancellationToken);
        if (business == null) return false;

        var now = DateTime.UtcNow;

        // Cascata: soft-delete em todos os filhos do negócio
        await _context.Services
            .Where(e => e.BusinessId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.DeletedAt, now)
                .SetProperty(e => e.UpdatedAt, now), cancellationToken);

        await _context.Appointments
            .Where(e => e.BusinessId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.DeletedAt, now)
                .SetProperty(e => e.UpdatedAt, now), cancellationToken);

        await _context.Clients
            .Where(e => e.BusinessId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.DeletedAt, now)
                .SetProperty(e => e.UpdatedAt, now), cancellationToken);

        await _context.Employees
            .Where(e => e.BusinessId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.DeletedAt, now)
                .SetProperty(e => e.UpdatedAt, now), cancellationToken);

        await _context.BusinessHours
            .Where(e => e.BusinessId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.DeletedAt, now)
                .SetProperty(e => e.UpdatedAt, now), cancellationToken);

        business.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
