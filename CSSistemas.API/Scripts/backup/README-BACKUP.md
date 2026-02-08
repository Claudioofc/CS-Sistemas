# Backup automático do banco CSSistemas (PostgreSQL)

Processo recomendado para backup seguro e automático.

---

## 1. Criar usuário só para backup (uma vez)

Conecte ao PostgreSQL como superusuário (ex.: usuário `postgres`) e execute o script:

```bash
psql -h localhost -U postgres -d CSSistemas -f "001_create_backup_user.sql"
```

Antes de rodar, **edite** `001_create_backup_user.sql` e troque `SENHA_FORTE_AQUI` por uma senha segura para o usuário `backup_cssistemas`. Esse usuário tem apenas permissão de **leitura** no banco (ideal para pg_dump).

---

## 2. Configurar senha sem deixar no script (.pgpass no Windows)

Para o script de backup não precisar de senha em texto aberto:

1. Crie a pasta (se não existir):  
   `%APPDATA%\postgresql\`
2. Crie o arquivo:  
   `%APPDATA%\postgresql\pgpass.conf`
3. Adicione uma linha no formato:  
   `localhost:5432:CSSistemas:backup_cssistemas:SUA_SENHA_DO_BACKUP`
   - Uma linha por conexão: `host:porta:database:usuário:senha`
4. Ajuste permissões (só seu usuário pode ler):  
   No PowerShell:  
   `icacls "%APPDATA%\postgresql\pgpass.conf" /inheritance:r /grant:r "%USERNAME%:R"`

Assim o `pg_dump` usa a senha do arquivo e você não precisa colocar senha no `.bat`.

---

## 3. Ajustar e testar o script de backup

1. Abra `backup_cssistemas.bat`.
2. Ajuste se precisar:
   - `PGHOST`, `PGPORT`, `PGUSER` – já usam o usuário `backup_cssistemas`.
   - `BACKUP_DIR` – pasta onde ficarão os `.dump` (ex.: `D:\Backups\CSSistemas`).
   - Se `pg_dump` não estiver no PATH, descomente a linha `PGDMP` e coloque o caminho completo do `pg_dump.exe` (ex.: `C:\Program Files\PostgreSQL\16\bin\pg_dump.exe`).
3. Crie a pasta de backup (ex.: `D:\Backups\CSSistemas`).
4. Rode o `.bat` manualmente (duplo clique ou pelo prompt) e confira se o arquivo `.dump` é criado na pasta.

O script também **remove backups com mais de 7 dias** (comando `forfiles`). Se quiser manter por mais tempo, altere o `/D -7` para ex.: `/D -30`.

---

## 4. Agendar execução (Task Scheduler – Windows)

1. Abra o **Agendador de Tarefas** (taskschd.msc).
2. **Criar Tarefa Básica** (ou “Criar tarefa…”).
3. Nome: ex. `Backup CSSistemas`.
4. **Gatilho:** Diário, no horário desejado (ex.: 02:00).
5. **Ação:** Iniciar um programa.
   - Programa/script: caminho completo do `backup_cssistemas.bat`  
     (ex.: `C:\...\CSSistemas.API\Scripts\backup\backup_cssistemas.bat`).
   - Iniciar em: pasta do script (ex.: `C:\...\CSSistemas.API\Scripts\backup`).
6. Em **Geral**, marque “Executar com privilégios mais altos” só se a pasta de backup exigir; caso contrário, deixe desmarcado.
7. Em **Configurações**, pode marcar “Executar tarefa o mais rápido possível após perder um agendamento”.
8. Salve e teste: clique com o botão direito na tarefa → **Executar**.

---

## 5. Deixar mais seguro (recomendado)

- **Cópia para outro lugar:** depois de gerar o `.dump`, copie o arquivo para outro disco, servidor ou nuvem (script extra ou ferramenta de sync). Assim, se o servidor falhar, o backup não some junto.
- **Testar restauração:** de tempos em tempos, restaure um backup em um banco de teste para garantir que o arquivo está íntegro.

---

## 6. Como restaurar um backup

Para restaurar um arquivo `.dump` (formato custom `-F c`):

```bash
pg_restore -h localhost -U postgres -d CSSistemas -c --if-exists backup.dump
```

- `-c --if-exists`: limpa (drop) objetos antes de recriar; use com cuidado em produção.
- Para criar em um banco **novo**: crie o banco vazio antes e use `-d NomeDoBancoNovo`.

Se tiver usado backup em SQL puro (`.sql`):

```bash
psql -h localhost -U postgres -d CSSistemas -f backup.sql
```

---

## Resumo do fluxo

| Etapa | O quê |
|-------|--------|
| 1 | Criar usuário `backup_cssistemas` (SQL) e configurar `.pgpass` |
| 2 | Ajustar `backup_cssistemas.bat` e rodar uma vez para testar |
| 3 | Agendar o `.bat` no Agendador de Tarefas (ex.: todo dia às 2h) |
| 4 | (Opcional) Copiar backups para outro disco/servidor/nuvem e testar restauração |

Os arquivos desta pasta (`Scripts/backup/`) **não** são executados na inicialização da API; são apenas para uso manual e agendado.
