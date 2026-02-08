-- Tabela BusinessHours (horários de funcionamento por dia da semana).
CREATE TABLE IF NOT EXISTS "BusinessHours" (
    "Id" uuid PRIMARY KEY,
    "BusinessId" uuid NOT NULL REFERENCES "Businesses"("Id") ON DELETE RESTRICT,
    "DayOfWeek" integer NOT NULL,
    "OpenAtMinutes" integer NOT NULL,
    "CloseAtMinutes" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL
);
