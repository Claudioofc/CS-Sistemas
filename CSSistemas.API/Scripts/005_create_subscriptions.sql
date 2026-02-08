-- Tabela Subscriptions (assinatura trial/plano pago).
CREATE TABLE IF NOT EXISTS "Subscriptions" (
    "Id" uuid PRIMARY KEY,
    "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "SubscriptionType" integer NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "EndsAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL
);
