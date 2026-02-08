-- Tabela Plans (planos de assinatura: Mensal, 6 meses, 1 ano).
CREATE TABLE IF NOT EXISTS "Plans" (
    "Id" uuid PRIMARY KEY,
    "Name" varchar(100) NOT NULL,
    "Price" decimal(18,2) NOT NULL,
    "BillingIntervalMonths" integer NOT NULL,
    "Features" varchar(500) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL
);
