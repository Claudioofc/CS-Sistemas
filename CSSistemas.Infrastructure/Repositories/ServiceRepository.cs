using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _context;

    public ServiceRepository(AppDbContext context) => _context = context;

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Service?> GetByIdAndBusinessIdAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
        => await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken);

    public async Task<Service?> GetByIdAndBusinessIdForUpdateAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
        => await _context.Services
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken);

    public async Task<IReadOnlyList<Service>> GetByBusinessIdAsync(Guid businessId, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Services.AsNoTracking().Where(s => s.BusinessId == businessId);
        if (onlyActive)
            query = query.Where(s => s.IsActive);
        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        await _context.Services.AddAsync(service, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Service service, CancellationToken cancellationToken = default)
    {
        _context.Services.Update(service);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
    {
        var service = await GetByIdAndBusinessIdForUpdateAsync(id, businessId, cancellationToken);
        if (service == null) return false;
        service.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
