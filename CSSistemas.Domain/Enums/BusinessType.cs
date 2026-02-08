namespace CSSistemas.Domain.Enums;

/// <summary>
/// Nicho do negócio (sistema multinicho). Define linguagem, tom da IA, serviços padrão e fluxo.
/// Uso inicial: apenas odontológico (Dentista). Demais valores para evolução futura.
/// </summary>
public enum BusinessType
{
    /// <summary>Odontologia — nicho em uso no MVP.</summary>
    Dentista = 0,
    Barbeiro = 1,
    Clinica = 2,
    Estetica = 3,
    PersonalTrainer = 4
}
