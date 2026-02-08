using CSSistemas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Data;

/// <summary>Define o admin do sistema via Admin:Email (e opcionalmente Admin:Password para criar o usuário se não existir).</summary>
public static class AdminSeed
{
    public static async Task EnsureAdminAsync(AppDbContext db, string? adminEmail, string? adminPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminEmail)) return;

        var email = adminEmail.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword.Trim());
            user = User.Create(email, passwordHash, "Admin");
            user.SetAsAdmin();
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);

            var trial = Subscription.CreateTrial(user.Id);
            db.Subscriptions.Add(trial);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (user == null) return;

        if (!user.IsAdmin)
        {
            user.SetAsAdmin();
            db.Users.Update(user);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
