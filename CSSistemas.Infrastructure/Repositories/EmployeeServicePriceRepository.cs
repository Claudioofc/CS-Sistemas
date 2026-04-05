using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class EmployeeServicePriceRepository : IEmployeeServicePriceRepository
{
    private readonly AppDbContext _db;

    public EmployeeServicePriceRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EmployeeServicePrice>> GetByEmployeeIdsAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0) return Array.Empty<EmployeeServicePrice>();
        return await _db.EmployeeServicePrices
            .Where(p => ids.Contains(p.EmployeeId))
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceAllForEmployeeAsync(Guid employeeId, IEnumerable<(Guid ServiceId, decimal Price)> prices, CancellationToken cancellationToken = default)
    {
        await _db.EmployeeServicePrices
            .Where(p => p.EmployeeId == employeeId)
            .ExecuteDeleteAsync(cancellationToken);

        var newPrices = prices
            .Where(p => p.Price >= 0)
            .Select(p => EmployeeServicePrice.Create(employeeId, p.ServiceId, p.Price))
            .ToList();

        if (newPrices.Count > 0)
        {
            _db.EmployeeServicePrices.AddRange(newPrices);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ReplaceAllForServiceAsync(Guid serviceId, Guid businessId, IEnumerable<(Guid EmployeeId, decimal Price)> prices, CancellationToken cancellationToken = default)
    {
        // Busca IDs dos funcionários do negócio para garantir isolamento
        var employeeIds = await _db.Set<CSSistemas.Domain.Entities.Employee>()
            .Where(e => e.BusinessId == businessId && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        await _db.EmployeeServicePrices
            .Where(p => p.ServiceId == serviceId && employeeIds.Contains(p.EmployeeId))
            .ExecuteDeleteAsync(cancellationToken);

        var newPrices = prices
            .Where(p => employeeIds.Contains(p.EmployeeId) && p.Price >= 0)
            .Select(p => EmployeeServicePrice.Create(p.EmployeeId, serviceId, p.Price))
            .ToList();

        if (newPrices.Count > 0)
        {
            _db.EmployeeServicePrices.AddRange(newPrices);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
