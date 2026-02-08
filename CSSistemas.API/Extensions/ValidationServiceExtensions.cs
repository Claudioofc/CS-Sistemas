using CSSistemas.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CSSistemas.API.Extensions;

/// <summary>Configuração de validação (FluentValidation).</summary>
public static class ValidationServiceExtensions
{
    public static IServiceCollection AddApiValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        return services;
    }
}
