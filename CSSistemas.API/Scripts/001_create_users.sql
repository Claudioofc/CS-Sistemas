-- Tabela Users (usuários do sistema). Executado apenas se não existir (EnsureCreated pode tê-la criado).
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid PRIMARY KEY,
    "Email" varchar(256) NOT NULL,
    "PasswordHash" varchar(500) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "ProfilePhotoUrl" varchar(500) NULL,
    "DocumentType" integer NULL,
    "DocumentNumber" varchar(20) NULL,
    "ResetToken" varchar(500) NULL,
    "ResetTokenExpiresAt" timestamp with time zone NULL,
    "IsAdmin" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email") WHERE "IsDeleted" = false;
