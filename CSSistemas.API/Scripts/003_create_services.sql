-- Tabela Services (serviços oferecidos por um negócio).
CREATE TABLE IF NOT EXISTS "Services" (
    "Id" uuid PRIMARY KEY,
    "BusinessId" uuid NOT NULL REFERENCES "Businesses"("Id") ON DELETE RESTRICT,
    "Name" varchar(200) NOT NULL,
    "DurationMinutes" integer NOT NULL,
    "Price" decimal(18,2) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL
);
