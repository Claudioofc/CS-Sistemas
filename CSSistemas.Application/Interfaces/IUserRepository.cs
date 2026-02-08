using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;

namespace CSSistemas.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Obtém usuário por id, rastreado para atualização (ex: perfil).</summary>
    Task<User?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Obtém usuário por e-mail, rastreado para atualização (ex: definir token de redefinição).</summary>
    Task<User?> GetByEmailForUpdateAsync(string email, CancellationToken cancellationToken = default);
    /// <summary>Obtém usuário por token de redefinição (válido e não expirado), rastreado para atualização.</summary>
    Task<User?> GetByResetTokenForUpdateAsync(string token, CancellationToken cancellationToken = default);
    /// <summary>Indica se já existe usuário com este documento (apenas dígitos). excludeUserId para ignorar o próprio usuário ao atualizar perfil.</summary>
    Task<bool> ExistsByDocumentAsync(DocumentType documentType, string documentNumberDigits, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    /// <summary>Lista todos os usuários (uso admin).</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Ids dos usuários administradores (para notificações de novo cadastro).</summary>
    Task<IReadOnlyList<Guid>> GetAdminIdsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
