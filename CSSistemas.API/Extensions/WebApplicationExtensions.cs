using CSSistemas.Application.Exceptions;
using CSSistemas.API.Middleware;
using CSSistemas.Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CSSistemas.API.Extensions;

/// <summary>Configuração do pipeline e inicialização (middleware, seed do banco).</summary>
public static class WebApplicationExtensions
{
    /// <summary>Configura tratamento de exceção global, Swagger (dev), HTTPS, CORS, auth e middleware de assinatura.</summary>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler(a => a.Run(async ctx =>
        {
            var feature = ctx.Features.Get<IExceptionHandlerPathFeature>();
            var ex = feature?.Error;
            var (statusCode, msg) = ex is CommException comm
                ? (comm.StatusCode, comm.Message)
                : (500, app.Environment.IsDevelopment()
                    ? (ex?.Message ?? "Erro interno.")
                    : "Ocorreu um erro. Tente novamente.");
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { message = msg });
        }));

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (!app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        app.UseRateLimiter();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<SubscriptionRequiredMiddleware>();
        app.MapControllers();
        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    ctx.Context.Response.Headers.CacheControl = "no-cache";
                else
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            }
        });
        app.MapFallbackToFile("index.html");

        return app;
    }

    /// <summary>Garante que o banco existe, executa scripts SQL e seed (admin, planos).</summary>
    public static async Task EnsureDatabaseAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var scriptsPath = Path.Combine(AppContext.BaseDirectory, "Scripts");
        await DatabaseScriptRunner.RunAsync(db, scriptsPath, CancellationToken.None);
        // Garante coluna CancelToken (cancelamento pelo link do e-mail), mesmo se o script 017 não tiver rodado.
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"Appointments\" ADD COLUMN IF NOT EXISTS \"CancelToken\" varchar(64) NULL");
        await OneTimeFixRunner.RunAppointmentTimezoneFixAsync(db, CancellationToken.None);

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];
        await AdminSeed.EnsureAdminAsync(db, adminEmail, adminPassword, CancellationToken.None);
        await PlansSeed.EnsurePlansAsync(db);
    }
}
