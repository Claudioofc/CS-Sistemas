using FluentValidation.Results;

namespace CSSistemas.Application.DTOs;

/// <summary>Resposta padronizada de erro de validação (DRY).</summary>
public record ValidationErrorResponse(string Mensagem, IReadOnlyList<CampoErro> Erros);

public record CampoErro(string Campo, string Mensagem);

/// <summary>Converte resultado do FluentValidation para resposta da API (um único lugar — DRY).</summary>
public static class ValidationErrorResponseExtensions
{
    public static ValidationErrorResponse ToValidationErrorResponse(this ValidationResult result)
    {
        var erros = result.Errors.Select(e => new CampoErro(e.PropertyName, e.ErrorMessage)).ToList();
        var mensagem = result.Errors.Count == 1 ? result.Errors[0].ErrorMessage : "Um ou mais campos estão inválidos.";
        return new ValidationErrorResponse(mensagem, erros);
    }
}
