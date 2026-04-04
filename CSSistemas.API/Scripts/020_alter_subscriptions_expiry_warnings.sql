ALTER TABLE "Subscriptions" ADD COLUMN IF NOT EXISTS "ExpiryWarning7DaySentAt" timestamp with time zone NULL;
ALTER TABLE "Subscriptions" ADD COLUMN IF NOT EXISTS "ExpiryWarning1DaySentAt" timestamp with time zone NULL;
