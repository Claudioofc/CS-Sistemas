-- Adiciona colunas de soft delete em tabelas que possam ter sido criadas antes (compatibilidade).
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone NULL;
ALTER TABLE "Businesses" ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;
ALTER TABLE "Businesses" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone NULL;
ALTER TABLE "Services" ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;
ALTER TABLE "Services" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone NULL;
ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;
ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone NULL;
