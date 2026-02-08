-- Tabela Appointments (agendamentos).
CREATE TABLE IF NOT EXISTS "Appointments" (
    "Id" uuid PRIMARY KEY,
    "BusinessId" uuid NOT NULL REFERENCES "Businesses"("Id") ON DELETE RESTRICT,
    "ServiceId" uuid NOT NULL REFERENCES "Services"("Id") ON DELETE RESTRICT,
    "ClientName" varchar(200) NOT NULL,
    "ClientPhone" varchar(20) NULL,
    "ClientEmail" varchar(256) NULL,
    "ScheduledAt" timestamp with time zone NOT NULL,
    "Status" integer NOT NULL,
    "Notes" varchar(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL
);
