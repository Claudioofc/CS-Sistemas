-- Índice único: um CPF/CNPJ só pode estar em uma conta (evita novo cadastro após usar o trial).
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_DocumentType_DocumentNumber"
ON "Users" ("DocumentType", "DocumentNumber")
WHERE "DocumentNumber" IS NOT NULL AND "IsDeleted" = false;
