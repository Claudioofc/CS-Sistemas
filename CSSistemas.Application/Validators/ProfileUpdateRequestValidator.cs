using CSSistemas.Application.DTOs.Auth;
using CSSistemas.Domain.Enums;
using FluentValidation;

namespace CSSistemas.Application.Validators;

public class ProfileUpdateRequestValidator : AbstractValidator<ProfileUpdateRequest>
{
    public ProfileUpdateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres.")
            .MaximumLength(200);

        RuleFor(x => x.ProfilePhotoUrl)
            .MaximumLength(500)
            .Must(BeValidPhotoUrlOrEmpty).When(x => !string.IsNullOrEmpty(x.ProfilePhotoUrl))
            .WithMessage("URL da foto inválida.");

        // Documento opcional na atualização (permite salvar só nome/foto); quando informado, valida.
        // Aceita com ou sem formatação (pontos, traço, barra); valida apenas os dígitos.
        RuleFor(x => x.DocumentNumber)
            .Must(OnlyDigitsOrFormatting).When(x => !string.IsNullOrWhiteSpace(x.DocumentNumber))
            .WithMessage("Documento deve conter apenas números (pontos, traço e barra são aceitos).");
        RuleFor(x => x.DocumentType)
            .NotNull().When(x => !string.IsNullOrWhiteSpace(x.DocumentNumber))
            .WithMessage("Selecione CPF ou CNPJ.");
        RuleFor(x => x.DocumentNumber)
            .NotEmpty().When(x => x.DocumentType.HasValue)
            .WithMessage("Informe o número do documento.");
        RuleFor(x => x.DocumentNumber)
            .Must(BeValidCpfLength).When(x => x.DocumentType == DocumentType.Cpf && !string.IsNullOrWhiteSpace(x.DocumentNumber))
            .WithMessage("CPF deve ter 11 dígitos.");
        RuleFor(x => x.DocumentNumber)
            .Must(BeValidCnpjLength).When(x => x.DocumentType == DocumentType.Cnpj && !string.IsNullOrWhiteSpace(x.DocumentNumber))
            .WithMessage("CNPJ deve ter 14 dígitos.");
    }

    /// <summary>Aceita URL absoluta (http/https) ou caminho relativo (ex.: /uploads/profile/xxx.png do upload).</summary>
    private static bool BeValidPhotoUrlOrEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var v = value.Trim();
        // Caminho relativo retornado pelo upload (ex.: /uploads/profile/xxx.png)
        if (v.StartsWith("/", StringComparison.Ordinal) && !v.Contains(".."))
            return true;
        // URL absoluta
        return Uri.TryCreate(v, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>Aceita apenas dígitos ou caracteres de formatação (. - /).</summary>
    private static bool OnlyDigitsOrFormatting(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        return value.All(c => char.IsDigit(c) || c == '.' || c == '-' || c == '/');
    }

    private static string DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static bool BeValidCpfLength(string? value)
    {
        return DigitsOnly(value).Length == 11;
    }

    private static bool BeValidCnpjLength(string? value)
    {
        return DigitsOnly(value).Length == 14;
    }
}
