namespace CSSistemas.Application.Interfaces;

/// <summary>
/// Unidade de trabalho para transações (DRY). Implementação na Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
