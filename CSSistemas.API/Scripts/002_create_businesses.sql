-- Tabela Businesses (negócios do cliente SaaS).
CREATE TABLE IF NOT EXISTS "Businesses" (
    "Id" uuid PRIMARY KEY,
    "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "Name" varchar(200) NOT NULL,
    "BusinessType" integer NOT NULL,
    "PublicSlug" varchar(100) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Businesses_PublicSlug" ON "Businesses" ("PublicSlug") WHERE "PublicSlug" IS NOT NULL AND "IsDeleted" = false;
