ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "EmployeeId"   UUID;
ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "EmployeeName" VARCHAR(200);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Appointments_Employees'
    ) THEN
        ALTER TABLE "Appointments"
            ADD CONSTRAINT "FK_Appointments_Employees"
            FOREIGN KEY ("EmployeeId") REFERENCES "Employees"("Id") ON DELETE SET NULL;
    END IF;
END$$;

CREATE INDEX IF NOT EXISTS "IX_Appointments_EmployeeId" ON "Appointments"("EmployeeId") WHERE "EmployeeId" IS NOT NULL AND "IsDeleted" = FALSE;
