using CSSistemas.API.Extensions;
using CSSistemas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace CSSistemas.API.Authorization;

/// <summary>Verifica se o usuário autenticado é admin (IsAdmin no banco), com cache de 5 minutos.</summary>
public class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AdminAuthorizationHandler(IUserRepository userRepository, IMemoryCache cache)
    {
        _userRepository = userRepository;
        _cache = cache;
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

        var cacheKey = $"admin_check:{userId.Value:N}";
        if (!_cache.TryGetValue(cacheKey, out bool isAdmin))
        {
            var user = await _userRepository.GetByIdAsync(userId.Value);
            isAdmin = user?.IsAdmin ?? false;
            _cache.Set(cacheKey, isAdmin, CacheDuration);
        }

        if (isAdmin)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
