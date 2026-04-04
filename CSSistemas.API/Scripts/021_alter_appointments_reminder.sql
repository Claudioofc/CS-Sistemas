ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "ReminderSentAt" timestamp with time zone NULL;
