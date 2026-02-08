-- Coluna IsAdmin em Users (administrador do sistema).
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsAdmin" boolean NOT NULL DEFAULT false;
