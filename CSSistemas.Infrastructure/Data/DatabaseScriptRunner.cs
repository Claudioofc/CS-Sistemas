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

    /// <summary>Divide o script em comandos por ponto e vírgula (ignora ; dentro de strings simples e blocos $$...$$).</summary>
    private static IEnumerable<string> SplitStatements(string sql)
    {
        var list = new List<string>();
        var sb = new System.Text.StringBuilder();
        var inSingle = false;
        string? dollarTag = null; // ex: "$$" ou "$tag$"

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];

            // Dentro de string simples
            if (inSingle)
            {
                sb.Append(c);
                if (c == '\'')
                {
                    // '' é aspas escapada no PostgreSQL
                    if (i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        sb.Append(sql[i + 1]);
                        i++;
                    }
                    else
                    {
                        inSingle = false;
                    }
                }
                continue;
            }

            // Dentro de dollar-quoted string (DO $$ ... END$$)
            if (dollarTag != null)
            {
                sb.Append(c);
                // Verifica se o final do StringBuilder forma o closing tag
                if (sb.Length >= dollarTag.Length)
                {
                    var tail = sb.ToString(sb.Length - dollarTag.Length, dollarTag.Length);
                    if (tail == dollarTag)
                        dollarTag = null;
                }
                continue;
            }

            // Início de dollar-quoted string: $ seguido de tag opcional e outro $
            if (c == '$')
            {
                var end = sql.IndexOf('$', i + 1);
                if (end >= 0)
                {
                    var tag = sql.Substring(i, end - i + 1); // ex: "$tag$" ou "$$"
                    sb.Append(tag);
                    dollarTag = tag;
                    i = end;
                    continue;
                }
            }

            // Início de string simples
            if (c == '\'')
            {
                inSingle = true;
                sb.Append(c);
                continue;
            }

            // Separador de comando
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
