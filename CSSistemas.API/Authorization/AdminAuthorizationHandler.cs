using CSSistemas.API.Extensions;
using CSSistemas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace CSSistemas.API.Authorization;

/// <summary>Verifica se o usuário autenticado é admin (IsAdmin no banco).</summary>
public class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly IUserRepository _userRepository;

    public AdminAuthorizationHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (userId == null)
        {
            context.Fail();
            return;
        }

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user != null && user.IsAdmin)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
