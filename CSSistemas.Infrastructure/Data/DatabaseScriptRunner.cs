using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Data;

/// <summary>Executa scripts SQL em ordem (por nome de arquivo). Usado no startup para criar/alterar tabelas.</summary>
public static class DatabaseScriptRunner
{
    /// <summary>Executa todos os arquivos .sql do diretório em ordem alfabética (001_..., 002_..., etc.).</summary>
    /// <param name="db">DbContext (Database).</param>
    /// <param name="scriptsDirectory">Caminho da pasta com os arquivos .sql.</param>
    /// <param name="cancellationToken">CancellationToken.</param>
    public static async Task RunAsync(DbContext db, string scriptsDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(scriptsDirectory))
            return;

        var files = Directory.GetFiles(scriptsDirectory, "*.sql")
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var path in files)
        {
            var sql = await File.ReadAllTextAsync(path, cancellationToken);
            if (string.IsNullOrWhiteSpace(sql))
                continue;
            foreach (var statement in SplitStatements(sql))
            {
                if (string.IsNullOrWhiteSpace(statement))
                    continue;
                await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);
            }
        }
    }

    /// <summary>Divide o script em comandos por ponto e vírgula (ignora ; dentro de strings simples).</summary>
    private static IEnumerable<string> SplitStatements(string sql)
    {
        var list = new List<string>();
        var sb = new System.Text.StringBuilder();
        var inSingle = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];

            if (inSingle)
            {
                sb.Append(c);
                if (c == '\'' && (i == 0 || sql[i - 1] != '\\'))
                    inSingle = false;
                continue;
            }

            if (c == '\'' && (i == 0 || sql[i - 1] != '\\'))
            {
                inSingle = true;
                sb.Append(c);
                continue;
            }

            if (c == ';')
            {
                var stmt = sb.ToString().Trim();
                if (stmt.Length > 0)
                    list.Add(stmt);
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        var last = sb.ToString().Trim();
        if (last.Length > 0)
            list.Add(last);

        return list;
    }
}
