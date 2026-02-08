-- Tabela Clients (clientes/pacientes de um negócio).
CREATE TABLE IF NOT EXISTS "Clients" (
    "Id" uuid PRIMARY KEY,
    "BusinessId" uuid NOT NULL REFERENCES "Businesses"("Id") ON DELETE RESTRICT,
    "Name" varchar(200) NOT NULL,
    "Phone" varchar(20) NULL,
    "Email" varchar(256) NULL,
    "Notes" varchar(1000) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL
);
