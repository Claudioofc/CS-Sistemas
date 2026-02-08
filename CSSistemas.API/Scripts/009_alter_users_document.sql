-- Garante todas as colunas da tabela Users (compatibilidade com banco criado antes do modelo atual).
-- Adiciona apenas se não existir. Evita erro 42703 (coluna não existe).
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ProfilePhotoUrl" varchar(500) NULL;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "DocumentType" integer NULL;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "DocumentNumber" varchar(20) NULL;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ResetToken" varchar(500) NULL;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ResetTokenExpiresAt" timestamp with time zone NULL;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsAdmin" boolean NOT NULL DEFAULT false;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone NULL;
