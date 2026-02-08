CREATE TABLE IF NOT EXISTS "Notifications" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
  "Type" varchar(50) NOT NULL,
  "ClientName" varchar(200) NOT NULL,
  "ScheduledAt" timestamp with time zone NOT NULL,
  "AppointmentId" uuid NULL,
  "ReadAt" timestamp with time zone NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NULL,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "DeletedAt" timestamp with time zone NULL
);

CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId" ON "Notifications" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Notifications_CreatedAt" ON "Notifications" ("CreatedAt" DESC);
