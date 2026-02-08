using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CSSistemas.Infrastructure.Data;

/// <summary>Executa correções one-time no banco (apenas uma vez por fix).</summary>
public static class OneTimeFixRunner
{
    private const string FixAppointmentTimezone = "fix_appointment_timezone";

    /// <summary>Corrige agendamentos que foram gravados com horário Brasil como UTC (soma 3h). Roda apenas uma vez.</summary>
    public static async Task RunAppointmentTimezoneFixAsync(DbContext db, CancellationToken cancellationToken = default)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT 1 FROM \"_OneTimeFixes\" WHERE \"Name\" = @p1 LIMIT 1";
        checkCmd.Parameters.Add(new NpgsqlParameter { ParameterName = "p1", Value = FixAppointmentTimezone });
        var alreadyApplied = await checkCmd.ExecuteScalarAsync(cancellationToken) != null;
        if (alreadyApplied)
            return;

        await using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = """
            UPDATE "Appointments"
            SET "ScheduledAt" = "ScheduledAt" + INTERVAL '3 hours', "UpdatedAt" = NOW()
            WHERE "IsDeleted" = false AND EXTRACT(HOUR FROM "ScheduledAt") BETWEEN 8 AND 17
            """;
        await updateCmd.ExecuteNonQueryAsync(cancellationToken);

        await using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = "INSERT INTO \"_OneTimeFixes\" (\"Name\") VALUES (@p1)";
        insertCmd.Parameters.Add(new NpgsqlParameter { ParameterName = "p1", Value = FixAppointmentTimezone });
        await insertCmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
