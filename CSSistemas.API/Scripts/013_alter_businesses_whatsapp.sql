-- Adiciona coluna WhatsAppPhone em Businesses (número para webhook e envio).
ALTER TABLE "Businesses" ADD COLUMN IF NOT EXISTS "WhatsAppPhone" varchar(20) NULL;
