-- Executar uma vez, conectado como superusuário (postgres) no banco CSSistemas.
-- Cria usuário só para backup (somente leitura). Troque 'SENHA_FORTE_AQUI' por uma senha segura.

CREATE USER backup_cssistemas WITH PASSWORD 'Cl38414904!';

GRANT CONNECT ON DATABASE "CSSistemas" TO backup_cssistemas;
GRANT USAGE ON SCHEMA public TO backup_cssistemas;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO backup_cssistemas;

-- Tabelas criadas no futuro também terão SELECT para backup_cssistemas (rodar como dono do banco, ex.: postgres)
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO backup_cssistemas;
