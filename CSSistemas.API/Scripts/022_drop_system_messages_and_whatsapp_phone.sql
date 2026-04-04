-- Remove tabela SystemMessages e coluna WhatsAppPhone de Businesses (resquícios do bot WhatsApp removido).
DROP TABLE IF EXISTS "SystemMessages";
ALTER TABLE "Businesses" DROP COLUMN IF EXISTS "WhatsAppPhone";
