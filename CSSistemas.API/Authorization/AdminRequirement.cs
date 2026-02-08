using Microsoft.AspNetCore.Authorization;

namespace CSSistemas.API.Authorization;

/// <summary>Requisito de autorização: usuário deve ser admin do sistema.</summary>
public class AdminRequirement : IAuthorizationRequirement
{
}
