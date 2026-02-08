using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly AppDbContext _context;

    public PlanRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<Plan>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.BillingIntervalMonths)
            .ToListAsync(cancellationToken);

    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
