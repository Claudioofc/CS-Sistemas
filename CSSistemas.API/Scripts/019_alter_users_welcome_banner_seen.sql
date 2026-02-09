-- Banner de boas-vindas: quando o usuário visualizou (null = ainda não viu).
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "WelcomeBannerSeenAt" timestamp with time zone NULL;
