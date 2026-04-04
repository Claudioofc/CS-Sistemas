using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context) => _context = context;

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<Employee?> GetByIdAndBusinessIdAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
        => await _context.Employees.FirstOrDefaultAsync(e => e.Id == id && e.BusinessId == businessId, cancellationToken);

    public async Task<IReadOnlyList<Employee>> GetByBusinessIdAsync(Guid businessId, bool onlyActive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Employees.AsNoTracking().Where(e => e.BusinessId == businessId);
        if (onlyActive) query = query.Where(e => e.IsActive);
        return await query.OrderBy(e => e.Name).ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
        => await _context.Employees.CountAsync(e => e.BusinessId == businessId && e.IsActive, cancellationToken);

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
    {
        var employee = await GetByIdAndBusinessIdAsync(id, businessId, cancellationToken);
        if (employee == null) return false;
        employee.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
