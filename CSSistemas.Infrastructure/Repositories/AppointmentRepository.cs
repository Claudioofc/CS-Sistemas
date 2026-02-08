using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using CSSistemas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;

    public AppointmentRepository(AppDbContext context) => _context = context;

    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Appointment?> GetByIdAndBusinessIdAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
        => await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.BusinessId == businessId, cancellationToken);

    public async Task<Appointment?> GetByIdAndBusinessIdForUpdateAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
        => await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.BusinessId == businessId, cancellationToken);

    public async Task<Appointment?> GetByCancelTokenAsync(string cancelToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cancelToken)) return null;
        return await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CancelToken == cancelToken.Trim(), cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByBusinessIdAsync(Guid businessId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = ApplyBusinessIdAndDateFilter(_context.Appointments.AsNoTracking(), businessId, from, to);
        return await query.OrderBy(a => a.ScheduledAt).ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetByBusinessIdPagedAsync(Guid businessId, DateTime? from, DateTime? to, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplyBusinessIdAndDateFilter(_context.Appointments.AsNoTracking(), businessId, from, to);
        var searchTrim = search?.Trim();
        if (!string.IsNullOrEmpty(searchTrim))
        {
            var term = searchTrim.ToLowerInvariant();
            query = query.Where(a =>
                (a.ClientName != null && a.ClientName.ToLower().Contains(term)) ||
                (a.ClientPhone != null && a.ClientPhone.Contains(term)));
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(a => a.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    private static IQueryable<Appointment> ApplyBusinessIdAndDateFilter(IQueryable<Appointment> source, Guid businessId, DateTime? from, DateTime? to)
    {
        var query = source.Where(a => a.BusinessId == businessId);
        if (from.HasValue)
        {
            var f = from.Value.Kind == DateTimeKind.Utc ? from.Value : DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(a => a.ScheduledAt >= f);
        }
        if (to.HasValue)
        {
            var t = to.Value.Kind == DateTimeKind.Utc ? to.Value : DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(a => a.ScheduledAt <= t);
        }
        return query;
    }

    public async Task<IReadOnlyList<Appointment>> GetByBusinessIdWithServiceAsync(Guid businessId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var f = from.Kind == DateTimeKind.Utc ? from : DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var t = to.Kind == DateTimeKind.Utc ? to : DateTime.SpecifyKind(to, DateTimeKind.Utc);
        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Service)
            .Where(a => a.BusinessId == businessId && a.ScheduledAt >= f && a.ScheduledAt <= t)
            .OrderBy(a => a.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasConflictAsync(Guid businessId, DateTime scheduledAt, int durationMinutes, Guid? excludeAppointmentId = null, CancellationToken cancellationToken = default)
    {
        var start = scheduledAt.Kind == DateTimeKind.Utc ? scheduledAt : DateTime.SpecifyKind(scheduledAt, DateTimeKind.Utc);
        var end = start.AddMinutes(durationMinutes);

        var query = _context.Appointments
            .Where(a => a.BusinessId == businessId && a.Status != AppointmentStatus.Cancelled)
            .Where(a => a.ScheduledAt < end && EF.Property<DateTime>(a, "ScheduledAt").AddMinutes(0) + TimeSpan.FromMinutes(0) < end);

        // Overlap: existing_start < new_end AND existing_end > new_start
        // existing_end = ScheduledAt + Duration (we need Service.DurationMinutes). Simpler: load appointments in range and check in memory, or use raw SQL.
        // EF doesn't have direct access to Service.DurationMinutes in same table. So we need to join Service or load appointments that start in [start - maxDuration, end + maxDuration]. For simplicity, check appointments that start in [start - 480, end + 480] (8h window) and then filter by overlap in memory. Better: get appointments where ScheduledAt < end, and for each we need end time. So join with Services.
        var candidates = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Service)
            .Where(a => a.BusinessId == businessId && a.Status != AppointmentStatus.Cancelled)
            .Where(a => a.ScheduledAt < end) // appointment start before new end
            .ToListAsync(cancellationToken);

        if (excludeAppointmentId.HasValue)
            candidates = candidates.Where(a => a.Id != excludeAppointmentId.Value).ToList();

        foreach (var a in candidates)
        {
            var existingEnd = a.ScheduledAt.AddMinutes(a.Service.DurationMinutes);
            if (existingEnd > start)
                return true;
        }
        return false;
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await _context.Appointments.AddAsync(appointment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default)
    {
        var appointment = await GetByIdAndBusinessIdForUpdateAsync(id, businessId, cancellationToken);
        if (appointment == null) return false;
        appointment.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
