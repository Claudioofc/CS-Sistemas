-- Tabela para registrar correções one-time (evita rodar o mesmo fix mais de uma vez).
CREATE TABLE IF NOT EXISTS "_OneTimeFixes" (
  "Name" varchar(100) PRIMARY KEY,
  "AppliedAt" timestamptz NOT NULL DEFAULT NOW()
);
