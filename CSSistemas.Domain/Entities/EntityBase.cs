namespace CSSistemas.Domain.Entities;

/// <summary>
/// Base para entidades do domínio (DRY). Id, auditoria e soft delete (IsDeleted/DeletedAt).
/// Evita delete físico (hard delete); registros são marcados como excluídos.
/// </summary>
public abstract class EntityBase
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }

    /// <summary>Soft delete: marca como excluído sem remover do banco.</summary>
    public void MarkAsDeleted()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
