@echo off
setlocal
REM ============================================================
REM Backup do banco CSSistemas (PostgreSQL).
REM Ajuste as variáveis abaixo. Use .pgpass para senha (recomendado).
REM ============================================================

set PGHOST=localhost
set PGPORT=5432
set PGUSER=backup_cssistemas
set PGDATABASE=CSSistemas

REM Pasta onde salvar os backups (crie a pasta antes)
set BACKUP_DIR=C:\Backups\CSSistemas
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

REM Data/hora no nome do arquivo (yyyy-MM-dd_HH-mm)
for /f "usebackq" %%a in (`powershell -NoProfile -Command "Get-Date -Format 'yyyy-MM-dd_HH-mm'"`) do set BACKUP_DATE=%%a
set BACKUP_FILE=%BACKUP_DIR%\CSSistemas_%BACKUP_DATE%.dump

REM Caminho do pg_dump (PostgreSQL 16 - ajuste se usar outra versao)
set "PGDMP=C:\Program Files\PostgreSQL\16\bin\pg_dump.exe"

echo [%date% %time%] Iniciando backup: %BACKUP_FILE%
"%PGDMP%" -F c -f "%BACKUP_FILE%" "%PGDATABASE%"
if errorlevel 1 (
  echo ERRO no backup. Verifique conexao, usuario e senha.
  exit /b 1
)
echo [%date% %time%] Backup concluido: %BACKUP_FILE%

REM --- Opcional: apagar backups com mais de 7 dias ---
forfiles /P "%BACKUP_DIR%" /M CSSistemas_*.dump /D -7 /C "cmd /c del @path" 2>nul
if errorlevel 1 (
  echo Nenhum backup antigo para remover ou forfiles nao disponivel.
)

endlocal
exit /b 0
