using CSSistemas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Data;

/// <summary>Seed dos planos de assinatura (Mensal R$ 89,90; 6 meses; 1 ano).</summary>
public static class PlansSeed
{
    public static async Task EnsurePlansAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Plans.AnyAsync(cancellationToken))
            return;

        db.Plans.AddRange(
            Plan.Create("Mensal", 89.90m, 1, "Cobrança mensal. Cancele quando quiser."),
            Plan.Create("6 meses", 499.40m, 6, "Aproximadamente 1 mês grátis. Economia em relação ao mensal."),
            Plan.Create("1 ano", 899m, 12, "2 meses grátis. Melhor custo-benefício.")
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
