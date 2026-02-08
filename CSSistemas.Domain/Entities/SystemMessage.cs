namespace CSSistemas.Domain.Entities;

/// <summary>Template de mensagem do sistema (ex.: lembrete de consulta, confirmação). Pode conter placeholders como {NomeCliente}, {Data}, {Horario}.</summary>
public class SystemMessage : EntityBase
{
    public Guid BusinessId { get; protected set; }
    /// <summary>Identificador do template (ex.: lembrete_consulta, confirmacao).</summary>
    public string Key { get; protected set; } = string.Empty;
    public string Title { get; protected set; } = string.Empty;
    /// <summary>Corpo da mensagem; pode conter placeholders.</summary>
    public string Body { get; protected set; } = string.Empty;
    public bool IsActive { get; protected set; } = true;

    public Business Business { get; protected set; } = null!;

    protected SystemMessage() { }

    public static SystemMessage Create(Guid businessId, string key, string title, string body)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId é obrigatório.", nameof(businessId));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Chave do template é obrigatória.", nameof(key));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título é obrigatório.", nameof(title));

        return new SystemMessage
        {
            BusinessId = businessId,
            Key = key.Trim().ToLowerInvariant(),
            Title = title.Trim(),
            Body = (body ?? string.Empty).Trim()
        };
    }

    public void Update(string key, string title, string body, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Chave do template é obrigatória.", nameof(key));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título é obrigatório.", nameof(title));
        Key = key.Trim().ToLowerInvariant();
        Title = title.Trim();
        Body = (body ?? string.Empty).Trim();
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
