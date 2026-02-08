using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _context;

    public ClientRepository(AppDbContext context) => _context = context;

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Client?> GetByIdAndBusinessIdAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
        => await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId, cancellationToken);

    public async Task<Client?> GetByIdAndBusinessIdForUpdateAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
        => await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId, cancellationToken);

    public async Task<IReadOnlyList<Client>> GetByBusinessIdAsync(Guid businessId, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Clients.AsNoTracking().Where(c => c.BusinessId == businessId);
        if (onlyActive)
            query = query.Where(c => c.IsActive);
        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddAsync(client, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Update(client);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
    {
        var client = await GetByIdAndBusinessIdForUpdateAsync(id, businessId, cancellationToken);
        if (client == null) return false;
        client.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
