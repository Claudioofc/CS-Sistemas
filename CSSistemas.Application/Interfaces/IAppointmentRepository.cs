using CSSistemas.Domain.Entities;

namespace CSSistemas.Application.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Appointment?> GetByIdAndBusinessIdAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
    Task<Appointment?> GetByIdAndBusinessIdForUpdateAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
    /// <summary>Busca agendamento pelo token de cancelamento (link do e-mail). Retorna null se não existir ou já estiver excluído.</summary>
    Task<Appointment?> GetByCancelTokenAsync(string cancelToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetByBusinessIdAsync(Guid businessId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    /// <summary>Agendamentos do negócio no período, com filtro opcional por nome/telefone e paginação.</summary>
    Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetByBusinessIdPagedAsync(Guid businessId, DateTime? from, DateTime? to, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    /// <summary>Agendamentos do negócio no período, com Service (para cálculo de ganhos).</summary>
    Task<IReadOnlyList<Appointment>> GetByBusinessIdWithServiceAsync(Guid businessId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid businessId, DateTime scheduledAt, int durationMinutes, Guid? excludeAppointmentId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    /// <summary>Soft delete: marca como excluído (não remove do banco).</summary>
    Task<bool> SoftDeleteAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
}
