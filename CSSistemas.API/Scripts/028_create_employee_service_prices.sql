CREATE TABLE IF NOT EXISTS "EmployeeServicePrices" (
    "EmployeeId" UUID         NOT NULL,
    "ServiceId"  UUID         NOT NULL,
    "Price"      DECIMAL(18,2) NOT NULL,
    CONSTRAINT "PK_EmployeeServicePrices" PRIMARY KEY ("EmployeeId", "ServiceId"),
    CONSTRAINT "FK_ESP_Employees" FOREIGN KEY ("EmployeeId") REFERENCES "Employees"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ESP_Services"  FOREIGN KEY ("ServiceId")  REFERENCES "Services"("Id")  ON DELETE CASCADE
);
