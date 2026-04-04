CREATE TABLE IF NOT EXISTS "Employees" (
    "Id"          UUID          NOT NULL DEFAULT gen_random_uuid(),
    "BusinessId"  UUID          NOT NULL,
    "Name"        VARCHAR(200)  NOT NULL,
    "Role"        VARCHAR(100),
    "IsActive"    BOOLEAN       NOT NULL DEFAULT TRUE,
    "CreatedAt"   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedAt"   TIMESTAMPTZ,
    "IsDeleted"   BOOLEAN       NOT NULL DEFAULT FALSE,
    "DeletedAt"   TIMESTAMPTZ,
    CONSTRAINT "PK_Employees" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Employees_Businesses" FOREIGN KEY ("BusinessId") REFERENCES "Businesses"("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_Employees_BusinessId" ON "Employees"("BusinessId") WHERE "IsDeleted" = FALSE;
