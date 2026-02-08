-- Tabela SystemMessages (templates de mensagens do sistema).
CREATE TABLE IF NOT EXISTS "SystemMessages" (
    "Id" uuid PRIMARY KEY,
    "BusinessId" uuid NOT NULL REFERENCES "Businesses"("Id") ON DELETE RESTRICT,
    "Key" varchar(100) NOT NULL,
    "Title" varchar(200) NOT NULL,
    "Body" varchar(4000) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL
);
