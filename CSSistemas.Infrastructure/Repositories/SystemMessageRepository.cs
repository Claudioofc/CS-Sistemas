using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class SystemMessageRepository : ISystemMessageRepository
{
    private readonly AppDbContext _context;

    public SystemMessageRepository(AppDbContext context) => _context = context;

    public async Task<SystemMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.SystemMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<SystemMessage?> GetByIdAndBusinessIdAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
        => await _context.SystemMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.BusinessId == businessId, cancellationToken);

    public async Task<SystemMessage?> GetByIdAndBusinessIdForUpdateAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
        => await _context.SystemMessages
            .FirstOrDefaultAsync(m => m.Id == id && m.BusinessId == businessId, cancellationToken);

    public async Task<SystemMessage?> GetByBusinessIdAndKeyAsync(Guid businessId, string key, CancellationToken cancellationToken = default)
        => await _context.SystemMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.BusinessId == businessId && m.Key == key, cancellationToken);

    public async Task<IReadOnlyList<SystemMessage>> GetByBusinessIdAsync(Guid businessId, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _context.SystemMessages.AsNoTracking().Where(m => m.BusinessId == businessId);
        if (onlyActive)
            query = query.Where(m => m.IsActive);
        return await query.OrderBy(m => m.Key).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SystemMessage message, CancellationToken cancellationToken = default)
    {
        await _context.SystemMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SystemMessage message, CancellationToken cancellationToken = default)
    {
        _context.SystemMessages.Update(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
    {
        var message = await GetByIdAndBusinessIdForUpdateAsync(id, businessId, cancellationToken);
        if (message == null) return false;
        message.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
